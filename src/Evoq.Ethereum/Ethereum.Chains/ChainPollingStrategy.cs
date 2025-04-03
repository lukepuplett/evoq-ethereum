using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Provides chain-specific polling strategies based on expected block times.
/// </summary>
internal class ChainPollingStrategy
{
    /// <summary>
    /// Gets the initial polling interval and maximum polling interval for a specific chain.
    /// </summary>
    public readonly record struct PollingInterval(TimeSpan Initial, TimeSpan Maximum);

    /// <summary>
    /// Known chain configurations mapped by chain ID.
    /// </summary>
    private static readonly Dictionary<string, PollingInterval> ChainConfigs = new()
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
        [ChainIds.EthereumSepolia] = new(TimeSpan.FromSeconds(4), TimeSpan.FromSeconds(16)),
        [ChainIds.OptimismSepolia] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.OptimismGoerli] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.BaseSepolia] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.BaseGoerli] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.ArbitrumGoerli] = new(TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(2)),
        [ChainIds.ScrollSepolia] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),
        [ChainIds.LineaGoerli] = new(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(4)),

        // Development
        [ChainIds.Hardhat] = new(TimeSpan.FromMilliseconds(50), TimeSpan.FromMilliseconds(200)),
    };

    /// <summary>
    /// Default intervals for unknown chains.
    /// </summary>
    private static readonly PollingInterval DefaultIntervals = new(
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(8));

    /// <summary>
    /// Gets the recommended polling intervals for a specific chain.
    /// </summary>
    public static PollingInterval GetForChain(ulong chainId) =>
        ChainConfigs.GetValueOrDefault(chainId.ToString(), DefaultIntervals);
}