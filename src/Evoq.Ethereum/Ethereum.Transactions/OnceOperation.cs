using System;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// An operation that interacts with the nonce store.
/// </summary>
public class OnceOperation
{
    // obviously useless between processes and container instances but
    // at least helps a little to mitigate nonce issues
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly INonceStore nonceStore;
    private readonly ILogger logger;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="OnceOperation"/> class.
    /// </summary>
    /// <param name="nonceStore">The nonce store to use for the operation.</param>
    /// <param name="loggerFactory">The logger factory to use for the operation.</param>
    public OnceOperation(INonceStore nonceStore, ILoggerFactory loggerFactory)
    {
        this.nonceStore = nonceStore;
        this.logger = loggerFactory.CreateLogger<OnceOperation>();
    }

    //

    /// <summary>
    /// The deadline for the operation.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(1);

    //

    /// <summary>
    /// Runs an operation with retries managed by the nonce store.
    /// </summary>
    /// <typeparam name="T">The type of the result of the operation.</typeparam>
    /// <param name="operationName">The name of the operation.</param>
    /// <param name="operation">A function that performs some operation under a nonce.</param>
    /// <param name="getExpectedFailure">A function that maps an exception to a common transaction failure.</param>
    /// <param name="cancellationToken">The cancellation token for the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="FailedToSubmitTransactionException">Thrown if the operation fails to submit.</exception>
    /// <exception cref="OutOfGasException">Thrown if the operation is out of gas.</exception>
    /// <exception cref="RevertedTransactionException">Thrown if the operation is reverted.</exception>
    /// <exception cref="ReceiptNotFoundException">Thrown if the receipt is not found.</exception>
    /// <exception cref="InsufficientFundsException">Thrown if the operation is rejected due to insufficient funds.</exception>
    public async Task<T> RunOperationAsync<T>(
        string operationName,
        Func<ulong, Task<T>> operation,
        Func<Exception, CommonTransactionFailure> getExpectedFailure,
        CancellationToken cancellationToken = default)
    {
        var nonce = await this.nonceStore.BeforeSubmissionAsync();
        var deadline = DateTimeOffset.UtcNow.Add(this.Timeout);

        while (true)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new FailedToSubmitTransactionException(
                    "Operation failed to submit within the deadline. The nonce store may be malfunctioning.");
            }

