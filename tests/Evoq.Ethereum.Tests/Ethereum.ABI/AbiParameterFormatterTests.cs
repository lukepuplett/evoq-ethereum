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
        // We need to use reflection to test this private method
        var method = typeof(AbiParameterFormatter).GetMethod("FormatTupleComponents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method.Invoke(null, new object[] { new List<ContractAbiParameter>() }) as string;

        // Assert
        Assert.AreEqual("", result);
    }

    [TestMethod]
    public void FormatTupleComponents_NullComponents_ReturnsEmptyString()
    {
        // We need to use reflection to test this private method
        var method = typeof(AbiParameterFormatter).GetMethod("FormatTupleComponents",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        // Act
        var result = method.Invoke(null, new object[] { null }) as string;

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
}