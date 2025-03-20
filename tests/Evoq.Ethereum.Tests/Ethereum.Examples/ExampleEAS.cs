using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.Conversion;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Examples;

[TestClass]
public class ExampleEAS
{
    [TestMethod]
    [Ignore]
    public async Task ExampleEAS_Send()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        SetupBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        //

        EthereumAddress senderAddress;
        SenderAccount senderAccount;

        SetupAccount(configuration, out senderAddress, out senderAccount);

        //

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);

        var getStartingNonce = async () => await chain.GetTransactionCountAsync(senderAddress);

        var noncePath = Path.Combine(Path.GetTempPath(), Path.Combine("hardhat-nonces", senderAddress.ToString()));
        var nonceStore = new FileSystemNonceStore(noncePath, loggerFactory, getStartingNonce);
        var sender = new Sender(senderAccount, nonceStore);

        var runner = new TransactionRunnerNative(sender, loggerFactory);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);
        var abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");
        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");

        var contract = chain.GetContract(schemaRegistryAddress, endpoint, sender, abiStream);

        //

        var registerEstimate = await contract.EstimateTransactionFeeAsync(
            "register",
            senderAddress,
            null,
            AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true));

        registerEstimate = registerEstimate.InEther();

        //

        var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);
        var registerArgs = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);

        var registerResult = await runner.RunTransactionAsync(
            contract,
            "register",
            registerOptions,
            registerArgs,
            CancellationToken.None);

        Console.WriteLine(registerResult);

        Assert.IsNotNull(registerResult);
        Assert.IsTrue(registerResult.Success);
    }

    [TestMethod]
    [Ignore]
    public async Task ExampleEAS_SendManually()
    {
        string baseUrl, chainName;
        IConfigurationRoot configuration;
        ulong chainId;
        ILoggerFactory loggerFactory;

        SetupBasics(out baseUrl, out configuration, out chainName, out chainId, out loggerFactory);

        EthereumAddress senderAddress;
        SenderAccount senderAccount;

        SetupAccount(configuration, out senderAddress, out senderAccount);

        var chain = Chain.CreateDefault(chainId, new Uri(baseUrl), loggerFactory!);

        var getStartingNonce = async () => await chain.GetTransactionCountAsync(senderAddress);

        var noncePath = Path.Combine(Path.GetTempPath(), Path.Combine("hardhat-nonces", senderAddress.ToString()));
        var nonceStore = new FileSystemNonceStore(noncePath, loggerFactory, getStartingNonce);

        // Read the ABI file using our helper method
        Stream abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");

        var sender = new Sender(senderAccount, nonceStore);
        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);
        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");

        var contract = chain.GetContract(schemaRegistryAddress, endpoint, sender, abiStream);

        // guess gas price

        var registerEstimate = await contract.EstimateTransactionFeeAsync(
            "register",
            senderAddress,
            null,
            AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true));

        registerEstimate = registerEstimate.InEther();

        // throw new Exception();

        BigInteger etherPriceInCents = 193045; // $1,930.45 as of 15 March 2025

        Assert.IsTrue(100_000 > registerEstimate.EstimatedGasLimit);
        Assert.AreEqual(EtherAmount.FromWei(2_000_000_000), registerEstimate.SuggestedMaxFeePerGas);
        Assert.IsTrue(registerEstimate.SuggestedMaxPriorityFeePerGas >= EtherAmount.FromWei(2_000_000_000), $"MaxPriorityFeePerGas is {registerEstimate.SuggestedMaxPriorityFeePerGas}");
        Assert.AreEqual(EtherAmount.FromWei(0), registerEstimate.CurrentBaseFeePerGas, $"BaseFeePerGas is {registerEstimate.CurrentBaseFeePerGas}");
        Assert.IsTrue(registerEstimate.EstimatedTotalFee.ToLocalCurrency(etherPriceInCents) < 90, $"EstimatedFee is {registerEstimate.EstimatedTotalFee.ToLocalCurrency(etherPriceInCents)}c");

        // call register

        var n = 1u;// await nonceStore.BeforeSubmissionAsync();

        var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);
        var registerArgs = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);

        try
        {
            Hex registerResult = await contract.InvokeMethodAsync("register", n, registerOptions, registerArgs);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.GetType().Name + ": " + ex.Message);
        }

        // get schema

        var abiPacker = new AbiEncoderPacked();
        var easSchemaUidSchema = AbiParameters.Parse("string schema, address resolver, bool revocable");
        var easSchemaUidSchemaValues = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);
        var easSchemaUid = KeccakHash.ComputeHash(abiPacker.EncodeParameters(easSchemaUidSchema, easSchemaUidSchemaValues).ToByteArray());

        var getSchemaResult = await contract.CallAsync("getSchema", senderAddress, AbiKeyValues.Create("uid", easSchemaUid));

        Assert.IsNotNull(getSchemaResult, "The call to getSchema returned a null result");

        if (!getSchemaResult.Values.TryFirst(out var first))
        {
            throw new Exception("The call to getSchema returned an empty dictionary");
        }

        if (first is not IDictionary<string, object?> firstDict)
        {
            throw new Exception("The call to getSchema returned an unexpected result");
        }

        var converter = new AbiConverter();
        var obj = converter.DictionaryToObject<SchemaRecordDto>(firstDict);

        Assert.IsNotNull(obj, "The call to getSchema returned an unexpected result");

        getSchemaResult.DeepVisitEach(pair => Console.WriteLine($"Result: {pair.Key}: {pair.Value}"));
    }

    //

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
        // baseUrl = "https://mainnet.infura.io/v3/";
        baseUrl = "http://localhost:8545";
        configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "GoogleCloud:ProjectName", "evoq-capricorn-timesheets" },                                 // for GCP
                    { "Blockchain:Ethereum:JsonRPC:GoogleSepolia:ProjectId", "evoq-capricorn-timesheets" }      // for GCP
                })
            .Build();
        chainName = ChainNames.Hardhat;
        chainId = ulong.Parse(ChainNames.GetChainId(chainName));
        loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(
                options => options.SingleLine = true).SetMinimumLevel(LogLevel.Debug));
    }
}

public record SchemaRecordDto(
    Hex Uid,
    EthereumAddress Resolver,
    bool Revocable,
    Hex Schema);
