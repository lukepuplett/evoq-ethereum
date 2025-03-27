using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Examples;

[TestClass]
public class ExampleChain
{
    private Chain _chain = null!;
    private ILoggerFactory _loggerFactory = null!;

    [TestInitialize]
    public void Setup()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;

        SetupBasics(out baseUrl, out configuration, out chainName, out chainId, out _loggerFactory);

        // Create chain instance
        _chain = Chain.CreateDefault(chainId, new Uri(baseUrl), _loggerFactory);
    }

    [TestMethod]
    public async Task Should_GetBasicChainInfo()
    {
        // Get chain ID
        var chainIdBigInt = await _chain.GetChainIdAsync();
        Assert.AreEqual(ulong.Parse(ChainNames.GetChainId(ChainNames.Hardhat)), chainIdBigInt, "Chain ID should match the configured chain ID");
        Console.WriteLine($"Chain ID: {chainIdBigInt}");

        // Get current block number
        var blockNumber = await _chain.GetBlockNumberAsync();
        Assert.IsTrue(blockNumber > 0, "Block number should be positive");
        Console.WriteLine($"Current block number: {blockNumber}");
    }

    [TestMethod]
    public async Task Should_GetGasAndFeeInfo()
    {
        // Get gas price
        var gasPrice = await _chain.GasPriceAsync();
        Assert.IsTrue(gasPrice > 0, "Gas price should be positive");
        Console.WriteLine($"Current gas price: {gasPrice} Wei");

        // Get base fee (EIP-1559)
        bool supportsEip1559 = false;
        try
        {
            var baseFee = await _chain.GetBaseFeeAsync();
            // Only consider it EIP-1559 if we get a non-zero base fee
            if (baseFee > 0)
            {
                Assert.IsTrue(baseFee >= 0, "Base fee should be non-negative");
                Console.WriteLine($"Current base fee: {baseFee} Wei");
                supportsEip1559 = true;
            }
            else
            {
                Console.WriteLine("Network returned zero base fee, treating as legacy chain");
                supportsEip1559 = false;
            }
        }
        catch (LegacyChainException ex)
        {
            Console.WriteLine($"Network does not support EIP-1559: {ex.Message}");
            supportsEip1559 = false;
        }

        // Get suggested EIP-1559 fees
        try
        {
            var (maxFee, priorityFee) = await _chain.SuggestEip1559FeesAsync();
            Assert.IsTrue(maxFee > 0, "Max fee should be positive");
            Assert.IsTrue(priorityFee > 0, "Priority fee should be positive");
            Assert.IsTrue(maxFee >= priorityFee, "Max fee should be greater than or equal to priority fee");
            Console.WriteLine($"Suggested max fee: {maxFee} Wei");
            Console.WriteLine($"Suggested priority fee: {priorityFee} Wei");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Network does not support EIP-1559: {ex.Message}");
            supportsEip1559 = false;
        }

        // Get fee history
        var feeHistory = await _chain.GetFeeHistoryAsync(
            blockCount: 10,
            newestBlock: BlockParameter.Latest,
            rewardPercentiles: new[] { 50.0 });
        Assert.IsNotNull(feeHistory, "Fee history should not be null");

        // Log the fee history details
        Console.WriteLine($"Fee history details:");
        Console.WriteLine($"- Oldest block: {feeHistory.OldestBlock}");
        Console.WriteLine($"- Base fee per gas count: {feeHistory.BaseFeePerGas.Length}");
        Console.WriteLine($"- Gas used ratio count: {feeHistory.GasUsedRatio.Length}");
        Console.WriteLine($"- Reward count: {feeHistory.Reward?.Length ?? 0}");

        // Only validate fee history if we have full EIP-1559 support
        if (supportsEip1559)
        {
            Assert.IsTrue(feeHistory.BaseFeePerGas.Length > 0, "Fee history should contain base fees for EIP-1559 networks");
            Assert.IsTrue(feeHistory.GasUsedRatio.Length > 0, "Fee history should contain gas used ratios for EIP-1559 networks");
            Assert.IsTrue(feeHistory.Reward?.Length > 0, "Fee history should contain rewards for EIP-1559 networks");
        }
        else
        {
            Console.WriteLine("Network does not fully support EIP-1559, skipping fee history validation");
        }
    }

    [TestMethod]
    public async Task Should_GetAccountInfo()
    {
        var senderAddress = new EthereumAddress("0x1111111111111111111111111111111111111111");

        // Get transaction count (nonce)
        var nonce = await _chain.GetTransactionCountAsync(senderAddress);
        Assert.IsTrue(nonce >= 0, "Nonce should be non-negative");
        Console.WriteLine($"Transaction count for {senderAddress}: {nonce}");

        // Get balance
        var balance = await _chain.GetBalanceAsync(senderAddress);
        Assert.IsNotNull(balance, "Balance should not be null");
        Assert.IsTrue(balance.WeiValue >= 0, "Balance should be non-negative");
        Console.WriteLine($"Balance for {senderAddress}: {balance}");
    }

    [TestMethod]
    [Ignore("Requires a real transaction hash")]
    public async Task Should_WaitForTransaction()
    {
        var txHash = Hex.Parse("0x..."); // Replace with actual transaction hash
        var (receipt, deadlineReached) = await _chain.TryWaitForTransactionAsync(
            txHash,
            timeout: TimeSpan.FromMinutes(5));

        if (deadlineReached)
        {
            Console.WriteLine("Transaction not found within timeout period");
        }
        else
        {
            Assert.IsNotNull(receipt, "Transaction receipt should not be null");
            Assert.IsTrue(receipt.Success, "Transaction should be successful");
            Console.WriteLine($"Transaction receipt: {receipt}");
        }
    }

    private static void SetupBasics(out string baseUrl, out IConfigurationRoot configuration, out string chainName, out ulong chainId, out ILoggerFactory loggerFactory)
    {
        baseUrl = "http://localhost:8545";
        configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "GoogleCloud:ProjectName", "evoq-capricorn-timesheets" },
                    { "Blockchain:Ethereum:JsonRPC:GoogleSepolia:ProjectId", "evoq-capricorn-timesheets" }
                })
            .Build();
        chainName = ChainNames.Hardhat;
        chainId = ulong.Parse(ChainNames.GetChainId(chainName));
        loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(
                options => options.SingleLine = true).SetMinimumLevel(LogLevel.Debug));
    }
}