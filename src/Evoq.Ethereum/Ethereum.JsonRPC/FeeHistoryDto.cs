using System;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents fee history data returned by eth_feeHistory RPC method.
/// Used to estimate gas fees by analyzing historical fee data.
/// Only available after the London fork (EIP-1559).
/// </summary>
public class FeeHistoryDto
{
    /// <summary>
    /// The oldest block number in the returned range.
    /// Hex-encoded block number.
    /// </summary>
    public string OldestBlock { get; set; } = string.Empty;

    /// <summary>
    /// Base fee per gas for each block in the returned range.
    /// Array of hex-encoded values in wei.
    /// The last value is the next block's predicted base fee per gas.
    /// </summary>
    public string[] BaseFeePerGas { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gas used ratio for each block in the returned range.
    /// Values are between 0 and 1, representing the percentage of gas used vs gas limit.
    /// Used to estimate network congestion.
    /// </summary>
    public double[] GasUsedRatio { get; set; } = Array.Empty<double>();

    /// <summary>
    /// Priority fee at the requested percentiles for each block in the returned range.
    /// Two-dimensional array where:
    /// - First dimension represents blocks
    /// - Second dimension represents priority fees at each requested percentile
    /// Values are hex-encoded in wei.
    /// </summary>
    /// <remarks>
    /// For example, if percentiles [25, 50, 75] were requested:
    /// - Reward[0][0] = 25th percentile priority fee for first block
    /// - Reward[0][1] = 50th percentile priority fee for first block
    /// - Reward[0][2] = 75th percentile priority fee for first block
    /// And so on for each block in the range.
    /// </remarks>
    public string[][] Reward { get; set; } = Array.Empty<string[]>();
}
