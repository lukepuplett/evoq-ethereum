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
    // [Ignore]
    public async Task ExampleEAS_GetSchema()
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

        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        Contract schemaRegistry = SetupSchemaRegistryContract(endpoint, chain, sender);

        //

        string attestationSignature = "bool isTest3";

        var schemaUIDSchema = AbiParameters.Parse("(string schema, address resolver, bool revocable)");
        var simpleRevocableBool = AbiKeyValues.Create(
            ("schema", attestationSignature),
            ("resolver", EthereumAddress.Zero),
            ("revocable", true));

        var packer = new AbiEncoderPacked();
        var schemaUID = KeccakHash.ComputeHash(packer.EncodeParameters(schemaUIDSchema, simpleRevocableBool).ToByteArray());
        // var schemaUID = KeccakHash.ComputeHash(Encoding.UTF8.GetBytes("fake"));

        Console.WriteLine("schemaUID: " + schemaUID.ToHexStruct());

        var caller = new RawContractCaller(endpoint);

        var schemaUidReturnedHex = await caller.CallAsync(schemaRegistry.Address, "getSchema(bytes32 uid)", ("uid", schemaUID));

        Assert.IsTrue(schemaUidReturnedHex.Length > 0);

        //

        var getSchemaDecodedResult = caller.DecodeParameters(
            "((bytes32 uid, address resolver, bool revocable, string schema) record)", schemaUidReturnedHex);

        Assert.IsTrue(getSchemaDecodedResult.Parameters.TryFirst(out var first));

        var record = first.Value as IDictionary<string, object?>;
        Assert.IsNotNull(record);
        Assert.IsTrue(record.Count == 4);
        Assert.IsTrue(record.TryGetValue("uid", out var uid));
        Assert.IsTrue(record.TryGetValue("schema", out var schema));
        Assert.IsTrue(record.TryGetValue("resolver", out var resolver));
        Assert.IsTrue(record.TryGetValue("revocable", out var revocable));

        if (uid is not byte[] uidBytes)
        {
            throw new Exception("uid is not a byte array");
        }

        var existingUIDHex = Hex.FromBytes(uidBytes);

        if (existingUIDHex.IsZeroValue())
        {
            // schema does not exist

            var registerEstimate = await schemaRegistry.EstimateTransactionFeeAsync(
                "register",
                senderAddress,
                null,
                simpleRevocableBool);

            registerEstimate = registerEstimate.InEther();

            //

            var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);

            var runner = new TransactionRunnerNative(sender, loggerFactory);

            var registerReceipt = await runner.RunTransactionAsync(
                schemaRegistry,
                "register",
                registerOptions,
                simpleRevocableBool,
                CancellationToken.None);

            Assert.IsNotNull(registerReceipt);
            Assert.IsTrue(registerReceipt.Success);

            Console.WriteLine(registerReceipt);

            // read the event data

            bool hasRegistered = schemaRegistry.TryReadEventLogsFromReceipt(
                registerReceipt, "Registered", out var indexed, out var data);

            if (!hasRegistered)
            {
                throw new Exception("The event was not found in the receipt");
            }

            if (data == null || data.None())
            {
                throw new Exception("The event data was not found in the receipt");
            }

            foreach (var (key, value) in data)
            {
                Console.WriteLine($"{key}: {value}");
            }

            Assert.IsNotNull(registerReceipt);
            Assert.IsTrue(registerReceipt.Success);
        }
        else
        {
            // schema exists
        }



        throw new NotImplementedException("Not implemented");

        //

    }

    [TestMethod]
    [Ignore]
    public async Task ExampleEAS_RegisterSchema()
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

        Sender sender = SetupSender(loggerFactory, senderAddress, senderAccount, chain);

        //

        var endpoint = new Endpoint(chainName, chainName, baseUrl, loggerFactory!);

        Contract schemaRegistry = SetupSchemaRegistryContract(endpoint, chain, sender);

        //

        var registerArgs = AbiKeyValues.Create(
            ("schema", "bool"),
            ("resolver", EthereumAddress.Zero),
            ("revocable", true));

        var registerEstimate = await schemaRegistry.EstimateTransactionFeeAsync(
            "register",
            senderAddress,
            null,
            registerArgs);

        registerEstimate = registerEstimate.InEther();

        //

        var registerOptions = new ContractInvocationOptions(registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);

        var runner = new TransactionRunnerNative(sender, loggerFactory);

        var registerReceipt = await runner.RunTransactionAsync(
            schemaRegistry,
            "register",
            registerOptions,
            registerArgs,
            CancellationToken.None);

        Console.WriteLine(registerReceipt);

        // read the event data

        bool hasRegistered = schemaRegistry.TryReadEventLogsFromReceipt(
            registerReceipt, "Registered", out var indexed, out var data);

        if (!hasRegistered)
        {
            throw new Exception("The event was not found in the receipt");
        }

        if (data == null || data.None())
        {
            throw new Exception("The event data was not found in the receipt");
        }

        foreach (var (key, value) in data)
        {
            Console.WriteLine($"{key}: {value}");
        }

        Assert.IsNotNull(registerReceipt);
        Assert.IsTrue(registerReceipt.Success);
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

    private static Contract SetupSchemaRegistryContract(Endpoint endpoint, Chain chain, Sender sender)
    {
        var abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");
        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");

        var contract = chain.GetContract(schemaRegistryAddress, endpoint, sender, abiStream);
        return contract;
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
