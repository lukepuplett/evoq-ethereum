using System.Numerics;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiEncoderTests
{
    [TestMethod]
    public void EncodeAddress_PadsCorrectly()
    {
        var address = new EthereumAddress("0x1234567890123456789012345678901234567890");
        var encoded = AbiEncoder.EncodeAddress(address);

        Assert.AreEqual(32, encoded.Length);
        CollectionAssert.AreEqual(
            new byte[12].Concat(address.ToByteArray()).ToArray(),
            encoded);
    }

    [TestMethod]
    public void EncodeUint256_PadsCorrectly()
    {
        var value = BigInteger.Parse("1234567890");
        var encoded = AbiEncoder.EncodeUint256(value);

        Assert.AreEqual(32, encoded.Length);
        Assert.AreEqual(value, new BigInteger(encoded.Reverse().ToArray()));
    }

    [TestMethod]
    public void EncodeBool_EncodesCorrectly()
    {
        var trueEncoded = AbiEncoder.EncodeBool(true);
        var falseEncoded = AbiEncoder.EncodeBool(false);

        Assert.AreEqual(32, trueEncoded.Length);
        Assert.AreEqual(32, falseEncoded.Length);
        Assert.AreEqual(1, trueEncoded[31]);
        Assert.AreEqual(0, falseEncoded[31]);
    }
}