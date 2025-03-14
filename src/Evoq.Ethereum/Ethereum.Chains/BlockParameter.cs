using System.Numerics;

namespace Evoq.Ethereum.Chains;

// Missing Feature 1: Support for EIP-1559 Fee Market (Base Fee and Priority Fee)
//
// The current implementation only provides a single gas price via GasPriceAsync, which aligns with the legacy eth_gasPrice
// JSON-RPC method. However, since the Ethereum London Hard Fork (August 2021) introduced EIP-1559, modern transactions
// (Type 2) use a fee market with a base fee (burned), a priority fee (tip to miners/validators), and a max fee (cap on total
// fee per gas). This class lacks methods to fetch or suggest these values, which are critical for accurate fee estimation in
// Type 2 transactions—the default in most Ethereum clients and wallets today.
// 
// Why It's Important:
// - Most transactions are Type 2 (EIP-1559), and relying solely on eth_gasPrice can lead to inaccurate fee estimates.
// - Without base fee and priority fee data, users might overpay or underpay, resulting in rejected or delayed transactions.
// - Modern Ethereum applications (e.g., wallets, dApps, EAS operations) need to suggest maxFeePerGas and maxPriorityFeePerGas
//   for optimal transaction inclusion.
// 
// Suggested Improvement:
// Add methods to fetch the base fee (e.g., from the latest block's baseFeePerGas) and suggest priority/max fees, possibly using
// eth_feeHistory or by analyzing recent blocks. Example implementation:
// 
// public async Task<BigInteger> GetBaseFeeAsync()
// {
//     var block = await this.chainClient.GetBlockByNumberAsync("latest", false);
//     return Contract.ConvertHexToBigInteger(block.BaseFeePerGas);
// }
// 
// public async Task<(BigInteger maxFeePerGas, BigInteger maxPriorityFeePerGas)> SuggestType2FeesAsync()
// {
//     var baseFee = await GetBaseFeeAsync();
//     var priorityFee = BigInteger.Parse("2000000000"); // 2 gwei as a default
//     var maxFee = baseFee * 2 + priorityFee; // Heuristic: 2x base fee + priority fee
//     return (maxFee, priorityFee);
// }

// Missing Feature 3: Chain Information and Block Data Access
//
// The Chain class lacks methods to retrieve basic chain information and block data, such as the chain ID (eth_chainId), current
// block number (eth_blockNumber), and block details (e.g., eth_getBlockByNumber). These are foundational for many Ethereum
// interactions, including transaction signing, fee calculations, and dApp synchronization.
// 
// Why It's Important:
// - Chain ID is required for signing transactions (EIP-155) to prevent replay attacks across networks (e.g., Mainnet vs. Sepolia).
// - Block number is useful for tracking the latest chain state or querying historical data.
// - Block details (e.g., baseFeePerGas, timestamp) are necessary for modern fee calculations (Type 2 transactions) and for
//   applications needing chain metadata (e.g., verifying EAS attestations with block timestamps).
// - Without these methods, users must rely on external tools or hardcoded values, which is error-prone and inefficient.
// 
// Suggested Improvement:
// Add methods to fetch chain ID, block number, and block details. Example implementation:
// 
// public async Task<BigInteger> GetChainIdAsync()
// {
//     var hex = await this.chainClient.ChainIdAsync();
//     return Contract.ConvertHexToBigInteger(hex);
// }
// 
// public async Task<BigInteger> GetBlockNumberAsync()
// {
//     var hex = await this.chainClient.GetBlockNumberAsync();
//     return Contract.ConvertHexToBigInteger(hex);
// }
// 
// public async Task<dynamic> GetBlockAsync(string blockNumberOrTag, bool includeTransactions)
// {
//     return await this.chainClient.GetBlockByNumberAsync(blockNumberOrTag, includeTransactions);
// }

/// <summary>
/// Represents a block parameter that can be either a block number or a tag.
/// </summary>
public class BlockParameter
{
    private readonly string value;

    private BlockParameter(string value)
    {
        this.value = value;
    }

    /// <summary>
    /// Creates a block parameter from a block number.
    /// </summary>
    public static BlockParameter FromNumber(BigInteger blockNumber)
    {
        return new BlockParameter(blockNumber.ToHexString());
    }

    /// <summary>
    /// Gets the "latest" block parameter.
    /// </summary>
    public static BlockParameter Latest => new BlockParameter("latest");

    /// <summary>
    /// Gets the "pending" block parameter.
    /// </summary>
    public static BlockParameter Pending => new BlockParameter("pending");

    /// <summary>
    /// Gets the "earliest" block parameter.
    /// </summary>
    public static BlockParameter Earliest => new BlockParameter("earliest");

    /// <summary>
    /// Gets the "safe" block parameter.
    /// </summary>
    public static BlockParameter Safe => new BlockParameter("safe");

    /// <summary>
    /// Gets the "finalized" block parameter.
    /// </summary>
    public static BlockParameter Finalized => new BlockParameter("finalized");

    /// <summary>
    /// Returns the string representation of the block parameter.
    /// </summary>
    public override string ToString() => value;
}