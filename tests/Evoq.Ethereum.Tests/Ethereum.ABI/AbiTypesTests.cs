namespace Evoq.Ethereum.ABI;

[TestClass]
public class SolidityTypesTests
{
    [TestMethod]
    [DataRow("uint8")]
    [DataRow("uint16")]
    [DataRow("uint256")]
    [DataRow("int8")]
    [DataRow("int256")]
    [DataRow("bytes1")]
    [DataRow("bytes32")]
    [DataRow("uint")]  // Shorthand for uint256
    [DataRow("int")]   // Shorthand for int256
    [DataRow("bytes")] // Dynamic bytes
    [DataRow("uint8[]")]
    [DataRow("bytes32[]")]
    [DataRow("uint256[5]")]
    [DataRow("uint256[][]")]     // Dynamic multi-dimensional array
    [DataRow("uint256[5][]")]    // Mixed fixed and dynamic array
    [DataRow("uint256[][5]")]    // Mixed dynamic and fixed array
    [DataRow("uint256[5][10]")]  // Fixed multi-dimensional array
    [DataRow("address[2][3]")]   // Fixed multi-dimensional array with address
    [DataRow("bool[1][]")]       // Mixed fixed and dynamic with bool
    public void IsValidType_ValidTypes_ReturnsTrue(string type)
    {
        Assert.IsTrue(AbiTypes.IsValidType(type), $"Type '{type}' should be valid");
    }

    [TestMethod]
    [DataRow("uint0")]     // Too small
    [DataRow("uint7")]     // Not multiple of 8
    [DataRow("uint264")]   // Too large
    [DataRow("bytes0")]    // Too small
    [DataRow("bytes33")]   // Too large
    [DataRow("uint[0")]    // Invalid array syntax
    [DataRow("")]          // Empty string
    [DataRow("notAType")]  // Invalid type
    [DataRow("uint256[0][]")]    // Invalid fixed size 0
    [DataRow("uint256[-1][5]")]  // Negative fixed size
    [DataRow("uint256[5]]")]     // Invalid bracket syntax
    [DataRow("uint256[[5]]")]    // Invalid bracket syntax
    [DataRow("uint256[5][")]     // Incomplete bracket
    public void IsValidType_InvalidTypes_ReturnsFalse(string type)
    {
        Assert.IsFalse(AbiTypes.IsValidType(type), $"Type '{type}' should be invalid");
    }

    [TestMethod]
    [DataRow("uint", "uint256")]
    [DataRow("int", "int256")]
    [DataRow("byte", "bytes1")]
    [DataRow("uint8", "uint8")]
    [DataRow("address", "address")]
    [DataRow("uint[5][10]", "uint256[5][10]")]
    [DataRow("uint[][]", "uint256[][]")]
    [DataRow("uint[5][]", "uint256[5][]")]
    public void TryGetCanonicalType_BasicTypes_ReturnsNormalizedType(string input, string expected)
    {
        Assert.AreEqual(expected, AbiTypes.TryGetCanonicalType(input, out var canonicalType) ? canonicalType : null);
    }

    [TestMethod]
    [DataRow("(uint8,uint8)", "(uint8,uint8)")]
    [DataRow("(uint8,(string,string))", "(uint8,(string,string))")] // (id, (firstname, lastname))
    public void TryGetCanonicalType_TupleTypes_ReturnsNormalizedType(string input, string expected)
    {
        Assert.AreEqual(expected, AbiTypes.TryGetCanonicalType(input, out var canonicalType) ? canonicalType : null);
    }

    [TestMethod]
    [DataRow("string[]", "string")]
    [DataRow("string[][]", "string[]")]
    [DataRow("bool[2][4][12]", "bool[2][4]")]
    [DataRow("uint256[2][]", "uint256[2]")]
    [DataRow("address[][]", "address[]")]
    [DataRow("bytes32[1][2][3]", "bytes32[1][2]")]
    public void TryRemoveOuterArrayDimension_ValidArrayTypes_ReturnsInnerType(string type, string expectedInnerType)
    {
        // Act
        var success = AbiTypes.TryRemoveOuterArrayDimension(type, out var actualInnerType);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(expectedInnerType, actualInnerType);
    }

