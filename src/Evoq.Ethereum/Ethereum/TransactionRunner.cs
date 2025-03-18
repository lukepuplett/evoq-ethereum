using System;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum;

/// <summary>
/// The expected failure of a transaction.
/// </summary>
public enum ExpectedFailure
{
    /// <summary>
    /// The transaction failed for an unexpected reason.
    /// </summary>
    Other,
    /// <summary>
    /// The transaction failed because it was out of gas.
    /// </summary>
    OutOfGas,
    /// <summary>
    /// The transaction failed because it was reverted.
    /// </summary>
    Reverted,
    /// <summary>
    /// The transaction failed because the nonce was too low.
    /// </summary>
    NonceTooLow,
}

/// <summary>
/// A transaction runner using a semaphore of one thread and retries managed by the nonce store.
/// </summary>
/// <remarks>
/// Running a transaction with a nonce store is a little tricky. The nonce store is responsible for
/// managing the nonce and ensuring that the transaction is submitted at the correct nonce. The
/// transaction runner is responsible for submitting the transaction at the correct nonce and
/// handling failures and retries in cooperation with the nonce store. The whole process is performed
/// in a loop with a semaphore of one thread to ensure that the nonce store is not overwhelmed since
/// correct nonce incrementing and gap detection is critical to successful blockchain operation.
/// </remarks>
/// <typeparam name="TContract">The type of the contract or blockchain gateway.</typeparam>
/// <typeparam name="TArgs">The type of the transaction arguments.</typeparam>
/// <typeparam name="TReceipt">The type of the transaction receipt.</typeparam>
public abstract class TransactionRunner<TContract, TArgs, TReceipt>
{
    // obviously useless between processes and container instances but
    // at least helps a little to mitigate nonce issues
    private readonly SemaphoreSlim semaphore = new(1, 1);
    private readonly INonceStore nonceStore;

    protected readonly ILogger logger;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRunner{TContract, TArgs, TReceipt}"/> class.
    /// </summary>
    /// <param name="nonceStore">The nonce store to use for the transaction.</param>
    /// <param name="loggerFactory">The logger factory to use for the transaction.</param>
    public TransactionRunner(
        INonceStore nonceStore,
        ILoggerFactory loggerFactory)
    {
        this.nonceStore = nonceStore;
        this.logger = loggerFactory.CreateLogger<TransactionRunner<TContract, TArgs, TReceipt>>();
    }

    //

