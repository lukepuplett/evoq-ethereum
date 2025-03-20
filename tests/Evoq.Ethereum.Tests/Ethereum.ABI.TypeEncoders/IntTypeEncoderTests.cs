using System.Numerics;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI.TypeEncoders;

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
        Assert.IsFalse(_encoder.TryEncode("int256", 1.5, out _));

        Assert.ThrowsException<ArgumentNullException>(() => _encoder.TryEncode("int256", null!, out _));
    }

    [TestMethod]
    public void TryEncode_UnsupportedAbiType_ReturnsFalse()
    {
        Assert.IsFalse(_encoder.TryEncode("int7", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("int257", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("uint256", 123, out _));
        Assert.IsFalse(_encoder.TryEncode("bool", 123, out _));
    }

    [TestMethod]
    public void TryDecode_ValidInt8Values_Succeeds()
    {
        // Arrange
        sbyte minValue = -128;
        sbyte maxValue = 127;

        // Act - first encode the values
        _encoder.TryEncode("int8", minValue, out var encodedMin);
        _encoder.TryEncode("int8", maxValue, out var encodedMax);

        // Act & Assert - then decode them back
        Assert.IsTrue(_encoder.TryDecode("int8", encodedMin, typeof(sbyte), out var decodedMin));
        Assert.IsTrue(_encoder.TryDecode("int8", encodedMax, typeof(sbyte), out var decodedMax));

        Assert.AreEqual(minValue, decodedMin);
        Assert.AreEqual(maxValue, decodedMax);
    }

    [TestMethod]
    public void TryDecode_ValidInt256Values_Succeeds()
    {
        // Arrange
        var minInt256 = BigInteger.Parse("-57896044618658097711785492504343953926634992332820282019728792003956564819968"); // -2^255

        // Act - first encode
        _encoder.TryEncode("int256", minInt256, out var encoded);

        // Act & Assert - then decode
        Assert.IsTrue(_encoder.TryDecode("int256", encoded, typeof(BigInteger), out var decoded));
        Assert.AreEqual(minInt256, decoded);
    }

    [TestMethod]
    public void TryDecode_ValueOutsideTargetRange_ReturnsFalse()
    {
        // Arrange - encode a value that's outside sbyte range
        short bigValue = 128;  // Too big for sbyte (127 max)
        short smallValue = -129; // Too small for sbyte (-128 min)
        _encoder.TryEncode("int16", bigValue, out var encodedBig);
        _encoder.TryEncode("int16", smallValue, out var encodedSmall);

        // Act & Assert
        Assert.IsFalse(_encoder.TryDecode("int16", encodedBig, typeof(sbyte), out var _));
        Assert.IsFalse(_encoder.TryDecode("int16", encodedSmall, typeof(sbyte), out var _));
    }

    [TestMethod]
    public void DecodeInt_InvalidBitSizes_ThrowsArgumentException()
    {
        // Arrange
        var data = new byte[32];

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.DecodeInt(7, data));  // Less than 8
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.DecodeInt(9, data));  // Not multiple of 8
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.DecodeInt(257, data)); // Greater than 256
    }

    [TestMethod]
    public void DecodeInt_InvalidDataLength_ThrowsArgumentException()
    {
        // Arrange
        var tooShort = new byte[31];
        var tooLong = new byte[33];

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.DecodeInt(256, tooShort));
        Assert.ThrowsException<ArgumentException>(() => IntTypeEncoder.DecodeInt(256, tooLong));
    }

    [TestMethod]
    public void TryDecode_UnsupportedType_ReturnsFalse()
    {
        // Arrange
        var data = new byte[32];

        // Act & Assert
        Assert.IsFalse(_encoder.TryDecode("int256", data, typeof(string), out _));
        Assert.IsFalse(_encoder.TryDecode("int256", data, typeof(double), out _));

        Assert.ThrowsException<ArgumentNullException>(() => _encoder.TryDecode("int256", null!, typeof(int), out _));
    }

    [TestMethod]
    public void TryDecode_UnsupportedAbiType_ReturnsFalse()
    {
        // Arrange
        var data = new byte[32];

        // Act & Assert
        Assert.IsFalse(_encoder.TryDecode("int7", data, typeof(int), out _));
        Assert.IsFalse(_encoder.TryDecode("int257", data, typeof(int), out _));
        Assert.IsFalse(_encoder.TryDecode("uint256", data, typeof(int), out _));
        Assert.IsFalse(_encoder.TryDecode("bool", data, typeof(int), out _));
    }

    [TestMethod]
    public void TryDecode_RoundTripAllTypes_Succeeds()
    {
        // Test round-trip encoding/decoding for all int types
        sbyte sbyteValue = -123;
        short shortValue = -12345;
        int intValue = -1234567890;
        long longValue = -1234567890123456789;

        // SByte
        _encoder.TryEncode("int8", sbyteValue, out var encodedSbyte);
        Assert.IsTrue(_encoder.TryDecode("int8", encodedSbyte, typeof(sbyte), out var decodedSbyte));
        Assert.AreEqual(sbyteValue, decodedSbyte);

        // Short
        _encoder.TryEncode("int16", shortValue, out var encodedShort);
        Assert.IsTrue(_encoder.TryDecode("int16", encodedShort, typeof(short), out var decodedShort));
        Assert.AreEqual(shortValue, decodedShort);

        // Int
        _encoder.TryEncode("int32", intValue, out var encodedInt);
        Assert.IsTrue(_encoder.TryDecode("int32", encodedInt, typeof(int), out var decodedInt));
        Assert.AreEqual(intValue, decodedInt);

        // Long
        _encoder.TryEncode("int64", longValue, out var encodedLong);
        Assert.IsTrue(_encoder.TryDecode("int64", encodedLong, typeof(long), out var decodedLong));
        Assert.AreEqual(longValue, decodedLong);
    }

    [TestMethod]
    public void TryEncode_Int128Type_Succeeds()
    {
        // Arrange
        var value = BigInteger.Parse("-170141183460469231731687303715884105728"); // -2^127

        // Act
        bool success = _encoder.TryEncode("int128", value, out var encoded);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(32, encoded.Length);
        Assert.AreEqual(0x80, encoded[16]); // Should have 0x80 at position 16 for min negative value

        // First 16 bytes should be all 1s for sign extension
        for (int i = 0; i < 16; i++)
        {
            Assert.AreEqual(0xFF, encoded[i]);
        }

        // Bytes 17-31 should be 0
        for (int i = 17; i < 32; i++)
        {
            Assert.AreEqual(0x00, encoded[i]);
        }
    }

    [TestMethod]
    public void TryEncode_IntAlias_EqualsInt256()
    {
        // Arrange
        var value = new BigInteger(42);

        // Act
        _encoder.TryEncode("int", value, out var encodedWithAlias);
        _encoder.TryEncode("int256", value, out var encodedWithExplicit);

        // Assert
        CollectionAssert.AreEqual(encodedWithAlias, encodedWithExplicit);
    }

    [TestMethod]
    public void TryDecode_SpecialValues_Succeeds()
    {
        // Test special values: -1, 0, 1
        var negOne = new BigInteger(-1);
        var zero = new BigInteger(0);
        var one = new BigInteger(1);

        // Encode
        _encoder.TryEncode("int256", negOne, out var encodedNegOne);
        _encoder.TryEncode("int256", zero, out var encodedZero);
        _encoder.TryEncode("int256", one, out var encodedOne);

        // Decode
        _encoder.TryDecode("int256", encodedNegOne, typeof(BigInteger), out var decodedNegOne);
        _encoder.TryDecode("int256", encodedZero, typeof(BigInteger), out var decodedZero);
        _encoder.TryDecode("int256", encodedOne, typeof(BigInteger), out var decodedOne);

        // Assert
        Assert.AreEqual(negOne, decodedNegOne);
        Assert.AreEqual(zero, decodedZero);
        Assert.AreEqual(one, decodedOne);

        // Check specific byte patterns
        // -1 should be all 0xFF bytes
        for (int i = 0; i < 32; i++)
        {
            Assert.AreEqual(0xFF, encodedNegOne[i]);
        }

        // 0 should be all 0x00 bytes
        for (int i = 0; i < 32; i++)
        {
            Assert.AreEqual(0x00, encodedZero[i]);
        }

        // 1 should be 0x00 for first 31 bytes and 0x01 for the last byte
        for (int i = 0; i < 31; i++)
        {
            Assert.AreEqual(0x00, encodedOne[i]);
        }
        Assert.AreEqual(0x01, encodedOne[31]);
    }

    [TestMethod]
    public void TryDecode_CrossTypeRoundTrip_Succeeds()
    {
        // Test encoding with one type and decoding with a compatible larger type
        sbyte sbyteValue = -42;

        // Encode as int8
        _encoder.TryEncode("int8", sbyteValue, out var encoded);

        // Decode as different types
        _encoder.TryDecode("int8", encoded, typeof(short), out var asShort);
        _encoder.TryDecode("int8", encoded, typeof(int), out var asInt);
        _encoder.TryDecode("int8", encoded, typeof(long), out var asLong);
        _encoder.TryDecode("int8", encoded, typeof(BigInteger), out var asBigInt);

        // Assert all are equal to the original value
        Assert.AreEqual((short)sbyteValue, asShort);
        Assert.AreEqual((int)sbyteValue, asInt);
        Assert.AreEqual((long)sbyteValue, asLong);
        Assert.AreEqual(new BigInteger(sbyteValue), asBigInt);
    }

    [TestMethod]
    public void TryEncode_BoundaryValues_Succeeds()
    {
        // Test boundary values for int32
        int minInt32 = int.MinValue; // -2,147,483,648
        int maxInt32 = int.MaxValue; // 2,147,483,647

        // Encode
        _encoder.TryEncode("int32", minInt32, out var encodedMin);
        _encoder.TryEncode("int32", maxInt32, out var encodedMax);

        // Decode
        _encoder.TryDecode("int32", encodedMin, typeof(int), out var decodedMin);
        _encoder.TryDecode("int32", encodedMax, typeof(int), out var decodedMax);

        // Assert
        Assert.AreEqual(minInt32, decodedMin);
        Assert.AreEqual(maxInt32, decodedMax);

        // Check specific byte patterns for min value
        // First 28 bytes should be 0xFF for sign extension
        for (int i = 0; i < 28; i++)
        {
            Assert.AreEqual(0xFF, encodedMin[i]);
        }
        // Last 4 bytes should be 0x80, 0x00, 0x00, 0x00
        Assert.AreEqual(0x80, encodedMin[28]);
        Assert.AreEqual(0x00, encodedMin[29]);
        Assert.AreEqual(0x00, encodedMin[30]);
        Assert.AreEqual(0x00, encodedMin[31]);

        // Check specific byte patterns for max value
        // First 28 bytes should be 0x00
        for (int i = 0; i < 28; i++)
        {
            Assert.AreEqual(0x00, encodedMax[i]);
        }
        // Last 4 bytes should be 0x7F, 0xFF, 0xFF, 0xFF
        Assert.AreEqual(0x7F, encodedMax[28]);
        Assert.AreEqual(0xFF, encodedMax[29]);
        Assert.AreEqual(0xFF, encodedMax[30]);
        Assert.AreEqual(0xFF, encodedMax[31]);
    }

    [TestMethod]
    public void TryEncode_SignExtension_IsCorrect()
    {
        // Test that negative values are correctly sign-extended
        short negativeValue = -1; // Should be all 1s in binary

        // Encode as int16
        _encoder.TryEncode("int16", negativeValue, out var encoded);

        // First 30 bytes should be 0xFF for sign extension
        for (int i = 0; i < 30; i++)
        {
            Assert.AreEqual(0xFF, encoded[i]);
        }
        // Last 2 bytes should be 0xFF, 0xFF
        Assert.AreEqual(0xFF, encoded[30]);
        Assert.AreEqual(0xFF, encoded[31]);

        // Decode back
        _encoder.TryDecode("int16", encoded, typeof(short), out var decoded);
        Assert.AreEqual(negativeValue, decoded);
    }
}