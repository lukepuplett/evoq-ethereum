using System;
using System.Numerics;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// A class that can convert a transaction fee estimate to a gas options object.
/// </summary>
public interface ISuggestedGasOptions
{
    /// <summary>
    /// Returns a new EIP-1559 gas options object with the estimated gas limit and suggested max fee per gas.
    /// </summary>
    /// <returns>A new EIP-1559 gas options object.</returns>
    GasOptions ToSuggestedGasOptions();
}

/// <summary>
/// A factory for creating gas options objects.
/// </summary>
public interface IGasOptionsFactory
{
    /// <summary>
    /// Returns a new EIP-1559 gas options object with the estimated gas limit and suggested max fee per gas.
    /// </summary>
    /// <returns>A new EIP-1559 gas options object.</returns>
    GasOptions ToGasOptions(Func<ITransactionFeeEstimate, GasOptions> gasOptionsFactory);
}

/// <summary>
/// Represents a complete transaction fee estimate for EIP-1559 transactions.
/// </summary>
/// <remarks>
/// EIP-1559 introduced a new fee market in Ethereum (London fork, August 2021) that uses a base fee 
/// which is burned and a priority fee which goes to miners/validators.
/// </remarks>
public interface ITransactionFeeEstimate : ISuggestedGasOptions, IGasOptionsFactory
{
    /// <summary>
    /// The estimated gas limit for the transaction.
    /// </summary>
    BigInteger EstimatedGasLimit { get; }

    /// <summary>
    /// The suggested maximum fee per gas (in wei).
    /// </summary>
    EtherAmount SuggestedMaxFeePerGas { get; }

    /// <summary>
    /// The suggested maximum priority fee per gas (in wei).
    /// </summary>
    EtherAmount SuggestedMaxPriorityFeePerGas { get; }

    /// <summary>
    /// The current base fee per gas (in wei).
    /// </summary>
    EtherAmount CurrentBaseFeePerGas { get; }

    /// <summary>
    /// The estimated total transaction fee in wei.
    /// </summary>
    EtherAmount EstimatedTotalFee { get; }

    /// <summary>
    /// The legacy gas price, for backward compatibility with pre-EIP-1559 transactions.
    /// </summary>
    EtherAmount LegacyGasPrice { get; }

    /// <summary>
    /// The estimated maximum total fee in wei.
    /// </summary>
    EtherAmount EstimatedMaxFee { get; }

    /// <summary>
    /// The estimated minimum total fee in wei.
    /// </summary>
    EtherAmount EstimatedMinFee { get; }

    /// <summary>
    /// The estimated priority fee in wei.
    /// </summary>
    EtherAmount EstimatedPriorityFee { get; }

    /// <summary>
    /// Returns a new TransactionFeeEstimate with all values converted to ether.
    /// </summary>
    ITransactionFeeEstimate InEther();
}

/// <summary>
/// Represents a complete transaction fee estimate for EIP-1559 transactions.
/// </summary>
/// <remarks>
/// EIP-1559 introduced a new fee market in Ethereum (London fork, August 2021) that uses a base fee 
/// which is burned and a priority fee which goes to miners/validators.
/// </remarks>
public class TransactionFeeEstimate : ITransactionFeeEstimate
{
    internal const int BaseFeeSafetyMarginScale = 2;

    /// <summary>
    /// The estimated gas limit for the transaction.
    /// </summary>
    /// <remarks>
    /// Gas is the unit of computation in Ethereum. Each operation costs a certain amount of gas.
    /// The gas limit is the maximum amount of gas you're willing to use for your transaction.
    /// If your transaction requires more gas than this limit, it will fail with an "out of gas" error.
    /// </remarks>
    public BigInteger EstimatedGasLimit { get; set; }

    /// <summary>
    /// The suggested maximum fee per gas (in wei).
    /// </summary>
    /// <remarks>
    /// This is the absolute maximum amount you're willing to pay per unit of gas, including both
    /// the base fee (burned) and the priority fee (to miners). Typically calculated as:
    /// maxFeePerGas = baseFeePerGas * 2 + maxPriorityFeePerGas
    /// The multiplier accounts for potential base fee increases between blocks.
    /// </remarks>
    public EtherAmount SuggestedMaxFeePerGas { get; set; }

    /// <summary>
    /// The suggested maximum priority fee per gas (in wei).
    /// </summary>
    /// <remarks>
    /// This is the "tip" you're willing to pay to miners/validators per unit of gas.
    /// Higher priority fees can result in faster transaction processing during network congestion.
    /// </remarks>
    public EtherAmount SuggestedMaxPriorityFeePerGas { get; set; }

    /// <summary>
    /// The current base fee per gas (in wei).
    /// </summary>
    /// <remarks>
    /// The base fee is determined by the network based on demand for block space.
    /// It's the minimum fee required for inclusion and is burned (removed from circulation).
    /// The base fee automatically adjusts based on network congestion.
    /// </remarks>
    public EtherAmount CurrentBaseFeePerGas { get; set; }

