using System;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests.Crypto;

[TestClass]
public class Secp256k1SignerTests
{
    [TestMethod]
    public void Sign_SimplePrivateKey_ReturnsCorrectSignature()
    {
        // Arrange
        // Simple private key: 0x1 (padded to 32 bytes)
        var privateKey = Hex.Parse("0000000000000000000000000000000000000000000000000000000000000001");

        // Message Hash: SHA-256("test")
        var messageHash = Hex.Parse("9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08");

        // Expected values (deterministic per RFC 6979)
        var expectedR = Hex.Parse("4e45e16932b8af514961a1d3a1a25fdf3f4f7732e9d624c6c61548ab5fb8cd41");
        var expectedS = Hex.Parse("18160ddd7bcdc9e57f9f6c7f1f2e06e3feda2a2a6e8f8f9c087db1c1e558c4f2");
        var expectedV = 27; // This may be 27 or 28 depending on implementation

        // Create signer and sign
        var signer = new Secp256k1Signer(privateKey.ToByteArray());

        // Act
        var signature = signer.Sign(messageHash.ToByteArray());

        // Assert
        Assert.AreEqual(expectedR, new Hex(signature.R), "R component does not match");
        Assert.AreEqual(expectedS, new Hex(signature.S), "S component does not match");
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