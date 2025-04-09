using System;
using System.Threading;
using Evoq.Ethereum.Contracts;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A context for interacting with the Ethereum blockchain.
/// </summary>
public class JsonRpcContext : IJsonRpcContext
{
    private static readonly Random rng = new Random();

    //

    /// <summary>
    /// The ID of the request.
    /// </summary>
    public Func<int> GetNextId { get; init; } = () => rng.Next();

    //

    /// <summary>
    /// The cancellation token to use for the interaction.
    /// </summary>
    public CancellationToken CancellationToken { get; init; } = CancellationToken.None;

    /// <summary>
    /// The cache to use for the interaction.
    /// </summary>
    public IJsonRpcCache? Cache { get; init; }

    //

    /// <summary>
    /// Whether the cancellation token has been cancelled.
    /// </summary>
    public bool IsCancelled => this.CancellationToken.IsCancellationRequested;
}

/// <summary>
/// A context for interacting with the Ethereum blockchain.
/// </summary>
public abstract class JsonRpcContext<TFeeEstimate> : JsonRpcContext
    where TFeeEstimate : ISuggestedGasOptions
{
    /// <summary>
    /// A function that converts a fee estimate to a gas options object.
    /// </summary>
    public Func<TFeeEstimate, GasOptions> FeeEstimateToGasOptions { get; init; } = (feeEstimate) => feeEstimate.ToSuggestedGasOptions();
}