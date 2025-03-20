using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Chain names as defined by EIP-7472.
/// </summary>
public static class ChainNames
{
    /// <summary>The main Ethereum network.</summary>
    public const string EthereumMainnet = "Ethereum Mainnet";

    /// <summary>The Optimism Layer 2 mainnet network.</summary>
    public const string OptimismMainnet = "Optimism Mainnet";

    /// <summary>The Polygon PoS mainnet network.</summary>
    public const string PolygonMainnet = "Polygon Mainnet";

    /// <summary>The Arbitrum One Layer 2 mainnet network.</summary>
    public const string ArbitrumMainnet = "Arbitrum Mainnet";

    /// <summary>The Arbitrum Nova Layer 2 mainnet network.</summary>
    public const string ArbitrumNova = "Arbitrum Nova";

    /// <summary>The Base Layer 2 mainnet network.</summary>
    public const string BaseMainnet = "Base Mainnet";

    /// <summary>The Scroll Layer 2 mainnet network.</summary>
    public const string ScrollMainnet = "Scroll Mainnet";

    /// <summary>The zkSync Era Layer 2 mainnet network.</summary>
    public const string ZkSyncMainnet = "ZkSync Mainnet";

    /// <summary>The Celo mainnet network.</summary>
    public const string CeloMainnet = "Celo Mainnet";

    /// <summary>The Blast Layer 2 mainnet network.</summary>
    public const string BlastMainnet = "Blast Mainnet";

    /// <summary>The Linea Layer 2 mainnet network.</summary>
    public const string LineaMainnet = "Linea Mainnet";

    /// <summary>The Ethereum Sepolia testnet.</summary>
    public const string EthereumSepolia = "Ethereum Sepolia";

    /// <summary>The Optimism Sepolia testnet.</summary>
    public const string OptimismSepolia = "Optimism Sepolia";

    /// <summary>The Optimism Goerli testnet.</summary>
    public const string OptimismGoerli = "Optimism Goerli";

    /// <summary>The Base Sepolia testnet.</summary>
    public const string BaseSepolia = "Base Sepolia";

    /// <summary>The Base Goerli testnet.</summary>
    public const string BaseGoerli = "Base Goerli";

    /// <summary>The Arbitrum Goerli testnet.</summary>
    public const string ArbitrumGoerli = "Arbitrum Goerli";

    /// <summary>The Polygon Amoy testnet.</summary>
    public const string PolygonAmoy = "Polygon Amoy";

    /// <summary>The Scroll Sepolia testnet.</summary>
    public const string ScrollSepolia = "Scroll Sepolia";

    /// <summary>The Linea Goerli testnet.</summary>
    public const string LineaGoerli = "Linea Goerli";

    /// <summary>The local Hardhat development network.</summary>
    public const string Hardhat = "Hardhat";

    /// <summary>
    /// Dictionary mapping chain names to their corresponding chain IDs.
    /// </summary>
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

    /// <summary>
    /// Gets the chain ID corresponding to a given chain name.
    /// </summary>
    /// <param name="chainName">The name of the chain to look up.</param>
    /// <returns>The chain ID as a string.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided chain name is not found in the dictionary.</exception>
    /// <remarks>
    /// Chain names and IDs are defined according to EIP-7472.
    /// For mainnet networks, these are canonical chain IDs registered in the Ethereum Chain Registry.
    /// For testnet networks, these are well-known testing networks associated with their mainnet counterparts.
    /// </remarks>
    public static string GetChainId(string chainName)
    {
        if (!_chainNames.TryGetValue(chainName, out var id))
        {
            throw new ArgumentException($"Chain name {chainName} not found");
        }
        return id;
    }
}