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
}
