using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI.TypeEncoders;

[TestClass]
public class BytesTypeEncoderTests
{
    private readonly BytesTypeEncoder _encoder = new();

    [TestMethod]
    public void TryEncode_ValidBytesValues_PadsTo32Bytes()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };

        // Act
        bool success = _encoder.TryEncode("bytes", value, out var encoded);

        // Assert
        Assert.IsTrue(success, "Should succeed");
        Assert.AreEqual(32, encoded.Length, "Should be 32 bytes");
        Assert.IsTrue(value.SequenceEqual(encoded.Take(value.Length)), "Should be the same sequence");

        // Rest is 0
        for (int i = value.Length; i < 32; i++)
        {
            Assert.AreEqual(0, encoded[i], "Should be 0");
        }
    }

    [TestMethod]
    public void TryEncode_ValidBytesValuesLongerThan32Bytes_PadsTo32Bytes()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33 };

        // Act
        bool success = _encoder.TryEncode("bytes", value, out var encoded);

        // Assert
        Assert.IsTrue(success, "Should succeed");
        Assert.AreEqual(64, encoded.Length, "Should be 64 bytes");
        Assert.IsTrue(value.SequenceEqual(encoded.Take(value.Length)), "Should be the same sequence");

        // Rest is 0
        for (int i = value.Length; i < 64; i++)
        {
            Assert.AreEqual(0, encoded[i], "Should be 0");
        }
    }

    [TestMethod]
    public void EncodeBytes_WithDefaultLength_PadsTo32Bytes()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value); // Default length = 32

        // Assert
        Assert.AreEqual(32, encoded.Length, "Should be padded to 32 bytes");
        Assert.IsTrue(value.SequenceEqual(encoded.Take(value.Length)), "Original bytes should be preserved");

        // Verify padding
        for (int i = value.Length; i < 32; i++)
        {
            Assert.AreEqual(0, encoded[i], "Padding should be zeros");
        }
    }

    [TestMethod]
    public void EncodeBytes_WithCustomLength_PadsToMultipleOfLength()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3, 4, 5 };
        int customLength = 8;

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value, customLength);

        // Assert
        Assert.AreEqual(8, encoded.Length, "Should be padded to 8 bytes");
        Assert.IsTrue(value.SequenceEqual(encoded.Take(value.Length)), "Original bytes should be preserved");

        // Verify padding
        for (int i = value.Length; i < 8; i++)
        {
            Assert.AreEqual(0, encoded[i], "Padding should be zeros");
        }
    }

    [TestMethod]
    public void EncodeBytes_WithExactMultipleOfLength_NoExtraPadding()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 };
        int customLength = 4;

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value, customLength);

        // Assert
        Assert.AreEqual(8, encoded.Length, "Should remain 8 bytes (already a multiple of 4)");
        Assert.IsTrue(value.SequenceEqual(encoded), "Should be identical to input");
    }

    [TestMethod]
    public void EncodeBytes_WithLargerInput_PadsToNextMultiple()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        int customLength = 4;

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value, customLength);

        // Assert
        Assert.AreEqual(12, encoded.Length, "Should be padded to 12 bytes (next multiple of 4)");
        Assert.IsTrue(value.SequenceEqual(encoded.Take(value.Length)), "Original bytes should be preserved");

        // Verify padding
        for (int i = value.Length; i < 12; i++)
        {
            Assert.AreEqual(0, encoded[i], "Padding should be zeros");
        }
    }

    [TestMethod]
    public void EncodeBytes_WithNegativeOneLength_NopadPadding()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3, 4, 5 };

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value, -1);

        // Assert
        Assert.AreEqual(5, encoded.Length, "Should not be padded");
        Assert.IsTrue(value.SequenceEqual(encoded), "Should be identical to input");
    }

    [TestMethod]
    public void EncodeBytes_WithEmptyArray_ReturnsEmptyArray()
    {
        // Arrange
        var value = new byte[0];

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value);

        // Assert
        Assert.AreEqual(0, encoded.Length, "Should be empty");
    }

    [TestMethod]
    public void EncodeBytes_WithNull_ReturnsEmptyArray()
    {
        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(null);

        // Assert
        Assert.AreEqual(0, encoded.Length, "Should be empty");
    }

    [TestMethod]
    public void EncodeBytes_WithPackedEncoding_ReturnsExactBytes()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3, 4, 5 };

        // Act - simulate packed encoding by using length = -1
        var encoded = BytesTypeEncoder.EncodeBytes(value, -1);

        // Assert
        Assert.AreEqual(5, encoded.Length, "Should be exactly 5 bytes");
        Assert.IsTrue(value.SequenceEqual(encoded), "Should be identical to input");
    }

    [TestMethod]
    public void EncodeBytes_WithLengthOne_PadsToMultipleOfOne()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };
        int customLength = 1;

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value, customLength);

        // Assert
        Assert.AreEqual(3, encoded.Length, "Should remain 3 bytes (already a multiple of 1)");
        Assert.IsTrue(value.SequenceEqual(encoded), "Should be identical to input");
    }

    [TestMethod]
    public void EncodeBytes_WithLargeCustomLength_PadsToExactLength()
    {
        // Arrange
        var value = new byte[] { 1, 2, 3 };
        int customLength = 64;

        // Act
        var encoded = BytesTypeEncoder.EncodeBytes(value, customLength);

        // Assert
        Assert.AreEqual(64, encoded.Length, "Should be padded to exactly 64 bytes");
        Assert.IsTrue(value.SequenceEqual(encoded.Take(value.Length)), "Original bytes should be preserved");

        // Verify padding
        for (int i = value.Length; i < 64; i++)
        {
            Assert.AreEqual(0, encoded[i], "Padding should be zeros");
        }
    }
}
