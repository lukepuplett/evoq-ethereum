using System.Numerics;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.Tests.ABI.TypeEncoders;

[TestClass]
public class UintTypeEncoderTests
{
    private readonly UintTypeEncoder _encoder = new();

    [TestMethod]
    public void TryEncode_ValidUint8Values_Succeeds()
    {
        // Arrange
        byte minValue = 0;
        byte maxValue = 255;

        // Act & Assert
        Assert.IsTrue(_encoder.TryEncode("uint8", minValue, out var encodedMin));
        Assert.IsTrue(_encoder.TryEncode("uint8", maxValue, out var encodedMax));

        Assert.AreEqual(32, encodedMin.Length);
        Assert.AreEqual(32, encodedMax.Length);

        // Check last byte for min value (0)
        Assert.AreEqual(0x00, encodedMin[31]);
        // Check last byte for max value (255)
        Assert.AreEqual(0xFF, encodedMax[31]);
    }

    [TestMethod]
    public void TryEncode_ValidUint256Values_Succeeds()
    {
        // Arrange
        var maxUint256 = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"); // 2^256 - 1

        // Act
        bool success = _encoder.TryEncode("uint256", maxUint256, out var encoded);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(32, encoded.Length);
        // All bytes should be 0xFF for max value
        for (int i = 0; i < 32; i++)
        {
            Assert.AreEqual(0xFF, encoded[i]);
        }
    }

    [TestMethod]
    public void TryEncode_NegativeValue_ReturnsFalse()
    {
        var negativeValue = new BigInteger(-1);
        Assert.IsFalse(_encoder.TryEncode("uint256", negativeValue, out _));
    }

    [TestMethod]
    public void EncodeUint_InvalidBitSizes_ThrowsArgumentException()
    {
        // Arrange
        var value = new BigInteger(123);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.EncodeUint(7, value));  // Less than 8
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.EncodeUint(9, value));  // Not multiple of 8
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.EncodeUint(257, value)); // Greater than 256
    }

    [TestMethod]
    public void EncodeUint_ValueOutOfRange_ThrowsArgumentException()
    {
        // Arrange
        var tooLarge = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639936"); // 2^256
        var negative = new BigInteger(-1);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.EncodeUint(256, tooLarge));
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.EncodeUint(256, negative));
    }

    [TestMethod]
    public void EncodeUint_ValueTooLargeForBits_ThrowsArgumentException()
    {
        // Arrange
        var value = new BigInteger(256); // Requires 9 bits

        // Act & Assert
        // Should fail for uint8 (8 bits can only hold 0-255)
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.EncodeUint(8, value));
    }

    [TestMethod]
    public void EncodeUint_SmallValueInLargeType_Succeeds()
    {
        // Arrange
        var value = new BigInteger(42);

        // Act
        var encoded = UintTypeEncoder.EncodeUint(256, value);

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
        Assert.IsFalse(_encoder.TryEncode("uint256", "not a number", out _));
        Assert.IsFalse(_encoder.TryEncode("uint256", null, out _));
        Assert.IsFalse(_encoder.TryEncode("uint256", 1.5, out _));
    }

    [TestMethod]
    public void TryEncode_UnsupportedAbiType_ReturnsFalse()
    {
        Assert.IsFalse(_encoder.TryEncode("uint7", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("uint257", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("int256", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("bool", 123, out _));
    }

    [TestMethod]
    public void TryEncode_MaxValuesForDifferentSizes_Succeeds()
    {
        // Test max values for different uint sizes
        Assert.IsTrue(_encoder.TryEncode("uint8", byte.MaxValue, out _));
        Assert.IsTrue(_encoder.TryEncode("uint16", ushort.MaxValue, out _));
        Assert.IsTrue(_encoder.TryEncode("uint32", uint.MaxValue, out _));
        Assert.IsTrue(_encoder.TryEncode("uint64", ulong.MaxValue, out _));
    }
}
