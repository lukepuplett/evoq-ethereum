namespace Evoq.Ethereum.ABI;
using System.Collections.Generic;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

[TestClass]
public class AbiExtensionsTests
{
    [TestMethod]
    public void GetFunctionSignature_SimpleFunction_ReturnsCorrectSignature()
    {
        var item = new ContractAbiItem
        {
            Type = "function",
            Name = "transfer",
            Inputs = new List<ContractAbiParameter>
            {
                new() { Type = "address" },
                new() { Type = "uint256" }
            }
        };

        var signature = item.GetFunctionSignature();
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void GetFunctionSignature_NonFunction_ThrowsException()
    {
        var item = new ContractAbiItem
        {
            Type = "event",
            Name = "Transfer"
        };

        _ = item.GetFunctionSignature();
    }

    [TestMethod]
    public void GetFunctionSignature_WithSimpleTuple_ReturnsCorrectSignature()
    {
        // Arrange
        var function = new ContractAbiItem
        {
            Type = "function",
            Name = "tupleFunction",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "uint256" },
                        new ContractAbiParameter { Type = "address" }
                    }
                }
            }
        };

        // Act
        var signature = function.GetFunctionSignature();

        // Assert
        // First, check that we have the correct number of input parameters
        Assert.AreEqual(1, signature.Inputs.Count, "Should have 1 input parameter (the tuple)");

        // Check that the first parameter is a tuple
        var firstParam = signature.Inputs[0];
        Assert.IsTrue(firstParam.IsTupleStrict, "First parameter should be a tuple");

        // Check that the tuple has the correct components
        Assert.IsTrue(firstParam.TryParseComponents(out var components), "Tuple components should not be null");
        Assert.AreEqual(2, components!.Count, "Tuple should have 2 components");
        Assert.AreEqual("uint256", components[0].AbiType, "First component should be uint256");
        Assert.AreEqual("address", components[1].AbiType, "Second component should be address");

        // Finally, check the canonical signature
        Assert.AreEqual("tupleFunction((uint256,address))", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void GetFunctionSignature_WithMultidimensionalTupleArray_ReturnsCorrectSignature()
    {
        // Arrange
        var function = new ContractAbiItem
        {
            Type = "function",
            Name = "multiDimArrayFunction",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "tuple[][]",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "uint256" },
                        new ContractAbiParameter { Type = "address" }
                    }
                }
            }
        };

        // Act
        var signature = function.GetFunctionSignature();

        // Assert
        Assert.AreEqual("multiDimArrayFunction((uint256,address)[][])", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void GetFunctionSignature_WithFixedSizeArray_ReturnsCorrectSignature()
    {
        // Arrange
        var function = new ContractAbiItem
        {
            Type = "function",
            Name = "fixedArrayFunction",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "tuple[2]",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "uint256" },
                        new ContractAbiParameter { Type = "address" }
                    }
                }
            }
        };

        // Act
        var signature = function.GetFunctionSignature();

        // Assert
        Assert.AreEqual("fixedArrayFunction((uint256,address)[2])", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void GetCanonicalType_WithSimpleTuple_ReturnsCorrectType()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter { Type = "address" }
            }
        };

        // Act
        var result = param.GetCanonicalType();

        // Assert
        Assert.AreEqual("(uint256,address)", result);
    }

    [TestMethod]
    public void GetFunctionSignature_PreservesParameterNames_ForBasicTypes()
    {
        // Arrange
        var item = new ContractAbiItem
        {
            Type = "function",
            Name = "transfer",
            Inputs = new List<ContractAbiParameter>
            {
                new() { Type = "address", Name = "recipient" },
                new() { Type = "uint256", Name = "amount" }
            }
        };

        // Act
        var signature = item.GetFunctionSignature();

        // Assert
        Assert.AreEqual(2, signature.Inputs.Count);
        Assert.AreEqual("recipient", signature.Inputs[0].Name);
        Assert.AreEqual("amount", signature.Inputs[1].Name);
        Assert.AreEqual("address", signature.Inputs[0].AbiType);
        Assert.AreEqual("uint256", signature.Inputs[1].AbiType);
    }

    [TestMethod]
    public void GetFunctionSignature_PreservesParameterNames_ForTupleTypes()
    {
        // Arrange
        var item = new ContractAbiItem
        {
            Type = "function",
            Name = "setPerson",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Name = "person",
                    Components = new List<ContractAbiParameter>
                    {
                        new() { Type = "string", Name = "name" },
                        new() { Type = "uint256", Name = "age" },
                        new() { Type = "address", Name = "wallet" }
                    }
                }
            }
        };

        // Act
        var signature = item.GetFunctionSignature();

        // Assert
        Assert.AreEqual(1, signature.Inputs.Count);
        Assert.AreEqual("person", signature.Inputs[0].Name);
        Assert.AreEqual("(string,uint256,address)", signature.Inputs[0].AbiType);

        // Verify tuple components have correct names
        Assert.IsTrue(signature.Inputs[0].TryParseComponents(out var components));
        Assert.IsNotNull(components);
        Assert.AreEqual(3, components.Count);
        Assert.AreEqual("name", components[0].Name);
        Assert.AreEqual("age", components[1].Name);
        Assert.AreEqual("wallet", components[2].Name);
    }

    [TestMethod]
    public void GetFunctionSignature_PreservesParameterNames_ForNestedTuples()
    {
        // Arrange
        var item = new ContractAbiItem
        {
            Type = "function",
            Name = "complexFunction",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Name = "userData",
                    Components = new List<ContractAbiParameter>
                    {
                        new() { Type = "uint256", Name = "id" },
                        new ContractAbiParameter
                        {
                            Type = "tuple",
                            Name = "fullName",
                            Components = new List<ContractAbiParameter>
                            {
                                new() { Type = "string", Name = "firstName" },
                                new() { Type = "string", Name = "lastName" }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var signature = item.GetFunctionSignature();

        // Assert
        Assert.AreEqual(1, signature.Inputs.Count);
        Assert.AreEqual("userData", signature.Inputs[0].Name);

        // Verify outer tuple components
        Assert.IsTrue(signature.Inputs[0].TryParseComponents(out var outerComponents));
        Assert.IsNotNull(outerComponents);
        Assert.AreEqual(2, outerComponents.Count);
        Assert.AreEqual("id", outerComponents[0].Name);
        Assert.AreEqual("fullName", outerComponents[1].Name);

        // Verify inner tuple components
        Assert.IsTrue(outerComponents[1].TryParseComponents(out var innerComponents));
        Assert.IsNotNull(innerComponents);
        Assert.AreEqual(2, innerComponents.Count);
        Assert.AreEqual("firstName", innerComponents[0].Name);
        Assert.AreEqual("lastName", innerComponents[1].Name);
    }

    [TestMethod]
    public void GetFunctionSignature_PreservesParameterNames_ForOutputParameters()
    {
        // Arrange
        var item = new ContractAbiItem
        {
            Type = "function",
            Name = "getValues",
            Inputs = new List<ContractAbiParameter>
            {
                new() { Type = "address", Name = "account" }
            },
            Outputs = new List<ContractAbiParameter>
            {
                new() { Type = "uint256", Name = "balance" },
                new() { Type = "bool", Name = "active" }
            }
        };

        // Act
        var signature = item.GetFunctionSignature();

        // Assert
        Assert.IsNotNull(signature.Outputs);
        Assert.AreEqual(2, signature.Outputs.Count);
        Assert.AreEqual("balance", signature.Outputs[0].Name);
        Assert.AreEqual("active", signature.Outputs[1].Name);
        Assert.AreEqual("uint256", signature.Outputs[0].AbiType);
        Assert.AreEqual("bool", signature.Outputs[1].AbiType);
    }

    [TestMethod]
    public void GetFunctionSignature_PreservesParameterNames_ForArrayTypes()
    {
        // Arrange
        var item = new ContractAbiItem
        {
            Type = "function",
            Name = "batchTransfer",
            Inputs = new List<ContractAbiParameter>
            {
                new() { Type = "address[]", Name = "recipients" },
                new() { Type = "uint256[]", Name = "amounts" }
            }
        };

        // Act
        var signature = item.GetFunctionSignature();

        // Assert
        Assert.AreEqual(2, signature.Inputs.Count);
        Assert.AreEqual("recipients", signature.Inputs[0].Name);
        Assert.AreEqual("amounts", signature.Inputs[1].Name);
        Assert.AreEqual("address[]", signature.Inputs[0].AbiType);
        Assert.AreEqual("uint256[]", signature.Inputs[1].AbiType);
    }

    [TestMethod]
    public void GetEventSignature_RegisteredEvent_PreservesIndexedFlags()
    {
        // Arrange
        var eventItem = new ContractAbiItem
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

        // Act
        var signature = eventItem.GetEventSignature();

        // Assert
        Assert.AreEqual(AbiItemType.Event, signature.ItemType, "Should be an event signature");
        Assert.AreEqual("Registered", signature.Name, "Event name should match");
        Assert.IsFalse(signature.IsAnonymous, "Event should not be anonymous");

        // Check parameters
        Assert.AreEqual(3, signature.Inputs.Count, "Should have 3 parameters");

        // Check indexed parameters
        var indexedParams = signature.Inputs.Where(p => p.IsIndexed).ToList();
        Assert.AreEqual(2, indexedParams.Count, "Should have 2 indexed parameters");

        // First indexed parameter (uid)
        Assert.AreEqual("uid", indexedParams[0].Name);
        Assert.AreEqual("bytes32", indexedParams[0].AbiType);
        Assert.IsTrue(indexedParams[0].IsIndexed);

        // Second indexed parameter (registerer)
        Assert.AreEqual("registerer", indexedParams[1].Name);
        Assert.AreEqual("address", indexedParams[1].AbiType);
        Assert.IsTrue(indexedParams[1].IsIndexed);

        // Check non-indexed tuple parameter
        var schemaParam = signature.Inputs.First(p => !p.IsIndexed);
        Assert.AreEqual("schema", schemaParam.Name);
        Assert.IsFalse(schemaParam.IsIndexed);
        Assert.IsTrue(schemaParam.IsTupleStrict);

        // Verify tuple components
        Assert.IsTrue(schemaParam.TryParseComponents(out var components));
        Assert.IsNotNull(components);
        Assert.AreEqual(4, components.Count);

        // Verify canonical signature format
        Assert.AreEqual(
            "Registered(bytes32,address,(bytes32,address,bool,string))",
            signature.GetCanonicalInputsSignature(),
            "Canonical signature should match expected format");
    }
}
