using Evoq.Blockchain;
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
        var privateKeyBase64 = configuration.GetValue<string>("Blockchain:Ethereum:Addresses:Hardhat1PrivateKey")!;
        var privateKey = Hex.Parse(privateKeyBase64);

        // Read the ABI file using our helper method
        Stream abiStream = AbiFileHelper.GetAbiStream("EASSchemaRegistry.abi.json");

        var account = new EthereumAddress("0x1234567890123456789012345678901234567890");
        var schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");
        var sender = new Sender(privateKey, nonceStore!);
        var contractClient = ContractClient.CreateDefault(new Uri(hardhatBaseUrl), sender, loggerFactory!);
        var contract = new Contract(contractClient, abiStream, schemaRegistryAddress);

        // random, non-existent schemaId
        var schemaIdHex = Hex.Parse("2ab49509aba579bdcbb82dbc86db6bb04efe44289b146964f07a75ecffbb7f1e");
        var result = await contract.CallAsync("getSchema", account, schemaIdHex);

        Assert.IsNotNull(result);
    }
}
