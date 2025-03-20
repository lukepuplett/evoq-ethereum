using System.Numerics;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.Conversion;

[TestClass]
public class AbiClrTypeConverterTests
{
    private readonly AbiClrTypeConverter converter;

    public AbiClrTypeConverterTests()
    {
        this.converter = new AbiClrTypeConverter();
    }

    [TestMethod]
    public void TryConvert_BigInteger_FromString_Succeeds()
    {
        // Arrange
        string value = "123456789012345678901234567890";
        var expected = BigInteger.Parse(value);

        // Act
        bool success = this.converter.TryConvert(value, typeof(BigInteger), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(BigInteger));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_BigInteger_FromInt_Succeeds()
    {
        // Arrange
        int value = 12345;
        var expected = new BigInteger(value);

        // Act
        bool success = this.converter.TryConvert(value, typeof(BigInteger), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(BigInteger));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_ByteArray_FromHexString_Succeeds()
    {
        // Arrange
        string value = "0x1a2b3c4d";
        var expected = Convert.FromHexString("1a2b3c4d");

        // Act
        bool success = this.converter.TryConvert(value, typeof(byte[]), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(byte[]));
        CollectionAssert.AreEqual(expected, (byte[])result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromString_Succeeds()
    {
        // Arrange
        string value = "0x1a2b3c4d";
        var expected = Hex.Parse(value);

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_EthereumAddress_FromString_Succeeds()
    {
        // Arrange
        string value = "0x1234567890123456789012345678901234567890";
        var expected = new EthereumAddress(value);

        // Act
        bool success = this.converter.TryConvert(value, typeof(EthereumAddress), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(EthereumAddress));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_Enum_FromString_Succeeds()
    {
        // Arrange
        string value = "Two";
        var expected = TestEnum.Two;

        // Act
        bool success = this.converter.TryConvert(value, typeof(TestEnum), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(TestEnum));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_Enum_FromInt_Succeeds()
    {
        // Arrange
        int value = 2;
        var expected = TestEnum.Two;

        // Act
        bool success = this.converter.TryConvert(value, typeof(TestEnum), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(TestEnum));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_String_FromInt_Succeeds()
    {
        // Arrange
        int value = 123;
        var expected = "123";

        // Act
        bool success = this.converter.TryConvert(value, typeof(string), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(string));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_Int_FromString_Succeeds()
    {
        // Arrange
        string value = "123";
        var expected = 123;

        // Act
        bool success = this.converter.TryConvert(value, typeof(int), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(int));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_Bool_FromString_Succeeds()
    {
        // Arrange
        string value = "true";
        var expected = true;

        // Act
        bool success = this.converter.TryConvert(value, typeof(bool), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(bool));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_WithAbiType_UsesCorrectConversion()
    {
        // Arrange
        string value = "123";
        string abiType = "uint256";
        var expected = BigInteger.Parse(value);

        // Act
        bool success = this.converter.TryConvert(value, typeof(BigInteger), out var result, abiType);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(BigInteger));
        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void TryConvert_IncompatibleTypes_ReturnsFalse()
    {
        // Arrange
        string value = "not a number";

        // Act
        bool success = this.converter.TryConvert(value, typeof(int), out var result);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_NullToReferenceType_Succeeds()
    {
        // Arrange
        object? value = null;

        // Act
        bool success = this.converter.TryConvert(value, typeof(string), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_NullToNullableValueType_Succeeds()
    {
        // Arrange
        object? value = null;

        // Act
        bool success = this.converter.TryConvert(value, typeof(int?), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_NullToNonNullableValueType_Fails()
    {
        // Arrange
        object? value = null;

        // Act
        bool success = this.converter.TryConvert(value, typeof(int), out var result);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_NullToBigInteger_Fails()
    {
        // Arrange
        object? value = null;

        // Act
        bool success = this.converter.TryConvert(value, typeof(BigInteger), out var result);

        // Assert
        Assert.IsFalse(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_NullToNullableBigInteger_Succeeds()
    {
        // Arrange
        object? value = null;

        // Act
        bool success = this.converter.TryConvert(value, typeof(BigInteger?), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_WithAbiTypeAndNullValue_HandlesCorrectly()
    {
        // Arrange
        object? value = null;
        string abiType = "uint256";

        // Act
        bool success = this.converter.TryConvert(value, typeof(BigInteger?), out var result, abiType);

        // Assert
        Assert.IsTrue(success);
        Assert.IsNull(result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromEmptyString_Succeeds()
    {
        // Arrange
        string value = "";

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Empty, result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromWhitespaceString_Fails()
    {
        // Arrange
        string value = "   ";

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsFalse(success);
    }

    [TestMethod]
    public void TryConvert_Hex_FromStringWithout0xPrefix_Succeeds()
    {
        // Arrange
        string value = "1a2b3c4d"; // Missing 0x prefix

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Parse("0x" + value), result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromOddLengthHexString_Succeeds()
    {
        // Arrange
        string value = "0x1a2b3"; // Odd number of hex digits after 0x

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Parse(value, HexParseOptions.AllowOddLength), result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromPaddedOddLengthHexString_Succeeds()
    {
        // Arrange
        string value = "0x01a2b3"; // Padded to even number of digits

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Parse(value), result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromLowercaseHexString_Succeeds()
    {
        // Arrange
        string value = "0xabcdef";

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Parse(value), result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromUppercaseHexString_Succeeds()
    {
        // Arrange
        string value = "0xABCDEF";

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Parse(value), result);
    }

    [TestMethod]
    public void TryConvert_Hex_FromMixedCaseHexString_Succeeds()
    {
        // Arrange
        string value = "0xaBcDeF";

        // Act
        bool success = this.converter.TryConvert(value, typeof(Hex), out var result);

        // Assert
        Assert.IsTrue(success);
        Assert.IsInstanceOfType(result, typeof(Hex));
        Assert.AreEqual(Hex.Parse(value), result);
    }

    public enum TestEnum
    {
        One = 1,
        Two = 2,
        Three = 3
    }
}