namespace Evoq.Ethereum.ABI;
using System.Collections.Generic;
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
    public void GetFunctionSelector_SimpleFunction_ReturnsCorrectSelector()
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

        var selector = item.GetFunctionSelector();
        CollectionAssert.AreEqual(
            Convert.FromHexString("a9059cbb"),
            selector);
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
    public void GetFunctionSelector_WithNestedTuple_ReturnsCorrectSelector()
    {
        // Arrange
        var function = new ContractAbiItem
        {
            Type = "function",
            Name = "nestedTupleFunction",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
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
                }
            }
        };

        // Act
        var selector = function.GetFunctionSelector();

        // Assert
        // Compute the expected selector
        var signatureBytes = System.Text.Encoding.UTF8.GetBytes("nestedTupleFunction((uint256,(bool,string)))");
        var fullHash = KeccakHash.ComputeHash(signatureBytes);
        var expectedSelector = new byte[4];
        Array.Copy(fullHash, expectedSelector, 4);

        CollectionAssert.AreEqual(expectedSelector, selector);
    }

    [TestMethod]
    public void GetFunctionSelector_WithTupleArray_ReturnsCorrectSelector()
    {
        // Arrange
        var function = new ContractAbiItem
        {
            Type = "function",
            Name = "tupleArrayFunction",
            Inputs = new List<ContractAbiParameter>
            {
                new ContractAbiParameter
                {
                    Type = "tuple[]",
                    Components = new List<ContractAbiParameter>
                    {
                        new ContractAbiParameter { Type = "uint256" },
                        new ContractAbiParameter { Type = "address" }
                    }
                }
            }
        };

        // Act
        var selector = function.GetFunctionSelector();

        // Assert
        // Compute the expected selector
        var signatureBytes = System.Text.Encoding.UTF8.GetBytes("tupleArrayFunction((uint256,address)[])");
        var fullHash = KeccakHash.ComputeHash(signatureBytes);
        var expectedSelector = new byte[4];
        Array.Copy(fullHash, expectedSelector, 4);

        CollectionAssert.AreEqual(expectedSelector, selector);
    }

    [TestMethod]
    public void GetFunctionSelector_WithComplexTuples_ReturnsCorrectSelector()
    {
        // Arrange
        var function = new ContractAbiItem
        {
            Type = "function",
            Name = "complexFunction",
            Inputs = new List<ContractAbiParameter>
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
            }
        };

        // Act
        var selector = function.GetFunctionSelector();

        // Assert
        // Get the canonical signature from the function
        var canonicalSignature = function.GetCanonicalSignature();

        // Verify the canonical signature is in the expected format
        Assert.AreEqual("complexFunction(address,(uint256,bytes32),(bool,(string,uint8))[])", canonicalSignature);

        // Compute the expected selector using the canonical signature
        var signatureBytes = System.Text.Encoding.UTF8.GetBytes(canonicalSignature);
        var fullHash = KeccakHash.ComputeHash(signatureBytes);
        var expectedSelector = new byte[4];
        Array.Copy(fullHash, expectedSelector, 4);

        CollectionAssert.AreEqual(expectedSelector, selector);
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
        // You'll need to make GetCanonicalType public or create a test helper method
        var result = TestHelpers.GetCanonicalType(param);

        // Assert
        Assert.AreEqual("(uint256,address)", result);
    }
}

// Helper class to access private methods for testing
public static class TestHelpers
{
    public static string GetCanonicalType(ContractAbiParameter param)
    {
        // This is a wrapper to call the private GetCanonicalType method
        // You can implement this using reflection or by making the original method internal with [InternalsVisibleTo]

        // Example using reflection:
        var method = typeof(AbiExtensions).GetMethod("GetCanonicalType",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        return (string)method.Invoke(null, new object[] { param });
    }
}