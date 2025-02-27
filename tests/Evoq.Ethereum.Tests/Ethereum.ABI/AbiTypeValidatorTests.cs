using System.Numerics;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiTypeValidatorTests
{
    private readonly AbiTypeValidator validator;

    public AbiTypeValidatorTests()
    {
        this.validator = new AbiEncoder().Validator;
    }

    [TestMethod]
    public void IsCompatible_AddressType_ValidatesCorrectly()
    {
        string m;

        // Valid .NET types
        Assert.IsTrue(this.validator.IsCompatible("address", new EthereumAddress("0x1234567890123456789012345678901234567890"), out m), m);
        Assert.IsTrue(this.validator.IsCompatible("address", "0x1234567890123456789012345678901234567890", out m), m);
        Assert.IsTrue(this.validator.IsCompatible("address", new byte[20], out m), m);

        // Invalid .NET types
        Assert.IsFalse(this.validator.IsCompatible("address", 123, out m), m); // Int32 is not a valid address
    }

    [TestMethod]
    public void IsCompatible_BasicTypes_ValidatesCorrectly()
    {
        string m;

        // Bool
        Assert.IsTrue(this.validator.IsCompatible("bool", true, out m), m);
        Assert.IsFalse(this.validator.IsCompatible("bool", 1, out m), m);

        // Address
        Assert.IsTrue(this.validator.IsCompatible("address", new EthereumAddress("0x1234567890123456789012345678901234567890"), out m), m);
        Assert.IsTrue(this.validator.IsCompatible("address", "0x1234567890123456789012345678901234567890", out m), m);
        Assert.IsFalse(this.validator.IsCompatible("address", 123, out m), m);

        // String
        Assert.IsTrue(this.validator.IsCompatible("string", "hello", out m), m);
        Assert.IsFalse(this.validator.IsCompatible("string", 123, out m), m);
    }

    [TestMethod]
    public void IsCompatible_IntegerTypes_ValidatesCorrectly()
    {
        string m;

        // uint8
        Assert.IsTrue(this.validator.IsCompatible("uint8", (byte)255, out m), m);
        Assert.IsFalse(this.validator.IsCompatible("uint8", 256, out m), m);

        // uint256
        Assert.IsTrue(this.validator.IsCompatible("uint", ulong.MaxValue, out m), m); // uint alias
        Assert.IsTrue(this.validator.IsCompatible("uint256", BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"), out m), m);

        // int256 (note tryEncoding is true for the last test because the encoder will catch the size mismatch)
        Assert.IsTrue(this.validator.IsCompatible("int", int.MinValue, out m), m); // int alias
        Assert.IsTrue(this.validator.IsCompatible("int8", 128, out m), m);

    }

    [TestMethod]
    public void IsCompatible_NegativeInt8_PassesSimpleTypeCheckButFailsEncoderCheck()
    {
        string m;

        // difference between tryEncoding and not tryEncoding
        Assert.IsTrue(this.validator.IsCompatible("int8", -129, out m), m); // passes because only type is checked
        Assert.IsFalse(this.validator.IsCompatible("int8", -129, out m, tryEncoding: true), m); // fails because encoder will check value
    }

    [TestMethod]
    public void IsCompatible_BytesTypes_ValidatesCorrectly()
    {
        string m;

        // bytes
        Assert.IsTrue(this.validator.IsCompatible("bytes", new byte[] { 1, 2, 3 }, out m), m);

        // bytes32
        Assert.IsTrue(this.validator.IsCompatible("bytes32", new byte[32], out m), m);

        // bytes32 wrong size only fails if tryEncoding
        Assert.IsTrue(this.validator.IsCompatible("bytes32", new byte[31], out m), m); // Wrong size
        Assert.IsFalse(this.validator.IsCompatible("bytes32", new byte[31], out m, tryEncoding: true), m); // Wrong size
    }

    [TestMethod]
    public void IsCompatible_RejectsInappropriateTypes()
    {
        string m;

        // Reject null
        Assert.IsFalse(this.validator.IsCompatible("uint256", null, out m), m);

        // Reject DateTime
        Assert.IsFalse(this.validator.IsCompatible("uint256", DateTime.Now, out m), m);
        Assert.IsFalse(this.validator.IsCompatible("uint256", DateTimeOffset.Now, out m), m);

        // Reject decimal (not a good fit for Solidity numbers)
        Assert.IsFalse(this.validator.IsCompatible("uint256", 1.23m, out m), m);
    }

    [TestMethod]
    public void ValidateParameters_FunctionSignature_ValidatesCorrectly()
    {
        string m;

        var signature = FunctionSignature.Parse("transfer(address,uint256)");
        var values = new List<object?>
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890"),
            BigInteger.Parse("1000000000000000000")
        };

        // Valid parameters
        Assert.IsTrue(this.validator.ValidateParameters(signature, values, out m), m);

        // Invalid parameters
        Assert.IsFalse(this.validator.ValidateParameters(
            signature,
            new List<object?> { "not an address", 123 },
            out m), m);

        // Wrong number of parameters
        Assert.IsFalse(this.validator.ValidateParameters(
            signature,
            new List<object?> { new EthereumAddress("0x1234567890123456789012345678901234567890") },
            out m), m);
    }

    [TestMethod]
    public void ValidateParameters_AbiFunction_ValidatesCorrectly()
    {
        var function = new AbiItem
        {
            Type = "function",
            Name = "transfer",
            Inputs = new List<Parameter>
            {
                new() { Type = "address" },
                new() { Type = "uint256" }
            }
        };

        var values = new List<object?>
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890"),
            BigInteger.Parse("1000000000000000000")
        };

        string m;

        // Valid parameters
        Assert.IsTrue(this.validator.ValidateParameters(function, values, out m), m);

        // Invalid parameters
        Assert.IsFalse(this.validator.ValidateParameters(
            function,
            ValueTuple.Create("not an address", DateTime.Now),
            out m), m); // Explicitly rejected type

        // Wrong number of parameters
        Assert.IsFalse(this.validator.ValidateParameters(
            function,
            ValueTuple.Create(new EthereumAddress("0x1234567890123456789012345678901234567890")),
            out m), m);
    }

    [TestMethod]
    public void ValidateParameters_NonFunction_ThrowsException()
    {
        string m;

        var eventItem = new AbiItem
        {
            Type = "event",
            Name = "Transfer"
        };

        Assert.ThrowsException<ArgumentException>(
            () => this.validator.ValidateParameters(eventItem, ("some value"), out m));
    }

    [TestMethod]
    public void IsCompatible_DynamicArrayTypes_ValidatesCorrectly()
    {
        string m;

        // Valid dynamic arrays
        Assert.IsTrue(this.validator.IsCompatible("uint8[]", new byte[] { 1, 2, 3 }, out m), m);
        Assert.IsTrue(this.validator.IsCompatible("bool[]", new[] { true, false, true }, out m), m);
        Assert.IsTrue(this.validator.IsCompatible("address[]", new[]
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890"),
            new EthereumAddress("0x0987654321098765432109876543210987654321")
        }, out m), m);

        // Invalid dynamic arrays
        Assert.IsFalse(this.validator.IsCompatible("uint8[]", new[] { "not", "numbers" }, out m), m);
        Assert.IsFalse(this.validator.IsCompatible("uint8[]", 123, out m), m); // Not an array

        Assert.IsTrue(this.validator.IsCompatible("address[]", new[] { "0x123", "0x456" }, out m), m); // Wrong element type
        Assert.IsFalse(this.validator.IsCompatible("address[]", new[] { "0x123", "0x456" }, out m, tryEncoding: true), m); // Wrong element type
    }

    [TestMethod]
    public void IsCompatible_FixedSizeArrayTypes_ValidatesCorrectly()
    {
        string m;

        // Valid fixed-size arrays
        Assert.IsTrue(this.validator.IsCompatible("uint8[3]", new byte[] { 1, 2, 3 }, out m), m);
        Assert.IsTrue(this.validator.IsCompatible("bool[2]", new[] { true, false }, out m), m);
        Assert.IsTrue(this.validator.IsCompatible("address[1]", new[]
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890")
        }, out m), m);

        // Invalid fixed-size arrays
        Assert.IsFalse(this.validator.IsCompatible("uint8[3]", new byte[] { 1, 2 }, out m), m); // Wrong length
        Assert.IsFalse(this.validator.IsCompatible("uint8[2]", new byte[] { 1, 2, 3 }, out m), m); // Wrong length
        Assert.IsFalse(this.validator.IsCompatible("bool[2]", new[] { "true", "false" }, out m), m); // Wrong element type
    }

    [TestMethod]
    public void IsCompatible_NestedArrayTypes_ValidatesCorrectly()
    {
        string m;

        // Valid nested arrays
        Assert.IsTrue(this.validator.IsCompatible("uint8[][]", new[]
        {
            new byte[] { 1, 2 },
            new byte[] { 3, 4 }
        }, out m), m);

        Assert.IsTrue(this.validator.IsCompatible("bool[2][]", new[]
        {
            new[] { true, false },
            new[] { false, true }
        }, out m), m);

        // Invalid nested arrays
        Assert.IsFalse(this.validator.IsCompatible("uint8[][]", new object[]
        {
            new byte[] { 1, 2 },
            new object[] { "not", "bytes" }
        }, out m), m);

        Assert.IsFalse(this.validator.IsCompatible("bool[2][]", new[]
        {
            new[] { true, false },
            new[] { true } // Wrong inner array length
        }, out m), m);
    }

    [TestMethod]
    public void IsCompatible_EmptyArrays_ValidatesCorrectly()
    {
        string m;

        // Empty dynamic arrays should be valid
        Assert.IsTrue(this.validator.IsCompatible("uint8[]", Array.Empty<byte>(), out m), m);
        Assert.IsTrue(this.validator.IsCompatible("bool[]", Array.Empty<bool>(), out m), m);

        // Empty fixed-size arrays should be invalid
        Assert.IsFalse(this.validator.IsCompatible("uint8[1]", Array.Empty<byte>(), out m), m);
        Assert.IsFalse(this.validator.IsCompatible("bool[2]", Array.Empty<bool>(), out m), m);
    }

    [TestMethod]
    public void IsCompatible_TupleTypes_ValidatesCorrectly()
    {
        string m;

        // Simple tuple
        var person = (Name: "Alice", Age: BigInteger.Parse("25"), Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890"));
        Assert.IsTrue(this.validator.IsCompatible("(string,uint256,address)", person, out m), m);

        // Nested tuple
        var complexData = (
            Person: (Name: "Bob", Age: BigInteger.Parse("30")),
            Active: true,
            Addresses: new[] { new EthereumAddress("0x1234567890123456789012345678901234567890") }
        );
        Assert.IsTrue(this.validator.IsCompatible("((string,uint256),bool,address[])", complexData, out m), m);

        // Invalid tuples
        Assert.IsFalse(this.validator.IsCompatible("(uint256,bool)", (123, "not a bool"), out m), m); // Wrong inner type
        Assert.IsFalse(this.validator.IsCompatible("(uint256,bool)", (BigInteger.One), out m), m); // Wrong number of components
        Assert.IsFalse(this.validator.IsCompatible("(uint256,bool)", "not a tuple", out m), m); // Not a tuple
    }

    [TestMethod]
    public void IsCompatible_ValidatesComplexGroupedParameters()
    {
        string m;

        // This represents a function like:
        // struct Permit {
        //     address owner;
        //     address spender;
        //     uint256 value;
        //     uint256 deadline;
        //     uint8 v;
        //     bytes32 r;
        //     bytes32 s;
        // }
        // function permit(Permit calldata permit)

        var permitType = "(address,address,uint256,uint256,uint8,bytes32,bytes32)";
        var permitData = (
            Owner: new EthereumAddress("0x1234567890123456789012345678901234567890"),
            Spender: new EthereumAddress("0x0987654321098765432109876543210987654321"),
            Value: BigInteger.Parse("1000000000000000000"),
            Deadline: BigInteger.Parse("1234567890"),
            V: (byte)28,
            R: new byte[32],
            S: new byte[32]
        );

        Assert.IsTrue(this.validator.IsCompatible(permitType, permitData, out m), m);

        // Test with invalid data
        var invalidPermitData = (
            Owner: "not an address",  // Wrong type
            Spender: new EthereumAddress("0x0987654321098765432109876543210987654321"),
            Value: BigInteger.Parse("1000000000000000000"),
            Deadline: DateTime.UtcNow,  // Wrong type
            V: (byte)28,
            R: new byte[32],
            S: new byte[31]  // Wrong length
        );

        Assert.IsFalse(this.validator.IsCompatible(permitType, invalidPermitData, out m), m);
    }

    [TestMethod]
    public void IsCompatible_PersonTuple_ValidatesCorrectly()
    {
        string m;

        // This represents a struct like:
        // struct Person {
        //     string name;
        //     uint256 age;
        //     address wallet;
        // }

        var personType = "(string,uint256,address)";
        var personData = (
            Name: "Alice",
            Age: BigInteger.Parse("25"),
            Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
        );

        Assert.IsTrue(this.validator.IsCompatible(personType, personData, out m), m);

        // Test with invalid data
        var invalidPersonData = (
            Name: "Alice",
            Age: DateTime.Now, // Wrong type
            Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
        );

        Assert.IsFalse(this.validator.IsCompatible(personType, invalidPersonData, out m), m);
    }

    [TestMethod]
    public void ValidateParameters_WithTuples_ValidatesCorrectly()
    {
        string m;

        var signature = FunctionSignature.Parse("setPersonData((string,uint256,address),bool)");

        var validParams = new List<object?>
        {
            (
                Name: "Alice",
                Age: BigInteger.Parse("25"),
                Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
            ),
            true
        };

        Assert.IsTrue(this.validator.ValidateParameters(signature, validParams, out m), m);

        var invalidParams = new List<object?>
        {
            (
                Name: "Alice",
                Age: DateTime.Now, // Wrong type
                Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
            ),
            true
        };

        Assert.IsFalse(this.validator.ValidateParameters(signature, invalidParams, out m), m);
    }

    [TestMethod]
    public void ParseTupleComponents_WithSimpleTypes_ParsesCorrectly()
    {
        // Basic types without parentheses
        var components = AbiTypeValidator.ParseTupleComponents("uint256,bool,address");
        CollectionAssert.AreEqual(
            new[] { "uint256", "bool", "address" },
            components);

        // Basic types with parentheses
        components = AbiTypeValidator.ParseTupleComponents("(uint256,bool,address)");
        CollectionAssert.AreEqual(
            new[] { "uint256", "bool", "address" },
            components);
    }

    [TestMethod]
    public void ParseTupleComponents_WithArrayTypes_ParsesCorrectly()
    {
        var components = AbiTypeValidator.ParseTupleComponents("uint256[],address[2],bool[]");
        CollectionAssert.AreEqual(
            new[] { "uint256[]", "address[2]", "bool[]" },
            components);
    }

    [TestMethod]
    public void ParseTupleComponents_WithNestedTuples_ParsesCorrectly()
    {
        // Nested tuple
        var components = AbiTypeValidator.ParseTupleComponents("(uint256,(address,bool))");
        CollectionAssert.AreEqual(
            new[] { "uint256", "(address,bool)" },
            components);

        // Complex nested tuple with arrays
        components = AbiTypeValidator.ParseTupleComponents("((uint256,bool[]),(address,bytes32)[],string)");
        CollectionAssert.AreEqual(
            new[] { "(uint256,bool[])", "(address,bytes32)[]", "string" },
            components);
    }

    [TestMethod]
    public void ParseTupleComponents_WithEmptyOrSingleComponent_ParsesCorrectly()
    {
        // Empty tuple
        var components = AbiTypeValidator.ParseTupleComponents("()");
        Assert.AreEqual(0, components.Length);

        // Single component
        components = AbiTypeValidator.ParseTupleComponents("uint256");
        CollectionAssert.AreEqual(new[] { "uint256" }, components);
    }
}