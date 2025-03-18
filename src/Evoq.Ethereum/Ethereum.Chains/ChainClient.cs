using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Performs operations on a chain.
/// </summary>
public class ChainClient
{
    //

    private readonly Random rng = new Random();

    private readonly IEthereumJsonRpc jsonRpc;

    //

    /// <summary>
    /// Initializes a new instance of the ChainClient class.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="jsonRpc">The JSON-RPC client.</param>
    public ChainClient(ulong chainId, IEthereumJsonRpc jsonRpc)
    {
        this.ChainId = chainId;
        this.jsonRpc = jsonRpc;
    }

    //

    /// <summary>
    /// Gets the chain ID.
    /// </summary>
    /// <returns>The chain ID.</returns>
    public ulong ChainId { get; }

    //

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    /// <returns>The current gas price.</returns>
    public async Task<Hex> GasPriceAsync()
    {
        return await this.jsonRpc.GasPriceAsync(this.GetRandomId());
    }

    /// <summary>
    /// Gets the chain ID.
    /// </summary>
    /// <returns>The chain ID as a hexadecimal string.</returns>
    public async Task<Hex> ChainIdAsync()
    {
        return await this.jsonRpc.ChainIdAsync(this.GetRandomId());
    }

    /// <summary>
    /// Gets the current block number.
    /// </summary>
    /// <returns>The current block number as a hexadecimal string.</returns>
    public async Task<Hex> GetBlockNumberAsync()
    {
        return await this.jsonRpc.BlockNumberAsync(this.GetRandomId());
    }

    /// <summary>
    /// Gets a block by number with transaction hashes.
    /// </summary>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <returns>The block data with transaction hashes.</returns>
    public async Task<BlockData<string>?> TryGetBlockByNumberWithTxHashesAsync(string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxHashesAsync(blockNumberOrTag, this.GetRandomId());

        if (dto == null)
        {
            return null;
        }

        return BlockData<string>.FromDto(dto, hash => hash);
    }

    /// <summary>
    /// Gets a block by number with transaction objects.
    /// </summary>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <returns>The block data with transaction objects.</returns>
    public async Task<BlockData<TransactionData>?> TryGetBlockByNumberWithTxObjectsAsync(string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxObjectsAsync(blockNumberOrTag, this.GetRandomId());

        if (dto == null)
        {
            return null;
        }

        return BlockData<TransactionData>.FromDto(dto, t => TransactionData.FromDto(t)!);
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
        var dto = await this.jsonRpc.FeeHistoryAsync(
            blockCount.NumberToHexStruct(),
            newestBlock,
            rewardPercentiles,
            this.GetRandomId());

        return new FeeHistory();
    }

    /// <summary>
    /// Gets the transaction count (nonce) for an address.
    /// </summary>
    /// <param name="address">The address to get the transaction count for.</param>
    /// <param name="blockParameter">The block parameter (defaults to "latest").</param>
    /// <returns>The transaction count as a hexadecimal value.</returns>
    public async Task<Hex> GetTransactionCountAsync(EthereumAddress address, string blockParameter = "latest")
    {
        return await this.jsonRpc.GetTransactionCountAsync(address, blockParameter, this.GetRandomId());
    }

    //

    /// <summary>
    /// Creates a default chain client.
    /// </summary>
    /// <param name="chainId">The chain ID.</param>
    /// <param name="uri">The URI of the chain.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <returns>The chain client.</returns>
    public static ChainClient CreateDefault(ulong chainId, Uri uri, ILoggerFactory loggerFactory)
    {
        var jsonRpc = new JsonRpcClient(uri, loggerFactory);

        return new ChainClient(chainId, jsonRpc);
    }

    //

    private int GetRandomId()
    {
        return this.rng.Next();
    }
}