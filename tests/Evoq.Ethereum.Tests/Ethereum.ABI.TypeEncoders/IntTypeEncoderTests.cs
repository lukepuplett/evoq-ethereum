using System.Numerics;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.Tests.ABI.TypeEncoders;

[TestClass]
public class IntTypeEncoderTests
{
    private readonly IntTypeEncoder _encoder = new();

    [TestMethod]
    public void TryEncode_ValidInt8Values_Succeeds()
    {
        // Arrange
        sbyte minValue = -128;
        sbyte maxValue = 127;

        // Act & Assert
        Assert.IsTrue(_encoder.TryEncode("int8", minValue, out var encodedMin));
        Assert.IsTrue(_encoder.TryEncode("int8", maxValue, out var encodedMax));

        Assert.AreEqual(32, encodedMin.Length);
        Assert.AreEqual(32, encodedMax.Length);

        // Check first byte for min value (-128)
        Assert.AreEqual(0x80, encodedMin[31]);
        // Check first byte for max value (127)
        Assert.AreEqual(0x7F, encodedMax[31]);
    }

    [TestMethod]
    public void TryEncode_ValidInt256Values_Succeeds()
    {
        // Arrange
        var value = BigInteger.Parse("-57896044618658097711785492504343953926634992332820282019728792003956564819968"); // Min int256

        // Act
        bool success = _encoder.TryEncode("int256", value, out var encoded);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(32, encoded.Length);
        Assert.AreEqual(0x80, encoded[0]); // Should start with 0x80 for min negative value
    }

    [TestMethod]
    public void TryEncode_ValueTooLargeForType_ReturnsFalse()
    {
        // int8 can only hold -128 to 127
        var tooBig = (short)128;
        var tooSmall = (short)-129;

        Assert.IsFalse(_encoder.TryEncode("int8", tooBig, out _));
        Assert.IsFalse(_encoder.TryEncode("int8", tooSmall, out _));
    }

    [TestMethod]
    public void EncodeInt_InvalidBitSizes_ThrowsArgumentException()
    {
        // Arrange
        var value = new BigInteger(123);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.EncodeInt(7, value));  // Less than 8
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.EncodeInt(9, value));  // Not multiple of 8
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.EncodeInt(257, value)); // Greater than 256
    }

    [TestMethod]
    public void EncodeInt_ValueOutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var tooLarge = BigInteger.Parse("57896044618658097711785492504343953926634992332820282019728792003956564819968"); // 2^255
        var tooSmall = BigInteger.Parse("-57896044618658097711785492504343953926634992332820282019728792003956564819969"); // -2^255 - 1

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.EncodeInt(256, tooLarge));
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.EncodeInt(256, tooSmall));
    }

    [TestMethod]
    public void EncodeInt_SmallValueInLargeType_Succeeds()
    {
        // Arrange
        var value = new BigInteger(42);

        // Act
        var encoded = IntTypeEncoder.EncodeInt(256, value);

        // Assert
        Assert.AreEqual(32, encoded.Length);
        Assert.AreEqual(42, encoded[31]); // Last byte should be 42
        // First 31 bytes should be 0
        for (int i = 0; i < 31; i++)
        {
            Assert.AreEqual(0, encoded[i]);
        }
    }

    [TestMethod]
    public void TryEncode_UnsupportedType_ReturnsFalse()
    {
        Assert.IsFalse(_encoder.TryEncode("int256", "not a number", out _));
        Assert.IsFalse(_encoder.TryEncode("int256", null, out _));
        Assert.IsFalse(_encoder.TryEncode("int256", 1.5, out _));
    }

    [TestMethod]
    public void TryEncode_UnsupportedAbiType_ReturnsFalse()
    {
        Assert.IsFalse(_encoder.TryEncode("int7", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("int257", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("uint256", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("bool", 123, out _));
    }
}