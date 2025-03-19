using System.Numerics;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// The finance data to go into a transaction.
/// </summary>
/// <remarks>
/// There is no field for gas price since it is not used post London hardfork. This assumes all transactions
/// are EIP-1559 transactions, which is likely in practice.
/// </remarks>
/// <param name="Value">The amount of ETH/native currency to send with this transaction, measured in wei.
/// For contract interactions that don't transfer ETH, this should be 0.</param>
/// <param name="GasLimit">The maximum amount of gas units that can be consumed by this transaction.
/// Acts as a safety limit to prevent excessive gas consumption and failed transactions.</param>
/// <param name="MaxFeePerGas">The absolute maximum fee per gas the sender is willing to pay (base fee + priority fee), measured in wei.
/// Used for EIP-1559 transactions. Should be greater than or equal to MaxPriorityFeePerGas.</param>
/// <param name="MaxPriorityFeePerGas">The maximum priority fee (tip) per gas the sender is willing to pay to validators, measured in wei.
/// Used for EIP-1559 transactions. This fee is added on top of the network's base fee.</param>
public readonly record struct TransactionFees(
    BigInteger Value,
    BigInteger GasLimit,
    BigInteger MaxFeePerGas,
    BigInteger MaxPriorityFeePerGas
);
