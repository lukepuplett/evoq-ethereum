using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A transaction runner for transfers.
/// </summary>
public class TransferRunnerNative
    : TransferRunner<TransferInvocationOptions, IDictionary<string, object?>, TransactionReceipt>
{
    private readonly ILogger logger;

    //

    /// <summary>
    /// Create a new instance of the TransferRunnerNative class.
    /// </summary>
    /// <param name="nonceStore">The nonce store to use for the transaction.</param>
    /// <param name="chain">The chain to use for the transaction.</param>
    /// <param name="loggerFactory">The logger factory to use for the transaction.</param>
    internal TransferRunnerNative(
        INonceStore nonceStore,
        Chain chain,
        ILoggerFactory loggerFactory)
        : base(nonceStore, loggerFactory)
    {
        this.Chain = chain;
        this.logger = loggerFactory.CreateLogger<TransferRunnerNative>();
    }

    //

    /// <summary>
    /// The chain.
    /// </summary>
    public Chain Chain { get; }

    //

    /// <summary>
    /// Submit a transfer using the native Ethereum client.
    /// </summary>
    /// <param name="context">The context to use for the transaction.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="nonce">The nonce to use for the transaction.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <returns>The transaction receipt.</returns>
    /// <exception cref="NotImplementedException"></exception>
    protected override async Task<TransactionReceipt> SubmitTransactionAsync(
        IJsonRpcContext context,
        TransferInvocationOptions options,
        ulong nonce,
        IDictionary<string, object?>? args)
    {
        var startTime = DateTime.UtcNow;

        var transactionHash = await this.Chain.TransferAsync(context, nonce, options);

        if (options.WaitForReceiptTimeout == TimeSpan.Zero)
        {
            this.logger.LogInformation("Transaction {Id}: hash: {Hash}, submitted-ms: {SubmitMs}",
                transactionHash, transactionHash, (DateTime.UtcNow - startTime).TotalMilliseconds);

            return TransactionReceipt.FromTransactionHash(transactionHash);
        }

        // Wait for the receipt

        var submittedAt = DateTime.UtcNow;
        var timeout = options.WaitForReceiptTimeout;

        var (receipt, deadlineReached) =
            await this.Chain.TryWaitForTransactionAsync(context, transactionHash, timeout);

        var receiptAt = DateTime.UtcNow;

        this.logger.LogInformation(
            "Transaction {Id}: hash: {Hash}, submitted-ms: {SubmitMs}, receipt-ms: {ReceiptMs}",
            transactionHash, receipt?.TransactionHash, (submittedAt - startTime).TotalMilliseconds, (receiptAt - submittedAt).TotalMilliseconds);

        if (deadlineReached)
        {
            throw new TransactionTimeoutException(transactionHash);
        }

        return receipt!;
    }

    /// <summary>
    /// Get the expected failure of a transfer.
    /// </summary>
    /// <param name="ex">The exception to get the expected failure for.</param>
    /// <returns>The expected failure.</returns>
    /// <exception cref="NotImplementedException"></exception>
    protected override CommonTransactionFailure GetExpectedFailure(Exception ex)
    {
        return TransactionRunnerNative.MapException(ex);
    }

    //

    /// <summary>
    /// Create a new instance of the TransferRunnerNative class.
    /// </summary>
    /// <param name="endpoint">The endpoint to use for the transaction.</param>
    /// <param name="sender">The sender to use for the transaction.</param>
    /// <returns>A new instance of the TransferRunnerNative class.</returns>
    public static TransferRunnerNative CreateDefault(
        Endpoint endpoint,
        Sender sender)
    {
        var chain = Chain.CreateDefault(endpoint, sender);

        return new TransferRunnerNative(sender.NonceStore, chain, endpoint.LoggerFactory);
    }
}