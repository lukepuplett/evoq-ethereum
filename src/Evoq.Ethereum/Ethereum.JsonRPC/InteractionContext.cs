using System;
using Evoq.Ethereum.Contracts;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A context for interacting with the Ethereum blockchain.
/// </summary>
public class InteractionContext : JsonRpcContext<ITransactionFeeEstimate>
{
    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public InteractionContext(
        Endpoint endpoint,
        Sender sender,
        Func<ITransactionFeeEstimate, GasOptions>? feeEstimateToGasOptions = null,
        IJsonRpcCache? cache = null)
        : base()
    {
        this.Endpoint = endpoint;
        this.Sender = sender;

        if (feeEstimateToGasOptions != null)
        {
            this.FeeEstimateToGasOptions = feeEstimateToGasOptions;
        }

        if (cache != null)
        {
            this.Cache = cache;
        }
    }

    //

    /// <summary>
    /// The endpoint to use for the interaction.
    /// </summary>
    public Endpoint Endpoint { get; init; }

    /// <summary>
    /// The sender to use for the interaction.
    /// </summary>
    public Sender Sender { get; init; }

    /// <summary>
    /// Gets or sets the maximum time to wait for a transaction receipt.
    /// </summary>
    public TimeSpan WaitForReceiptTimeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Gets or sets whether to wait for the transaction receipt.
    /// When set to false, WaitForReceiptTimeout is set to zero.
    /// </summary>
    public bool ShouldWaitForReceipt
    {
        get => this.WaitForReceiptTimeout > TimeSpan.Zero;
        set => this.WaitForReceiptTimeout = value ? TimeSpan.FromMinutes(2) : TimeSpan.Zero;
    }

    //

    /// <summary>
    /// Returns a string representation of the interaction context.
    /// </summary>
    /// <returns>A string representation of the interaction context.</returns>
    public override string ToString()
    {
        return $"Endpoint: {this.Endpoint}, Sender: {this.Sender}";
    }

}
