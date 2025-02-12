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
}