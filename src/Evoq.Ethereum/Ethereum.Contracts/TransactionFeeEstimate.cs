using System.Numerics;

namespace Evoq.Ethereum.Contracts;

/// <summary>
/// Represents a complete transaction fee estimate for EIP-1559 transactions.
/// </summary>
/// <remarks>
/// EIP-1559 introduced a new fee market in Ethereum (London fork, August 2021) that uses a base fee 
/// which is burned and a priority fee which goes to miners/validators.
/// </remarks>
public class TransactionFeeEstimate
{
    /// <summary>
    /// The estimated gas limit for the transaction.
    /// </summary>
    /// <remarks>
    /// Gas is the unit of computation in Ethereum. Each operation costs a certain amount of gas.
    /// The gas limit is the maximum amount of gas you're willing to use for your transaction.
    /// If your transaction requires more gas than this limit, it will fail with an "out of gas" error.
    /// </remarks>
    public BigInteger GasLimit { get; set; }

    /// <summary>
    /// The suggested maximum fee per gas (in wei).
    /// </summary>
    /// <remarks>
    /// This is the absolute maximum amount you're willing to pay per unit of gas, including both
    /// the base fee (burned) and the priority fee (to miners). Typically calculated as:
    /// maxFeePerGas = baseFeePerGas * 2 + maxPriorityFeePerGas
    /// The multiplier accounts for potential base fee increases between blocks.
    /// </remarks>
    public EtherAmount MaxFeePerGas { get; set; }

    /// <summary>
    /// The suggested maximum priority fee per gas (in wei).
    /// </summary>
    /// <remarks>
    /// This is the "tip" you're willing to pay to miners/validators per unit of gas.
    /// Higher priority fees can result in faster transaction processing during network congestion.
    /// </remarks>
    public EtherAmount MaxPriorityFeePerGas { get; set; }

    /// <summary>
    /// The current base fee per gas (in wei).
    /// </summary>
    /// <remarks>
    /// The base fee is determined by the network based on demand for block space.
    /// It's the minimum fee required for inclusion and is burned (removed from circulation).
    /// The base fee automatically adjusts based on network congestion.
    /// </remarks>
    public EtherAmount BaseFeePerGas { get; set; }

    /// <summary>
    /// The estimated total transaction fee in wei.
    /// </summary>
    /// <remarks>
    /// Calculated as: (baseFeePerGas + maxPriorityFeePerGas) * gasLimit
    /// The actual fee may be lower if the transaction uses less than the gas limit.
    /// </remarks>
    public EtherAmount EstimatedFee { get; set; }

    /// <summary>
    /// The legacy gas price, for backward compatibility with pre-EIP-1559 transactions.
    /// </summary>
    /// <remarks>
    /// Before EIP-1559, transactions used a single gas price. This value combines the base fee
    /// and priority fee to provide an equivalent gas price for legacy (Type 0) transactions.
    /// </remarks>
    public EtherAmount GasPrice { get; set; }

    /// <summary>
    /// The estimated maximum total fee in wei.
    /// </summary>
    /// <remarks>
    /// This is the worst-case scenario fee: GasLimit * MaxFeePerGas
    /// You'll never pay more than this amount, regardless of base fee changes.
    /// </remarks>
    public EtherAmount MaxFee => GasLimit * MaxFeePerGas;

    /// <summary>
    /// The estimated minimum total fee in wei.
    /// </summary>
    /// <remarks>
    /// This is the minimum possible fee if only the base fee is paid: GasLimit * BaseFeePerGas
    /// In practice, you'll always pay at least the priority fee on top of this.
    /// </remarks>
    public EtherAmount MinFee => GasLimit * BaseFeePerGas;

    /// <summary>
    /// The estimated priority fee in wei.
    /// </summary>
    /// <remarks>
    /// This is the portion of the fee that goes to miners/validators: GasLimit * MaxPriorityFeePerGas
    /// </remarks>
    public EtherAmount PriorityFee => GasLimit * MaxPriorityFeePerGas;
}