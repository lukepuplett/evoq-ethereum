using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A simple in-memory nonce store that stores nonces in a HashSet.
/// </summary>
public class InMemoryNonceStore : INonceStore
{
    private readonly HashSet<uint> nonceStore = new();
    private readonly Dictionary<uint, DateTimeOffset> nonceFailedTimes = new();
    private readonly ILogger<InMemoryNonceStore> logger;
    private readonly Func<Task<BigInteger>>? getTransactionCount;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="InMemoryNonceStore"/> class.
    /// </summary>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="getTransactionCount">Optional function to get the current transaction count from an external source (e.g., blockchain).</param>
    public InMemoryNonceStore(
        ILoggerFactory loggerFactory,
        Func<Task<BigInteger>>? getTransactionCount = null)
    {
        this.logger = loggerFactory.CreateLogger<InMemoryNonceStore>();
        this.getTransactionCount = getTransactionCount;
    }

    //

    /// <summary>
    /// Gets the next available nonce.
    /// </summary>
    /// <returns>The next available nonce.</returns>
    public Task<uint> BeforeSubmissionAsync()
    {
        uint nonce = 0;
        while (true)
        {
            lock (nonceStore)
            {
                if (getTransactionCount != null)
                {
                    try
                    {
                        var currentNonce = getTransactionCount().Result;
                        nonce = (uint)currentNonce;
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to get nonce from external source, using 0");
                    }
                }

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

    /// <summary>
    /// Gets the next available nonce after a too-low nonce.
    /// </summary>
    /// <param name="nonce">The nonce to check.</param>
    /// <returns>The next available nonce.</returns>
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

    /// <summary>
    /// Handles a transaction that was reverted.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task<NonceRollbackResponse> AfterTransactionRevertedAsync(uint nonce)
    {
        logger.LogError("Transaction with nonce {Nonce} reverted.", nonce);

        return Task.FromResult(NonceRollbackResponse.NotRemovedGasSpent);
    }

    /// <summary>
    /// Handles a transaction that ran out of gas.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task<NonceRollbackResponse> AfterTransactionOutOfGas(uint nonce)
    {
        logger.LogError("Transaction with nonce {Nonce} out of gas.", nonce);

        return Task.FromResult(NonceRollbackResponse.NotRemovedGasSpent);
    }

    /// <summary>
    /// Handles a successful transaction submission.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
    public Task AfterSubmissionSuccessAsync(uint nonce)
    {
        nonceFailedTimes.Remove(nonce);
        logger.LogDebug("Transaction with nonce {Nonce} succeeded, cleared from failed times", nonce);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles a failed transaction submission.
    /// </summary>
    /// <param name="nonce">The nonce of the transaction.</param>
    /// <returns>The response to the caller.</returns>
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
