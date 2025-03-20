using System;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.Transactions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;

namespace Evoq.Ethereum.Tests.Ethereum.Crypto
{
    [TestClass]
    public class RsvSignatureTests
    {
        [TestMethod]
        public void Constructor_SetsProperties()
        {
            // Arrange
            var v = new BigInteger("28");
            var r = new BigInteger("123456789");
            var s = new BigInteger("987654321");

            // Act
            var signature = new RsvSignature(v, r, s);

            // Assert
            Assert.AreEqual(v, signature.V);
            Assert.AreEqual(r, signature.R);
            Assert.AreEqual(s, signature.S);
        }

        [TestMethod]
        public void GetRecoveryId_LegacySignature_ReturnsCorrectValue()
        {
            // Arrange
            var chainId = new BigInteger("1"); // Ethereum mainnet

            // Legacy V values (27 and 28)
            var signature27 = new RsvSignature(new BigInteger("27"), BigInteger.One, BigInteger.One);
            var signature28 = new RsvSignature(new BigInteger("28"), BigInteger.One, BigInteger.One);

            // Act
            var recoveryId27 = signature27.GetRecoveryId(chainId);
            var recoveryId28 = signature28.GetRecoveryId(chainId);

            // Assert
            Assert.AreEqual(0, recoveryId27);
            Assert.AreEqual(1, recoveryId28);
        }

        [TestMethod]
        public void GetRecoveryId_EIP155Signature_ReturnsCorrectValue()
        {
            // Arrange
            var chainId = new BigInteger("1"); // Ethereum mainnet

            // EIP-155 V values for Ethereum mainnet (37 and 38)
            var signature37 = new RsvSignature(new BigInteger("37"), BigInteger.One, BigInteger.One);
            var signature38 = new RsvSignature(new BigInteger("38"), BigInteger.One, BigInteger.One);

            // Act
            var recoveryId37 = signature37.GetRecoveryId(chainId);
            var recoveryId38 = signature38.GetRecoveryId(chainId);

            // Assert
            Assert.AreEqual(0, recoveryId37);
            Assert.AreEqual(1, recoveryId38);
        }

        [TestMethod]
        public void HasEIP155ReplayProtection_EIP155Signature_ReturnsTrue()
        {
            // Arrange
            var chainId = new BigInteger("1"); // Ethereum mainnet

            // EIP-155 V values for Ethereum mainnet (37 and 38)
            var signature37 = new RsvSignature(new BigInteger("37"), BigInteger.One, BigInteger.One);
            var signature38 = new RsvSignature(new BigInteger("38"), BigInteger.One, BigInteger.One);

            // Act
            var hasProtection37 = signature37.HasEIP155ReplayProtection(chainId);
            var hasProtection38 = signature38.HasEIP155ReplayProtection(chainId);

            // Assert
            Assert.IsTrue(hasProtection37);
            Assert.IsTrue(hasProtection38);
        }

        [TestMethod]
        public void HasEIP155ReplayProtection_LegacySignature_ReturnsFalse()
        {
            // Arrange
            var chainId = new BigInteger("1"); // Ethereum mainnet

            // Legacy V values (27 and 28)
            var signature27 = new RsvSignature(new BigInteger("27"), BigInteger.One, BigInteger.One);
            var signature28 = new RsvSignature(new BigInteger("28"), BigInteger.One, BigInteger.One);

            // Act
            var hasProtection27 = signature27.HasEIP155ReplayProtection(chainId);
            var hasProtection28 = signature28.HasEIP155ReplayProtection(chainId);

            // Assert
            Assert.IsFalse(hasProtection27);
            Assert.IsFalse(hasProtection28);
        }

        [TestMethod]
        public void GetYParity_LegacySignature_ReturnsCorrectValue()
        {
            // Arrange
            // Legacy V values (27 and 28)
            var signature27 = new RsvSignature(new BigInteger("27"), BigInteger.One, BigInteger.One);
            var signature28 = new RsvSignature(new BigInteger("28"), BigInteger.One, BigInteger.One);

            // Act
            var yParity27 = signature27.GetYParity();
            var yParity28 = signature28.GetYParity();

            // Assert
            Assert.AreEqual((ulong)0, yParity27);
            Assert.AreEqual((ulong)1, yParity28);
        }

