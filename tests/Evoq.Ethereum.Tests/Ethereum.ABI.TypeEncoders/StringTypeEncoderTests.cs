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
}
