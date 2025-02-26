using System.Numerics;
using Evoq.Ethereum.ABI;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI.TypeEncoders;

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
        Assert.IsFalse(_encoder.TryEncode("uint256", 1.5, out _));

        Assert.ThrowsException<ArgumentNullException>(() => _encoder.TryEncode("uint256", null, out _));
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

    [TestMethod]
    public void TryDecode_ValidUint8Values_Succeeds()
    {
        // Arrange
        byte minValue = 0;
        byte maxValue = 255;

        // Act - first encode the values
        _encoder.TryEncode("uint8", minValue, out var encodedMin);
        _encoder.TryEncode("uint8", maxValue, out var encodedMax);

        // Act & Assert - then decode them back
        Assert.IsTrue(_encoder.TryDecode("uint8", encodedMin, typeof(byte), out var decodedMin));
        Assert.IsTrue(_encoder.TryDecode("uint8", encodedMax, typeof(byte), out var decodedMax));

        Assert.AreEqual(minValue, decodedMin);
        Assert.AreEqual(maxValue, decodedMax);
    }

    [TestMethod]
    public void TryDecode_ValidUint256Values_Succeeds()
    {
        // Arrange
        var maxUint256 = BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"); // 2^256 - 1

        // Act - first encode
        _encoder.TryEncode("uint256", maxUint256, out var encoded);

        // Act & Assert - then decode
        Assert.IsTrue(_encoder.TryDecode("uint256", encoded, typeof(BigInteger), out var decoded));
        Assert.AreEqual(maxUint256, decoded);
    }

    [TestMethod]
    public void TryDecode_ValueTooLargeForTargetType_ReturnsFalse()
    {
        // Arrange - encode a value that's too big for byte
        ushort bigValue = 256;
        _encoder.TryEncode("uint16", bigValue, out var encoded);

        // Act & Assert - try to decode into byte (which can only hold 0-255)
        Assert.IsFalse(_encoder.TryDecode("uint16", encoded, typeof(byte), out var _));
    }

    [TestMethod]
    public void DecodeUint_InvalidBitSizes_ThrowsArgumentException()
    {
        // Arrange
        var data = new byte[32];

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.DecodeUint(7, data));  // Less than 8
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.DecodeUint(9, data));  // Not multiple of 8
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.DecodeUint(257, data)); // Greater than 256
    }

    [TestMethod]
    public void DecodeUint_InvalidDataLength_ThrowsArgumentException()
    {
        // Arrange
        var tooShort = new byte[31];
        var tooLong = new byte[33];

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.DecodeUint(256, tooShort));
        Assert.ThrowsException<ArgumentException>(() => UintTypeEncoder.DecodeUint(256, tooLong));
    }

    [TestMethod]
    public void TryDecode_UnsupportedType_ReturnsFalse()
    {
        // Arrange
        var data = new byte[32];

        // Act & Assert
        Assert.IsFalse(_encoder.TryDecode("uint256", data, typeof(string), out _));
        Assert.IsFalse(_encoder.TryDecode("uint256", data, typeof(double), out _));

        Assert.ThrowsException<ArgumentNullException>(() => _encoder.TryDecode("uint256", null, typeof(uint), out _));
    }

    [TestMethod]
    public void TryDecode_UnsupportedAbiType_ReturnsFalse()
    {
        // Arrange
        var data = new byte[32];

        // Act & Assert
        Assert.IsFalse(_encoder.TryDecode("uint7", data, typeof(uint), out _));
        Assert.IsFalse(_encoder.TryDecode("uint257", data, typeof(uint), out _));
        Assert.IsFalse(_encoder.TryDecode("int256", data, typeof(uint), out _));
        Assert.IsFalse(_encoder.TryDecode("bool", data, typeof(uint), out _));
    }

    [TestMethod]
    public void TryDecode_RoundTripAllTypes_Succeeds()
    {
        // Test round-trip encoding/decoding for all uint types
        byte byteValue = 123;
        ushort ushortValue = 12345;
        uint uintValue = 1234567890;
        ulong ulongValue = 1234567890123456789;

        // Byte
        _encoder.TryEncode("uint8", byteValue, out var encodedByte);
        Assert.IsTrue(_encoder.TryDecode("uint8", encodedByte, typeof(byte), out var decodedByte));
        Assert.AreEqual(byteValue, decodedByte);

        // UShort
        _encoder.TryEncode("uint16", ushortValue, out var encodedUshort);
        Assert.IsTrue(_encoder.TryDecode("uint16", encodedUshort, typeof(ushort), out var decodedUshort));
        Assert.AreEqual(ushortValue, decodedUshort);

        // UInt
        _encoder.TryEncode("uint32", uintValue, out var encodedUint);
        Assert.IsTrue(_encoder.TryDecode("uint32", encodedUint, typeof(uint), out var decodedUint));
        Assert.AreEqual(uintValue, decodedUint);

        // ULong
        _encoder.TryEncode("uint64", ulongValue, out var encodedUlong);
        Assert.IsTrue(_encoder.TryDecode("uint64", encodedUlong, typeof(ulong), out var decodedUlong));
        Assert.AreEqual(ulongValue, decodedUlong);
    }

    [TestMethod]
    public void TryEncode_Uint128Type_Succeeds()
    {
        // Arrange
        var value = BigInteger.Parse("340282366920938463463374607431768211455"); // 2^128 - 1 (max uint128)

        // Act
        bool success = _encoder.TryEncode("uint128", value, out var encoded);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(32, encoded.Length);

        // First 16 bytes should be 0
        for (int i = 0; i < 16; i++)
        {
            Assert.AreEqual(0x00, encoded[i]);
        }

        // Last 16 bytes should be all 0xFF for max value
        for (int i = 16; i < 32; i++)
        {
            Assert.AreEqual(0xFF, encoded[i]);
        }
    }

    [TestMethod]
    public void TryDecode_SpecialValues_Succeeds()
    {
        // Test special values: 0, 1, 2
        var zero = new BigInteger(0);
        var one = new BigInteger(1);
        var two = new BigInteger(2);

        // Encode
        _encoder.TryEncode("uint256", zero, out var encodedZero);
        _encoder.TryEncode("uint256", one, out var encodedOne);
        _encoder.TryEncode("uint256", two, out var encodedTwo);

        // Decode
        _encoder.TryDecode("uint256", encodedZero, typeof(BigInteger), out var decodedZero);
        _encoder.TryDecode("uint256", encodedOne, typeof(BigInteger), out var decodedOne);
        _encoder.TryDecode("uint256", encodedTwo, typeof(BigInteger), out var decodedTwo);

        // Assert
        Assert.AreEqual(zero, decodedZero);
        Assert.AreEqual(one, decodedOne);
        Assert.AreEqual(two, decodedTwo);

        // Check specific byte patterns
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

        // 2 should be 0x00 for first 31 bytes and 0x02 for the last byte
        for (int i = 0; i < 31; i++)
        {
            Assert.AreEqual(0x00, encodedTwo[i]);
        }
        Assert.AreEqual(0x02, encodedTwo[31]);
    }

    [TestMethod]
    public void TryDecode_CrossTypeRoundTrip_Succeeds()
    {
        // Test encoding with one type and decoding with a compatible larger type
        byte byteValue = 42;

        // Encode as uint8
        _encoder.TryEncode("uint8", byteValue, out var encoded);

        // Decode as different types
        _encoder.TryDecode("uint8", encoded, typeof(ushort), out var asUshort);
        _encoder.TryDecode("uint8", encoded, typeof(uint), out var asUint);
        _encoder.TryDecode("uint8", encoded, typeof(ulong), out var asUlong);
        _encoder.TryDecode("uint8", encoded, typeof(BigInteger), out var asBigInt);

        // Assert all are equal to the original value
        Assert.AreEqual((ushort)byteValue, asUshort);
        Assert.AreEqual((uint)byteValue, asUint);
        Assert.AreEqual((ulong)byteValue, asUlong);
        Assert.AreEqual(new BigInteger(byteValue), asBigInt);
    }

    [TestMethod]
    public void TryEncode_BoundaryValues_Succeeds()
    {
        // Test boundary values for uint32
        uint minUint32 = 0;
        uint maxUint32 = uint.MaxValue; // 4,294,967,295

        // Encode
        _encoder.TryEncode("uint32", minUint32, out var encodedMin);
        _encoder.TryEncode("uint32", maxUint32, out var encodedMax);

        // Decode
        _encoder.TryDecode("uint32", encodedMin, typeof(uint), out var decodedMin);
        _encoder.TryDecode("uint32", encodedMax, typeof(uint), out var decodedMax);

        // Assert
        Assert.AreEqual(minUint32, decodedMin);
        Assert.AreEqual(maxUint32, decodedMax);

        // Check specific byte patterns for min value (0)
        // All 32 bytes should be 0x00
        for (int i = 0; i < 32; i++)
        {
            Assert.AreEqual(0x00, encodedMin[i]);
        }

        // Check specific byte patterns for max value
        // First 28 bytes should be 0x00
        for (int i = 0; i < 28; i++)
        {
            Assert.AreEqual(0x00, encodedMax[i]);
        }
        // Last 4 bytes should be 0xFF, 0xFF, 0xFF, 0xFF
        Assert.AreEqual(0xFF, encodedMax[28]);
        Assert.AreEqual(0xFF, encodedMax[29]);
        Assert.AreEqual(0xFF, encodedMax[30]);
        Assert.AreEqual(0xFF, encodedMax[31]);
    }

    [TestMethod]
    public void TryEncode_PowersOfTwo_Succeeds()
    {
        // Test powers of two which are important boundary cases
        var one = BigInteger.Parse("1");                  // 2^0
        var twoToThe8 = BigInteger.Parse("256");          // 2^8
        var twoToThe16 = BigInteger.Parse("65536");       // 2^16
        var twoToThe32 = BigInteger.Parse("4294967296");  // 2^32

        // Encode
        _encoder.TryEncode("uint256", one, out var encodedOne);
        _encoder.TryEncode("uint256", twoToThe8, out var encoded8);
        _encoder.TryEncode("uint256", twoToThe16, out var encoded16);
        _encoder.TryEncode("uint256", twoToThe32, out var encoded32);

        // Decode
        _encoder.TryDecode("uint256", encodedOne, typeof(BigInteger), out var decodedOne);
        _encoder.TryDecode("uint256", encoded8, typeof(BigInteger), out var decoded8);
        _encoder.TryDecode("uint256", encoded16, typeof(BigInteger), out var decoded16);
        _encoder.TryDecode("uint256", encoded32, typeof(BigInteger), out var decoded32);

        // Assert
        Assert.AreEqual(one, decodedOne);
        Assert.AreEqual(twoToThe8, decoded8);
        Assert.AreEqual(twoToThe16, decoded16);
        Assert.AreEqual(twoToThe32, decoded32);

        // Check byte patterns for 2^8 (256)
        // First 30 bytes should be 0x00
        for (int i = 0; i < 30; i++)
        {
            Assert.AreEqual(0x00, encoded8[i]);
        }
        // Last 2 bytes should be 0x01, 0x00
        Assert.AreEqual(0x01, encoded8[30]);
        Assert.AreEqual(0x00, encoded8[31]);
    }

    [TestMethod]
    public void TryDecode_DecodeToSmallerType_SucceedsWhenInRange()
    {
        // Test decoding to a smaller type when the value is in range
        byte smallValue = 42;

        // Encode as uint32
        _encoder.TryEncode("uint32", smallValue, out var encoded);

        // Should be able to decode to byte since value is in range
        Assert.IsTrue(_encoder.TryDecode("uint32", encoded, typeof(byte), out var decoded));
        Assert.AreEqual(smallValue, decoded);
    }

    [TestMethod]
    public void TryDecode_RoundTripWithDifferentAbiTypes_Succeeds()
    {
        // Test encoding with one ABI type and decoding with another
        byte value = 123;

        // Encode as uint8
        _encoder.TryEncode("uint8", value, out var encoded);

        // Decode as uint16, uint32, etc.
        Assert.IsTrue(_encoder.TryDecode("uint16", encoded, typeof(byte), out var decodedAs16));
        Assert.IsTrue(_encoder.TryDecode("uint32", encoded, typeof(byte), out var decodedAs32));
        Assert.IsTrue(_encoder.TryDecode("uint64", encoded, typeof(byte), out var decodedAs64));
        Assert.IsTrue(_encoder.TryDecode("uint256", encoded, typeof(byte), out var decodedAs256));

        // All should be equal to the original value
        Assert.AreEqual(value, decodedAs16);
        Assert.AreEqual(value, decodedAs32);
        Assert.AreEqual(value, decodedAs64);
        Assert.AreEqual(value, decodedAs256);
    }

    [TestMethod]
    public void TryEncode_ValueTooLargeForUint128_ReturnsFalse()
    {
        // Arrange
        var tooLargeForUint128 = BigInteger.Parse("340282366920938463463374607431768211456"); // 2^128 (one more than max uint128)

        // Act
        bool result = _encoder.TryEncode("uint128", tooLargeForUint128, out _);

        // Assert
        Assert.IsFalse(result, "TryEncode should return false for a value too large for uint128");
    }
}
