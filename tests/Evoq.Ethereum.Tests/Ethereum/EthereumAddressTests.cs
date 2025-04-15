using System;
using Evoq.Blockchain;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests;

[TestClass]
public class EthereumAddressTests
{
    private const string ValidAddress = "0xC02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2";
    private const string ValidAddressNoPrefix = "C02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2";
    private const string ValidSignature = "0x2577724e0ef468edbd9a809baa71457d5aba8f31015356d4a0d44d6374f89bf32ad1f076275148dd2da9970ad07c4348f274da08e6b403bf108beb8b688aa38c1c";
    private const string ValidMessage = "Hello World";
    private const string VitalikAddress = "0x000000000000000000000000d8dA6BF26964aF9D7eEd9e03E53415D37aA96045";

    // 32-byte padded version of ValidAddress (first 12 bytes are zero)
    private const string PaddedValidAddressHex = "0x000000000000000000000000C02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2";

    [TestMethod]
    public void Constructor_WithValidBytes_CreatesInstance()
    {
        // Arrange
        byte[] bytes = Convert.FromHexString(ValidAddressNoPrefix);

        // Act
        var address = new EthereumAddress(bytes);

        // Assert
        Assert.AreEqual(ValidAddress, address.ToString());
    }

    [TestMethod]
    public void Constructor_WithEmptyBytes_ThrowsArgumentException()
    {
        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new EthereumAddress(Array.Empty<byte>()));
    }

    [TestMethod]
    public void Constructor_WithSingleZeroByte_CreatesZeroAddress()
    {
        // Arrange
        byte[] bytes = new byte[] { 0 };

        // Act
        var address = new EthereumAddress(bytes);

        // Assert
        Assert.IsTrue(address.IsZero);
        Assert.AreEqual("0x0000000000000000000000000000000000000000", address.ToString());
    }

    [TestMethod]
    public void Constructor_With20ZeroBytes_CreatesZeroAddress()
    {
        // Arrange
        byte[] bytes = new byte[20]; // All zeros

        // Act
        var address = new EthereumAddress(bytes);

        // Assert
        Assert.IsTrue(address.IsZero);
    }

    [TestMethod]
    public void Constructor_With32BytesPadded_ParsesCorrectly()
    {
        // Arrange
        byte[] bytes = Convert.FromHexString(PaddedValidAddressHex[2..]);

        // Act
        var address = new EthereumAddress(bytes);

        // Assert
        Assert.AreEqual(ValidAddress, address.ToString());
        Assert.AreEqual(20, address.Address.Length); // Should store only 20 bytes
    }

    [TestMethod]
    public void Constructor_With32BytesNonZeroPrefix_ThrowsArgumentException()
    {
        // Arrange
        byte[] bytes = new byte[32];
        bytes[0] = 1; // First byte is not zero
        Buffer.BlockCopy(Convert.FromHexString(ValidAddressNoPrefix), 0, bytes, 12, 20);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new EthereumAddress(bytes));
    }

    [TestMethod]
    public void Constructor_With31Bytes_ThrowsArgumentException()
    {
        // Arrange
        byte[] bytes = new byte[31]; // Wrong length, should be 20 or 32

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new EthereumAddress(bytes));
    }

    [TestMethod]
    public void Constructor_With33Bytes_ThrowsArgumentException()
    {
        // Arrange
        byte[] bytes = new byte[33]; // Wrong length, should be 20 or 32

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new EthereumAddress(bytes));
    }

    [TestMethod]
    public void Constructor_WithHexPaddedAddress_ParsesCorrectly()
    {
        // Arrange
        var paddedHex = Hex.Parse(PaddedValidAddressHex);

        // Act
        var address = new EthereumAddress(paddedHex);

        // Assert
        Assert.AreEqual(ValidAddress, address.ToString());
        Assert.AreEqual(20, address.Address.Length); // Should store only 20 bytes
    }

    [TestMethod]
    public void Constructor_WithInvalidLengthViaHex_ThrowsArgumentException()
    {
        // Arrange
        byte[] bytes = new byte[10]; // Wrong length, should be 20

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => new EthereumAddress(bytes.ToHexStruct()));
    }

    [TestMethod]
    public void Parse_WithRealEthAddress_ReturnsCorrectAddress()
    {
        // Arrange
        const string realAddress = "0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3";

        // Act
        var address = EthereumAddress.Parse(realAddress, EthereumAddressChecksum.DoNotCheck);

        // Assert
        Assert.AreEqual(realAddress, address.ToString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("\n")]
    public void Parse_WithEmptyOrWhitespace_ThrowsFormatException(string input)
    {
        // Act & Assert
        Assert.ThrowsException<FormatException>(() =>
            EthereumAddress.Parse(input, EthereumAddressChecksum.DoNotCheck));
    }

    [TestMethod]
    [DataRow("  0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3  ")]
    [DataRow("\t0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3\t")]
    [DataRow(" 0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3\n")]
    public void Parse_WithValidAddressAndWhitespace_ParsesSuccessfully(string input)
    {
        // Arrange
        const string expected = "0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3";

        // Act
        var address = EthereumAddress.Parse(input, EthereumAddressChecksum.DoNotCheck);

        // Assert
        Assert.AreEqual(expected, address.ToString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow(" ")]
    [DataRow("\t")]
    [DataRow("\n")]
    public void TryParse_WithEmptyOrWhitespace_ReturnsFalse(string input)
    {
        // Act
        bool success = EthereumAddress.TryParse(
            input, EthereumAddressChecksum.DoNotCheck, out var address);

        // Assert
        Assert.IsFalse(success);
        Assert.AreEqual(EthereumAddress.Zero, address);
    }

    [TestMethod]
    [DataRow("  0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3  ")]
    [DataRow("\t0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3\t")]
    [DataRow(" 0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3\n")]
    public void TryParse_WithValidAddressAndWhitespace_ParsesSuccessfully(string input)
    {
        // Arrange
        const string expected = "0x7F5b46a72872b3Dc8Fbb2403f4d55e6b34EdCcD3";

        // Act
        bool success = EthereumAddress.TryParse(
            input, EthereumAddressChecksum.DoNotCheck, out var address);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(expected, address.ToString());
    }

    [TestMethod]
    public void Parse_VitalikAddress_ParsesSuccessfully()
    {
        // Act
        var address = EthereumAddress.Parse(VitalikAddress, EthereumAddressChecksum.AlwaysCheck);

        // Assert
        Assert.AreEqual(VitalikAddress, address.ToPadded(64));
    }

    [TestMethod]
    public void Parse_VitalikAddressLowerCase_ReturnsChecksummed()
    {
        // Arrange
        string lowercaseAddress = VitalikAddress.ToLowerInvariant();

        // Act
        var address = EthereumAddress.Parse(lowercaseAddress, EthereumAddressChecksum.DoNotCheck);

        // Assert
        Assert.AreEqual(VitalikAddress, address.ToPadded(64));
    }

    [TestMethod]
    public void Parse_VitalikAddressWithoutPrefix_ParsesSuccessfully()
    {
        // Arrange
        string addressWithoutPrefix = VitalikAddress[2..];

        // Act
        var address = EthereumAddress.Parse(addressWithoutPrefix, EthereumAddressChecksum.AlwaysCheck);

        // Assert
        Assert.AreEqual(VitalikAddress, address.ToPadded(64));
    }

    [TestMethod]
    public void Parse_WithValidAddress_ReturnsEthereumAddress()
    {
        // Act
        var address = EthereumAddress.Parse(ValidAddress);

        // Assert
        Assert.AreEqual(ValidAddress, address.ToString());
    }

    [TestMethod]
    public void Parse_WithValidAddressNoPrefix_ReturnsEthereumAddress()
    {
        // Act
        var address = EthereumAddress.Parse(ValidAddressNoPrefix);

        // Assert
        Assert.AreEqual(ValidAddress, address.ToString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("0x")]
    [DataRow("0x1234")] // Too short
    [DataRow("0xG02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2")] // Invalid hex
    public void Parse_WithInvalidAddress_ThrowsFormatException(string invalidAddress)
    {
        Assert.ThrowsException<FormatException>(() => EthereumAddress.Parse(invalidAddress));
    }

    [TestMethod]
    public void TryParse_WithValidAddress_ReturnsTrue()
    {
        // Act
        bool success = EthereumAddress.TryParse(
            ValidAddress, EthereumAddressChecksum.DoNotCheck, out var address);

        // Assert
        Assert.IsTrue(success);
        Assert.AreEqual(ValidAddress, address.ToString());
    }

    [TestMethod]
    [DataRow("")]
    [DataRow("0x")]
    [DataRow("0x1234")] // Too short
    [DataRow("0xG02aaA39b223FE8D0A0e5C4F27eAD9083C756Cc2")] // Invalid hex
    public void TryParse_WithInvalidAddress_ReturnsFalse(string invalidAddress)
    {
        // Act
        bool success = EthereumAddress.TryParse(
            invalidAddress, EthereumAddressChecksum.DoNotCheck, out var address);

        // Assert
        Assert.IsFalse(success);
        Assert.AreEqual(EthereumAddress.Zero, address);
    }

    [TestMethod]
    public void HasSigned_WithInvalidSignature_ReturnsFalse()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);
        var invalidSignature = "0x1234567890123456789012345678901234567890123456789012345678901234123456789012345678901234567890123456789012345678901234567890123400";

        // Act
        bool result = address.HasSigned(ValidMessage, invalidSignature);

        // Assert
        Assert.IsFalse(result);
    }

    [TestMethod]
    public void ToString_ReturnsChecksumAddress()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress.ToLower(), EthereumAddressChecksum.DoNotCheck);

        // Assert
        Assert.AreEqual(ValidAddress, address.ToString()); // Should return checksum format
    }

    [TestMethod]
    public void ToString_WithShortZero_ReturnsShortZeroAddress()
    {
        // Arrange
        var address = EthereumAddress.Zero;

        // Assert
        Assert.AreEqual("0x0", address.ToString(shortZero: true));
    }

    [TestMethod]
    public void Equals_WithSameAddress_ReturnsTrue()
    {
        // Arrange
        var address1 = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);
        var address2 = EthereumAddress.Parse(ValidAddress.ToLower(), EthereumAddressChecksum.DoNotCheck);

        // Act & Assert
        Assert.IsTrue(address1.Equals(address2));
        Assert.IsTrue(address1 == address2);
        Assert.IsFalse(address1 != address2);
    }

    [TestMethod]
    public void Equals_WithDifferentAddress_ReturnsFalse()
    {
        // Arrange
        var address1 = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);
        var address2 = EthereumAddress.Zero;

        // Act & Assert
        Assert.IsFalse(address1.Equals(address2));
        Assert.IsFalse(address1 == address2);
        Assert.IsTrue(address1 != address2);
    }

    [TestMethod]
    public void GetHashCode_WithSameAddress_ReturnsSameHash()
    {
        // Arrange
        var address1 = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);
        var address2 = EthereumAddress.Parse(ValidAddress.ToLower(), EthereumAddressChecksum.DoNotCheck);

        // Act & Assert
        Assert.AreEqual(address1.GetHashCode(), address2.GetHashCode());
    }

    [TestMethod]
    public void Zero_ReturnsZeroAddress()
    {
        // Assert
        Assert.AreEqual("0x0000000000000000000000000000000000000000", EthereumAddress.Zero.ToString());
    }

    [TestMethod]
    public void IsZero_WithZeroAddress_ReturnsTrue()
    {
        // Arrange
        var zeroAddress = EthereumAddress.Zero;

        // Assert
        Assert.IsTrue(zeroAddress.IsZero);
    }

    [TestMethod]
    public void IsZero_WithNonZeroAddress_ReturnsFalse()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);

        // Assert
        Assert.IsFalse(address.IsZero);
    }

    [TestMethod]
    public void ToPadded_WithInvalidLength_ThrowsArgumentException()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => address.ToPadded(20)); // Too short
    }

    [TestMethod]
    public void ToPadded_WithCustomLength_ReturnsPaddedAddress()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);

        // Act
        string padded = address.ToPadded(50);

        // Assert
        Assert.AreEqual(52, padded.Length); // 50 + "0x"
        Assert.IsTrue(padded.StartsWith("0x" + new string('0', 10)));
        Assert.IsTrue(padded.EndsWith(ValidAddress[2..]));
    }

    [TestMethod]
    public void ToPadded_With32Bytes_ReturnsCorrectlyPaddedAddress()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);

        // Act
        string padded = address.ToPadded(64); // 32 bytes = 64 hex chars

        // Assert
        Assert.AreEqual(66, padded.Length); // 64 + "0x"
        Assert.IsTrue(padded.StartsWith("0x" + new string('0', 24))); // 12 bytes = 24 hex chars
        Assert.IsTrue(padded.EndsWith(ValidAddress[2..]));
    }

    [TestMethod]
    public void AddressFormats_ValidLengths()
    {
        // Standard address format
        Assert.AreEqual(42, ValidAddress.Length);  // With 0x
        Assert.AreEqual(40, ValidAddressNoPrefix.Length);  // Without 0x

        // Padded address format (32 bytes)
        Assert.AreEqual(66, VitalikAddress.Length);  // With 0x
        Assert.AreEqual(64, VitalikAddress[2..].Length);  // Without 0x

        // All should parse to valid 20-byte addresses
        var addr1 = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);
        var addr2 = EthereumAddress.Parse(ValidAddressNoPrefix, EthereumAddressChecksum.DoNotCheck);
        var addr3 = EthereumAddress.Parse(VitalikAddress, EthereumAddressChecksum.AlwaysCheck);

        // Internal byte representation should always be 20 bytes
        Assert.AreEqual(20, addr1.Address.Length);
        Assert.AreEqual(20, addr2.Address.Length);
        Assert.AreEqual(20, addr3.Address.Length);
    }

    [TestMethod]
    public void ToHex_ReturnsCorrectHexValue()
    {
        // Arrange
        var address = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);

        // Act
        var hex = address.Address;

        // Assert
        Assert.AreEqual(ValidAddress.ToLowerInvariant(), hex.ToString());
    }

    [TestMethod]
    public void ToHex_WithZeroAddress_ReturnsZeroHexWithFullLength()
    {
        // Special case for zero address - should be 20 bytes, but we want to return 32 bytes

        // Arrange
        var address = EthereumAddress.Zero;

        // Act
        var hex = address.Address;

        // Assert
        Assert.AreEqual("0x0000000000000000000000000000000000000000", hex.ToString());
    }

    [TestMethod]
    public void ToByteArray_ReturnsExactly20Bytes()
    {
        // Test standard address
        var address1 = EthereumAddress.Parse(ValidAddress, EthereumAddressChecksum.DoNotCheck);
        Assert.AreEqual(20, address1.ToByteArray().Length);

        // Test padded address
        var address2 = EthereumAddress.Parse(VitalikAddress, EthereumAddressChecksum.AlwaysCheck);
        Assert.AreEqual(20, address2.ToByteArray().Length);

        // Test zero address - should also be 20 bytes
        var address3 = EthereumAddress.Zero;
        Assert.AreEqual(20, address3.ToByteArray().Length);
        CollectionAssert.AreEqual(new byte[20], address3.ToByteArray());
    }

    [TestMethod]
    public void Default_EthereumAddress_Behavior()
    {
        // Arrange
        var defaultAddress = default(EthereumAddress);

        // Act & Assert
        Assert.IsTrue(defaultAddress.IsEmpty);
        Assert.IsFalse(defaultAddress.IsZero);
        Assert.AreEqual("0x", defaultAddress.ToString());

        // Test exception when trying to get padded representation
        Assert.ThrowsException<InvalidOperationException>(() => defaultAddress.ToPadded(40));

        // Test equality
        Assert.AreEqual(default(EthereumAddress), defaultAddress);
        Assert.AreNotEqual(EthereumAddress.Zero, defaultAddress);

        // Test byte array
        var bytes = defaultAddress.ToByteArray();
        Assert.IsNotNull(bytes);
        Assert.AreEqual(0, bytes.Length); // Should return empty array for default address
    }

    [TestMethod]
    public void Empty_Address_Properties()
    {
        // Arrange
        var emptyAddress = EthereumAddress.Empty;

        // Assert
        Assert.IsTrue(emptyAddress.IsEmpty);
        Assert.IsFalse(emptyAddress.IsZero);
        Assert.IsFalse(emptyAddress.HasValue);
        Assert.AreEqual("0x", emptyAddress.ToString());
    }

    [TestMethod]
    public void Empty_And_Zero_Are_Different()
    {
        // Assert
        Assert.AreNotEqual(EthereumAddress.Empty, EthereumAddress.Zero);
        Assert.IsFalse(EthereumAddress.Empty == EthereumAddress.Zero);
        Assert.IsTrue(EthereumAddress.Empty != EthereumAddress.Zero);
    }

    [TestMethod]
    public void Empty_Addresses_Are_Equal()
    {
        // Arrange
        var empty1 = EthereumAddress.Empty;
        var empty2 = default(EthereumAddress);

        // Assert
        Assert.AreEqual(empty1, empty2);
        Assert.IsTrue(empty1 == empty2);
        Assert.IsFalse(empty1 != empty2);
    }

    [TestMethod]
    [DataRow(32)] // Too short
    [DataRow(64)] // Wrong size
    [DataRow(66)] // Too long
    public void FromPublicKey_WithInvalidLength_ThrowsArgumentException(int length)
    {
        // Arrange
        var invalidKey = new byte[length];
        Array.Fill(invalidKey, (byte)0x42);
        var hexKey = new Hex(invalidKey);

        // Act & Assert
        Assert.ThrowsException<ArgumentException>(() => EthereumAddress.FromPublicKey(hexKey));
    }

    [TestMethod]
    public void FromPublicKey_WithCompressedKey_ThrowsNotImplementedException()
    {
        // Arrange
        var compressedKey = new byte[33];
        Array.Fill(compressedKey, (byte)0x42);
        var hexKey = new Hex(compressedKey);

        // Act & Assert
        Assert.ThrowsException<NotImplementedException>(() => EthereumAddress.FromPublicKey(hexKey));
    }

    [TestMethod]
    public void ConvertToChecksumFormat_WithTestVector_ReturnsCorrectChecksum()
    {
        // Arrange
        var input = "0x5aaeb6053f3e94c9b9a09f33669435e7ef1beaed";  // lowercase
        var expected = "0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed";  // correct checksum

        // Act
        var result = EthereumAddress.ConvertToChecksumFormat(input);

        // Assert
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    [DataRow("0x52908400098527886E0F7030069857D2E4169EE7", "All caps")] // All caps
    [DataRow("0x8617E340B3D01FA5F11F306F4090FD50E238070D", "All caps")]
    [DataRow("0xde709f2102306220921060314715629080e2fb77", "All lower")] // All Lower
    [DataRow("0x27b1fdb04752bbc536007a920d24acb045561c26", "All lower")]
    [DataRow("0x5aAeb6053F3E94C9b9A09f33669435E7Ef1BeAed", "Mixed")] // Normal mixed case
    [DataRow("0xfB6916095ca1df60bB79Ce92cE3Ea74c37c5d359", "Mixed")]
    [DataRow("0xdbF03B407c01E7cD3CBea99509d93f8DDDC8C6FB", "Mixed")]
    [DataRow("0xD1220A0cf47c7B9Be7A2E6BA89F429762e7b9aDb", "Mixed")]
    public void ConvertToChecksumFormat_WithEIP55TestVectors_ReturnsCorrectChecksum(string expected, string testCase)
    {
        // Arrange
        var input = expected.ToLower();  // Test with lowercase input

        // Act
        var result = EthereumAddress.ConvertToChecksumFormat(input);

        // Assert
        Assert.AreEqual(expected, result, $"Failed on {testCase} test vector");
    }
}

