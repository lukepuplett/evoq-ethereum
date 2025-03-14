using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.Contracts;

namespace Evoq.Ethereum.Chains;

// NOTE / see comments, bottom

/// <summary>
/// A class that represents a chain.
/// </summary>
public class Chain
{
    private readonly ChainClient chainClient;

    //

    /// <summary>
    /// Initializes a new instance of the Chain class.
    /// </summary>
    /// <param name="chainClient">The chain client.</param>
    public Chain(ChainClient chainClient)
    {
        this.chainClient = chainClient;
    }

    //

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    /// <returns>The current gas price.</returns>
    public async Task<BigInteger> GasPriceAsync()
    {
        var hex = await this.chainClient.GasPriceAsync();

        return hex.ToBigInteger();
    }

    /// <summary>
    /// Estimates the gas usage for a simple ETH transfer.
    /// </summary>
    /// <returns>The estimated gas usage for a simple ETH transfer.</returns>
    public Task<BigInteger> GetEthTransferGasAsync()
    {
        return Task.FromResult(BigInteger.Parse("21000"));
    }

    /// <summary>
    /// Gets the base fee per gas from the latest block.
    /// </summary>
    /// <returns>The base fee per gas in wei.</returns>
    public async Task<BigInteger> GetBaseFeeAsync()
    {
        var block = await this.chainClient.GetBlockByNumberWithTxObjectsAsync("latest");

        return block.BaseFeePerGas;
    }

    /// <summary>
    /// Gets the current chain ID.
    /// </summary>
    /// <returns>The chain ID as a BigInteger.</returns>
    public async Task<BigInteger> GetChainIdAsync()
    {
        var hex = await this.chainClient.ChainIdAsync();

        return hex.ToBigInteger();
    }

    /// <summary>
    /// Gets the current block number.
    /// </summary>
    /// <returns>The current block number as a BigInteger.</returns>
    public async Task<BigInteger> GetBlockNumberAsync()
    {
        var hex = await this.chainClient.GetBlockNumberAsync();

        return hex.ToBigInteger();
    }

    // removed GetBlockByNumberAsync which is now implemented in ChainClient using
    // two flavours of GetBlockByNumberAsync
    // - GetBlockByNumberWithTxHashesAsync
    // - GetBlockByNumberWithTxObjectsAsync
    //
    // LLM also generated a TransactionFeeEstimate class which is a good idea
    // but I don't know how it should be prepared and which RPC calls are needed
    // to populate the class.
    //
    // Presumably this class would be a good place to surface it but I am not sure
    // how to do that yet.


    /// <summary>
    /// Suggests appropriate fee values for EIP-1559 (Type 2) transactions.
    /// </summary>
    /// <returns>A tuple containing the suggested maxFeePerGas and maxPriorityFeePerGas in wei.</returns>
    public async Task<(BigInteger maxFeePerGas, BigInteger maxPriorityFeePerGas)> SuggestEip1559FeesAsync()
    {
        var baseFee = await GetBaseFeeAsync();

        // Default priority fee of 2 Gwei
        var priorityFee = BigInteger.Parse("2000000000");

        // Use fee history to get a more accurate priority fee suggestion
        var feeHistory = await GetFeeHistoryAsync(10, "latest", new[] { 50.0 });
        if (feeHistory != null && feeHistory.Reward.Length > 0 && feeHistory.Reward[0].Length > 0)
        {
            // Use the median (50th percentile) priority fee from recent blocks
            priorityFee = feeHistory.Reward[0][0];
        }

        // Max fee is typically set to 2x base fee + priority fee to account for base fee increases
        var maxFee = (baseFee * 2) + priorityFee;

        return (maxFee, priorityFee);
    }

    /// <summary>
    /// Gets fee history data for recent blocks.
    /// </summary>
    /// <param name="blockCount">Number of blocks to analyze.</param>
    /// <param name="newestBlock">The newest block to consider ("latest" or block number).</param>
    /// <param name="rewardPercentiles">Percentiles to sample for priority fees.</param>
    /// <returns>Fee history data including base fees and priority fee percentiles.</returns>
    public async Task<FeeHistory> GetFeeHistoryAsync(ulong blockCount, string newestBlock, double[] rewardPercentiles)
    {
        return await this.chainClient.GetFeeHistoryAsync(blockCount, newestBlock, rewardPercentiles);
    }

    /// <summary>
    /// Estimates gas for a transaction with the given parameters.
    /// </summary>
    /// <param name="from">The sender address.</param>
    /// <param name="to">The recipient address.</param>
    /// <param name="value">The amount of ETH to send (in wei).</param>
    /// <param name="data">The transaction data (for contract interactions).</param>
    /// <returns>The estimated gas limit for the transaction.</returns>
    public async Task<BigInteger> EstimateGasAsync(string from, string to, string? value = null, string? data = null)
    {
        var hex = await this.chainClient.EstimateGasAsync(from, to, value, data);

        return hex.ToBigInteger();
    }

    /// <summary>
    /// Provides a complete transaction fee estimate for EIP-1559 transactions.
    /// </summary>
    /// <param name="from">The sender address.</param>
    /// <param name="to">The recipient address.</param>
    /// <param name="value">The amount of ETH to send (in wei).</param>
    /// <param name="data">The transaction data (for contract interactions).</param>
    /// <returns>A complete fee estimate including gas limit and EIP-1559 fee parameters.</returns>
    public async Task<TransactionFeeEstimate> EstimateTransactionFeeAsync(
        string from,
        string to,
        string? value = null,
        string? data = null)
    {
        var gasLimit = await EstimateGasAsync(from, to, value, data);
        var (maxFeePerGas, maxPriorityFeePerGas) = await SuggestEip1559FeesAsync();
        var baseFee = await GetBaseFeeAsync();

        return new TransactionFeeEstimate
        {
            GasLimit = gasLimit,
            MaxFeePerGas = maxFeePerGas,
            MaxPriorityFeePerGas = maxPriorityFeePerGas,
            BaseFeePerGas = baseFee,
            EstimatedFeeInWei = baseFee * gasLimit + (maxPriorityFeePerGas * gasLimit)
        };
    }
}

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