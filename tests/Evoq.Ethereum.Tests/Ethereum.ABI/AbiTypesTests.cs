using System.Collections;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiTypesTests
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
    [DataRow("(uint8,uint8)")]
    [DataRow("(uint8,(string,string))")] // (id, (firstname, lastname))
    [DataRow("(bool valid,address owner)")]
    public void IsValidTuple_ValidTypes_ReturnsTrue(string type)
    {
        Assert.IsTrue(AbiTypes.IsValidTuple(type), $"Type '{type}' should be valid");
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
        Assert.IsTrue(AbiTypes.TryGetCanonicalType(input, out var canonicalType));
        Assert.AreEqual(expected, canonicalType);
    }

    [TestMethod]
    [DataRow("(bool valid,address owner)", "(bool,address)")]
    [DataRow("(uint8,uint8)", "(uint8,uint8)")]
    [DataRow("(uint8,(string,string))", "(uint8,(string,string))")] // (id, (firstname, lastname))
    public void TryGetCanonicalType_TupleTypes_ReturnsNormalizedType(string input, string expected)
    {
        Assert.IsTrue(AbiTypes.TryGetCanonicalType(input, out var canonicalType));
        Assert.AreEqual(expected, canonicalType);
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
        var success = AbiTypes.TryGetArrayInnerType(type, out var actualInnerType);

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
        var success = AbiTypes.TryGetArrayInnerType(type, out var innerType);

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
        Assert.IsTrue(AbiTypes.TryGetBitsSize(type, out var bits));
        Assert.AreEqual(expectedBits, bits);
    }

    [TestMethod]
    [DataRow("bytes8", 8)]
    [DataRow("bytes32", 32)]
    public void TryGetBytes_ValidTypes_ReturnsExpectedBytes(string type, int expectedBytes)
    {
        Assert.IsTrue(AbiTypes.TryGetBytesSize(type, out var bytes));
        Assert.AreEqual(expectedBytes, bytes);
    }

    [TestMethod]
    [DataRow("bool", 1)]
    [DataRow("address", 20 * 8)]
    [DataRow("bytes32", 32 * 8)]
    public void TryGetBits_NonIntegerTypes_ReturnsExpectedBits(string type, int expectedBits)
    {
        Assert.IsTrue(AbiTypes.TryGetBitsSize(type, out var bits));
        Assert.AreEqual(expectedBits, bits);
    }

    [TestMethod]
    [DataRow("byte", "bytes1")]
    // [DataRow("fixed", "fixed128x18")]   // not supported
    // [DataRow("ufixed", "ufixed128x18")] // not supported
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

    [TestMethod]
    [DataRow("(uint256,bool)", true, 2)]
    [DataRow("(uint8,string,address)", true, 3)]
    [DataRow("(uint256,bytes32,bool[])", true, 3)]
    [DataRow("(uint256,(string,bool))", true, 2)]
    [DataRow("((address,uint256),bytes32,bool)", true, 3)]
    [DataRow("(uint256,uint256[])", true, 2)]
    [DataRow("()", true, 0)]  // Empty tuple
    public void TryGetTupleLength_ValidTuples_ReturnsExpectedLength(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetTupleLength(type, includeArrays: false, out int length);

        // Assert
        Assert.IsTrue(success, $"Should successfully get tuple length for {type}");
        Assert.AreEqual(expectedLength, length, $"Tuple length for {type} should be {expectedLength}");
    }

    [TestMethod]
    [DataRow("uint256")]
    [DataRow("bytes32")]
    [DataRow("address")]
    [DataRow("bool")]
    [DataRow("string")]
    [DataRow("bytes")]
    [DataRow("uint256[]")]
    [DataRow("(uint256,bool")]  // Incomplete tuple
    [DataRow("uint256,bool)")]  // Incomplete tuple
    [DataRow("[uint256,bool]")] // Wrong brackets
    public void TryGetTupleLength_NonTupleTypes_ReturnsFalse(string type)
    {
        // Act
        bool success = AbiTypes.TryGetTupleLength(type, includeArrays: false, out int length);

        // Assert
        Assert.IsFalse(success, $"Should return false for non-tuple type {type}");
        Assert.AreEqual(0, length, "Length should be 0 for non-tuple types");
    }

    [TestMethod]
    [DataRow("uint8", typeof(byte))]
    [DataRow("uint16", typeof(ushort))]
    [DataRow("uint32", typeof(uint))]
    [DataRow("uint64", typeof(ulong))]
    [DataRow("uint128", typeof(System.Numerics.BigInteger))]
    [DataRow("uint256", typeof(System.Numerics.BigInteger))]
    [DataRow("uint", typeof(System.Numerics.BigInteger))] // Default uint is uint256
    public void TryGetDefaultClrType_UintTypes_ReturnsExpectedType(string abiType, Type expectedType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsTrue(success, $"Failed to get CLR type for {abiType}");
        Assert.AreEqual(expectedType, actualType, $"CLR type for {abiType} should be {expectedType.Name}");
    }

    [TestMethod]
    [DataRow("int8", typeof(sbyte))]
    [DataRow("int16", typeof(short))]
    [DataRow("int32", typeof(int))]
    [DataRow("int64", typeof(long))]
    [DataRow("int128", typeof(System.Numerics.BigInteger))]
    [DataRow("int256", typeof(System.Numerics.BigInteger))]
    [DataRow("int", typeof(System.Numerics.BigInteger))] // Default int is int256
    public void TryGetDefaultClrType_IntTypes_ReturnsExpectedType(string abiType, Type expectedType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsTrue(success, $"Failed to get CLR type for {abiType}");
        Assert.AreEqual(expectedType, actualType, $"CLR type for {abiType} should be {expectedType.Name}");
    }

    [TestMethod]
    [DataRow("bytes1", typeof(byte))]
    [DataRow("bytes32", typeof(byte[]))]
    [DataRow("byte", typeof(byte))] // byte is alias for bytes1
    [DataRow("bytes", typeof(byte[]))]
    [DataRow("string", typeof(string))]
    [DataRow("address", typeof(Evoq.Ethereum.EthereumAddress))]
    [DataRow("bool", typeof(bool))]
    public void TryGetDefaultClrType_OtherTypes_ReturnsExpectedType(string abiType, Type expectedType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsTrue(success, $"Failed to get CLR type for {abiType}");
        Assert.AreEqual(expectedType, actualType, $"CLR type for {abiType} should be {expectedType.Name}");
    }

    [TestMethod]
    [DataRow("uint8[]", typeof(byte[]))]
    [DataRow("bool[]", typeof(bool[]))]
    [DataRow("string[]", typeof(string[]))]
    [DataRow("uint256[][]", typeof(System.Numerics.BigInteger[][]))]
    [DataRow("bool[][2][]", typeof(bool[][][]))]
    [DataRow("address[3][2]", typeof(EthereumAddress[][]))]
    public void TryGetDefaultClrType_ArrayTypes_ReturnsExpectedType(string abiType, Type expectedType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsTrue(success, $"Failed to get CLR type for {abiType}");
        Assert.AreEqual(expectedType, actualType, $"CLR type for {abiType} should be {expectedType.FullName}");
    }

    [TestMethod]
    [DataRow("(uint256,bool)", typeof(Dictionary<string, object?>))]
    [DataRow("(uint8,string,address)", typeof(Dictionary<string, object?>))]
    public void TryGetDefaultClrType_TupleTypes_ReturnsExpectedType(string abiType, Type expectedType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsTrue(success, $"Failed to get CLR type for {abiType}");
        Assert.AreEqual(expectedType, actualType, $"CLR type for {abiType} should be {expectedType.Name}");
    }

    [TestMethod]
    [DataRow("uint257")] // Invalid uint size
    [DataRow("int512")] // Invalid int size
    [DataRow("bytes33")] // Invalid bytes size
    [DataRow("notAType")] // Invalid type name
    [DataRow("uint[")] // Incomplete array
    [DataRow("uint[0]")] // Invalid array size
    public void TryGetDefaultClrType_InvalidTypes_ReturnsFalse(string abiType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsFalse(success, $"Should return false for invalid type {abiType}");
        Assert.AreEqual(typeof(object), actualType, "Default type should be object for invalid types");
    }

    #region HasLengthSuffix Tests

    [TestMethod]
    [DataRow("uint8", true, 1)]
    [DataRow("uint16", true, 2)]
    [DataRow("uint256", true, 32)]
    [DataRow("int8", true, 1)]
    [DataRow("int256", true, 32)]
    [DataRow("bytes1", true, 1)]
    [DataRow("bytes32", true, 32)]
    [DataRow("uint256[3]", true, 3)]
    [DataRow("bytes32[5]", true, 5)]
    [DataRow("address[10]", true, 10)]
    public void HasLengthSuffix_TypesWithLength_ReturnsTrue(string type, bool expectedResult, int expectedLength)
    {
        // Act
        bool result = AbiTypes.HasLengthSuffix(type, out int length);

        // Assert
        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(expectedLength, length);
    }

    [TestMethod]
    [DataRow("uint", false, 0)]
    [DataRow("int", false, 0)]
    [DataRow("bytes", false, 0)]
    [DataRow("string", false, 0)]
    [DataRow("address", false, 0)]
    [DataRow("bool", false, 0)]
    [DataRow("uint256[]", false, 0)]
    [DataRow("bytes32[]", false, 0)]
    [DataRow("address[]", false, 0)]
    public void HasLengthSuffix_TypesWithoutLength_ReturnsFalse(string type, bool expectedResult, int expectedLength)
    {
        // Act
        bool result = AbiTypes.HasLengthSuffix(type, out int length);

        // Assert
        Assert.AreEqual(expectedResult, result);
        Assert.AreEqual(expectedLength, length);
    }

    #endregion

    #region TryGetArrayOuterLength Tests

    [TestMethod]
    [DataRow("uint256[3]", true, 3)]
    [DataRow("uint256[10]", true, 10)]
    [DataRow("bytes32[5]", true, 5)]
    [DataRow("address[7]", true, 7)]
    [DataRow("bool[1]", true, 1)]
    [DataRow("uint256[3][5]", true, 5)]
    [DataRow("uint256[][5]", true, 5)]
    public void TryGetArrayOuterLength_FixedArrays_ReturnsExpectedLength(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayOuterLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    [TestMethod]
    [DataRow("uint256[]", true, -1)]
    [DataRow("bytes32[]", true, -1)]
    [DataRow("address[]", true, -1)]
    [DataRow("bool[]", true, -1)]
    [DataRow("uint256[][3]", true, 3)]
    [DataRow("uint256[3][]", true, -1)]
    public void TryGetArrayOuterLength_DynamicArrays_ReturnsDynamicLength(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayOuterLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    [TestMethod]
    [DataRow("uint256", false, 0)]
    [DataRow("bytes32", false, 0)]
    [DataRow("address", false, 0)]
    [DataRow("bool", false, 0)]
    [DataRow("string", false, 0)]
    [DataRow("bytes", false, 0)]
    public void TryGetArrayOuterLength_NonArrayTypes_ReturnsFalse(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayOuterLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    #endregion

    #region TryGetArrayMultiLength Tests

    [TestMethod]
    [DataRow("uint256[2]", true, 2)]
    [DataRow("uint256[2][3]", true, 6)]
    [DataRow("uint256[2][3][4]", true, 24)]
    [DataRow("bytes32[5][2]", true, 10)]
    [DataRow("address[3][2][1]", true, 6)]
    public void TryGetArrayMultiLength_FixedArrays_ReturnsExpectedLength(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayMultiLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    [TestMethod]
    [DataRow("uint256[]", true, -1)]
    [DataRow("uint256[2][]", true, -1)]
    [DataRow("uint256[][2]", true, -1)]
    [DataRow("uint256[2][3][]", true, -1)]
    [DataRow("uint256[][3][4]", true, -1)]
    public void TryGetArrayMultiLength_DynamicArrays_ReturnsDynamicLength(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayMultiLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    [TestMethod]
    [DataRow("uint256", false, 0)]
    [DataRow("bytes32", false, 0)]
    [DataRow("address", false, 0)]
    [DataRow("bool", false, 0)]
    [DataRow("string", false, 0)]
    [DataRow("bytes", false, 0)]
    public void TryGetArrayMultiLength_NonArrayTypes_ReturnsFalse(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayMultiLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    #endregion

    #region TryGetArrayBaseType Tests

    [TestMethod]
    [DataRow("uint256[]", true, "uint256")]
    [DataRow("uint256[3]", true, "uint256")]
    [DataRow("bytes32[]", true, "bytes32")]
    [DataRow("address[5]", true, "address")]
    [DataRow("bool[10]", true, "bool")]
    [DataRow("string[]", true, "string")]
    [DataRow("uint256[][3]", true, "uint256")]
    [DataRow("uint256[2][3]", true, "uint256")]
    public void TryGetArrayBaseType_ArrayTypes_ReturnsExpectedBaseType(string type, bool expectedSuccess, string expectedBaseType)
    {
        // Act
        bool success = AbiTypes.TryGetArrayBaseType(type, out string? baseType);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedBaseType, baseType);
    }

    [TestMethod]
    [DataRow("uint256", false, null)]
    [DataRow("bytes32", false, null)]
    [DataRow("address", false, null)]
    [DataRow("bool", false, null)]
    [DataRow("string", false, null)]
    [DataRow("bytes", false, null)]
    public void TryGetArrayBaseType_NonArrayTypes_ReturnsFalse(string type, bool expectedSuccess, string? expectedBaseType)
    {
        // Act
        bool success = AbiTypes.TryGetArrayBaseType(type, out string? baseType);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedBaseType, baseType);
    }

    #endregion

    #region IsValidBaseType Tests

    [TestMethod]
    [DataRow("uint8")]
    [DataRow("uint16")]
    [DataRow("uint256")]
    [DataRow("int8")]
    [DataRow("int256")]
    [DataRow("bytes1")]
    [DataRow("bytes32")]
    [DataRow("uint")]
    [DataRow("int")]
    [DataRow("bytes")]
    [DataRow("string")]
    [DataRow("address")]
    [DataRow("bool")]
    [DataRow("byte")]
    public void IsValidBaseType_ValidBaseTypes_ReturnsTrue(string type)
    {
        // Act
        bool result = AbiTypes.IsValidBaseType(type);

        // Assert
        Assert.IsTrue(result, $"Type '{type}' should be a valid base type");
    }

    [TestMethod]
    [DataRow("uint0")]
    [DataRow("uint7")]
    [DataRow("uint264")]
    [DataRow("int0")]
    [DataRow("int7")]
    [DataRow("int264")]
    [DataRow("bytes0")]
    [DataRow("bytes33")]
    [DataRow("notAType")]
    [DataRow("uint256[]")]
    [DataRow("bytes32[5]")]
    [DataRow("address[10]")]
    [DataRow("(uint256,bool)")]
    public void IsValidBaseType_InvalidBaseTypes_ReturnsFalse(string type)
    {
        // Act
        bool result = AbiTypes.IsValidBaseType(type);

        // Assert
        Assert.IsFalse(result, $"Type '{type}' should not be a valid base type");
    }

    #endregion

    #region IsTuple Tests

    [TestMethod]
    [DataRow("(uint256,bool)")]
    [DataRow("(uint8,string,address)")]
    [DataRow("(uint256,bytes32,bool[])")]
    [DataRow("(uint256,(string,bool))")]
    [DataRow("(uint256,uint256[])")]
    public void IsTuple_ValidTupleTypes_ReturnsTrue(string type)
    {
        // Act
        bool result = AbiTypes.IsTuple(type, includeArrays: false);

        // Assert
        Assert.IsTrue(result, $"Type '{type}' should be recognized as a tuple");
    }

    [TestMethod]
    [DataRow("(uint256,bool)[]")]
    [DataRow("(uint8,string,address)[2]")]
    [DataRow("(uint256,bytes32,bool[])[][2]")]
    [DataRow("(uint256,(string,bool))[][2]")]
    public void IsTupleArray_ValidTupleArrayTypes_ReturnsTrue(string type)
    {
        // Act
        bool result = AbiTypes.IsTuple(type, includeArrays: true);

        // Assert
        Assert.IsTrue(result, $"Type '{type}' should be recognized as a tuple");
    }

    [TestMethod]
    [DataRow("uint256")]
    [DataRow("bytes32")]
    [DataRow("address")]
    [DataRow("bool")]
    [DataRow("string")]
    [DataRow("bytes")]
    [DataRow("uint256[]")]
    [DataRow("(uint256,bool")]
    [DataRow("uint256,bool)")]
    public void IsTuple_NonTupleTypes_ReturnsFalse(string type)
    {
        // Act
        bool result = AbiTypes.IsTuple(type, includeArrays: false);

        // Assert
        Assert.IsFalse(result, $"Type '{type}' should not be recognized as a tuple");
    }

    #endregion

    #region Additional TryGetCanonicalType Tests

    [TestMethod]
    [DataRow("((address,bool)[],string)[]", "((address,bool)[],string)[]")]
    [DataRow("((address,bool)[2],string)[]", "((address,bool)[2],string)[]")]
    [DataRow("((address,bool)[],string)[3]", "((address,bool)[],string)[3]")]
    [DataRow("((address,bool)[2],string)[3]", "((address,bool)[2],string)[3]")]
    [DataRow("((address,bool)[2][3],string)[5]", "((address,bool)[2][3],string)[5]")]
    [DataRow("(uint,(address,bool)[3])[2]", "(uint256,(address,bool)[3])[2]")]
    [DataRow("(uint,(address,bool)[])[2][]", "(uint256,(address,bool)[])[2][]")]
    [DataRow("uint[2][3]", "uint256[2][3]")]
    [DataRow("uint[][3]", "uint256[][3]")]
    [DataRow("uint[2][]", "uint256[2][]")]
    [DataRow("uint[][]", "uint256[][]")]
    [DataRow("(uint,bool)[2][3]", "(uint256,bool)[2][3]")]
    [DataRow("(uint,bool)[][3]", "(uint256,bool)[][3]")]
    [DataRow("(uint,bool)[2][]", "(uint256,bool)[2][]")]
    [DataRow("(uint,bool)[][]", "(uint256,bool)[][]")]
    public void TryGetCanonicalType_ComplexArrayTypes_ReturnsCanonicalType(string input, string expected)
    {
        // Act
        bool success = AbiTypes.TryGetCanonicalType(input, out var canonicalType);

        // Assert
        Assert.IsTrue(success, $"Failed to get canonical type for {input}");
        Assert.AreEqual(expected, canonicalType, $"Canonical type for {input} should be {expected}");
    }

    #endregion

    #region Simple Tuple Arrays Tests

    [TestMethod]
    [DataRow("(uint256,bool)[]")]
    [DataRow("(address,uint256)[5]")]
    [DataRow("(string,bytes32)[][]")]
    [DataRow("(uint8,uint16,uint32)[3][2]")]
    public void IsValidType_SimpleTupleArrays_ReturnsTrue(string type)
    {
        Assert.IsTrue(AbiTypes.IsValidType(type), $"Type '{type}' should be valid");
    }

    [TestMethod]
    [DataRow("(uint256,bool)[]", "(uint256,bool)[]")]
    [DataRow("(address,uint256)[5]", "(address,uint256)[5]")]
    [DataRow("(string,bytes32)[][]", "(string,bytes32)[][]")]
    [DataRow("(uint8,uint16,uint32)[3][2]", "(uint8,uint16,uint32)[3][2]")]
    public void TryGetCanonicalType_SimpleTupleArrays_ReturnsCanonicalType(string input, string expected)
    {
        // Act
        bool success = AbiTypes.TryGetCanonicalType(input, out var canonicalType);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(expected, canonicalType);
    }

    [TestMethod]
    [DataRow("(uint256,bool)[]", typeof(Dictionary<string, object?>[]))]
    [DataRow("(address,uint256)[5]", typeof(Dictionary<string, object?>[]))]
    [DataRow("(string,bytes32)[][]", typeof(Dictionary<string, object?>[][]))]
    [DataRow("(uint8,uint16,uint32)[3][2]", typeof(Dictionary<string, object?>[][]))]
    public void TryGetDefaultClrType_SimpleTupleArrays_ReturnsExpectedType(string abiType, Type expectedType)
    {
        // Act
        bool success = AbiTypes.TryGetDefaultClrType(abiType, out Type actualType);

        // Assert
        Assert.IsTrue(success, $"Failed to get CLR type for {abiType}");
        Assert.AreEqual(expectedType, actualType, $"CLR type for {abiType} should be {expectedType.FullName}");
    }

    [TestMethod]
    [DataRow("(uint256,bool)[]", true)]
    [DataRow("(address,uint256)[5]", false)]
    [DataRow("(string,bytes32)[][]", true)]
    [DataRow("(uint8,uint16,uint32)[3][2]", false)]
    public void IsDynamic_SimpleTupleArrays_ReturnsExpectedResult(string type, bool expected)
    {
        // Act
        bool result = AbiTypes.IsDynamic(type);

        // Assert
        Assert.AreEqual(expected, result, $"IsDynamic for '{type}' should return {expected}");
    }

    [TestMethod]
    [DataRow("(uint256,bool)[]", true, -1)]
    [DataRow("(address,uint256)[5]", true, 5)]
    [DataRow("(string,bytes32)[][]", true, -1)]
    [DataRow("(uint8,uint16,uint32)[3][2]", true, 2)]
    public void TryGetArrayOuterLength_SimpleTupleArrays_ReturnsExpectedLength(string type, bool expectedSuccess, int expectedLength)
    {
        // Act
        bool success = AbiTypes.TryGetArrayOuterLength(type, out int length);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedLength, length);
    }

    [TestMethod]
    [DataRow("(uint256,bool)[]", true, "(uint256,bool)")]
    [DataRow("(address,uint256)[5]", true, "(address,uint256)")]
    [DataRow("(string,bytes32)[][]", true, "(string,bytes32)")]
    [DataRow("(uint8,uint16,uint32)[3][2]", true, "(uint8,uint16,uint32)")]
    public void TryGetArrayBaseType_SimpleTupleArrays_ReturnsExpectedBaseType(string type, bool expectedSuccess, string expectedBaseType)
    {
        // Act
        bool success = AbiTypes.TryGetArrayBaseType(type, out string? baseType);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedBaseType, baseType);
    }

    [TestMethod]
    [DataRow("(uint8[],uint16,uint32)[3][2]", true, "(uint8[],uint16,uint32)")]
    [DataRow("((address,bool)[],string)[]", true, "((address,bool)[],string)")]
    [DataRow("((address[],bool)[],string)[]", true, "((address[],bool)[],string)")]
    public void TryGetArrayBaseType_TupleArrays_ReturnsExpectedBaseType(string type, bool expectedSuccess, string expectedBaseType)
    {
        // Act
        bool success = AbiTypes.TryGetArrayBaseType(type, out string? baseType);

        // Assert
        Assert.AreEqual(expectedSuccess, success);
        Assert.AreEqual(expectedBaseType, baseType);
    }
    #endregion

    #region TryGetArrayDimensions Complex Types Tests

    [TestMethod]
    [DataRow("((address,bool)[],string)[]", "-1")]
    [DataRow("((address,bool)[2],string)[]", "-1")]
    [DataRow("((address,bool)[],string)[3]", "3")]
    [DataRow("((address,bool)[2],string)[3]", "3")]
    [DataRow("((address,bool)[2][3],string)[5]", "5")]
    [DataRow("(uint256,(address,bool)[3])[2]", "2")]
    [DataRow("(uint256,(address,bool)[])[2][]", "-1,2")]
    public void TryGetArrayDimensions_ComplexNestedTypes_ReturnsExpectedDimensions(string type, string expectedDimensions)
    {
        // Act
        bool success = AbiTypes.TryGetArrayDimensions(type, out var dimensions);

        // Assert
        Assert.IsTrue(success, $"Failed to get dimensions for {type}");
        Assert.IsNotNull(dimensions, $"Dimensions should not be null for {type}");
        Assert.AreEqual(expectedDimensions, string.Join(",", dimensions), $"Dimensions for {type} should be {expectedDimensions}");
    }

    [TestMethod]
    [DataRow("uint256[2][3]", "3,2")]
    [DataRow("uint256[][3]", "3,-1")]
    [DataRow("uint256[2][]", "-1,2")]
    [DataRow("uint256[][]", "-1,-1")]
    [DataRow("(uint256,bool)[2][3]", "3,2")]
    [DataRow("(uint256,bool)[][3]", "3,-1")]
    [DataRow("(uint256,bool)[2][]", "-1,2")]
    [DataRow("(uint256,bool)[][]", "-1,-1")]
    [DataRow("((uint256,bool)[2],string)[3][4]", "4,3")]
    [DataRow("((uint256,bool)[],string)[3][4]", "4,3")]
    [DataRow("((uint256,bool)[2],string)[][4]", "4,-1")]
    [DataRow("((uint256,bool)[],string)[][4]", "4,-1")]
    [DataRow("((uint256,bool)[2],string)[3][]", "-1,3")]
    [DataRow("((uint256,bool)[],string)[3][]", "-1,3")]
    [DataRow("((uint256,bool)[2],string)[][]", "-1,-1")]
    [DataRow("((uint256,bool)[],string)[][]", "-1,-1")]
    public void TryGetArrayDimensions_MultiDimensionalArrays_ReturnsExpectedDimensions(string type, string expectedDimensions)
    {
        // Act
        bool success = AbiTypes.TryGetArrayDimensions(type, out var dimensions);

        // Assert
        Assert.IsTrue(success, $"Failed to get dimensions for {type}");
        Assert.IsNotNull(dimensions, $"Dimensions should not be null for {type}");
        Assert.AreEqual(expectedDimensions, string.Join(",", dimensions), $"Dimensions for {type} should be {expectedDimensions}");
    }

    #endregion

    #region CanBePacked Tests

    [TestMethod]
    [DataRow("uint8", true)]       // Types shorter than 32 bytes are concatenated directly
    [DataRow("uint16", true)]
    [DataRow("uint256", true)]     // All basic types can be packed
    [DataRow("int8", true)]
    [DataRow("int256", true)]
    [DataRow("bytes1", true)]
    [DataRow("bytes32", true)]
    [DataRow("bool", true)]
    [DataRow("address", true)]
    [DataRow("string", true)]      // Dynamic types are encoded in-place without length
    [DataRow("bytes", true)]
    [DataRow("uint8[]", false)]    // Arrays are not directly packable (elements are padded)
    [DataRow("bytes1[]", false)]
    [DataRow("string[]", false)]
    [DataRow("uint256[]", false)]
    [DataRow("(uint8,uint16)", false)]  // Structs are not supported in packed mode
    [DataRow("(uint256,bool)", false)]
    [DataRow("(string,bytes)", false)]
    [DataRow("uint8[2]", false)]   // Fixed arrays are still arrays (elements are padded)
    [DataRow("uint256[2]", false)]
    [DataRow("bytes1[3]", false)]
    [DataRow("(uint8,bytes1)[2]", false)]  // Arrays of structs are not supported
    [DataRow("uint8[][2]", false)] // Nested arrays are not supported
    [DataRow("uint8[2][]", false)]
    public void CanBePacked_ReturnsExpectedResult(string type, bool expected)
    {
        // Act
        bool result = AbiTypes.CanBePacked(type);

        // Assert
        Assert.AreEqual(expected, result, $"Type '{type}' should {(expected ? "" : "not ")}be packable");
    }

    #endregion

    #region IsPackingSupported Tests

    [TestMethod]
    [DataRow("uint8", true)]       // Basic types are supported
    [DataRow("uint16", true)]
    [DataRow("uint256", true)]
    [DataRow("int8", true)]
    [DataRow("int256", true)]
    [DataRow("bytes1", true)]
    [DataRow("bytes32", true)]
    [DataRow("bool", true)]
    [DataRow("address", true)]
    [DataRow("string", true)]      // Dynamic types are supported
    [DataRow("bytes", true)]
    [DataRow("uint8[]", true)]     // Simple arrays are supported
    [DataRow("bytes1[]", true)]
    [DataRow("string[]", true)]
    [DataRow("uint256[]", true)]
    [DataRow("uint8[2]", true)]    // Fixed arrays are supported
    [DataRow("uint256[2]", true)]
    [DataRow("bytes1[3]", true)]
    [DataRow("(uint8,uint16)", false)]  // Structs are not supported
    [DataRow("(uint256,bool)", false)]
    [DataRow("(string,bytes)", false)]
    [DataRow("(uint8,bytes1)[2]", false)]  // Arrays of structs are not supported
    [DataRow("uint8[][2]", false)] // Nested arrays are not supported
    [DataRow("uint8[2][]", false)] // Nested arrays are not supported
    [DataRow("uint8[][]", false)]  // Nested arrays are not supported
    [DataRow("string[][]", false)] // Nested arrays are not supported
    public void IsPackingSupported_ReturnsExpectedResult(string type, bool expected)
    {
        // Act
        bool result = AbiTypes.IsPackingSupported(type);

        // Assert
        Assert.AreEqual(expected, result, $"Type '{type}' should {(expected ? "" : "not ")}be supported in packed encoding");
    }

    #endregion
}