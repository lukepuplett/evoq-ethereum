using System;
using System.Text;
using Evoq.Ethereum.ABI.TypeEncoders;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests.Ethereum.ABI.TypeEncoders;

[TestClass]
public class StringTypeEncoderTests
{
    [TestMethod]
    public void EncodeString_ShouldPadTo32ByteBoundary()
    {
        // Arrange
        string input = "Hello, Ethereum!";
        byte[] expectedBytes = new byte[32]; // 32-byte boundary
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        Array.Copy(stringBytes, 0, expectedBytes, 0, stringBytes.Length);

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input);

        // Assert
        Assert.AreEqual(32, result.Length); // Should be padded to 32 bytes
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void EncodeString_LongerThan32Bytes_ShouldPadToMultipleOf32()
    {
        // Arrange
        string input = "This is a longer string that exceeds the 32-byte boundary and needs more padding";
        int expectedLength = ((Encoding.UTF8.GetBytes(input).Length + 31) / 32) * 32;
        byte[] expectedBytes = new byte[expectedLength];
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        Array.Copy(stringBytes, 0, expectedBytes, 0, stringBytes.Length);

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input);

        // Assert
        Assert.AreEqual(expectedLength, result.Length);
        CollectionAssert.AreEqual(expectedBytes, result);
    }

    [TestMethod]
    public void DecodeString_ShouldRemoveNullPadding()
    {
        // Arrange
        string expected = "Hello, Ethereum!";
        byte[] paddedBytes = new byte[32];
        byte[] stringBytes = Encoding.UTF8.GetBytes(expected);
        Array.Copy(stringBytes, 0, paddedBytes, 0, stringBytes.Length);

        // Act
        string result = StringTypeEncoder.DecodeString(paddedBytes);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryEncode_WithValidString_ShouldReturnTrue()
    {
        // Arrange
        var encoder = new StringTypeEncoder();
        string input = "Test string";

        // Act
        bool success = encoder.TryEncode("string", input, out byte[] encoded);

        // Assert
        Assert.IsTrue(success);
        Assert.IsTrue(encoded.Length > 0);
        Assert.AreEqual(32, encoded.Length);
    }

    [TestMethod]
    public void TryDecode_WithValidData_ShouldReturnTrue()
    {
        // Arrange
        var encoder = new StringTypeEncoder();
        string expected = "Test string";
        byte[] paddedBytes = new byte[32];
        byte[] stringBytes = Encoding.UTF8.GetBytes(expected);
        Array.Copy(stringBytes, 0, paddedBytes, 0, stringBytes.Length);

        // Act
        bool success = encoder.TryDecode("string", paddedBytes, typeof(string), out object? decoded);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNotNull(decoded);
        Assert.AreEqual(expected, decoded);
    }

    [TestMethod]
    public void RoundTrip_SimpleString_ShouldReturnOriginalValue()
    {
        // Arrange
        string original = "Hello, Ethereum!";

        // Act
        byte[] encoded = StringTypeEncoder.EncodeString(original);
        string decoded = StringTypeEncoder.DecodeString(encoded);

        // Assert
        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void RoundTrip_LongString_ShouldReturnOriginalValue()
    {
        // Arrange
        string original = "This is a longer string that exceeds the 32-byte boundary and needs more padding to test roundtrip functionality";

        // Act
        byte[] encoded = StringTypeEncoder.EncodeString(original);
        string decoded = StringTypeEncoder.DecodeString(encoded);

        // Assert
        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void RoundTrip_EmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        string original = string.Empty;

        // Act
        byte[] encoded = StringTypeEncoder.EncodeString(original);
        string decoded = StringTypeEncoder.DecodeString(encoded);

        // Assert
        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void RoundTrip_UsingTryEncodeDecode_ShouldReturnOriginalValue()
    {
        // Arrange
        var encoder = new StringTypeEncoder();
        string original = "Test roundtrip using TryEncode and TryDecode";

        // Act
        encoder.TryEncode("string", original, out byte[] encoded);
        encoder.TryDecode("string", encoded, typeof(string), out object? decoded);

        // Assert
        Assert.IsNotNull(decoded);
        Assert.AreEqual(original, decoded);
    }

    [TestMethod]
    public void EncodeString_WithDefaultLength_PadsTo32Bytes()
    {
        // Arrange
        string input = "Hello, Ethereum!";
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input); // Default length = 32

        // Assert
        Assert.AreEqual(32, result.Length, "Should be padded to 32 bytes");
        for (int i = 0; i < stringBytes.Length; i++)
        {
            Assert.AreEqual(stringBytes[i], result[i], "Original bytes should be preserved");
        }

        // Verify padding
        for (int i = stringBytes.Length; i < 32; i++)
        {
            Assert.AreEqual(0, result[i], "Padding should be zeros");
        }
    }

    [TestMethod]
    public void EncodeString_WithCustomLength_PadsToMultipleOfLength()
    {
        // Arrange
        string input = "Hello";
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        int customLength = 8;

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, customLength);

        // Assert
        Assert.AreEqual(8, result.Length, "Should be padded to 8 bytes");
        for (int i = 0; i < stringBytes.Length; i++)
        {
            Assert.AreEqual(stringBytes[i], result[i], "Original bytes should be preserved");
        }

        // Verify padding
        for (int i = stringBytes.Length; i < 8; i++)
        {
            Assert.AreEqual(0, result[i], "Padding should be zeros");
        }
    }

    [TestMethod]
    public void EncodeString_WithExactMultipleOfLength_NoExtraPadding()
    {
        // Arrange
        string input = "ABCD"; // 4 ASCII bytes
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        int customLength = 4;

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, customLength);

        // Assert
        Assert.AreEqual(4, result.Length, "Should remain 4 bytes (already a multiple of 4)");
        CollectionAssert.AreEqual(stringBytes, result, "Should be identical to input bytes");
    }

    [TestMethod]
    public void EncodeString_WithLargerInput_PadsToNextMultiple()
    {
        // Arrange
        string input = "This is a longer string"; // More than 8 bytes
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        int customLength = 8;
        int expectedLength = ((stringBytes.Length + customLength - 1) / customLength) * customLength;

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, customLength);

        // Assert
        Assert.AreEqual(expectedLength, result.Length, $"Should be padded to {expectedLength} bytes (next multiple of {customLength})");
        for (int i = 0; i < stringBytes.Length; i++)
        {
            Assert.AreEqual(stringBytes[i], result[i], "Original bytes should be preserved");
        }

        // Verify padding
        for (int i = stringBytes.Length; i < expectedLength; i++)
        {
            Assert.AreEqual(0, result[i], "Padding should be zeros");
        }
    }

    [TestMethod]
    public void EncodeString_WithNegativeOneLength_NopadPadding()
    {
        // Arrange
        string input = "Hello, Ethereum!";
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, -1);

        // Assert
        Assert.AreEqual(stringBytes.Length, result.Length, "Should not be padded");
        CollectionAssert.AreEqual(stringBytes, result, "Should be identical to input bytes");
    }

    [TestMethod]
    public void EncodeString_WithEmptyString_ReturnsEmptyOrPaddedArray()
    {
        // Arrange
        string input = string.Empty;

        // Act - with default padding
        byte[] result1 = StringTypeEncoder.EncodeString(input);

        // Act - with no padding
        byte[] result2 = StringTypeEncoder.EncodeString(input, -1);

        // Assert
        Assert.AreEqual(32, result1.Length, "With padding should be 32 bytes");
        Assert.AreEqual(0, result2.Length, "Without padding should be empty");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentNullException))]
    public void EncodeString_WithNull_ThrowsArgumentNullException()
    {
        // Act
        StringTypeEncoder.EncodeString(null);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void EncodeString_WithZeroLength_ThrowsArgumentOutOfRangeException()
    {
        // Act
        StringTypeEncoder.EncodeString("test", 0);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentOutOfRangeException))]
    public void EncodeString_WithNegativeLength_ThrowsArgumentOutOfRangeException()
    {
        // Act
        StringTypeEncoder.EncodeString("test", -2);
    }

    [TestMethod]
    public void EncodeString_WithPackedEncoding_ReturnsExactBytes()
    {
        // Arrange
        string input = "Hello, Ethereum!";
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);

        // Act - simulate packed encoding by using length = -1
        byte[] result = StringTypeEncoder.EncodeString(input, -1);

        // Assert
        Assert.AreEqual(stringBytes.Length, result.Length, "Should be exactly the same length as input bytes");
        CollectionAssert.AreEqual(stringBytes, result, "Should be identical to input bytes");
    }

    [TestMethod]
    public void EncodeString_WithLengthOne_PadsToMultipleOfOne()
    {
        // Arrange
        string input = "ABC";
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        int customLength = 1;

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, customLength);

        // Assert
        Assert.AreEqual(stringBytes.Length, result.Length, "Should remain the same length (already a multiple of 1)");
        CollectionAssert.AreEqual(stringBytes, result, "Should be identical to input bytes");
    }

    [TestMethod]
    public void EncodeString_WithUnicodeCharacters_HandlesCorrectly()
    {
        // Arrange
        string input = "Hello, 世界!"; // Unicode characters
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, -1); // No padding

        // Assert
        Assert.AreEqual(stringBytes.Length, result.Length, "Should have correct byte length for UTF-8");
        CollectionAssert.AreEqual(stringBytes, result, "Should correctly encode Unicode characters");
    }

    [TestMethod]
    public void EncodeString_WithLargeCustomLength_PadsToExactLength()
    {
        // Arrange
        string input = "Hello";
        byte[] stringBytes = Encoding.UTF8.GetBytes(input);
        int customLength = 64;

        // Act
        byte[] result = StringTypeEncoder.EncodeString(input, customLength);

        // Assert
        Assert.AreEqual(64, result.Length, "Should be padded to exactly 64 bytes");
        for (int i = 0; i < stringBytes.Length; i++)
        {
            Assert.AreEqual(stringBytes[i], result[i], "Original bytes should be preserved");
        }

        // Verify padding
        for (int i = stringBytes.Length; i < 64; i++)
        {
            Assert.AreEqual(0, result[i], "Padding should be zeros");
        }
    }
}
