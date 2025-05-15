using System;
using System.Threading.Tasks;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// The expected failure of a transaction.
/// </summary>
public enum CommonTransactionFailure
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
    /// <summary>
    /// The transaction failed because the nonce was too high.
    /// </summary>
    NonceTooHigh,
    /// <summary>
    /// The transaction submitted successfully but the receipt was not found.
    /// </summary>
    ReceiptNotFound,
    /// <summary>
    /// The transaction failed because the sender had insufficient funds.
    /// </summary>
    InsufficientFunds,
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
/// <typeparam name="TOptions">The type of the transaction options.</typeparam>
/// <typeparam name="TArgs">The type of the transaction arguments.</typeparam>
/// <typeparam name="TReceipt">The type of the transaction receipt.</typeparam>
public abstract class TransactionRunner<TContract, TOptions, TArgs, TReceipt>
{
    private readonly INonceStore nonceStore;
    private readonly ILoggerFactory loggerFactory;

    /// <summary>
    /// The logger to use for the transaction.
    /// </summary>
    protected readonly ILogger logger;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionRunner{TContract, TOptions, TArgs, TReceipt}"/> class.
    /// </summary>
    /// <param name="nonceStore">The nonce store to use for the transaction.</param>
    /// <param name="loggerFactory">The logger factory to use for the transaction.</param>
    public TransactionRunner(
        INonceStore nonceStore,
        ILoggerFactory loggerFactory)
    {
        this.nonceStore = nonceStore ?? throw new ArgumentNullException(nameof(nonceStore));
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        this.logger = loggerFactory.CreateLogger<TransactionRunner<TContract, TOptions, TArgs, TReceipt>>();
    }

    //

    /// <summary>
    /// Runs a transaction with retries managed by the nonce store.
    /// </summary>
    /// <param name="context">The context to use for the transaction.</param>
    /// <param name="contract">The contract or blockchain gateway to submit the transaction to.</param>
    /// <param name="functionName">The name of the function to call on the contract.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="args">The arguments to use for the transaction.</param>
    /// <returns>The transaction receipt.</returns>
    /// <exception cref="FailedToSubmitTransactionException">Thrown if the transaction fails to submit.</exception>
    /// <exception cref="OutOfGasException">Thrown if the transaction is out of gas.</exception>
    /// <exception cref="RevertedTransactionException">Thrown if the transaction is reverted.</exception>
    public async Task<TReceipt> RunTransactionAsync(
        IJsonRpcContext context, TContract contract, string functionName, TOptions options, TArgs args)
    {
        var operation = new OnceOperation(this.nonceStore, this.loggerFactory);

        return await operation.RunOperationAsync(
            functionName,
            (nonce) => this.SubmitTransactionAsync(context, contract, functionName, nonce, options, args), // <-- call the abstract method
            this.GetExpectedFailure,
            context.CancellationToken);
    }

    //

    /// <summary>
    /// Implementors should use the args passed in to assemble the transaction and submit it; exceptions will be run through <see cref="GetExpectedFailure"/> to determine if it is a known failure that should be retried.
    /// </summary>
    /// <param name="context">The context to use for the transaction.</param>
    /// <param name="contract">The contract or blockchain gateway to submit the transaction to.</param>
    /// <param name="functionName">The name of the function to call on the contract.</param>
    /// <param name="nonce">The nonce to use for the transaction.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="args">The arguments to use for the transaction.</param>
    /// <returns>The transaction receipt.</returns>
    protected abstract Task<TReceipt> SubmitTransactionAsync(
        IJsonRpcContext context, TContract contract, string functionName, ulong nonce, TOptions options, TArgs args);

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
    protected abstract CommonTransactionFailure GetExpectedFailure(Exception ex);
}