    /// <summary>
    /// Runs a transaction with retries managed by the nonce store.
    /// </summary>
    /// <param name="contract">The contract or blockchain gateway to submit the transaction to.</param>
    /// <param name="functionName">The name of the function to call on the contract.</param>
    /// <param name="fees">The fees for the transaction.</param>
    /// <param name="args">The arguments to use for the transaction.</param>
    /// <returns>The transaction receipt.</returns>
    /// <exception cref="FailedToSubmitTransactionException">Thrown if the transaction fails to submit.</exception>
    /// <exception cref="OutOfGasTransactionException">Thrown if the transaction is out of gas.</exception>
    /// <exception cref="RevertedTransactionException">Thrown if the transaction is reverted.</exception>
    public async Task<TReceipt> RunTransactionAsync(
        TContract contract, string functionName, TransactionFees fees, TArgs args)
    {
        var deadline = DateTimeOffset.UtcNow.AddMinutes(1);
        var nonce = await this.nonceStore.BeforeSubmissionAsync();

        while (true)
        {
            if (DateTimeOffset.UtcNow > deadline)
            {
                throw new FailedToSubmitTransactionException(
                    "Transaction failed to submit within the deadline. The nonce store may be malfunctioning.");
            }

            try
            {
                this.semaphore.Wait(); // thread blocking wait; not expecting high contention

                TReceipt receipt = await this.SubmitTransactionAsync(contract, functionName, fees, nonce, args);

                await this.nonceStore.AfterSubmissionSuccessAsync(nonce);

                return receipt;
            }
            catch (Exception nonceTooLow)
            when (this.GetExpectedFailure(nonceTooLow) == ExpectedFailure.NonceTooLow)
            {
                // nonce too low, we need to increment the nonce and retry

                this.logger.LogWarning($"{functionName}: nonce too low. Incrementing and retrying");

                nonce = await this.nonceStore.AfterNonceTooLowAsync(nonce);
            }
            catch (Exception outOfGas)
            when (this.GetExpectedFailure(outOfGas) == ExpectedFailure.OutOfGas)
            {
                // transaction out of gas

                this.logger.LogWarning($"{functionName}: transaction out of gas.");

                var r = await this.nonceStore.AfterTransactionOutOfGas(nonce);

                this.logger.LogInformation($"{functionName}: store response: {r}");

                switch (r)
                {
                    case NonceRollbackResponse.NotRemovedGasSpent:
                        // this is the expected response
                        throw new OutOfGasTransactionException(
                            $"Transaction out of gas: {r}. The nonce was retained.");
                    case NonceRollbackResponse.NotRemovedShouldRetry:
                        // this is unexpected, ignoring retry
                        throw new OutOfGasTransactionException(
                            $"Transaction out of gas: {r}. The nonce was retained.");
                    case NonceRollbackResponse.RemovedOkay:
                        // nonce was removed and no gap was created
                        throw new OutOfGasTransactionException(
                            $"Transaction out of gas: {r}. The nonce was removed but should not have been. No gap was detected.");
                    case NonceRollbackResponse.RemovedGapDetected:
                        // consider filling the gap
                        throw new OutOfGasTransactionException(
                            $"Transaction out of gas: {r}. The nonce was removed but should not have been. A gap was detected.")
                        {
                            WasNonceGapCreated = true
                        };
                    default:
                        // other response
                        throw new OutOfGasTransactionException(
                            $"Transaction out of gas: {r}. The nonce may not have been removed.");
                }
            }
            catch (Exception reverted)
            when (this.GetExpectedFailure(reverted) == ExpectedFailure.Reverted)
            {
                // transaction reverted

                this.logger.LogWarning($"{functionName}: transaction reverted.");

                var r = await this.nonceStore.AfterTransactionRevertedAsync(nonce);

                this.logger.LogInformation($"{functionName}: store response: {r}");

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
            catch (Exception other)
            {
                // the transaction failed to submit, could be a network issue between us and the RPC node
                // or it could be a bug in the .NET SDK or the RPC node - usually we need to just try again
                // but that depends on the nonce store response

                this.logger.LogError(other, $"{functionName}: transaction failed to submit: '{other.Message}'");

                var r = await this.nonceStore.AfterSubmissionFailureAsync(nonce);

                this.logger.LogInformation($"{functionName}: store response: {r}");

                switch (r)
                {
                    case NonceRollbackResponse.NotRemovedShouldRetry:
                        // log and allow while loop to continue
                        await Task.Delay(3000);
                        break;
                    case NonceRollbackResponse.RemovedOkay:
                        // nonce was removed and no gap was created
                        throw new FailedToSubmitTransactionException(
                            $"Transaction failed: {r}. The nonce was removed and no gap was created.", other);
                    case NonceRollbackResponse.RemovedGapDetected:
                        // consider filling the gap
                        throw new FailedToSubmitTransactionException(
                            $"Transaction failed: {r}. The nonce was removed and a gap was created.", other)
                        {
                            WasNonceGapCreated = true
                        };
                    default:
                        // other response
                        throw new FailedToSubmitTransactionException(
                            $"Transaction failed: {r}. This response was unexpected. The nonce store may be malfunctioning.", other);
                }
            }
            finally
            {
                this.semaphore.Release();
            }
        }
    }

    //

    /// <summary>
    /// Implementors should use the args passed in to assemble the transaction and submit it.
    /// </summary>
    /// <param name="contract">The contract or blockchain gateway to submit the transaction to.</param>
    /// <param name="functionName">The name of the function to call on the contract.</param>
    /// <param name="fees">The fees for the transaction.</param>
    /// <param name="nonce">The nonce to use for the transaction.</param>
    /// <param name="args">The arguments to use for the transaction.</param>
    /// <returns>The transaction receipt.</returns>
    protected abstract Task<TReceipt> SubmitTransactionAsync(
        TContract contract, string functionName, TransactionFees fees, uint nonce, TArgs args);

    /// <summary>
    /// Implementors should return the expected failure of a transaction.
    /// </summary>
    /// <remarks>
    /// This is used to determine if the transaction failed because of a known issue and should be
    /// retried. The implementer is expected to have knowledge of the various exceptions that may be
    /// thrown by its transaction runner implementation.
    /// </remarks>
    /// <param name="ex">The exception that occurred.</param>
    /// <returns>The expected failure of the transaction.</returns>
    protected abstract ExpectedFailure GetExpectedFailure(Exception ex);
}