        [TestMethod]
        public void GetYParity_EIP155Signature_ReturnsCorrectValue()
        {
            // Arrange
            // EIP-155 V values for Ethereum mainnet (37 and 38)
            var signature37 = new RsvSignature(new BigInteger("37"), BigInteger.One, BigInteger.One);
            var signature38 = new RsvSignature(new BigInteger("38"), BigInteger.One, BigInteger.One);

            // Act
            var yParity37 = signature37.GetYParity();
            var yParity38 = signature38.GetYParity();

            // Assert
            Assert.AreEqual((ulong)0, yParity37);
            Assert.AreEqual((ulong)1, yParity38);
        }

        [TestMethod]
        public void GetYParity_WithFeatures_EIP1559Transaction_ReturnsCorrectValue()
        {
            // Arrange
            // Create signatures with different V values
            var signature27 = new RsvSignature(new BigInteger("27"), BigInteger.One, BigInteger.One);
            var signature28 = new RsvSignature(new BigInteger("28"), BigInteger.One, BigInteger.One);
            var signature37 = new RsvSignature(new BigInteger("37"), BigInteger.One, BigInteger.One);
            var signature38 = new RsvSignature(new BigInteger("38"), BigInteger.One, BigInteger.One);
            var signature0 = new RsvSignature(BigInteger.Zero, BigInteger.One, BigInteger.One);
            var signature1 = new RsvSignature(BigInteger.One, BigInteger.One, BigInteger.One);

            // Create a mock EIP-1559 transaction
            var eip1559Tx = new TransactionType2(
                chainId: 1,
                nonce: 0,
                maxPriorityFeePerGas: BigInteger.One,
                maxFeePerGas: BigInteger.One,
                gasLimit: 21000,
                to: new byte[20],
                value: BigInteger.Zero,
                data: new byte[0],
                accessList: null,
                signature: null
            );

            // Act
            var yParity27 = signature27.GetYParity(eip1559Tx);
            var yParity28 = signature28.GetYParity(eip1559Tx);
            var yParity37 = signature37.GetYParity(eip1559Tx);
            var yParity38 = signature38.GetYParity(eip1559Tx);
            var yParity0 = signature0.GetYParity(eip1559Tx);
            var yParity1 = signature1.GetYParity(eip1559Tx);

            // Assert
            // For EIP-1559 transactions:
            // - V=27 should give y-parity=0
            // - V=28 should give y-parity=1
            // - V=37 should give y-parity=0
            // - V=38 should give y-parity=1
            // - V=0 should give y-parity=0
            // - V=1 should give y-parity=1
            Assert.AreEqual((ulong)0, yParity27);
            Assert.AreEqual((ulong)1, yParity28);
            Assert.AreEqual((ulong)0, yParity37);
            Assert.AreEqual((ulong)1, yParity38);
            Assert.AreEqual((ulong)0, yParity0);
            Assert.AreEqual((ulong)1, yParity1);
        }

        [TestMethod]
        public void GetYParity_WithFeatures_LegacyTransaction_ReturnsCorrectValue()
        {
            // Arrange
            // Create signatures with different V values
            var signature27 = new RsvSignature(new BigInteger("27"), BigInteger.One, BigInteger.One);
            var signature28 = new RsvSignature(new BigInteger("28"), BigInteger.One, BigInteger.One);
            var signature37 = new RsvSignature(new BigInteger("37"), BigInteger.One, BigInteger.One);
            var signature38 = new RsvSignature(new BigInteger("38"), BigInteger.One, BigInteger.One);

            // Create a mock legacy transaction
            var legacyTx = new TransactionType0(
                nonce: 0,
                gasPrice: BigInteger.One,
                gasLimit: 21000,
                to: new byte[20],
                value: BigInteger.Zero,
                data: new byte[0],
                signature: null
            );

            // Act
            var yParity27 = signature27.GetYParity(legacyTx);
            var yParity28 = signature28.GetYParity(legacyTx);
            var yParity37 = signature37.GetYParity(legacyTx);
            var yParity38 = signature38.GetYParity(legacyTx);

            // Assert
            // For legacy transactions, the method should check if V is odd or even
            // - V=27 is odd, so y-parity=1
            // - V=28 is even, so y-parity=0
            // - V=37 is odd, so y-parity=1
            // - V=38 is even, so y-parity=0
            Assert.AreEqual((ulong)1, yParity27);
            Assert.AreEqual((ulong)0, yParity28);
            Assert.AreEqual((ulong)1, yParity37);
            Assert.AreEqual((ulong)0, yParity38);
        }

