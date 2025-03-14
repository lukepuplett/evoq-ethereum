using System.Numerics;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Represents a complete transaction fee estimate for EIP-1559 transactions.
/// </summary>
public class TransactionFeeEstimate
{
    /// <summary>
    /// The estimated gas limit for the transaction.
    /// </summary>
    public BigInteger GasLimit { get; set; }

    /// <summary>
    /// The suggested maximum fee per gas (in wei).
    /// </summary>
    public BigInteger MaxFeePerGas { get; set; }

    /// <summary>
    /// The suggested maximum priority fee per gas (in wei).
    /// </summary>
    public BigInteger MaxPriorityFeePerGas { get; set; }

    /// <summary>
    /// The current base fee per gas (in wei).
    /// </summary>
    public BigInteger BaseFeePerGas { get; set; }

    /// <summary>
    /// The estimated total transaction fee in wei.
    /// </summary>
    public BigInteger EstimatedFeeInWei { get; set; }

    /// <summary>
    /// The legacy gas price, for backward compatibility with pre-EIP-1559 transactions.
    /// </summary>
    public BigInteger GasPrice { get; set; }

    /// <summary>
    /// The estimated maximum total fee in wei (GasLimit * MaxFeePerGas).
    /// </summary>
    public BigInteger MaxFeeInWei => GasLimit * MaxFeePerGas;

    /// <summary>
    /// The estimated minimum total fee in wei (GasLimit * BaseFeePerGas).
    /// </summary>
    public BigInteger MinFeeInWei => GasLimit * BaseFeePerGas;

    /// <summary>
    /// The estimated priority fee in wei (GasLimit * MaxPriorityFeePerGas).
    /// </summary>
    public BigInteger PriorityFeeInWei => GasLimit * MaxPriorityFeePerGas;

    /// <summary>
    /// Gets the estimated fee in Ether.
    /// </summary>
    public decimal EstimatedFeeInEther => ConvertWeiToEther(EstimatedFeeInWei);

    /// <summary>
    /// Gets the maximum fee in Ether.
    /// </summary>
    public decimal MaxFeeInEther => ConvertWeiToEther(MaxFeeInWei);

    /// <summary>
    /// Gets the minimum fee in Ether.
    /// </summary>
    public decimal MinFeeInEther => ConvertWeiToEther(MinFeeInWei);

    /// <summary>
    /// Gets the priority fee in Ether.
    /// </summary>
    public decimal PriorityFeeInEther => ConvertWeiToEther(PriorityFeeInWei);

    /// <summary>
    /// Converts wei to ether.
    /// </summary>
    /// <param name="wei">The amount in wei.</param>
    /// <returns>The amount in ether.</returns>
    private static decimal ConvertWeiToEther(BigInteger wei)
    {
        // 1 Ether = 10^18 Wei
        BigInteger divisor = BigInteger.Pow(10, 18);

        // Handle potential precision loss when converting to decimal
        decimal result = 0;
        BigInteger remainder = wei;

        if (wei >= divisor)
        {
            BigInteger quotient = BigInteger.DivRem(wei, divisor, out remainder);
            result = (decimal)quotient;
        }

        if (remainder > 0)
        {
            decimal fractionalPart = (decimal)remainder / (decimal)divisor;
            result += fractionalPart;
        }

        return result;
    }
}