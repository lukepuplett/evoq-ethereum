using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.JsonRPC.Tests;

[TestClass]
public class RawContractCallerTests
{
    private static Endpoint endpoint;
    private static RawContractCaller caller;
    private static EthereumAddress schemaRegistryAddress;

    [ClassInitialize]
    public static void ClassInitialize(TestContext context)
    {
        // Set up logging
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddSimpleConsole(options => options.SingleLine = true)
                   .SetMinimumLevel(LogLevel.Debug));

        // Create endpoint for Hardhat
        endpoint = new Endpoint("hardhat", "hardhat", "http://localhost:8545", loggerFactory);

        // Create the caller
        caller = new RawContractCaller(endpoint);

        // Schema Registry address from Hardhat deployment
        schemaRegistryAddress = new EthereumAddress("0x5FbDB2315678afecb367f032d93F642f64180aa3");
    }

    [Ignore("Depends on the EAS Schema Registry contract being deployed")]
    [TestMethod]
    public async Task GetSchema_WithNonExistentUID_ReturnsEmptySchemaRecord()
    {
        // Checks that the call is made and returns a record with no schema UID - basically, it doesn't
        // throw when we make the call.

        // Arrange
        var context = new JsonRpcContext();

        // Create a random UID that won't exist
        var randomBytes = new byte[32];
        new Random().NextBytes(randomBytes);
        var randomUID = Hex.FromBytes(randomBytes);

        // Act
        var encodedSchemaRecordHex = await caller.CallAsync(
            context,
            schemaRegistryAddress,
            "getSchema(bytes32 uid)",
            ("uid", randomUID));

        var secondSchemaRecordHex = await caller.CallAsync(
            context,
            schemaRegistryAddress,
            "getSchema(bytes32)",
            randomUID);

        // Assert
        Assert.IsNotNull(encodedSchemaRecordHex, "encodedSchemaRecordHex should not be null");
        Assert.IsTrue(encodedSchemaRecordHex.Length > 0, "encodedSchemaRecordHex should not be empty");
        Assert.IsNotNull(secondSchemaRecordHex, "secondSchemaRecordHex should not be null");
        Assert.IsTrue(secondSchemaRecordHex.Length > 0, "secondSchemaRecordHex should not be empty");

        // Decode the result as a schema record
        var decodedResult = caller.DecodeParameters(
            "((bytes32 uid, address resolver, bool revocable, string schema) record)",
            encodedSchemaRecordHex);

        Assert.IsTrue(decodedResult.Parameters.TryFirst(out var first), "Should be able to get first parameter");
        var record = first.Value as IDictionary<string, object?>;
        Assert.IsNotNull(record, "Record should not be null");
        Assert.IsTrue(record.Count == 4, "Record should have exactly 4 fields");

        // Verify all fields are zero/empty
        Assert.IsTrue(record.TryGetValue("uid", out var uid), "Should have uid field");
        Assert.IsTrue(record.TryGetValue("schema", out var schema), "Should have schema field");
        Assert.IsTrue(record.TryGetValue("resolver", out var resolver), "Should have resolver field");
        Assert.IsTrue(record.TryGetValue("revocable", out var revocable), "Should have revocable field");

        var uidHex = Hex.FromBytes((byte[])uid!);
        Assert.IsTrue(uidHex.IsZeroValue(), "UID should be zero value");
        Assert.AreEqual("", schema, "Schema should be empty string");
        Assert.AreEqual(EthereumAddress.Zero, resolver, "Resolver should be zero address");
        Assert.IsFalse((bool)revocable!, "Revocable should be false");
    }
}