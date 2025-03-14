using System.Numerics;

namespace Evoq.Ethereum;

/// <summary>
/// Provides common Ethereum value constants and conversion utilities.
/// </summary>
/// <remarks>
/// Contains predefined values for common denominations and gas limits to make
/// working with Ethereum values more readable and less error-prone.
/// </remarks>
public static class WeiAmounts
{
    // Denominations

    /// <summary>
    /// One wei (the smallest unit of Ether).
    /// </summary>
    public static readonly BigInteger Wei = 1;

    /// <summary>
    /// One kwei (1,000 wei).
    /// </summary>
    public static readonly BigInteger Kwei = BigInteger.Parse("1000");

    /// <summary>
    /// One mwei (1,000,000 wei).
    /// </summary>
    public static readonly BigInteger Mwei = BigInteger.Parse("1000000");

    /// <summary>
    /// One gwei (1,000,000,000 wei) - commonly used for gas prices.
    /// </summary>
    public static readonly BigInteger Gwei = BigInteger.Parse("1000000000");

    /// <summary>
    /// One szabo (1,000,000,000,000 wei).
    /// </summary>
    public static readonly BigInteger Szabo = BigInteger.Parse("1000000000000");

    /// <summary>
    /// One finney (1,000,000,000,000,000 wei).
    /// </summary>
    public static readonly BigInteger Finney = BigInteger.Parse("1000000000000000");

    /// <summary>
    /// One ether (1,000,000,000,000,000,000 wei) - the main denomination.
    /// </summary>
    public static readonly BigInteger Ether = BigInteger.Parse("1000000000000000000");

    // Common gas limits

    /// <summary>
    /// Standard gas limit for a simple ETH transfer (21,000 gas).
    /// </summary>
    public static readonly BigInteger EthTransferGas = BigInteger.Parse("21000");

    /// <summary>
    /// Typical gas limit for an ERC-20 token transfer (65,000 gas).
    /// </summary>
    public static readonly BigInteger Erc20TransferGas = BigInteger.Parse("65000");

    /// <summary>
    /// Typical gas limit for an ERC-721 NFT transfer (100,000 gas).
    /// </summary>
    public static readonly BigInteger Erc721TransferGas = BigInteger.Parse("100000");

    /// <summary>
    /// Safe gas limit for a contract deployment (4,000,000 gas).
    /// </summary>
    public static readonly BigInteger ContractDeploymentGas = BigInteger.Parse("4000000");

    // Common gas prices

    /// <summary>
    /// Low priority gas price (1 gwei).
    /// </summary>
    public static readonly BigInteger LowPriorityFee = 1 * Gwei;

    /// <summary>
    /// Standard priority gas price (2 gwei).
    /// </summary>
    public static readonly BigInteger StandardPriorityFee = 2 * Gwei;

    /// <summary>
    /// High priority gas price (3 gwei).
    /// </summary>
    public static readonly BigInteger HighPriorityFee = 3 * Gwei;

    /// <summary>
    /// Urgent priority gas price (5 gwei).
    /// </summary>
    public static readonly BigInteger UrgentPriorityFee = 5 * Gwei;

    // Conversion methods

    /// <summary>
    /// Converts wei to gwei.
    /// </summary>
    /// <param name="wei">The amount in wei.</param>
    /// <returns>The amount in gwei.</returns>
    public static decimal WeiToGwei(BigInteger wei)
    {
        return (decimal)wei / (decimal)Gwei;
    }

    /// <summary>
    /// Converts gwei to wei.
    /// </summary>
    /// <param name="gwei">The amount in gwei.</param>
    /// <returns>The amount in wei.</returns>
    public static BigInteger GweiToWei(decimal gwei)
    {
        return (BigInteger)(gwei * (decimal)Gwei);
    }

    /// <summary>
    /// Converts wei to ether.
    /// </summary>
    /// <param name="wei">The amount in wei.</param>
    /// <returns>The amount in ether.</returns>
    public static decimal WeiToEther(BigInteger wei)
    {
        return (decimal)wei / (decimal)Ether;
    }

    /// <summary>
    /// Converts ether to wei.
    /// </summary>
    /// <param name="ether">The amount in ether.</param>
    /// <returns>The amount in wei.</returns>
    public static BigInteger EtherToWei(decimal ether)
    {
        return (BigInteger)(ether * (decimal)Ether);
    }

    /// <summary>
    /// Creates a wei value from a gwei amount.
    /// </summary>
    /// <param name="gwei">The amount in gwei.</param>
    /// <returns>The amount in wei.</returns>
    public static BigInteger WeiFromGwei(decimal gwei)
    {
        return GweiToWei(gwei);
    }

    /// <summary>
    /// Creates a wei value from an ether amount.
    /// </summary>
    /// <param name="ether">The amount in ether.</param>
    /// <returns>The amount in wei.</returns>
    public static BigInteger WeiFromEther(decimal ether)
    {
        return EtherToWei(ether);
    }

    /// <summary>
    /// How much one unit of local currency is worth in wei.
    /// </summary>
    /// <param name="valueOfOneEther">The value of a single ether in local currency.</param>
    /// <returns>The amount in wei.</returns>
    public static BigInteger LocalCurrencyInWei(BigInteger valueOfOneEther)
    {
        // If 1 ETH = $1926, then $1 = (1/1926) ETH = (10^18 / 1926) wei
        return Ether / valueOfOneEther;
    }

    /// <summary>
    /// How much the amount of wei is worth in local currency.
    /// </summary>
    /// <param name="valueOfOneEther">The value of a single ether in local currency.</param>
    /// <param name="quantityOfWei">The amount of wei.</param>
    /// <returns>The amount in local currency.</returns>
    public static BigInteger WeiInLocalCurrency(BigInteger valueOfOneEther, BigInteger quantityOfWei)
    {
        return valueOfOneEther * quantityOfWei / Ether;
    }
}