            try
            {
                this.semaphore.Wait(cancellationToken); // thread blocking wait; not expecting high contention

                var receipt = await operation(nonce);

                await this.nonceStore.AfterSubmissionSuccessAsync(nonce);

                return receipt;
            }
            catch (Exception nonceTooLow)
                when (getExpectedFailure(nonceTooLow) == CommonTransactionFailure.NonceTooLow)
            {
                // nonce too low, we need to increment the nonce and retry

                this.logger.LogWarning($"{operationName}: nonce too low. Incrementing and retrying");

                nonce = await this.nonceStore.AfterNonceTooLowAsync(nonce);
            }
            catch (Exception outOfGas)
                when (getExpectedFailure(outOfGas) == CommonTransactionFailure.OutOfGas)
            {
                // out of gas

                this.logger.LogWarning($"{operationName}: out of gas.");

                var r = await this.nonceStore.AfterTransactionOutOfGas(nonce);

                this.logger.LogInformation($"{operationName}: store response: {r}");

                switch (r)
                {
                    case NonceRollbackResponse.NotRemovedGasSpent:
                        // this is the expected response
                        throw new OutOfGasException(
                            $"Transaction out of gas: {r}. The nonce was retained.");
                    case NonceRollbackResponse.NotRemovedShouldRetry:
                        // this is unexpected, ignoring retry
                        throw new OutOfGasException(
                            $"Transaction out of gas: {r}. The nonce was retained.");
                    case NonceRollbackResponse.RemovedOkay:
                        // nonce was removed and no gap was created
                        throw new OutOfGasException(
                            $"Transaction out of gas: {r}. The nonce was removed but should not have been. No gap was detected.");
                    case NonceRollbackResponse.RemovedGapDetected:
                        // consider filling the gap
                        throw new OutOfGasException(
                            $"Transaction out of gas: {r}. The nonce was removed but should not have been. A gap was detected.")
                        {
                            WasNonceGapCreated = true
                        };
                    default:
                        // other response
                        throw new OutOfGasException(
                            $"Transaction out of gas: {r}. The nonce may not have been removed.");
                }
            }
            catch (Exception reverted)
                when (getExpectedFailure(reverted) == CommonTransactionFailure.Reverted)
            {
                // reverted

                this.logger.LogWarning($"{operationName}: reverted.");

                var r = await this.nonceStore.AfterTransactionRevertedAsync(nonce);

                this.logger.LogInformation($"{operationName}: store response: {r}");

                switch (r)
                {
                    case NonceRollbackResponse.NotRemovedGasSpent:
                        // this is the expected response
                        throw new RevertedTransactionException(
                            $"Transaction reverted: {r}. The nonce was retained.");
                    case NonceRollbackResponse.NotRemovedShouldRetry:
                        // this is unexpected, ignoring retry
                        throw new RevertedTransactionException(
                            $"Transaction reverted: {r}. The nonce was retained.");
                    case NonceRollbackResponse.RemovedOkay:
                        // nonce was removed and no gap was created
                        throw new RevertedTransactionException(
                            $"Transaction reverted: {r}. The nonce was removed but should not have been. No gap was detected.");
                    case NonceRollbackResponse.RemovedGapDetected:
                        // consider filling the gap
                        throw new RevertedTransactionException(
                            $"Transaction reverted: {r}. The nonce was removed but should not have been. A gap was detected.")
                        {
                            WasNonceGapCreated = true
                        };
                    default:
                        // other response
                        throw new RevertedTransactionException(
                            $"Transaction reverted: {r}. The nonce may not have been removed.");
                }
            }
            catch (Exception receiptNotFound)
                when (getExpectedFailure(receiptNotFound) == CommonTransactionFailure.ReceiptNotFound)
            {
                // receipt not found, this burns the nonce and we cannot retry since we already have
                // a deadline to honor

                Hex transactionHash = Hex.Empty;

                if (receiptNotFound is ReceiptNotFoundException receiptNotFoundException)
                {
                    transactionHash = receiptNotFoundException.TransactionHash;

                    this.logger.LogWarning(
                        $"{operationName}: receipt not found. Nonce {nonce} consumed on successfully submission of {transactionHash}");

                    await this.nonceStore.AfterSubmissionSuccessAsync(nonce);

                    throw;
                }
                else
                {
                    this.logger.LogWarning(
                        $"{operationName}: receipt not found. Nonce {nonce} consumed on successfully submission of {transactionHash}");

                    await this.nonceStore.AfterSubmissionSuccessAsync(nonce);

                    throw new ReceiptNotFoundException(Hex.Empty, receiptNotFound);
                }
            }
            catch (Exception insufficientFunds)
                when (getExpectedFailure(insufficientFunds) == CommonTransactionFailure.InsufficientFunds)
            {
                // Operation rejected due to insufficient funds
                this.logger.LogWarning($"{operationName}: insufficient funds.");

                // Notify the nonce store about the submission failure
                var r = await this.nonceStore.AfterSubmissionFailureAsync(nonce);

                this.logger.LogInformation($"{operationName}: store response: {r}");

                // Different handling based on nonce store response
                switch (r)
                {
                    case NonceRollbackResponse.NotRemovedShouldRetry:
                        // We could implement retry logic here, but insufficient funds 
                        // likely requires user intervention
                        throw new InsufficientFundsException(
                            "Operation rejected: Insufficient funds to cover gas costs plus value.",
                            insufficientFunds);
                    case NonceRollbackResponse.RemovedOkay:
                        throw new InsufficientFundsException(
                            $"Operation rejected due to insufficient funds: {r}. The nonce was removed.",
                            insufficientFunds);
                    case NonceRollbackResponse.RemovedGapDetected:
                        throw new InsufficientFundsException(
                            $"Operation rejected due to insufficient funds: {r}. The nonce was removed and a gap was created.",
                            insufficientFunds)
                        {
                            WasNonceGapCreated = true
                        };
                    default:
                        throw new InsufficientFundsException(
                            $"Operation rejected due to insufficient funds: {r}.",
                            insufficientFunds);
                }
            }
            catch (Exception other)
            {
                // the transaction failed to submit, could be a network issue between us and the RPC node
                // or it could be a bug in the .NET SDK or the RPC node - usually we need to just try again
                // but that depends on the nonce store response

                // "nonce too high" is a special case that we can retry because other nodes may be processing
                // other transactions and we just need to wait for our node to catch up, otherwise it indicates
                // a bug in the nonce store or the node is misbehaving

                this.logger.LogError(other, $"{operationName}: failed with unexpected error: '{other.Message}'");

                var r = await this.nonceStore.AfterSubmissionFailureAsync(nonce);

                this.logger.LogInformation($"{operationName}: store response: {r}");

                switch (r)
                {
                    case NonceRollbackResponse.NotRemovedShouldRetry:
                        // log and allow while loop to continue
                        await Task.Delay(3000);
                        break;
                    case NonceRollbackResponse.RemovedOkay:
                        // nonce was removed and no gap was created
                        throw new FailedToSubmitTransactionException(
                            $"Operation failed: {r}. The nonce was removed and no gap was created.", other);
                    case NonceRollbackResponse.RemovedGapDetected:
                        // consider filling the gap
                        throw new FailedToSubmitTransactionException(
                            $"Operation failed: {r}. The nonce was removed and a gap was created.", other)
                        {
                            WasNonceGapCreated = true
                        };
                    default:
                        // other response
                        throw new FailedToSubmitTransactionException(
                            $"Operation failed: {r}. This response was unexpected. The nonce store may be malfunctioning.", other);
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }
}