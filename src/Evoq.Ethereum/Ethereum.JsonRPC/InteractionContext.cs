using System;
using System.Threading;
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
        Func<ITransactionFeeEstimate, GasOptions>? feeEstimateToGasOptions = null)
        : base()
    {
        this.Endpoint = endpoint;
        this.Sender = sender;

        if (feeEstimateToGasOptions != null)
        {
            this.FeeEstimateToGasOptions = feeEstimateToGasOptions;
        }
    }
}

/// <summary>
/// A context for interacting with the Ethereum blockchain.
/// </summary>
public abstract class JsonRpcContext<TFeeEstimate>
    where TFeeEstimate : ISuggestedGasOptions
{
    /// <summary>
    /// The endpoint to use for the interaction.
    /// </summary>
    public Endpoint Endpoint { get; init; }

    /// <summary>
    /// The sender to use for the interaction.
    /// </summary>
    public Sender Sender { get; init; }

    /// <summary>
    /// A function that converts a fee estimate to a gas options object.
    /// </summary>
    public Func<TFeeEstimate, GasOptions> FeeEstimateToGasOptions { get; init; } = (feeEstimate) => feeEstimate.ToSuggestedGasOptions();

    /// <summary>
    /// The cancellation token to use for the interaction.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    //

    /// <summary>
    /// Whether the cancellation token has been cancelled.
    /// </summary>
    public bool IsCancelled => this.CancellationToken.IsCancellationRequested;

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