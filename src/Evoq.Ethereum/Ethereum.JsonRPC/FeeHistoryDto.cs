namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents fee history data returned by eth_feeHistory RPC method.
/// </summary>
public class FeeHistoryDto
{
    /// <summary>
    /// The oldest block number in the returned range.
    /// </summary>
    public string OldestBlock { get; set; }

    /// <summary>
    /// Base fee per gas for each block in the returned range.
    /// </summary>
    public string[] BaseFeePerGas { get; set; }

    /// <summary>
    /// Gas used ratio for each block in the returned range.
    /// </summary>
    public double[] GasUsedRatio { get; set; }

    /// <summary>
    /// Priority fee at the requested percentiles for each block in the returned range.
    /// </summary>
    public string[][] Reward { get; set; }
}
