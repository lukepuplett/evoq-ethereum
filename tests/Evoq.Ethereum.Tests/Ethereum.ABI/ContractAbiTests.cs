using System.Linq;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class ContractAbiTests
{
    [TestMethod]
    public void GetFunctions_ReturnsOnlyFunctions()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer" },
            new() { Type = "event", Name = "Transfer" },
            new() { Type = "function", Name = "balanceOf" },
            new() { Type = "error", Name = "InsufficientBalance" }
        });

        // Act
        var functions = abi.GetFunctions().ToList();

        // Assert
        Assert.AreEqual(2, functions.Count);
        Assert.IsTrue(functions.All(f => f.Type == "function"));
        CollectionAssert.Contains(functions.Select(f => f.Name).ToList(), "transfer");
        CollectionAssert.Contains(functions.Select(f => f.Name).ToList(), "balanceOf");
    }

    [TestMethod]
    public void GetFunction_WithExistingName_ReturnsFunction()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer" },
            new() { Type = "event", Name = "Transfer" }
        });

        // Act
        var found = abi.TryGetFunction("transfer", out var function);

        // Assert
        Assert.IsTrue(found);
        Assert.IsNotNull(function);
        Assert.AreEqual("function", function.Type);
        Assert.AreEqual("transfer", function.Name);
    }

    [TestMethod]
    public void GetFunction_WithNonExistentName_ReturnsNull()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer" }
        });

        // Act
        var found = abi.TryGetFunction("nonexistent", out var function);

        // Assert
        Assert.IsFalse(found);
    }

    [TestMethod]
    public void GetFunctions_WithOverloadedName_ReturnsAllOverloads()
    {
        // Arrange
        var abi = new ContractAbi(new List<ContractAbiItem>
        {
            new() { Type = "function", Name = "transfer", Inputs = new List<ContractAbiParameter> { new() { Type = "address" } } },
            new() { Type = "function", Name = "transfer", Inputs = new List<ContractAbiParameter> { new() { Type = "address" }, new() { Type = "uint256" } } },
            new() { Type = "event", Name = "Transfer" }
        });

        // Act
        var overloads = abi.GetFunctions("transfer").ToList();

        // Assert
        Assert.AreEqual(2, overloads.Count);
        Assert.IsTrue(overloads.All(f => f.Type == "function" && f.Name == "transfer"));
        Assert.AreEqual(1, overloads[0].Inputs.Count);
        Assert.AreEqual(2, overloads[1].Inputs.Count);
    }

    [TestMethod]
    public void GetEvents_RegisteredEvent_ParsesIndexedParametersCorrectly()
    {
        // Arrange
        var abiItem = new ContractAbiItem
        {
            Type = "event",
            Name = "Registered",
            Anonymous = false,
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Name = "uid",
                    Type = "bytes32",
                    Indexed = true,
                    InternalType = "bytes32"
                },
                new ContractAbiParameter
                {
                    Name = "registerer",
                    Type = "address",
                    Indexed = true,
                    InternalType = "address"
                },
                new ContractAbiParameter
                {
                    Name = "schema",
                    Type = "tuple",
                    Indexed = false,
                    InternalType = "struct SchemaRecord",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter
                        {
                            Name = "uid",
                            Type = "bytes32",
                            InternalType = "bytes32"
                        },
                        new ContractAbiParameter
                        {
                            Name = "resolver",
                            Type = "address",
                            InternalType = "contract ISchemaResolver"
                        },
                        new ContractAbiParameter
                        {
                            Name = "revocable",
                            Type = "bool",
                            InternalType = "bool"
                        },
                        new ContractAbiParameter
                        {
                            Name = "schema",
                            Type = "string",
                            InternalType = "string"
                        }
                    }
                }
            }
        };

        var abi = new ContractAbi(new List<ContractAbiItem> { abiItem });

        // Act
        var found = abi.TryGetEvent("Registered", out var eventItem);

        // Assert
        Assert.IsTrue(found, "Should find the Registered event");
        Assert.IsNotNull(eventItem, "Event item should not be null");
        Assert.AreEqual("event", eventItem.Type);
        Assert.AreEqual("Registered", eventItem.Name);
        Assert.IsFalse(eventItem.Anonymous ?? false, "Event should not be anonymous");

        // Verify inputs
        Assert.AreEqual(3, eventItem.Inputs.Count, "Should have 3 input parameters");

        // Check first indexed parameter (uid)
        var uidParam = eventItem.Inputs[0];
        Assert.AreEqual("uid", uidParam.Name);
        Assert.AreEqual("bytes32", uidParam.Type);
        Assert.IsTrue(uidParam.Indexed, "uid should be indexed");

        // Check second indexed parameter (registerer)
        var registererParam = eventItem.Inputs[1];
        Assert.AreEqual("registerer", registererParam.Name);
        Assert.AreEqual("address", registererParam.Type);
        Assert.IsTrue(registererParam.Indexed, "registerer should be indexed");

        // Check tuple parameter (schema)
        var schemaParam = eventItem.Inputs[2];
        Assert.AreEqual("schema", schemaParam.Name);
        Assert.AreEqual("tuple", schemaParam.Type);
        Assert.IsFalse(schemaParam.Indexed, "schema tuple should not be indexed");

        // Verify tuple components
        Assert.IsNotNull(schemaParam.Components, "Schema tuple should have components");
        Assert.AreEqual(4, schemaParam.Components.Count, "Schema tuple should have 4 components");

        // Verify specific tuple components
        var components = schemaParam.Components;
        Assert.AreEqual("uid", components[0].Name);
        Assert.AreEqual("bytes32", components[0].Type);

        Assert.AreEqual("resolver", components[1].Name);
        Assert.AreEqual("address", components[1].Type);

        Assert.AreEqual("revocable", components[2].Name);
        Assert.AreEqual("bool", components[2].Type);

        Assert.AreEqual("schema", components[3].Name);
        Assert.AreEqual("string", components[3].Type);
    }
}