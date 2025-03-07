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
    [Ignore]
    public void ExampleEAS_CreateWallet()
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

        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    { "GoogleCloud:ProjectName", "evoq-capricorn-timesheets" },                                 // for GCP JSON-RPC
                    { "Blockchain:Ethereum:JsonRPC:GoogleSepolia:ProjectId", "evoq-capricorn-timesheets" }      // for GCP JSON-RPC
                })
            .Build();

        using var loggerFactory = LoggerFactory.Create(
            builder => builder.AddSimpleConsole(
                options => options.SingleLine = true).SetMinimumLevel(LogLevel.Debug));

        // var logger = loggerFactory.CreateLogger<ExampleEAS>();

        INonceStore nonceStore = new InMemoryNonceStore(loggerFactory);

        // Read the ABI file using our helper method
        Stream abiStream = AbiFileHelper.GetAbiStream("EAS.abi.json");

        var account = new EthereumAddress("0x1234567890123456789012345678901234567890");
        var sender = new Sender(Hex.Parse("0x1234567890123456789012345678901234567890"), nonceStore!);
        var contract = new Contract(abiStream);
        var contractCaller = ContractCaller.CreateDefault(new Uri("https://mainnet.infura.io/v3/"), sender, loggerFactory!);

        var schemaId = contractCaller.CallAsync(contract, "GetSchema", account, 1, 2, 3);
    }
}

/// <summary>
/// Helper class for loading ABI files from the project
/// </summary>
public static class AbiFileHelper
{
    /// <summary>
    /// Gets a stream for an ABI file that is embedded as a resource or located in the project
    /// </summary>
    /// <param name="fileName">Name of the ABI file</param>
    /// <returns>Stream containing the ABI content</returns>
    public static Stream GetAbiStream(string fileName)
    {
        // First try to load as an embedded resource
        var assembly = typeof(AbiFileHelper).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resourceName != null)
        {
            return assembly.GetManifestResourceStream(resourceName)!;
        }

        // If not found as embedded resource, try various relative paths
        var possiblePaths = new[]
        {
            // First check in the output directory (where Content files would be copied)
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName),
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Abis", fileName),
            
            // Then check current directory and working directory
            fileName,
            Path.Combine("Abis", fileName),
            Path.Combine(Directory.GetCurrentDirectory(), fileName),
            Path.Combine(Directory.GetCurrentDirectory(), "Abis", fileName),
        };

        foreach (var path in possiblePaths)
        {
            if (File.Exists(path))
            {
                return File.OpenRead(path);
            }
        }

        throw new FileNotFoundException($"Could not find ABI file: {fileName}. Make sure it's included as an embedded resource or in the correct location.");
    }
}
