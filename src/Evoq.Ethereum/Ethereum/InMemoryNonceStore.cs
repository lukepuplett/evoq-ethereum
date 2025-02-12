using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum;

public class InMemoryNonceStore : INonceStore
{
    private readonly HashSet<uint> nonceStore = new();
    private readonly Dictionary<uint, DateTimeOffset> nonceFailedTimes = new();
    private readonly ILogger<InMemoryNonceStore> logger;

    //

    public InMemoryNonceStore(ILoggerFactory loggerFactory)
    {
        this.logger = loggerFactory.CreateLogger<InMemoryNonceStore>();
    }

    //

    public Task<uint> BeforeSubmissionAsync()
    {
        uint nonce = 0;
        while (true)
        {
            lock (nonceStore)
            {
                if (!nonceStore.Contains(nonce))
                {
                    nonceStore.Add(nonce);
                    logger.LogDebug("Reserved nonce {Nonce}", nonce);

                    return Task.FromResult(nonce);
                }
            }
            logger.LogDebug("Nonce {Nonce} already exists, trying next", nonce);
            nonce++;
        }
    }

    public async Task<uint> AfterNonceTooLowAsync(uint nonce)
    {
        uint nextNonce;
        do
        {
            nextNonce = await BeforeSubmissionAsync();
        } while (nextNonce <= nonce);

        logger.LogDebug("Found next available nonce {NextNonce} after too-low nonce {Nonce}", nextNonce, nonce);

        return nextNonce;
    }

    public Task<NonceRollbackResponse> AfterTransactionRevertedAsync(uint nonce)
    {
        logger.LogError("Transaction with nonce {Nonce} reverted.", nonce);

        return Task.FromResult(NonceRollbackResponse.NotRemovedGasSpent);
    }

    public Task<NonceRollbackResponse> AfterTransactionOutOfGas(uint nonce)
    {
        logger.LogError("Transaction with nonce {Nonce} out of gas.", nonce);

        return Task.FromResult(NonceRollbackResponse.NotRemovedGasSpent);
    }

    public Task AfterSubmissionSuccessAsync(uint nonce)
    {
        nonceFailedTimes.Remove(nonce);
        logger.LogDebug("Transaction with nonce {Nonce} succeeded, cleared from failed times", nonce);

        return Task.CompletedTask;
    }

    public Task<NonceRollbackResponse> AfterSubmissionFailureAsync(uint nonce)
    {
        if (nonceFailedTimes.TryGetValue(nonce, out var failedTime))
        {
            if (failedTime.AddSeconds(10) > DateTimeOffset.UtcNow)
            {
                logger.LogInformation(
                    "Nonce {Nonce} failed recently. Caller should keep trying for now.", nonce);

                return Task.FromResult(NonceRollbackResponse.NotRemovedShouldRetry);
            }

            // fall through

            return RemoveNonceUnderLockAsync(nonce);
        }

        nonceFailedTimes[nonce] = DateTimeOffset.UtcNow;
        logger.LogDebug("First-time failure for nonce {Nonce}, recording for retry", nonce);

        return Task.FromResult(NonceRollbackResponse.NotRemovedShouldRetry);
    }

    //

    private Task<NonceRollbackResponse> RemoveNonceUnderLockAsync(uint nonce)
    {
        lock (nonceStore)
        {
            if (!nonceStore.Contains(nonce))
            {
                logger.LogWarning("Nonce {Nonce} not found in store", nonce);

                return Task.FromResult(NonceRollbackResponse.NonceNotFound);
            }

            nonceStore.Remove(nonce);
            nonceFailedTimes.Remove(nonce);

            var hasHigherNonce = nonceStore.Any(n => n > nonce);
            if (hasHigherNonce)
            {
                logger.LogWarning("Removed nonce {Nonce}, created gap as higher nonces exist", nonce);

                return Task.FromResult(NonceRollbackResponse.RemovedGapDetected);
            }
            else
            {
                logger.LogDebug("Removed nonce {Nonce}, no gap created", nonce);

                return Task.FromResult(NonceRollbackResponse.RemovedOkay);
            }
        }
    }
}
