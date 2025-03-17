using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiPackedEncoderTests
{
    [TestMethod]
    public void EncodePacked_SolidityExample_MatchesExpectedOutput()
    {
        // This test verifies the example from Solidity docs:
        // int16(-1), bytes1(0x42), uint16(0x03), string("Hello, world!")
        // Expected output: 0xffff42000348656c6c6f2c20776f726c6421

        // Arrange
        var encoder = new AbiEncoderPacked();
        var parameters = AbiParameters.Parse("(int16 a, bytes1 b, uint16 c, string d)");
        var values = new Dictionary<string, object?>
        {
            { "a", (short)-1 },           // int16(-1)
            { "b", Hex.FromBytes(new byte[] { 0x42 }) }, // bytes1(0x42)
            { "c", (ushort)3 },           // uint16(0x03)
            { "d", "Hello, world!" }      // string("Hello, world!")
        };

        // Expected result from Solidity docs
        var expectedHex = "ffff42000348656c6c6f2c20776f726c6421";
        var expected = Hex.Parse(expectedHex);

        // Act
        var result = encoder.EncodeParameters(parameters, values);

        // Assert
        Assert.IsNotNull(result, "Result should not be null");
        Assert.IsNotNull(result.ToByteArray(), "Result should not be null");
        Assert.AreEqual(expectedHex, Hex.FromBytes(result.ToByteArray()), "Result should match expected hex");
        Assert.AreEqual(expected, result.ToHexStruct(), "Result should match expected hex");
    }

    [TestMethod]
    public void EncodePacked_IndividualTypes_MatchesExpectedOutput()
    {
        // Test each type individually to ensure correct packing
        var encoder = new AbiEncoderPacked();

        // int16(-1) -> 0xffff
        var int16Params = AbiParameters.Parse("(int16 value)");
        var int16Values = new Dictionary<string, object?> { { "value", (short)-1 } };
        var int16Expected = "ffff";

        // bytes1(0x42) -> 0x42
        var bytes1Params = AbiParameters.Parse("(bytes1 value)");
        var bytes1Values = new Dictionary<string, object?> { { "value", new byte[] { 0x42 } } };
        var bytes1Expected = "42";

        // uint16(0x03) -> 0x0003
        var uint16Params = AbiParameters.Parse("(uint16 value)");
        var uint16Values = new Dictionary<string, object?> { { "value", (ushort)3 } };
        var uint16Expected = "0003";

        // string("Hello, world!") -> 0x48656c6c6f2c20776f726c6421
        var stringParams = AbiParameters.Parse("(string value)");
        var stringValues = new Dictionary<string, object?> { { "value", "Hello, world!" } };
        var stringExpected = "48656c6c6f2c20776f726c6421"; // Hex of "Hello, world!" in UTF-8

        // Act
        var int16Result = encoder.EncodeParameters(int16Params, int16Values);
        var bytes1Result = encoder.EncodeParameters(bytes1Params, bytes1Values);
        var uint16Result = encoder.EncodeParameters(uint16Params, uint16Values);
        var stringResult = encoder.EncodeParameters(stringParams, stringValues);

        // Assert
        Assert.AreEqual(int16Expected, Hex.FromBytes(int16Result.ToByteArray()));
        Assert.AreEqual(bytes1Expected, Hex.FromBytes(bytes1Result.ToByteArray()));
        Assert.AreEqual(uint16Expected, Hex.FromBytes(uint16Result.ToByteArray()));
        Assert.AreEqual(stringExpected, Hex.FromBytes(stringResult.ToByteArray()));
    }

    [TestMethod]
    public void EncodePacked_CombinationOfTypes_MatchesExpectedOutput()
    {
        // Test various combinations to ensure correct packing
        var encoder = new AbiEncoderPacked();

        // Test 1: uint8(1), uint16(2) -> 0x010002
        var test1Params = AbiParameters.Parse("(uint8 a, uint16 b)");
        var test1Values = new Dictionary<string, object?> { { "a", (byte)1 }, { "b", (ushort)2 } };
        var test1Expected = "010002";

        // Test 2: bool(true), address(0x1234...), uint32(0x12345678)
        var test2Params = AbiParameters.Parse("(bool a, address b, uint32 c)");
        var test2Values = new Dictionary<string, object?>
        {
            { "a", true },
            { "b", EthereumAddress.Parse("0x1234567890123456789012345678901234567890") },
            { "c", (uint)0x12345678 }
        };
        var test2Expected = "01" + // bool true
                           "1234567890123456789012345678901234567890" + // address
                           "12345678"; // uint32

        // Test 3: bytes(0x1234), string("ABC")
        var test3Params = AbiParameters.Parse("(bytes a, string b)");
        var test3Values = new Dictionary<string, object?>
        {
            { "a", new byte[] { 0x12, 0x34 } },
            { "b", "ABC" }
        };
        var test3Expected = "1234" + // bytes
                           "414243"; // string "ABC" in UTF-8

        // Act
        var test1Result = encoder.EncodeParameters(test1Params, test1Values);
        var test2Result = encoder.EncodeParameters(test2Params, test2Values);
        var test3Result = encoder.EncodeParameters(test3Params, test3Values);

        // Assert
        Assert.AreEqual(test1Expected, Hex.FromBytes(test1Result.ToByteArray()));
        Assert.AreEqual(test2Expected, Hex.FromBytes(test2Result.ToByteArray()));
        Assert.AreEqual(test3Expected, Hex.FromBytes(test3Result.ToByteArray()));
    }

    [TestMethod]
    public void EncodePacked_DynamicTypes_MatchesExpectedOutput()
    {
        // Test dynamic types (string, bytes) in packed encoding
        var encoder = new AbiEncoderPacked();

        // Test 1: string(""), string("A"), string("AB")
        var test1Params = AbiParameters.Parse("(string a, string b, string c)");
        var test1Values = new Dictionary<string, object?>
        {
            { "a", "" },
            { "b", "A" },
            { "c", "AB" }
        };
        var test1Expected = "4142"; // "A" + "AB" in UTF-8 (empty string contributes nothing)

        // Test 2: bytes(0x), bytes(0x12), bytes(0x1234)
        var test2Params = AbiParameters.Parse("(bytes a, bytes b, bytes c)");
        var test2Values = new Dictionary<string, object?>
        {
            { "a", new byte[0] },
            { "b", new byte[] { 0x12 } },
            { "c", new byte[] { 0x12, 0x34 } }
        };
        var test2Expected = "121234"; // Empty bytes contributes nothing

        // Act
        var test1Result = encoder.EncodeParameters(test1Params, test1Values);
        var test2Result = encoder.EncodeParameters(test2Params, test2Values);

        // Assert
        Assert.AreEqual(test1Expected, Hex.FromBytes(test1Result.ToByteArray()));
        Assert.AreEqual(test2Expected, Hex.FromBytes(test2Result.ToByteArray()));
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void EncodePacked_UnsupportedTypes_ThrowsException()
    {
        // Test that unsupported types throw exceptions
        var encoder = new AbiEncoderPacked();

        // Arrays are not supported in packed encoding
        var testParams = AbiParameters.Parse("(uint8[] value)");
        var testValues = new Dictionary<string, object?> { { "value", new byte[] { 1, 2, 3 } } };

        // Act - should throw
        encoder.EncodeParameters(testParams, testValues);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void EncodePacked_NestedArrays_ThrowsException()
    {
        // Test that nested arrays throw exceptions
        var encoder = new AbiEncoderPacked();

        // Nested arrays are not supported in packed encoding
        var testParams = AbiParameters.Parse("(uint8[][] value)");
        var testValues = new Dictionary<string, object?>
        {
            { "value", new byte[][] { new byte[] { 1, 2 }, new byte[] { 3, 4 } } }
        };

        // Act - should throw
        encoder.EncodeParameters(testParams, testValues);
    }

    [TestMethod]
    [ExpectedException(typeof(InvalidOperationException))]
    public void EncodePacked_Tuples_ThrowsException()
    {
        // Test that tuples throw exceptions
        var encoder = new AbiEncoderPacked();

        // Structs/tuples are not supported in packed encoding
        var testParams = AbiParameters.Parse("((uint8,uint8) value)");
        var innerTuple = new Dictionary<string, object?> { { "0", (byte)1 }, { "1", (byte)2 } };
        var testValues = new Dictionary<string, object?> { { "value", innerTuple } };

        // Act - should throw
        encoder.EncodeParameters(testParams, testValues);
    }

    [TestMethod]
    public void EncodePacked_MixedSupportedAndUnsupportedTypes_ThrowsException()
    {
        // Test behavior when mixing supported and unsupported types
        var encoder = new AbiEncoderPacked();

        // Mix of supported and unsupported types
        var testParams = AbiParameters.Parse("(uint8 a, uint8[] b)");
        var testValues = new Dictionary<string, object?>
        {
            { "a", (byte)1 },
            { "b", new byte[] { 2, 3 } }
        };

        // Act & Assert
        try
        {
            var result = encoder.EncodeParameters(testParams, testValues);
            Assert.Fail("Expected an exception but none was thrown");
        }
        catch (InvalidOperationException)
        {
            // Expected exception
        }
    }

    [TestMethod]
    public void EncodePacked_EmptyParameters_ReturnsEmptyResult()
    {
        // Test behavior with empty parameters
        var encoder = new AbiEncoderPacked();
        var emptyParams = AbiParameters.Parse("()");
        var emptyValues = new Dictionary<string, object?>();

        // Act
        var result = encoder.EncodeParameters(emptyParams, emptyValues);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.ToByteArray());
        Assert.AreEqual(0, result.ToByteArray().Length);
    }
}
