namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiParameterFormatterTests
{
    [TestMethod]
    public void FormatParameter_SimpleType_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter { Type = "uint256" };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("uint256", result);
    }

    [TestMethod]
    public void FormatParameter_SimpleTuple_ReturnsCorrectFormat()
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
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,address)", result);
    }

    [TestMethod]
    public void FormatParameter_NestedTuple_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "bool" },
                        new ContractAbiParameter { Type = "string" }
                    }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,(bool,string))", result);
    }

    [TestMethod]
    public void FormatParameter_TupleArray_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple[]",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter { Type = "address" }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,address)[]", result);
    }

    [TestMethod]
    public void FormatParameters_MultipleTuples_ReturnsCorrectFormat()
    {
        // Arrange
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter { Type = "address" },
            new ContractAbiParameter
            {
                Type = "tuple",
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "uint256" },
                    new ContractAbiParameter { Type = "bytes32" }
                }
            },
            new ContractAbiParameter
            {
                Type = "tuple[]",
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "bool" },
                    new ContractAbiParameter
                    {
                        Type = "tuple",
                        Components = new List<ContractAbiParameter>
                        {
                            new ContractAbiParameter { Type = "string" },
                            new ContractAbiParameter { Type = "uint8" }
                        }
                    }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters);

        // Assert
        Assert.AreEqual("(address,(uint256,bytes32),(bool,(string,uint8))[])", result);
    }

    [TestMethod]
    public void FormatParameter_NestedTupleArray_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple[]",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "bool" },
                        new ContractAbiParameter { Type = "string" }
                    }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,(bool,string))[]", result);
    }

    [TestMethod]
    public void FormatParameter_MultidimensionalTupleArray_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple[][]",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter { Type = "address" }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,address)[][]", result);
    }

    [TestMethod]
    public void FormatParameter_FixedSizeArray_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple[2]",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter { Type = "address" }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,address)[2]", result);
    }

    [TestMethod]
    public void FormatParameter_ComplexNestedTuples_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256" },
                new ContractAbiParameter
                {
                    Type = "tuple[]",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "bool" },
                        new ContractAbiParameter
                        {
                            Type = "tuple",
                            Components = new List<ContractAbiParameter>
                            {
                                new ContractAbiParameter { Type = "string" },
                                new ContractAbiParameter { Type = "uint8" }
                            }
                        }
                    }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param);

        // Assert
        Assert.AreEqual("(uint256,(bool,(string,uint8))[])", result);
    }

    [TestMethod]
    public void FormatParameters_EmptyParameters_ReturnsEmptyParentheses()
    {
        // Arrange
        var parameters = new List<ContractAbiParameter>();

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters);

        // Assert
        Assert.AreEqual("()", result);
    }

    [TestMethod]
    public void FormatParameters_NullParameters_ReturnsEmptyParentheses()
    {
        // Act
        var result = AbiParameterFormatter.FormatParameters(null);

        // Assert
        Assert.AreEqual("()", result);
    }

    [TestMethod]
    public void FormatParameters_MixedArrayTypes_ReturnsCorrectFormat()
    {
        // Arrange
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter { Type = "uint256[]" },
            new ContractAbiParameter
            {
                Type = "tuple[3][]",
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "bool" },
                    new ContractAbiParameter { Type = "address" }
                }
            },
            new ContractAbiParameter { Type = "string[][5]" }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters);

        // Assert
        Assert.AreEqual("(uint256[],(bool,address)[3][],string[][5])", result);
    }

    [TestMethod]
    public void FormatParameters_ComplexRealWorldExample_ReturnsCorrectFormat()
    {
        // Arrange - This mimics a complex real-world function from EAS or similar contracts
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter { Type = "address" }, // recipient
            new ContractAbiParameter
            {
                Type = "tuple[]", // attestations
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "bytes32" }, // schema
                    new ContractAbiParameter
                    {
                        Type = "tuple",
                        Components = new List<ContractAbiParameter>
                        {
                            new ContractAbiParameter { Type = "address" }, // recipient
                            new ContractAbiParameter { Type = "uint64" },  // expirationTime
                            new ContractAbiParameter { Type = "bool" },    // revocable
                            new ContractAbiParameter { Type = "bytes32" }, // refUID
                            new ContractAbiParameter { Type = "bytes" },   // data
                            new ContractAbiParameter { Type = "uint256" }  // value
                        }
                    }
                }
            },
            new ContractAbiParameter { Type = "uint256" } // value
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters);

        // Assert
        Assert.AreEqual("(address,(bytes32,(address,uint64,bool,bytes32,bytes,uint256))[],uint256)", result);
    }

    [TestMethod]
    public void FormatTupleComponents_EmptyComponents_ReturnsEmptyString()
    {
        // Act
        var result = AbiParameterFormatter.FormatTupleComponents(new List<ContractAbiParameter>(), false);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void FormatTupleComponents_NullComponents_ReturnsEmptyString()
    {
        // Act
        var result = AbiParameterFormatter.FormatTupleComponents(null!, false);

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void FormatParameters_ComplexReturnTypes_ReturnsCorrectFormat()
    {
        // Arrange - Create parameters that represent complex return types
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter { Type = "uint256" },
            new ContractAbiParameter
            {
                Type = "tuple[]",
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "bool" },
                    new ContractAbiParameter { Type = "string" }
                }
            },
            new ContractAbiParameter
            {
                Type = "tuple[3]",
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "address" },
                    new ContractAbiParameter { Type = "bytes32" }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters);

        // Assert
        Assert.AreEqual("(uint256,(bool,string)[],(address,bytes32)[3])", result);
    }

    [TestMethod]
    public void FormatParameters_EASAttestationReturnType_ReturnsCorrectFormat()
    {
        // Arrange - Based on the getAttestation function from EAS
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter
            {
                Type = "tuple",
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "bytes32" }, // uid
                    new ContractAbiParameter { Type = "bytes32" }, // schema
                    new ContractAbiParameter { Type = "uint64" },  // time
                    new ContractAbiParameter { Type = "uint64" },  // expirationTime
                    new ContractAbiParameter { Type = "uint64" },  // revocationTime
                    new ContractAbiParameter { Type = "bytes32" }, // refUID
                    new ContractAbiParameter { Type = "address" }, // recipient
                    new ContractAbiParameter { Type = "address" }, // attester
                    new ContractAbiParameter { Type = "bool" },    // revocable
                    new ContractAbiParameter { Type = "bytes" }    // data
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters);

        // Assert
        // This test doesn't involve arrays, so the format should remain the same
        Assert.AreEqual("((bytes32,bytes32,uint64,uint64,uint64,bytes32,address,address,bool,bytes))", result);
    }

    [TestMethod]
    public void FormatParameter_WithName_ShouldIncludeNameInOutput()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "uint256",
            Name = "amount"
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param, includeNames: true);

        // Assert
        Assert.AreEqual("uint256 amount", result);
    }

    [TestMethod]
    public void FormatParameter_WithNameAndIncludeNamesTrue_IncludesNameInOutput()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "uint256",
            Name = "amount"
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param, includeNames: true);

        // Assert
        Assert.AreEqual("uint256 amount", result);
    }

    [TestMethod]
    public void FormatParameter_WithNameAndIncludeNamesFalse_ExcludesNameFromOutput()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "uint256",
            Name = "amount"
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param, includeNames: false);

        // Assert
        Assert.AreEqual("uint256", result);
    }

    [TestMethod]
    public void FormatParameters_WithNamesAndIncludeNamesTrue_IncludesNamesInOutput()
    {
        // Arrange
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter { Type = "address", Name = "recipient" },
            new ContractAbiParameter { Type = "uint256", Name = "amount" }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(parameters, includeNames: true);

        // Assert
        Assert.AreEqual("(address recipient,uint256 amount)", result);
    }

    [TestMethod]
    public void FormatParameter_TupleWithNames_IncludesNamesWhenRequested()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple",
            Name = "person",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "string", Name = "name" },
                new ContractAbiParameter { Type = "uint256", Name = "age" },
                new ContractAbiParameter { Type = "address", Name = "wallet" }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param, includeNames: true);

        // Assert
        Assert.AreEqual("(string name,uint256 age,address wallet) person", result);
    }

    [TestMethod]
    public void FormatParameter_NestedTupleWithNames_IncludesNamesWhenRequested()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "tuple",
            Name = "userData",
            Components = new List<ContractAbiParameter>
            {
                new ContractAbiParameter { Type = "uint256", Name = "id" },
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Name = "fullName",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "string", Name = "firstName" },
                        new ContractAbiParameter { Type = "string", Name = "lastName" }
                    }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(param, includeNames: true);

        // Assert
        Assert.AreEqual("(uint256 id,(string firstName,string lastName) fullName) userData", result);
    }

    [TestMethod]
    public void FormatParameter_IndexedEventParameter_ReturnsCorrectFormat()
    {
        // Arrange
        var param = new ContractAbiParameter
        {
            Type = "address",
            Name = "sender",
            Indexed = true
        };

        // Act
        var result = AbiParameterFormatter.FormatParameter(
            param, includeNames: true, includeIndexed: true);

        // Assert
        Assert.AreEqual("address indexed sender", result);
    }

    [TestMethod]
    public void FormatParameters_EventWithMixedIndexedParameters_ReturnsCorrectFormat()
    {
        // Arrange
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter { Type = "address", Name = "from", Indexed = true },
            new ContractAbiParameter { Type = "address", Name = "to", Indexed = true },
            new ContractAbiParameter { Type = "uint256", Name = "value" }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(
            parameters, includeNames: true, includeIndexed: true);

        // Assert
        Assert.AreEqual("(address indexed from,address indexed to,uint256 value)", result);
    }

    [TestMethod]
    public void FormatParameters_ComplexEventWithIndexedTuple_ReturnsCorrectFormat()
    {
        // Arrange
        var parameters = new List<ContractAbiParameter>
        {
            new ContractAbiParameter
            {
                Type = "tuple",
                Name = "data",
                Indexed = true,
                Components = new List<ContractAbiParameter>
                {
                    new ContractAbiParameter { Type = "uint256", Name = "id" },
                    new ContractAbiParameter { Type = "address", Name = "owner" }
                }
            },
            new ContractAbiParameter { Type = "string", Name = "description" }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(
            parameters, includeNames: true, includeIndexed: true);

        // Assert
        Assert.AreEqual("((uint256 id,address owner) indexed data,string description)", result);
    }

    [TestMethod]
    public void FormatParameters_RegisteredEventFromContractAbi_ReturnsCorrectFormat()
    {
        // Arrange
        var contractAbiItem = new ContractAbiItem
        {
            Type = "event",
            Name = "Registered",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "bytes32",
                    Name = "uid",
                    Indexed = true
                },
                new ContractAbiParameter
                {
                    Type = "address",
                    Name = "registerer",
                    Indexed = true
                },
                new ContractAbiParameter
                {
                    Type = "tuple",
                    Name = "schema",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "bytes32", Name = "uid" },
                        new ContractAbiParameter { Type = "address", Name = "resolver" },
                        new ContractAbiParameter { Type = "bool", Name = "revocable" },
                        new ContractAbiParameter { Type = "string", Name = "schema" }
                    }
                }
            }
        };

        // Act
        var result = AbiParameterFormatter.FormatParameters(
            contractAbiItem.Inputs,
            includeNames: true,
            includeIndexed: true);

        // Assert
        Assert.AreEqual(
            "(bytes32 indexed uid,address indexed registerer,(bytes32 uid,address resolver,bool revocable,string schema) schema)",
            result,
            "Event signature format should match EAS schema registry exactly");
    }
}