    [TestMethod]
    [DataRow("string")]
    [DataRow("uint256")]
    [DataRow("bool")]
    [DataRow("address")]
    [DataRow("bytes32")]
    [DataRow("")]
    [DataRow("[1]")] // Invalid type
    [DataRow("uint256[")] // Incomplete array
    [DataRow("uint256]]")] // Invalid brackets
    public void TryRemoveOuterArrayDimension_NonArrayTypes_ReturnsFalse(string type)
    {
        // Act
        var success = AbiTypes.TryRemoveOuterArrayDimension(type, out var innerType);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(innerType);
    }

    [TestMethod]
    [DataRow("uint256[2][3]", false)]  // Fixed array of fixed arrays is not dynamic
    [DataRow("uint256[][3]", true)]   // Fixed array of dynamic arrays is dynamic
    [DataRow("uint256[2][]", true)]   // Dynamic array of fixed arrays is dynamic
    [DataRow("string[2]", true)]      // Fixed array of dynamic type is dynamic
    [DataRow("bytes[3]", true)]       // Fixed array of dynamic type is dynamic
    [DataRow("bytes32[2]", false)]     // Fixed array of static type is not dynamic
    public void IsDynamicType_ArrayTypes_ReturnsExpectedResult(string type, bool expected)
    {
        Assert.AreEqual(expected, AbiTypes.IsDynamic(type));
    }

    [TestMethod]
    [DataRow("string", true)]
    [DataRow("bytes", true)]
    [DataRow("uint256", false)]
    [DataRow("bytes32", false)]
    [DataRow("bool", false)]
    [DataRow("address", false)]
    public void IsDynamicType_BasicTypes_ReturnsExpectedResult(string type, bool expected)
    {
        Assert.AreEqual(expected, AbiTypes.IsDynamic(type));
    }

    [TestMethod]
    [DataRow("uint256[][3]", true)]
    [DataRow("uint256[][2][10][12]", true)]
    [DataRow("uint256[2][2][10][12]", false)]
    [DataRow("string[2][2][10][12]", true)]
    [DataRow("uint256[2][3]", false)]
    public void IsDynamicArray_ReturnsExpectedResult(string type, bool expected)
    {
        Assert.AreEqual(expected, AbiTypes.IsDynamicArray(type));
    }

    [TestMethod]
    [DataRow("uint256", 256)]
    [DataRow("uint8", 8)]
    [DataRow("int256", 256)]
    [DataRow("int16", 16)]
    [DataRow("uint", 256)]    // Default size
    [DataRow("int", 256)]     // Default size
    public void TryGetBits_ValidTypes_ReturnsExpectedBits(string type, int expectedBits)
    {
        Assert.IsTrue(AbiTypes.TryGetMaxBitSize(type, out var bits));
        Assert.AreEqual(expectedBits, bits);
    }

    [TestMethod]
    [DataRow("bytes8", 8)]
    [DataRow("bytes32", 32)]
    public void TryGetBytes_ValidTypes_ReturnsExpectedBytes(string type, int expectedBytes)
    {
        Assert.IsTrue(AbiTypes.TryGetMaxBytesSize(type, out var bytes));
        Assert.AreEqual(expectedBytes, bytes);
    }

    [TestMethod]
    [DataRow("bool")]
    [DataRow("address")]
    [DataRow("string")]
    [DataRow("bytes")]
    [DataRow("bytes32")]
    public void TryGetBits_NonIntegerTypes_ReturnsFalse(string type)
    {
        Assert.IsFalse(AbiTypes.TryGetMaxBitSize(type, out _));
    }

    [TestMethod]
    [DataRow("byte", "bytes1")]
    [DataRow("fixed", "fixed128x18")]
    [DataRow("ufixed", "ufixed128x18")]
    public void TryGetCanonicalType_SpecialCases_ReturnsExpectedType(string input, string expected)
    {
        Assert.AreEqual(expected, AbiTypes.TryGetCanonicalType(input, out var canonicalType) ? canonicalType : null);
    }

    [TestMethod]
    [DataRow("uint256[]", "-1")]
    [DataRow("uint256[2][]", "-1,2")]
    [DataRow("uint256[][2]", "2,-1")]
    [DataRow("uint256[2][3]", "3,2")]
    public void TryGetArrayDimensions_ValidArrayTypes_ReturnsExpectedDimensions(string type, string expectedDimensions)
    {
        Assert.IsTrue(AbiTypes.TryGetArrayDimensions(type, out var dimensions));
        Assert.AreEqual(expectedDimensions, string.Join(",", dimensions!));
    }


}