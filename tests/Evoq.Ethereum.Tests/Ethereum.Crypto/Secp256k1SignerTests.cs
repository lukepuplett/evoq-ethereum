using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Tests.Crypto;

[TestClass]
public class Secp256k1SignerTests
{
    [TestMethod]
    public void Sign_EIP155Test_ReturnsCorrectSignature()
    {
        // Arrange
        var privateKey = Hex.Parse("0x4646464646464646464646464646464646464646464646464646464646464646");

        // https://github.com/ethereum/EIPs/blob/master/EIPS/eip-155.md
        //
        // Transaction:
        // nonce = 9, gasprice = 20 * 10**9, startgas = 21000, to = 0x3535353535353535353535353535353535353535, value = 10**18, data=''
        //
        // Message Hash:
        // 9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08
        //
        var messageHash = Hex.Parse("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");

        // Expected values (deterministic per RFC 6979)
        //
        // https://github.com/ethereum/EIPs/blob/master/EIPS/eip-155.md
        //
        // (37, 18515461264373351373200002665853028612451056578545711640558177340181847433846, 46948507304638947509940763649030358759909902576025900602547168820602576006531)
        //
        var expectedR = new BigInteger("18515461264373351373200002665853028612451056578545711640558177340181847433846");
        var expectedS = new BigInteger("46948507304638947509940763649030358759909902576025900602547168820602576006531");
        var expectedV = 37;

        // Create signer and payload
        var signer = new Secp256k1Signer(privateKey.ToByteArray());
        var payload = new SigningPayload
        {
            Data = messageHash.ToByteArray(),
            IsEIP155 = true,
            ChainId = 1
        };

        // Act
        var signature = signer.Sign(payload);

        // Assert
        Assert.AreEqual(expectedR.ToHex(), signature.R, "R component does not match");
        Assert.AreEqual(expectedS.ToHex(), signature.S, "S component does not match");
        Assert.AreEqual(expectedV, signature.V, "V component does not match");
    }

    [TestMethod]
    public void Sign_InvalidPrivateKey_ThrowsArgumentException()
    {
        // Arrange
        var invalidPrivateKey = new byte[31]; // Should be 32 bytes

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new Secp256k1Signer(invalidPrivateKey));
    }

    [TestMethod]
    public void Sign_NullPrivateKey_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new Secp256k1Signer(null));
    }
}