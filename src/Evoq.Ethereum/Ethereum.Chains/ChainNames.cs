using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Chain names as defined by EIP-7472.
/// </summary>
public static class ChainNames
{
    // Mainnets
    public const string EthereumMainnet = "Ethereum Mainnet";
    public const string OptimismMainnet = "Optimism Mainnet";
    public const string PolygonMainnet = "Polygon Mainnet";
    public const string ArbitrumMainnet = "Arbitrum Mainnet";
    public const string ArbitrumNova = "Arbitrum Nova";
    public const string BaseMainnet = "Base Mainnet";
    public const string ScrollMainnet = "Scroll Mainnet";
    public const string ZkSyncMainnet = "ZkSync Mainnet";
    public const string CeloMainnet = "Celo Mainnet";
    public const string BlastMainnet = "Blast Mainnet";
    public const string LineaMainnet = "Linea Mainnet";

    // Testnets
    public const string EthereumSepolia = "Ethereum Sepolia";
    public const string OptimismSepolia = "Optimism Sepolia";
    public const string OptimismGoerli = "Optimism Goerli";
    public const string BaseSepolia = "Base Sepolia";
    public const string BaseGoerli = "Base Goerli";
    public const string ArbitrumGoerli = "Arbitrum Goerli";
    public const string PolygonAmoy = "Polygon Amoy";
    public const string ScrollSepolia = "Scroll Sepolia";
    public const string LineaGoerli = "Linea Goerli";

    // Development
    public const string Hardhat = "Hardhat";

    //

    private static readonly Dictionary<string, string> _chainNames = new()
    {
        // Mainnets
        { EthereumMainnet, ChainIds.EthereumMainnet },
        { OptimismMainnet, ChainIds.OptimismMainnet },
        { PolygonMainnet, ChainIds.PolygonMainnet },
        { ArbitrumMainnet, ChainIds.ArbitrumMainnet },
        { ArbitrumNova, ChainIds.ArbitrumNova },
        { BaseMainnet, ChainIds.BaseMainnet },
        { ScrollMainnet, ChainIds.ScrollMainnet },
        { ZkSyncMainnet, ChainIds.ZkSyncMainnet },
        { CeloMainnet, ChainIds.CeloMainnet },
        { BlastMainnet, ChainIds.BlastMainnet },
        { LineaMainnet, ChainIds.LineaMainnet },

        // Testnets
        { EthereumSepolia, ChainIds.EthereumSepolia },
        { OptimismSepolia, ChainIds.OptimismSepolia },
        { OptimismGoerli, ChainIds.OptimismGoerli },
        { BaseSepolia, ChainIds.BaseSepolia },
        { BaseGoerli, ChainIds.BaseGoerli },
        { ArbitrumGoerli, ChainIds.ArbitrumGoerli },
        { PolygonAmoy, ChainIds.PolygonAmoy },
        { ScrollSepolia, ChainIds.ScrollSepolia },
        { LineaGoerli, ChainIds.LineaGoerli },

        // Development
        { Hardhat, ChainIds.Hardhat },
    };

    //

    /// <summary>
    /// Get the ID of a chain from its name.
    /// </summary>
    /// <param name="chainName">The name of the chain.</param>
    /// <returns>The ID of the chain.</returns>
    /// <exception cref="ArgumentException">Thrown if the chain name is not found.</exception>
    public static string GetChainId(string chainName)
    {
        if (!_chainNames.TryGetValue(chainName, out var id))
        {
            throw new ArgumentException($"Chain name {chainName} not found");
        }
        return id;
    }
}