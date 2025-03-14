using System.Diagnostics;
using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.Conversion;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Examples;

[TestClass]
public class ExampleEAS
{
    [TestMethod]
    // [Ignore]
    public async Task ExampleEAS_CreateWallet()
    {
        // Call the GetSchema method on Ethereum Attestation Service

        // What do we need?
        //
        // ABI of EAS contract in order to call the GetSchema method
        // Address of EAS contract
        //
        // How do we get this?
        //
        // Use the ContractAbiReader to read the ABI from the EAS contract

        // A hypothetical Contract class would be able to produce function
        // signatures for the methods in the ABI, and hold the address of
        // the contract.
        //
        // contract.CallAsync("GetSchema", schemaId);
        //
        // contractCaller.CallAsync(contract, "GetSchema", schemaId);
        //
        // ContractCaller is a class that can be used to call methods on a
        // contract. Is is configured with a IEthereumJsonRpc, IAbiEncoder,
        // IAbiDecode, and a ITransactionSigner, and a INonceStore.
        //
        // !! We need something to compute the gas price.
        //


        // Then what?
        //
        // Get the function signature of the GetSchema method
        //
        // Call the GetSchema method with a value for the schemaId.
        //
        // This means ABI encoding the function signature and the schemaId.
        //
        // We don't need a signed transaction because we are not mutating the state.
        //
        // We do need to select a HTTP provider, and a JSON-RPC method.

        string infuraBaseUrl = "https://mainnet.infura.io/v3/";
        string hardhatBaseUrl = "http://localhost:8545";

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "GoogleCloud:ProjectName", "evoq-capricorn-timesheets" },                                 // for GCP
                    { "Blockchain:Ethereum:JsonRPC:GoogleSepolia:ProjectId", "evoq-capricorn-timesheets" }      // for GCP
                })
            .Build();

        using var loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(
                options => options.SingleLine = true).SetMinimumLevel(LogLevel.Debug));

        INonceStore nonceStore = new InMemoryNonceStore(loggerFactory);

        var privateKeyStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1PrivateKey")!;
        var privateKeyHex = Hex.Parse(privateKeyStr);

        var senderAddressStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1Address")!;
        var senderAddress = new EthereumAddress(senderAddressStr);

        // Read the ABI file using our helper method
        Stream abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");

        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");
        var sender = new Sender(privateKeyHex, nonceStore!);
        var contractClient = ContractClient.CreateDefault(new Uri(hardhatBaseUrl), sender, loggerFactory!);
        var contract = new Contract(contractClient, abiStream, schemaRegistryAddress);

        // guess gas price

        var chainClient = ChainClient.CreateDefault(new Uri(hardhatBaseUrl), sender, loggerFactory!);
        var chain = new Chain(chainClient);

        var guess = await contract.EstimateTransactionFeeAsync(
            chain,
            "register",
            senderAddress,
            null,
            AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true));

        Assert.IsTrue(100_000 > guess.GasLimit);
        Assert.AreEqual(EthereumAmount.FromWei(2_000_000_000), guess.MaxFeePerGas);
        Assert.AreEqual(EthereumAmount.FromWei(0), guess.MaxPriorityFeePerGas);
        Assert.AreEqual(EthereumAmount.FromWei(0), guess.BaseFeePerGas);
        Assert.AreEqual(EthereumAmount.FromWei(0), guess.EstimatedFee);
        Assert.AreEqual(0, guess.EstimatedFeeInEther);

        // original est

        var registerArgs = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);

        var registerGas = await contract.EstimateGasAsync("register", senderAddress, null, registerArgs);

        Assert.IsTrue(registerGas < 100_000, "Register gas is less than 100_000"); // just over 93_000

        var registerGasOptions = new EIP1559GasOptions(100_000, BigInteger.Zero, BigInteger.Zero); // ZEROES!!
        var registerOptions = new ContractInvocationOptions(1, registerGasOptions, null);
        var registerResult = await contract.InvokeMethodAsync("register", senderAddress, registerOptions, registerArgs);

        // get schema

        var schemaIdHex = Hex.Parse("2ab49509aba579bdcbb82dbc86db6bb04efe44289b146964f07a75ecffbb7f1e"); // random, non-existent schemaId

        var getSchemaResult = await contract.CallAsync("getSchema", senderAddress, AbiKeyValues.Create("uid", schemaIdHex));

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
}

public record SchemaRecordDto(
    Hex Uid,
    EthereumAddress Resolver,
    bool Revocable,
    Hex Schema);
