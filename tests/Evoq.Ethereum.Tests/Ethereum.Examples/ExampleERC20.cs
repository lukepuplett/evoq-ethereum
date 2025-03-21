using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Examples;

// [TestClass] // Uncomment to trial the example once you have a local hardhat node and deployed the ERC20 contract
public class ExampleERC20
{
    // [TestMethod] // Uncomment to trial the example once you have a local hardhat node and deployed the ERC20 contract
    public async Task ExampleERC20_Transfer()
    {
        // Setup basics (chain, endpoint, logging)
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;
        SetupBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        // Setup sender account
        EthereumAddress senderAddress;
        SenderAccount senderAccount;
        SetupAccount(configuration, out senderAddress, out senderAccount);

        // Create chain and endpoint
        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);
        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        // Setup sender with nonce management
        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain);

        // Create a transaction runner
        var runner = new TransactionRunnerNative(sender, loggerFactory);

        // Example: Transfer 1 DAI token
        var tokenAddress = new EthereumAddress("0x6b175474e89094c44da98b954eedeac495271d0f"); // DAI token
        var recipientAddress = new EthereumAddress("0x1111111111111111111111111111111111111111");
        var amount = BigInteger.Parse("1000000000000000000"); // 1 DAI (18 decimals)

        // Create contract instance
        var abiStream = AbiFileHelper.GetAbiStream("ERC20.abi.json");
        var tokenContract = chain.GetContract(tokenAddress, endpoint, sender, abiStream);

        // Estimate gas for the transfer
        var estimate = await tokenContract.EstimateTransactionFeeAsync(
            "transfer",
            senderAddress,
            null,
            AbiKeyValues.Create(
                ("to", recipientAddress),
                ("amount", amount)));

        // Create transaction options
        var options = new ContractInvocationOptions(estimate.ToSuggestedGasOptions(), EtherAmount.Zero);

        // Execute the transfer
        var receipt = await runner.RunTransactionAsync(
            tokenContract,
            "transfer",
            options,
            AbiKeyValues.Create(
                ("to", recipientAddress),
                ("amount", amount)),
            CancellationToken.None);

        // Verify the transaction
        Assert.IsNotNull(receipt);
        Assert.IsTrue(receipt.Success);

        // Read the Transfer event
        bool hasTransfer = tokenContract.TryReadEventLogsFromReceipt(
            receipt, "Transfer", out var indexed, out var data);

        Assert.IsTrue(hasTransfer);
        Assert.IsNotNull(indexed);
        Assert.IsNotNull(data);

        // Verify event data
        var fromAddress = (EthereumAddress)indexed["from"]!;
        var toAddress = (EthereumAddress)indexed["to"]!;
        var value = (BigInteger)data["value"]!;

        Assert.AreEqual(senderAddress, fromAddress);
        Assert.AreEqual(recipientAddress, toAddress);
        Assert.AreEqual(amount, value);

        // Read the new balance
        var balance = await tokenContract.CallAsync<BigInteger>(
            "balanceOf",
            senderAddress,
            AbiKeyValues.Create(("account", senderAddress)));

        Console.WriteLine($"New balance: {balance}");
    }

    // [TestMethod] // Uncomment to trial the example once you have a local hardhat node and deployed the ERC20 contract
    public async Task ExampleERC20_ApproveAndTransferFrom()
    {
        // Setup basics (chain, endpoint, logging)
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;
        SetupBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        // Setup accounts
        EthereumAddress senderAddress;
        SenderAccount senderAccount;
        SetupAccount(configuration, out senderAddress, out senderAccount);

        var spenderAddress = new EthereumAddress("0x2222222222222222222222222222222222222222");
        var spenderAccount = new SenderAccount(
            Hex.Parse("0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb"),
            spenderAddress);

        // Create chain and endpoint
        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);
        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        // Setup senders with nonce management
        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain);
        Sender spender = SetupSender(loggerFactory, spenderAddress, spenderAccount, chain);

        // Create transaction runners
        var senderRunner = new TransactionRunnerNative(sender, loggerFactory);
        var spenderRunner = new TransactionRunnerNative(spender, loggerFactory);

        // Example: Approve and transferFrom 1 DAI token
        var tokenAddress = new EthereumAddress("0x6b175474e89094c44da98b954eedeac495271d0f"); // DAI token
        var amount = BigInteger.Parse("1000000000000000000"); // 1 DAI (18 decimals)

        // Create contract instance
        var abiStream = AbiFileHelper.GetAbiStream("ERC20.abi.json");
        var tokenContract = chain.GetContract(tokenAddress, endpoint, sender, abiStream);

        // Step 1: Approve spender
        var approveEstimate = await tokenContract.EstimateTransactionFeeAsync(
            "approve",
            senderAddress,
            null,
            AbiKeyValues.Create(
                ("spender", spenderAddress),
                ("amount", amount)));

        var approveOptions = new ContractInvocationOptions(approveEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);

        var approveReceipt = await senderRunner.RunTransactionAsync(
            tokenContract,
            "approve",
            approveOptions,
            AbiKeyValues.Create(
                ("spender", spenderAddress),
                ("amount", amount)),
            CancellationToken.None);

        Assert.IsNotNull(approveReceipt);
        Assert.IsTrue(approveReceipt.Success);

        // Verify Approval event
        bool hasApproval = tokenContract.TryReadEventLogsFromReceipt(
            approveReceipt, "Approval", out var approvalIndexed, out var approvalData);

        Assert.IsTrue(hasApproval);
        Assert.IsNotNull(approvalIndexed);
        Assert.IsNotNull(approvalData);

        var ownerAddress = (EthereumAddress)approvalIndexed["owner"]!;
        var approvedSpender = (EthereumAddress)approvalIndexed["spender"]!;
        var approvedAmount = (BigInteger)approvalData["value"]!;

        Assert.AreEqual(senderAddress, ownerAddress);
        Assert.AreEqual(spenderAddress, approvedSpender);
        Assert.AreEqual(amount, approvedAmount);

        // Step 2: TransferFrom
        var transferFromEstimate = await tokenContract.EstimateTransactionFeeAsync(
            "transferFrom",
            spenderAddress,
            null,
            AbiKeyValues.Create(
                ("from", senderAddress),
                ("to", spenderAddress),
                ("amount", amount)));

        var transferFromOptions = new ContractInvocationOptions(transferFromEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);

        var transferFromReceipt = await spenderRunner.RunTransactionAsync(
            tokenContract,
            "transferFrom",
            transferFromOptions,
            AbiKeyValues.Create(
                ("from", senderAddress),
                ("to", spenderAddress),
                ("amount", amount)),
            CancellationToken.None);

        Assert.IsNotNull(transferFromReceipt);
        Assert.IsTrue(transferFromReceipt.Success);

        // Verify Transfer event
        bool hasTransfer = tokenContract.TryReadEventLogsFromReceipt(
            transferFromReceipt, "Transfer", out var transferIndexed, out var transferData);

        Assert.IsTrue(hasTransfer);
        Assert.IsNotNull(transferIndexed);
        Assert.IsNotNull(transferData);

        var fromAddress = (EthereumAddress)transferIndexed["from"]!;
        var toAddress = (EthereumAddress)transferIndexed["to"]!;
        var value = (BigInteger)transferData["value"]!;

        Assert.AreEqual(senderAddress, fromAddress);
        Assert.AreEqual(spenderAddress, toAddress);
        Assert.AreEqual(amount, value);

        // Read the new balances
        var senderBalance = await tokenContract.CallAsync<BigInteger>(
            "balanceOf",
            senderAddress,
            AbiKeyValues.Create(("account", senderAddress)));

        var spenderBalance = await tokenContract.CallAsync<BigInteger>(
            "balanceOf",
            spenderAddress,
            AbiKeyValues.Create(("account", spenderAddress)));

        Console.WriteLine($"Sender balance: {senderBalance}");
        Console.WriteLine($"Spender balance: {spenderBalance}");
    }

    private static Sender SetupSender(ILoggerFactory loggerFactory, EthereumAddress senderAddress, SenderAccount senderAccount, Chain chain)
    {
        var getStartingNonce = async () => await chain.GetTransactionCountAsync(senderAddress);

        var noncePath = Path.Combine(Path.GetTempPath(), Path.Combine("hardhat-nonces", senderAddress.ToString()));
        var nonceStore = new FileSystemNonceStore(noncePath, loggerFactory, getStartingNonce);
        var sender = new Sender(senderAccount, nonceStore);
        return sender;
    }

    private static void SetupAccount(IConfigurationRoot configuration, out EthereumAddress senderAddress, out SenderAccount senderAccount)
    {
        var privateKeyStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1PrivateKey")!;
        var privateKeyHex = Hex.Parse(privateKeyStr);

        var senderAddressStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1Address")!;
        senderAddress = new EthereumAddress(senderAddressStr);
        senderAccount = new SenderAccount(privateKeyHex, senderAddress);
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