using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Provides chain-specific polling strategies based on expected block times.
/// </summary>
public class ChainPollingStrategy
{
    /// <summary>
    /// Gets the initial polling interval and maximum polling interval for a specific chain.
    /// </summary>
    public readonly record struct PollingIntervals(TimeSpan Initial, TimeSpan Maximum);

    /// <summary>
    /// Known chain configurations mapped by chain ID.
    /// </summary>
    private static readonly Dictionary<string, PollingIntervals> ChainConfigs = new()
    {
        // Mainnet chains
        [ChainIds.EthereumMainnet] = new(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(12)),
        [ChainIds.OptimismMainnet] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.PolygonMainnet] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.ArbitrumMainnet] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.ArbitrumNova] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.BaseMainnet] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.ScrollMainnet] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.ZkSyncMainnet] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.LineaMainnet] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),

        // Testnets (generally faster polling as they're less loaded)
        [ChainIds.EthereumSepolia] = new(TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(8)),
        [ChainIds.OptimismSepolia] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.OptimismGoerli] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.BaseSepolia] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.BaseGoerli] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.ArbitrumGoerli] = new(TimeSpan.FromMilliseconds(250), TimeSpan.FromSeconds(1)),
        [ChainIds.ScrollSepolia] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.LineaGoerli] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),

        // Development
        [ChainIds.Hardhat] = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(200)),
    };

    /// <summary>
    /// Default intervals for unknown chains.
    /// </summary>
    private static readonly PollingIntervals DefaultIntervals = new(
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(8));

    /// <summary>
    /// Gets the recommended polling intervals for a specific chain.
    /// </summary>
    public static PollingIntervals GetForChain(ulong chainId) =>
        ChainConfigs.GetValueOrDefault(chainId.ToString(), DefaultIntervals);
}