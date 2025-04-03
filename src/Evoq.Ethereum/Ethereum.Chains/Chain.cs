using System;
using System.IO;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// A class that represents a specific blockchain.
/// </summary>
public class Chain
{
    private readonly ChainClient chainClient;
    private readonly ChainPollingStrategy.PollingInterval pollingInterval;

    //

    /// <summary>
    /// Initializes a new instance of the Chain class.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="chainClient">The chain client.</param>
    internal Chain(ulong chainId, ChainClient chainClient)
    {
        this.chainClient = chainClient;
        this.pollingInterval = ChainPollingStrategy.GetForChain(chainId);

        this.ChainId = chainId;
    }

    //

    /// <summary>
    /// Gets the chain ID.
    /// </summary>
    public ulong ChainId { get; }

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
        return Task.FromResult(WeiAmounts.EthTransferGas);
    }

    /// <summary>
    /// Gets the base fee per gas from the latest block.
    /// </summary>
    /// <returns>The base fee per gas in wei.</returns>
    /// <exception cref="LegacyChainException">Thrown when the network doesn't support EIP-1559 (pre-London fork).</exception>
    public async Task<BigInteger> GetBaseFeeAsync()
    {
        var block = await this.chainClient.TryGetBlockByNumberWithTxObjectsAsync(BlockParameter.Latest.ToString());

        if (block == null)
        {
            throw new ChainRequestException("Failed to retrieve the latest block.");
        }

        if (block.BaseFeePerGas == null)
        {
            throw new LegacyChainException(
                "The network does not support EIP-1559 (London fork). " +
                "Base fee is only available on networks that have implemented the London fork. " +
                "Use legacy gas price methods for pre-London networks.");
        }

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
    /// <exception cref="InvalidOperationException">Thrown when the network doesn't support EIP-1559 (pre-London fork).</exception>
    public async Task<(BigInteger MaxFeePerGasInWei, BigInteger MaxPriorityFeePerGasInWei)> SuggestEip1559FeesAsync()
    {
        // Fetch the current base fee from the network (in wei).
        // This is the minimum fee per gas set by the Ethereum protocol for the latest block.
        // Throws InvalidOperationException if the network doesn't support EIP-1559 (pre-London fork).
        var baseFee = await this.GetBaseFeeAsync();

        // Set a default priority fee (tip) of 2 Gwei (2,000,000,000 wei).
        // This is the amount paid to miners/validators to prioritize the transaction.
        // Assumes WeiAmounts.StandardPriorityFee is predefined as 2 Gwei.
        var tip = WeiAmounts.StandardPriorityFee;

        // Fetch fee history for the last 10 blocks to refine the tip.
        // Parameters: 10 blocks, latest block, 50th percentile reward (median tip).
        // This helps estimate what others are paying as tips in recent blocks.
        var tipHistory = await this.GetFeeHistoryAsync(10, BlockParameter.Latest, new[] { 50.0 });

        // Check if fee history data is valid and contains reward data.
        // tipHistory.Reward is an array of arrays: each inner array has percentile tips for a block.
        if (tipHistory != null && tipHistory.Reward.Length > 0 && tipHistory.Reward[0].Length > 0)
        {
            // Use the median tip (50th percentile) from the most recent block in the history.
            // This overrides the default 2 Gwei with a data-driven value based on network conditions.
            tip = tipHistory.Reward[0][0];
        }

        // Calculate the maxFeePerGas, which is the maximum you're willing to pay per gas unit.
        // Formula: (baseFee * 2) + tip where 2 is the safety margin
        // - Doubling baseFee provides a buffer for potential increases (up to 12.5% per block).
        // - Adding tip ensures the total covers both the base fee and priority fee.
        // BigInteger.Max ensures maxFee is at least as large as tip (though rare, as baseFee >= 0).
        var maxFee = BigInteger.Max((baseFee * TransactionFeeEstimate.BaseFeeSafetyMarginScale) + tip, tip);

        // Return the tuple with suggested values in wei.
        // - maxFee: The cap on total fee per gas (base fee + priority fee).
        // - tip: The suggested priority fee to incentivize inclusion.
        // Actual cost will be: Gas Used * (current baseFee + effective tip), capped by maxFee.
        return (maxFee, tip);
    }

    /// <summary>
    /// Gets fee history data for recent blocks.
    /// </summary>
    /// <param name="blockCount">Number of blocks to analyze.</param>
    /// <param name="newestBlock">The newest block to consider ("latest" or block number).</param>
    /// <param name="rewardPercentiles">Percentiles to sample for priority fees.</param>
    /// <returns>Fee history data including base fees and priority fee percentiles.</returns>
    public async Task<FeeHistory> GetFeeHistoryAsync(
        ulong blockCount,
        BlockParameter newestBlock,
        double[] rewardPercentiles)
    {
        return await this.chainClient.GetFeeHistoryAsync(
            blockCount,
            newestBlock.ToString(),
            rewardPercentiles);
    }

    /// <summary>
    /// Gets the transaction count (nonce) for an address.
    /// </summary>
    /// <param name="address">The address to get the transaction count for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The transaction count as a BigInteger.</returns>
    public async Task<BigInteger> GetTransactionCountAsync(EthereumAddress address, string blockParameter = "latest")
    {
        var hex = await this.chainClient.GetTransactionCountAsync(address, blockParameter);
        return hex.ToBigInteger();
    }

    /// <summary>
    /// Gets the balance of an Ethereum address at a specific block.
    /// </summary>
    /// <param name="address">The address to get the balance for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The balance as an EtherAmount in Wei.</returns>
    public async Task<EtherAmount> GetBalanceAsync(EthereumAddress address, string blockParameter = "latest")
    {
        return await this.chainClient.GetBalanceAsync(address, blockParameter);
    }

    /// <summary>
    /// Gets the code of a contract at a specific Ethereum address.
    /// </summary>
    /// <param name="address">The Ethereum address.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <returns>The code of the contract.</returns>
    public async Task<Hex> GetCodeAsync(EthereumAddress address, string blockParameter = "latest")
    {
        return await this.chainClient.GetCodeAsync(address, blockParameter);
    }

    //

    /// <summary>
    /// Gets a contract instance.
    /// </summary>
    /// <param name="address">The address of the contract.</param>
    /// <param name="endpoint">The endpoint to use to call the contract.</param>
    /// <param name="sender">The sender; if null, the ContractCaller will be read-only; attempts to send transactions will throw.</param>
    /// <param name="abiDocument">The ABI document for the contract.</param>
    /// <returns>A contract instance.</returns>
    public Contract GetContract(EthereumAddress address, Endpoint endpoint, Sender sender, Stream abiDocument)
    {
        var contractClient = ContractClient.CreateDefault(endpoint, sender);

        return new Contract(
            this.ChainId, this.chainClient, contractClient, abiDocument, address, endpoint.LoggerFactory);
    }

    //

    /// <summary>
    /// Creates a default chain instance from an endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>A default chain instance.</returns>
    public static Chain CreateDefault(Endpoint endpoint)
    {
        var chainId = ulong.Parse(ChainNames.GetChainId(endpoint.NetworkName));

        return CreateDefault(chainId, new Uri(endpoint.URL), endpoint.LoggerFactory);
    }

    /// <summary>
    /// Creates a default chain instance.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="uri">The URI of the chain.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>A default chain instance.</returns>
    public static Chain CreateDefault(ulong chainId, Uri uri, ILoggerFactory loggerFactory)
    {
        var chainClient = ChainClient.CreateDefault(uri, loggerFactory);

        return new Chain(chainId, chainClient);
    }

    /// <summary>
    /// Waits for a transaction to be mined and returns its receipt.
    /// </summary>
    /// <param name="transactionHash">The transaction hash to wait for.</param>
    /// <param name="timeout">The maximum time to wait. Defaults to 5 minutes if not specified.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transaction receipt.</returns>
    /// <exception cref="ReceiptNotFoundException">Thrown when the transaction is not found within the timeout period.</exception>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    public async Task<(TransactionReceipt? Receipt, bool DeadlineReached)> TryWaitForTransactionAsync(
        Hex transactionHash,
        TimeSpan? timeout = null,
        CancellationToken cancellationToken = default)
    {
        timeout ??= TimeSpan.FromMinutes(5);
        var deadline = DateTime.UtcNow + timeout.Value;

        var (receipt, deadlineReached) = await this.chainClient.TryWaitForTransactionReceiptAsync(
            transactionHash,
            this.pollingInterval,
            deadline,
            cancellationToken);

        if (receipt == null)
        {
            throw new ReceiptNotFoundException(transactionHash);
        }

        return (receipt, deadlineReached);
    }
}
