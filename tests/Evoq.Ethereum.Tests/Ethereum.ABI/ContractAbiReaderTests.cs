using System.Text;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class ContractAbiReaderTests
{
    [TestMethod]
    public void Read_SimpleAbi_ParsesCorrectly()
    {
        var json = """
        [
            {
                "inputs": [
                    {
                        "internalType": "address",
                        "name": "recipient",
                        "type": "address"
                    },
                    {
                        "internalType": "uint256",
                        "name": "amount",
                        "type": "uint256"
                    }
                ],
                "name": "transfer",
                "outputs": [
                    {
                        "internalType": "bool",
                        "name": "",
                        "type": "bool"
                    }
                ],
                "stateMutability": "nonpayable",
                "type": "function"
            }
        ]
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        ContractAbi abi = AbiJsonReader.Read(stream);

        Assert.AreEqual(1, abi.Items.Count);
        var function = abi.Items[0];
        Assert.AreEqual("function", function.Type);
        Assert.AreEqual("transfer", function.Name);
        Assert.AreEqual(2, function.Inputs.Count);
        Assert.AreEqual(1, function.Outputs?.Count);
    }

    [TestMethod]
    public void Read_ComplexAbi_ParsesCorrectly()
    {
        var json = """
        [
            {
                "anonymous": false,
                "inputs": [
                    {
                        "indexed": true,
                        "internalType": "address",
                        "name": "owner",
                        "type": "address"
                    },
                    {
                        "components": [
                            {
                                "internalType": "string",
                                "name": "name",
                                "type": "string"
                            },
                            {
                                "internalType": "uint256[]",
                                "name": "values",
                                "type": "uint256[]"
                            }
                        ],
                        "indexed": false,
                        "internalType": "struct Token.Data",
                        "name": "data",
                        "type": "tuple"
                    }
                ],
                "name": "DataUpdated",
                "type": "event"
            },
            {
                "inputs": [
                    {
                        "internalType": "bytes32[]",
                        "name": "hashes",
                        "type": "bytes32[]"
                    },
                    {
                        "internalType": "string[]",
                        "name": "names",
                        "type": "string[]"
                    }
                ],
                "name": "batchProcess",
                "outputs": [
                    {
                        "internalType": "bool[]",
                        "name": "results",
                        "type": "bool[]"
                    }
                ],
                "stateMutability": "view",
                "type": "function"
            }
        ]
        """;

        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        ContractAbi abi = AbiJsonReader.Read(stream);

        Assert.AreEqual(2, abi.Items.Count);

        // Verify event
        var eventItem = abi.Items[0];
        Assert.AreEqual("event", eventItem.Type);
        Assert.AreEqual("DataUpdated", eventItem.Name);
        Assert.AreEqual(2, eventItem.Inputs.Count);
        Assert.IsTrue(eventItem.Inputs[0].Indexed);
        Assert.AreEqual("address", eventItem.Inputs[0].Type);

        // Verify struct (tuple) input
        var structInput = eventItem.Inputs[1];
        Assert.AreEqual("tuple", structInput.Type);
        Assert.AreEqual(2, structInput.Components!.Count);
        Assert.AreEqual("string", structInput.Components[0].Type);
        Assert.AreEqual("uint256[]", structInput.Components[1].Type);

        // Verify function
        var function = abi.Items[1];
        Assert.AreEqual("function", function.Type);
        Assert.AreEqual("batchProcess", function.Name);
        Assert.AreEqual(2, function.Inputs.Count);
        Assert.AreEqual("bytes32[]", function.Inputs[0].Type);
        Assert.AreEqual("string[]", function.Inputs[1].Type);
        Assert.AreEqual(1, function.Outputs!.Count);
        Assert.AreEqual("bool[]", function.Outputs[0].Type);

        var found = abi.TryGetFunction("batchProcess", out var function2);
        Assert.IsTrue(found);
        var signature = function2!.GetFunctionSignature();
        Assert.AreEqual("batchProcess(bytes32[],string[])", signature.GetCanonicalInputsSignature());
    }
}