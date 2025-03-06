using Evoq.Blockchain;
using Org.BouncyCastle.Crypto.Digests;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.Crypto;

[TestClass]
public class Secp256k1SignerTests
{
    // Expected values (deterministic per RFC 6979)
    //
    // https://github.com/ethereum/EIPs/blob/master/EIPS/eip-155.md
    //
    // (37, 18515461264373351373200002665853028612451056578545711640558177340181847433846, 46948507304638947509940763649030358759909902576025900602547168820602576006531)
    //
    private const string EIP155_TX_RLP = "0xec098504a817c800825208943535353535353535353535353535353535353535880de0b6b3a764000080018080";
    private const string EIP155_TX_HASH = "0xdaf5a779ae972f972197303d7b574746c7ef83eadac0f2791ad23db92e4c8e53";
    private const string EIP155_PRIVATE_KEY = "0x4646464646464646464646464646464646464646464646464646464646464646";
    private const string EIP155_EXPECTED_R = "18515461264373351373200002665853028612451056578545711640558177340181847433846";
    private const string EIP155_EXPECTED_S = "46948507304638947509940763649030358759909902576025900602547168820602576006531";
    private const string EIP155_EXPECTED_V = "37";

    [TestMethod]
    public void NethereumSigner_Sign_EIP155Test_ReturnsCorrectSignature()
    {
        // GOAL
        //
        // To test that the hash we produce is the same as the hash produced by the author of the EIP,
        // then we use Nethereum to sign the hash and compare the results with the EIP.

        KeccakDigest keccak = new KeccakDigest(256);

        // Arrange

        var eipRLPTransactionHex = Hex.Parse(EIP155_TX_RLP);
        var eipTransactionHashHex = Hex.Parse(EIP155_TX_HASH);
        var eipPrivateKeyHex = Hex.Parse(EIP155_PRIVATE_KEY);

        var eipRLPBytes = eipRLPTransactionHex.ToByteArray();

        var hashBytes = new byte[32];
        keccak.BlockUpdate(eipRLPBytes, 0, eipRLPBytes.Length);
        keccak.DoFinal(hashBytes, 0);
        var ourHashHex = new Hex(hashBytes);

        var chainId = 1;

        Assert.AreEqual(eipTransactionHashHex, ourHashHex, "The hash we produced does not match the expected hash from EIP155");

        var expectedR = new BigInteger(EIP155_EXPECTED_R);
        var expectedS = new BigInteger(EIP155_EXPECTED_S);
        var expectedV = new BigInteger(EIP155_EXPECTED_V);

        // Act
        var signature = NethereumSigner.Sign(eipPrivateKeyHex, eipTransactionHashHex, (ulong)chainId);

        // Assert
        Assert.AreEqual(expectedR.ToHexStruct(), signature.R, "R component does not match");
        Assert.AreEqual(expectedS.ToHexStruct(), signature.S, "S component does not match");
        Assert.AreEqual(expectedV.ToHexStruct(), signature.V, "V component does not match");
    }

    [TestMethod]
    public void Sign_EIP155Test_ReturnsCorrectSignature()
    {
        // Arrange
        var privateKey = Hex.Parse(EIP155_PRIVATE_KEY);
        var messageHash = Hex.Parse(EIP155_TX_HASH);

        var expectedR = new BigInteger(EIP155_EXPECTED_R);
        var expectedS = new BigInteger(EIP155_EXPECTED_S);
        var expectedV = new BigInteger(EIP155_EXPECTED_V);

        // Create signer and payload
        var signer = new Secp256k1Signer(privateKey.ToByteArray());
        var payload = new SigningPayload
        {
            Data = messageHash.ToByteArray(),
            IsEIP155 = true,
            ChainId = BigInteger.ValueOf(1)
        };

        // Act
        var signature = signer.Sign(payload);

        // Assert
        Assert.AreEqual(expectedR, signature.R, "R component does not match");
        Assert.AreEqual(expectedS, signature.S, "S component does not match");
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