using System;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;

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
    /// <param name="jsonRpc">The JSON-RPC client.</param>
    public ChainClient(IEthereumJsonRpc jsonRpc)
    {
        this.jsonRpc = jsonRpc;
    }

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

    // implement the two flavours of getBlockByNumberAsync

    public async Task<BlockData<string>> GetBlockByNumberWithTxHashesAsync(string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxHashesAsync(this.GetRandomId(), blockNumberOrTag);

        return BlockData<string>.FromDto(dto, hash => hash);
    }

    public async Task<BlockData<TransactionData>> GetBlockByNumberWithTxObjectsAsync(string blockNumberOrTag)
    {
        var dto = await this.jsonRpc.GetBlockByNumberWithTxObjectsAsync(this.GetRandomId(), blockNumberOrTag);

        return BlockData<TransactionData>.FromDto(dto, TransactionData.FromDto);
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
            this.GetRandomId(),
            blockCount.NumberToHexStruct(),
            newestBlock,
            rewardPercentiles);

        return new FeeHistory();
    }

    /// <summary>
    /// Estimates gas for a transaction with the given parameters.
    /// </summary>
    /// <param name="from">The sender address.</param>
    /// <param name="to">The recipient address.</param>
    /// <param name="value">The amount of ETH to send (in wei).</param>
    /// <param name="data">The transaction data (for contract interactions).</param>
    /// <returns>The estimated gas as a hexadecimal string.</returns>
    public async Task<Hex> EstimateGasAsync(string from, string to, string? value = null, string? data = null)
    {
        var transactionParams = new TransactionParamsDto
        {
            From = from,
            To = to,
            Value = value,
            Data = data
        };

        return await this.jsonRpc.EstimateGasAsync(transactionParams, this.GetRandomId());
    }

    //

    private int GetRandomId()
    {
        return this.rng.Next();
    }
}