    /// <summary>
    /// The estimated total transaction fee in wei.
    /// </summary>
    /// <remarks>
    /// Calculated as: (baseFeePerGas + maxPriorityFeePerGas) * gasLimit
    /// The actual fee may be lower if the transaction uses less than the gas limit.
    /// </remarks>
    public EtherAmount EstimatedTotalFee { get; set; }

    /// <summary>
    /// The legacy gas price, for backward compatibility with pre-EIP-1559 transactions.
    /// </summary>
    /// <remarks>
    /// Before EIP-1559, transactions used a single gas price. This value combines the base fee
    /// and priority fee to provide an equivalent gas price for legacy (Type 0) transactions.
    /// </remarks>
    public EtherAmount LegacyGasPrice { get; set; }

    /// <summary>
    /// The estimated maximum total fee in wei.
    /// </summary>
    /// <remarks>
    /// This is the worst-case scenario fee: GasLimit * MaxFeePerGas
    /// You'll never pay more than this amount, regardless of base fee changes.
    /// </remarks>
    public EtherAmount EstimatedMaxFee => EstimatedGasLimit * SuggestedMaxFeePerGas;

    /// <summary>
    /// The estimated minimum total fee in wei.
    /// </summary>
    /// <remarks>
    /// This is the minimum possible fee if only the base fee is paid: GasLimit * BaseFeePerGas
    /// In practice, you'll always pay at least the priority fee on top of this.
    /// </remarks>
    public EtherAmount EstimatedMinFee => EstimatedGasLimit * CurrentBaseFeePerGas;

    /// <summary>
    /// The estimated priority fee in wei.
    /// </summary>
    /// <remarks>
    /// This is the portion of the fee that goes to miners/validators: GasLimit * MaxPriorityFeePerGas
    /// </remarks>
    public EtherAmount EstimatedPriorityFee => EstimatedGasLimit * SuggestedMaxPriorityFeePerGas;

    //

    /// <summary>
    /// Returns a new TransactionFeeEstimate with all values converted to ether.
    /// </summary>
    public ITransactionFeeEstimate InEther() => new TransactionFeeEstimate
    {
        EstimatedGasLimit = EstimatedGasLimit,
        SuggestedMaxFeePerGas = SuggestedMaxFeePerGas.InEther,
        SuggestedMaxPriorityFeePerGas = SuggestedMaxPriorityFeePerGas.InEther,
        CurrentBaseFeePerGas = CurrentBaseFeePerGas.InEther,
        EstimatedTotalFee = EstimatedTotalFee.InEther,
        LegacyGasPrice = LegacyGasPrice.InEther,
    };

    /// <summary>
    /// Returns a new EIP-1559 gas options object with the estimated gas limit and suggested max fee per gas.
    /// </summary>
    /// <returns>A new EIP-1559 gas options object.</returns>
    public GasOptions ToSuggestedGasOptions() => new EIP1559GasOptions(
        gasLimit: (ulong)EstimatedGasLimit,
        maxFeePerGas: SuggestedMaxFeePerGas,
        maxPriorityFeePerGas: SuggestedMaxPriorityFeePerGas);

    /// <summary>
    /// Returns a new EIP-1559 gas options object with the estimated gas limit and suggested max fee per gas.
    /// </summary>
    /// <returns>A new EIP-1559 gas options object.</returns>
    public GasOptions ToGasOptions(Func<ITransactionFeeEstimate, GasOptions> gasOptionsFactory)
    {
        return gasOptionsFactory(this);
    }
}

/// <summary>
/// Extension methods for the GasOptions class.
/// </summary>
public static class GasOptionsExtensions
{
    /// <summary>
    /// Returns a new EIP-1559 gas options object with the estimated gas limit and suggested max fee per gas.
    /// </summary>
    /// <param name="estimate">The transaction fee estimate.</param>
    /// <param name="tipFactor">The multiplier for the max priority fee per gas, 1.0 is normal and 2.0 is urgent, while 0.5 is whenever possible.</param>
    /// <returns>A new EIP-1559 gas options object.</returns>
    public static GasOptions ToSuggestedWithTip(this IGasOptionsFactory estimate, decimal tipFactor) =>
        estimate.ToGasOptions(estimate =>
        {
            var safetyMargin = TransactionFeeEstimate.BaseFeeSafetyMarginScale;
            var scaledTip = estimate.SuggestedMaxPriorityFeePerGas * tipFactor;
            var scaledMaxFee = estimate.CurrentBaseFeePerGas * safetyMargin + scaledTip;  // Recalculate with new tip

            return new EIP1559GasOptions(
                gasLimit: (ulong)estimate.EstimatedGasLimit,
                maxFeePerGas: scaledMaxFee,
                maxPriorityFeePerGas: scaledTip);
        });
}