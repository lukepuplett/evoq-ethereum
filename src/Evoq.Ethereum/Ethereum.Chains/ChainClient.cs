using System;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Performs stateless blockchain operations.
/// </summary>
internal class ChainClient
{
    private readonly Random rng = new Random();
    private readonly IEthereumJsonRpc jsonRpc;

    //

    /// <summary>
    /// Initializes a new instance of the ChainClient class.
    /// </summary>
    /// <param name="jsonRpc">The JSON-RPC client.</param>
    internal ChainClient(IEthereumJsonRpc jsonRpc)
    {
        this.jsonRpc = jsonRpc;
    }

    //

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    /// <returns>The current gas price.</returns>
    internal async Task<Hex> GasPriceAsync(IJsonRpcContext context)
    {
        return await this.jsonRpc.GasPriceAsync(context);
    }

    /// <summary>
    /// Gets the chain ID.
    /// </summary>
    /// <returns>The chain ID as a hexadecimal string.</returns>
    internal async Task<Hex> ChainIdAsync(IJsonRpcContext context)
    {
        return await this.jsonRpc.ChainIdAsync(context);
    }

    /// <summary>
    /// Gets the current block number.
    /// </summary>
    /// <returns>The current block number as a hexadecimal string.</returns>
    internal async Task<Hex> GetBlockNumberAsync(IJsonRpcContext context)
    {
        return await this.jsonRpc.BlockNumberAsync(context);
    }

    /// <summary>
    /// Gets a block by number with transaction hashes.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <returns>The block data with transaction hashes.</returns>
    internal async Task<BlockData<string>?> TryGetBlockByNumberWithTxHashesAsync(IJsonRpcContext context, string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxHashesAsync(context, blockNumberOrTag);

        if (dto == null)
        {
            return null;
        }

        return BlockData<string>.FromDto(dto, hash => hash);
    }

    /// <summary>
    /// Gets a block by number with transaction objects.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <returns>The block data with transaction objects.</returns>
    internal async Task<BlockData<TransactionData>?> TryGetBlockByNumberWithTxObjectsAsync(IJsonRpcContext context, string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxObjectsAsync(context, blockNumberOrTag);

        if (dto == null)
        {
            return null;
        }

        return BlockData<TransactionData>.FromDto(dto, t => TransactionData.FromDto(t)!);
    }

    /// <summary>
    /// Gets fee history data for recent blocks.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="blockCount">Number of blocks to analyze.</param>
    /// <param name="newestBlock">The newest block to consider ("latest" or block number).</param>
    /// <param name="rewardPercentiles">Percentiles to sample for priority fees.</param>
    /// <returns>Fee history data including base fees and priority fee percentiles.</returns>
    internal async Task<FeeHistory> GetFeeHistoryAsync(IJsonRpcContext context, ulong blockCount, string newestBlock, double[] rewardPercentiles)
    {
        var dto = await this.jsonRpc.FeeHistoryAsync(
            context,
            blockCount.NumberToHexStruct(),
            newestBlock,
            rewardPercentiles);

        return new FeeHistory();
    }

    /// <summary>
    /// Gets the transaction count (nonce) for an address.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="address">The address to get the transaction count for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The transaction count as a hexadecimal value.</returns>
    internal async Task<Hex> GetTransactionCountAsync(IJsonRpcContext context, EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetTransactionCountAsync(context, address, blockParameter);
    }

    /// <summary>
    /// Gets the transaction receipt for a given transaction hash.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="transactionHash">The hash of the transaction.</param>
    /// <returns>The transaction receipt, or null if the transaction is not found or pending.</returns>
    internal async Task<TransactionReceipt?> TryGetTransactionReceiptAsync(IJsonRpcContext context, Hex transactionHash)
    {
        var dto = await this.jsonRpc.GetTransactionReceiptAsync(context, transactionHash);

        return TransactionReceipt.FromDto(dto);
    }

    /// <summary>
    /// Waits for a transaction receipt until a deadline is reached.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="transactionHash">The transaction hash to wait for.</param>
    /// <param name="pollingInterval">The polling interval.</param>
    /// <param name="deadline">The absolute deadline after which polling will stop.</param>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>The transaction receipt, or null if not found before the deadline.</returns>
    /// <exception cref="OperationCanceledException">Thrown when the operation is canceled.</exception>
    internal async Task<(TransactionReceipt? Receipt, bool DeadlineReached)> TryWaitForTransactionReceiptAsync(
        IJsonRpcContext context,
        Hex transactionHash,
        ChainPollingStrategy.PollingInterval pollingInterval,
        DateTime deadline,
        CancellationToken cancellationToken = default)
    {
        var currentInterval = pollingInterval.Initial;

        while (DateTime.UtcNow < deadline)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var receipt = await TryGetTransactionReceiptAsync(context, transactionHash);
            if (receipt != null)
            {
                return (receipt, false);
            }

            var delayTime = DateTime.UtcNow + currentInterval < deadline
                ? currentInterval
                : deadline - DateTime.UtcNow;

            if (delayTime > TimeSpan.Zero)
            {
                await Task.Delay(delayTime, cancellationToken);
            }

            // Implement exponential backoff with chain-specific maximum
            currentInterval = TimeSpan.FromTicks(Math.Min(
                currentInterval.Ticks * 2,
                pollingInterval.Maximum.Ticks));
        }

        return (null, true);
    }

    /// <summary>
    /// Gets the balance of an Ethereum address at a specific block.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="address">The address to get the balance for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The balance as an EtherAmount in Wei.</returns>
    internal async Task<EtherAmount> GetBalanceAsync(IJsonRpcContext context, EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetBalanceAsync(context, address, blockParameter);
    }

    /// <summary>
    /// Gets the code of a contract at a specific Ethereum address.
    /// </summary>
    /// <param name="context">The JSON-RPC context.</param>
    /// <param name="address">The Ethereum address.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <returns>The code of the contract.</returns>
    internal async Task<Hex> GetCodeAsync(IJsonRpcContext context, EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetCodeAsync(context, address, blockParameter);
    }

    //

    /// <summary>
    /// Creates a default chain client.
    /// </summary>
    /// <param name="uri">The URI of the chain.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The chain client.</returns>
    internal static ChainClient CreateDefault(Uri uri, ILoggerFactory loggerFactory)
    {
        var jsonRpc = new JsonRpcClient(uri, loggerFactory);

        return new ChainClient(jsonRpc);
    }
}