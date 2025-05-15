using System;
using System.Threading.Tasks;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A transaction runner for transfers.
/// </summary>
/// <typeparam name="TOptions">The type of the transaction options.</typeparam>
/// <typeparam name="TArgs">The type of the transaction arguments.</typeparam>
/// <typeparam name="TReceipt">The type of the transaction receipt.</typeparam>
public abstract class TransferRunner<TOptions, TArgs, TReceipt>
{
    private readonly INonceStore nonceStore;
    private readonly ILoggerFactory loggerFactory;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="TransferRunner{TOptions, TArgs, TReceipt}"/> class.
    /// </summary>
    /// <param name="nonceStore">The nonce store to use for the transaction.</param>
    /// <param name="loggerFactory">The logger factory to use for the transaction.</param>
    public TransferRunner(
        INonceStore nonceStore,
        ILoggerFactory loggerFactory)
    {
        this.nonceStore = nonceStore ?? throw new ArgumentNullException(nameof(nonceStore));
        this.loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
    }

    //

    /// <summary>
    /// Runs a transfer with retries managed by the nonce store.
    /// </summary>
    /// <param name="context">The context to use for the transaction.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="args">The arguments to use for the transaction.</param>
    public async Task<TReceipt> RunTransferAsync(
        IJsonRpcContext context, TOptions options, TArgs? args)
    {
        var operation = new OnceOperation(this.nonceStore, this.loggerFactory);

        return await operation.RunOperationAsync(
            "Transfer",
            (nonce) => this.SubmitTransactionAsync(context, options, nonce, args),
            this.GetExpectedFailure,
            context.CancellationToken);
    }

    //

    /// <summary>
    /// Implementors should use the args passed in to assemble the transaction and submit it; exceptions will be run through <see cref="GetExpectedFailure"/> to determine if it is a known failure that should be retried.
    /// </summary>
    /// <param name="context">The context to use for the transaction.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="nonce">The nonce to use for the transaction.</param>
    /// <param name="args">The arguments to use for the transaction.</param>
    /// <returns>The transaction receipt.</returns>
    protected abstract Task<TReceipt> SubmitTransactionAsync(
        IJsonRpcContext context, TOptions options, ulong nonce, TArgs? args);

    /// <summary>
    /// Implementors should return the expected failure of a transaction.
    /// </summary>
    /// <param name="ex">The exception that occurred.</param>
    /// <returns>The expected failure of the transaction.</returns>
    protected abstract CommonTransactionFailure GetExpectedFailure(Exception ex);
}