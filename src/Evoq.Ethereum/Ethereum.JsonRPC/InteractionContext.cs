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