        [TestMethod]
        public void GetYParity_SpecificTestCase_ReturnsExpectedValue()
        {
            // This test is for the specific case that was failing in the RLP encoder

            // Arrange
            // Create a signature with the V value from the failing test case
            // Assuming the V value was 28 (based on the expected y-parity being 1)
            var signature = new RsvSignature(new BigInteger("28"),
                new BigInteger("0102030405060708090a0b0c0d0e0f101112131415161718191a1b1c1d1e1f20", 16),
                new BigInteger("1234567890abcdef", 16));

            // Create a mock EIP-1559 transaction
            var eip1559Tx = new TransactionType2(
                chainId: 1,
                nonce: 123,
                maxPriorityFeePerGas: new BigInteger("77359400", 16),
                maxFeePerGas: new BigInteger("0ba43b7400", 16),
                gasLimit: 21000,
                to: new byte[20] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0x10, 0x11, 0x12, 0x13, 0x14 },
                value: new BigInteger("0de0b6b3a7640000", 16),
                data: new byte[] { 0xca, 0xfe, 0xba, 0xbe },
                accessList: null,
                signature: signature
            );

            // Act
            var yParity = signature.GetYParity(eip1559Tx);

            // Assert
            // The expected y-parity is 1 based on the hex string comparison
            Assert.AreEqual((ulong)1, yParity);

            // Also test the direct Constants.VToYParity method
            var yParityFromConstants = Signing.VToYParity(signature.V);
            Assert.AreEqual((ulong)1, yParityFromConstants);
        }

        [TestMethod]
        public void ToByteArray_ReturnsCorrect65ByteArray()
        {
            // Arrange
            var r = new BigInteger("123456789abcdef", 16);
            var s = new BigInteger("fedcba987654321", 16);
            var v = BigInteger.ValueOf(27);

            var signature = new RsvSignature(v, r, s);

            // Act
            var bytes = signature.ToByteArray();

            // Assert
            Assert.AreEqual(65, bytes.Length);

            // Extract components
            var rBytes = new byte[32];
            var sBytes = new byte[32];
            Array.Copy(bytes, 0, rBytes, 0, 32);
            Array.Copy(bytes, 32, sBytes, 0, 32);
            var vByte = bytes[64];

            // Verify R, S, V components
            Assert.AreEqual(r, new BigInteger(1, rBytes));
            Assert.AreEqual(s, new BigInteger(1, sBytes));
            Assert.AreEqual(27, vByte);
        }

        [TestMethod]
        public void ToByteArray_RoundTrip_PreservesValues()
        {
            // Arrange
            var originalBytes = new byte[65];
            for (int i = 0; i < 32; i++) originalBytes[i] = (byte)i;        // R values
            for (int i = 0; i < 32; i++) originalBytes[32 + i] = (byte)i;   // S values
            originalBytes[64] = 27;                                          // V value

            var original = RsvSignature.FromBytes(originalBytes);

            // Act
            var roundTripBytes = original.ToByteArray();
            var roundTrip = RsvSignature.FromBytes(roundTripBytes);

            // Assert
            Assert.AreEqual(original.R, roundTrip.R);
            Assert.AreEqual(original.S, roundTrip.S);
            Assert.AreEqual(original.V, roundTrip.V);
            CollectionAssert.AreEqual(originalBytes, roundTripBytes);
        }
    }
}