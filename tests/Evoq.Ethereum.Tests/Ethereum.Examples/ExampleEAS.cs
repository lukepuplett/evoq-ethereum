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

        string chainName = ChainNames.Hardhat;
        ulong chainId = ulong.Parse(ChainNames.GetChainId(chainName));

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

        var privateKeyStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1PrivateKey")!;
        var privateKeyHex = Hex.Parse(privateKeyStr);

        var senderAddressStr = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1Address")!;
        var senderAddress = new EthereumAddress(senderAddressStr);

        var chainClient = ChainClient.CreateDefault(chainId, new Uri(hardhatBaseUrl), loggerFactory!);
        var chain = new Chain(chainClient);

        var getStartingNonce = async () => await chain.GetTransactionCountAsync(senderAddress);

        var path = Path.Combine(Path.GetTempPath(), Path.Combine("hardhat-nonces", senderAddressStr));
        var nonceStore = new FileSystemNonceStore(path, loggerFactory, getStartingNonce);

        // Read the ABI file using our helper method
        Stream abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");

        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");
        var sender = new Sender(privateKeyHex, nonceStore);
        var endpoint = new Endpoint(chainName, chainName, hardhatBaseUrl, loggerFactory!);
        var contractClient = ContractClient.CreateDefault(endpoint, sender);
        var contract = new Contract(contractClient, abiStream, schemaRegistryAddress);

        // guess gas price

        var registerEstimate = await contract.EstimateTransactionFeeAsync(
            chain,
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

        var n = await nonceStore.BeforeSubmissionAsync();

        var registerOptions = new ContractInvocationOptions(n, registerEstimate.ToSuggestedGasOptions(), EtherAmount.Zero);
        var registerArgs = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);

        // JSON-RPC error: -32602 - leading zero
        var registerResult = await contract.InvokeMethodAsync("register", senderAddress, registerOptions, registerArgs);

        // get schema

        var abiPacker = new AbiEncoderPacked();
        var easSchemaUidSchema = AbiParameters.Parse("string schema, address resolver, bool revocable");
        var easSchemaUidSchemaValues = AbiKeyValues.Create("schema", "bool", "resolver", EthereumAddress.Zero, "revocable", true);
        var easSchemaUid = KeccakHash.ComputeHash(abiPacker.EncodeParameters(easSchemaUidSchema, easSchemaUidSchemaValues).ToHexStruct());

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
}

public record SchemaRecordDto(
    Hex Uid,
    EthereumAddress Resolver,
    bool Revocable,
    Hex Schema);
