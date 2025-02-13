using System;
using System.Collections.Generic;

namespace Evoq.Ethereum;

/// <summary>
/// Chain IDs as defined by EIP-7472.
/// </summary>
public static class ChainIds
{
    // Mainnets
    public const string EthereumMainnet = "1";
    public const string OptimismMainnet = "10";
    public const string PolygonMainnet = "137";
    public const string ArbitrumMainnet = "42161";
    public const string ArbitrumNova = "42170";
    public const string BaseMainnet = "8453";
    public const string ScrollMainnet = "534352";
    public const string ZkSyncMainnet = "324";
    public const string CeloMainnet = "42220";
    public const string BlastMainnet = "81457";
    public const string LineaMainnet = "59144";

    // Testnets
    public const string EthereumSepolia = "11155111";
    public const string OptimismSepolia = "11155420";
    public const string OptimismGoerli = "420";
    public const string BaseSepolia = "84532";
    public const string BaseGoerli = "84531";
    public const string ArbitrumGoerli = "421613";
    public const string PolygonAmoy = "80002";
    public const string ScrollSepolia = "534351";
    public const string LineaGoerli = "59140";

    // Development
    public const string Hardhat = "31337";

    // 

    private static readonly Dictionary<string, string> _chainIds = new()
    {
        // Mainnets
        { EthereumMainnet, ChainNames.EthereumMainnet },
        { OptimismMainnet, ChainNames.OptimismMainnet },
        { PolygonMainnet, ChainNames.PolygonMainnet },
        { ArbitrumMainnet, ChainNames.ArbitrumMainnet },
        { ArbitrumNova, ChainNames.ArbitrumNova },
        { BaseMainnet, ChainNames.BaseMainnet },
        { ScrollMainnet, ChainNames.ScrollMainnet },
        { ZkSyncMainnet, ChainNames.ZkSyncMainnet },
        { CeloMainnet, ChainNames.CeloMainnet },
        { BlastMainnet, ChainNames.BlastMainnet },
        { LineaMainnet, ChainNames.LineaMainnet },

        // Testnets
        { EthereumSepolia, ChainNames.EthereumSepolia },
        { OptimismSepolia, ChainNames.OptimismSepolia },
        { OptimismGoerli, ChainNames.OptimismGoerli },
        { BaseSepolia, ChainNames.BaseSepolia },
        { BaseGoerli, ChainNames.BaseGoerli },
        { ArbitrumGoerli, ChainNames.ArbitrumGoerli },
        { PolygonAmoy, ChainNames.PolygonAmoy },
        { ScrollSepolia, ChainNames.ScrollSepolia },
        { LineaGoerli, ChainNames.LineaGoerli },

        // Development
        { Hardhat, ChainNames.Hardhat },
    };

    //

    /// <summary>
    /// Get the name of a chain from its ID.
    /// </summary>
    /// <param name="chainId">The ID of the chain.</param>
    /// <returns>The name of the chain.</returns>
    /// <exception cref="ArgumentException">Thrown if the chain ID is not found.</exception>
    public static string GetChainName(string chainId)
    {
        if (!_chainIds.TryGetValue(chainId, out var name))
        {
            throw new ArgumentException($"Chain ID {chainId} not found");
        }
        return name;
    }
}
