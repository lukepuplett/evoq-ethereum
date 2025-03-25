using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A transaction runner that uses our internal native Ethereum client to submit transactions.
/// </summary>
public class TransactionRunnerNative
    : TransactionRunner<Contract, ContractInvocationOptions, IDictionary<string, object?>, TransactionReceipt>
{
    private readonly Sender sender;

    //

    /// <summary>
    /// Create a new instance of the TransactionRunnerNative class.
    /// </summary>
    /// <param name="sender">The sender to use for the transaction.</param>
    /// <param name="loggerFactory">The logger factory to use for the transaction.</param>
    public TransactionRunnerNative(Sender sender, ILoggerFactory loggerFactory)
        : base(sender.NonceStore, loggerFactory)
    {
        if (sender.SenderAccount.PrivateKey.IsZeroValue())
        {
            throw new ArgumentNullException(nameof(sender));
        }

        this.sender = sender;
    }

    //

    /// <summary>
    /// Submit a transaction using the native Ethereum client.
    /// </summary>
    /// <param name="contract">The contract to submit the transaction to.</param>
    /// <param name="functionName">The name of the function to call on the contract.</param>
    /// <param name="nonce">The nonce to use for the transaction.</param>
    /// <param name="options">The options for the transaction.</param>
    /// <param name="args">The arguments to pass to the function.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction receipt.</returns>
    protected override async Task<TransactionReceipt> SubmitTransactionAsync(
        Contract contract,
        string functionName,
        ulong nonce,
        ContractInvocationOptions options,
        IDictionary<string, object?> args,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var startTime = DateTime.UtcNow;

            var id = await contract.InvokeMethodAsync(
                functionName,
                nonce,
                options,
                args,
                cancellationToken);

            var submitDuration = DateTime.UtcNow - startTime;

            var (receipt, deadlineReached) = await contract.Chain.TryWaitForTransactionAsync(
                id, TimeSpan.FromSeconds(120), cancellationToken);

            if (deadlineReached)
            {
                throw new TransactionTimeoutException(id);
            }

            return receipt!;
        }
        catch (Exception ex)
        {
            throw new FailedToSubmitTransactionException(
                $"Transaction failed to submit transaction. {ex.Message}",
                ex);
        }
    }

    //

    /// <summary>
    /// Get the expected failure of a transaction.
    /// </summary>
    /// <param name="ex">The exception to get the expected failure for.</param>
    /// <returns>The expected failure.</returns>
    protected override CommonTransactionFailure GetExpectedFailure(Exception ex)
    {
        if (ex is OutOfGasException)
        {
            return CommonTransactionFailure.OutOfGas;
        }

        if (ex is InvalidNonceException invalidNonce && invalidNonce.Message.Contains("nonce too low", StringComparison.OrdinalIgnoreCase))
        {
            return CommonTransactionFailure.NonceTooLow;
        }

        if (ex is RevertedTransactionException)
        {
            return CommonTransactionFailure.Reverted;
        }

        return CommonTransactionFailure.Other;
    }
}
