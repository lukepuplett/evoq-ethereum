using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Chain IDs as defined by EIP-7472.
/// </summary>
public static class ChainIds
{
    /// <summary>The Ethereum mainnet chain ID (1).</summary>
    public const string EthereumMainnet = "1";

    /// <summary>The Optimism mainnet chain ID (10).</summary>
    public const string OptimismMainnet = "10";

    /// <summary>The Polygon PoS mainnet chain ID (137).</summary>
    public const string PolygonMainnet = "137";

    /// <summary>The Arbitrum One mainnet chain ID (42161).</summary>
    public const string ArbitrumMainnet = "42161";

    /// <summary>The Arbitrum Nova mainnet chain ID (42170).</summary>
    public const string ArbitrumNova = "42170";

    /// <summary>The Base mainnet chain ID (8453).</summary>
    public const string BaseMainnet = "8453";

    /// <summary>The Scroll mainnet chain ID (534352).</summary>
    public const string ScrollMainnet = "534352";

    /// <summary>The zkSync Era mainnet chain ID (324).</summary>
    public const string ZkSyncMainnet = "324";

    /// <summary>The Celo mainnet chain ID (42220).</summary>
    public const string CeloMainnet = "42220";

    /// <summary>The Blast mainnet chain ID (81457).</summary>
    public const string BlastMainnet = "81457";

    /// <summary>The Linea mainnet chain ID (59144).</summary>
    public const string LineaMainnet = "59144";

    /// <summary>The Ethereum Sepolia testnet chain ID (11155111).</summary>
    public const string EthereumSepolia = "11155111";

    /// <summary>The Optimism Sepolia testnet chain ID (11155420).</summary>
    public const string OptimismSepolia = "11155420";

    /// <summary>The Optimism Goerli testnet chain ID (420).</summary>
    public const string OptimismGoerli = "420";

    /// <summary>The Base Sepolia testnet chain ID (84532).</summary>
    public const string BaseSepolia = "84532";

    /// <summary>The Base Goerli testnet chain ID (84531).</summary>
    public const string BaseGoerli = "84531";

    /// <summary>The Arbitrum Goerli testnet chain ID (421613).</summary>
    public const string ArbitrumGoerli = "421613";

    /// <summary>The Polygon Amoy testnet chain ID (80002).</summary>
    public const string PolygonAmoy = "80002";

    /// <summary>The Scroll Sepolia testnet chain ID (534351).</summary>
    public const string ScrollSepolia = "534351";

    /// <summary>The Linea Goerli testnet chain ID (59140).</summary>
    public const string LineaGoerli = "59140";

    /// <summary>The local Hardhat development network chain ID (31337).</summary>
    public const string Hardhat = "31337";

    /// <summary>
    /// Dictionary mapping chain IDs to their corresponding chain names.
    /// This is the reverse mapping of the one in ChainNames.
    /// </summary>
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

    /// <summary>
    /// Gets the chain name corresponding to a given chain ID.
    /// </summary>
    /// <param name="chainId">The numeric chain ID as a string.</param>
    /// <returns>The canonical name of the chain as defined in EIP-7472.</returns>
    /// <exception cref="ArgumentException">Thrown when the provided chain ID is not found in the dictionary.</exception>
    /// <remarks>
    /// Chain IDs are registered in the Ethereum Chain Registry.
    /// For mainnet networks, these are canonical chain IDs.
    /// For testnet networks, these are well-known testing network IDs.
    /// </remarks>
    public static string GetChainName(string chainId)
    {
        if (!_chainIds.TryGetValue(chainId, out var name))
        {
            throw new ArgumentException($"Chain ID {chainId} not found");
        }
        return name;
    }
}
