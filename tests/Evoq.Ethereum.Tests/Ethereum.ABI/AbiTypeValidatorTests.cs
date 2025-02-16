using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiTypeValidatorTests
{
    [TestMethod]
    public void IsCompatible_BasicTypes_ValidatesCorrectly()
    {
        // Bool
        Assert.IsTrue(AbiTypeValidator.IsCompatible("bool", true));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("bool", 1));

        // Address
        Assert.IsTrue(AbiTypeValidator.IsCompatible("address", new EthereumAddress("0x1234567890123456789012345678901234567890")));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("address", "0x1234"));

        // String
        Assert.IsTrue(AbiTypeValidator.IsCompatible("string", "hello"));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("string", 123));
    }

    [TestMethod]
    public void IsCompatible_IntegerTypes_ValidatesCorrectly()
    {
        // uint8
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint8", (byte)255));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8", 256));

        // uint256
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint", ulong.MaxValue)); // uint alias
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint256", BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935")));

        // int256
        Assert.IsTrue(AbiTypeValidator.IsCompatible("int", int.MinValue)); // int alias
        Assert.IsFalse(AbiTypeValidator.IsCompatible("int8", 128)); // Too large for int8
    }

    [TestMethod]
    public void IsCompatible_BytesTypes_ValidatesCorrectly()
    {
        // bytes
        Assert.IsTrue(AbiTypeValidator.IsCompatible("bytes", new byte[] { 1, 2, 3 }));

        // bytes32
        Assert.IsTrue(AbiTypeValidator.IsCompatible("bytes32", new byte[32]));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("bytes32", new byte[31])); // Wrong size
    }

    [TestMethod]
    public void IsCompatible_RejectsInappropriateTypes()
    {
        // Reject null
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint256", null));

        // Reject DateTime
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint256", DateTime.Now));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint256", DateTimeOffset.Now));

        // Reject decimal (not a good fit for Solidity numbers)
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint256", 1.23m));
    }

    [TestMethod]
    public void ValidateParameters_FunctionSignature_ValidatesCorrectly()
    {
        var signature = FunctionSignature.Parse("transfer(address,uint256)");
        var values = (
            To: new EthereumAddress("0x1234567890123456789012345678901234567890"),
            Amount: BigInteger.Parse("1000000000000000000")
        );

        // Valid parameters
        Assert.IsTrue(AbiTypeValidator.ValidateParameters(signature, values));

        // Invalid parameters
        Assert.IsFalse(AbiTypeValidator.ValidateParameters(
            signature,
            ValueTuple.Create("not an address", 123)));

        // Wrong number of parameters
        Assert.IsFalse(AbiTypeValidator.ValidateParameters(
            signature,
            ValueTuple.Create(new EthereumAddress("0x1234567890123456789012345678901234567890"))));
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

        var values = (
            To: new EthereumAddress("0x1234567890123456789012345678901234567890"),
            Amount: BigInteger.Parse("1000000000000000000")
        );

        // Valid parameters
        Assert.IsTrue(AbiTypeValidator.ValidateParameters(function, values));

        // Invalid parameters
        Assert.IsFalse(AbiTypeValidator.ValidateParameters(
            function,
            ValueTuple.Create("not an address", DateTime.Now))); // Explicitly rejected type

        // Wrong number of parameters
        Assert.IsFalse(AbiTypeValidator.ValidateParameters(
            function,
            ValueTuple.Create(new EthereumAddress("0x1234567890123456789012345678901234567890"))));
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void ValidateParameters_NonFunction_ThrowsException()
    {
        var eventItem = new AbiItem
        {
            Type = "event",
            Name = "Transfer"
        };

        AbiTypeValidator.ValidateParameters(eventItem, ("some value"));
    }

    [TestMethod]
    public void IsCompatible_DynamicArrayTypes_ValidatesCorrectly()
    {
        // Valid dynamic arrays
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint8[]", new byte[] { 1, 2, 3 }));
        Assert.IsTrue(AbiTypeValidator.IsCompatible("bool[]", new[] { true, false, true }));
        Assert.IsTrue(AbiTypeValidator.IsCompatible("address[]", new[]
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890"),
            new EthereumAddress("0x0987654321098765432109876543210987654321")
        }));

        // Invalid dynamic arrays
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8[]", new[] { "not", "numbers" }));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8[]", 123)); // Not an array
        Assert.IsFalse(AbiTypeValidator.IsCompatible("address[]", new[] { "0x123", "0x456" })); // Wrong element type
    }

    [TestMethod]
    public void IsCompatible_FixedSizeArrayTypes_ValidatesCorrectly()
    {
        // Valid fixed-size arrays
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint8[3]", new byte[] { 1, 2, 3 }));
        Assert.IsTrue(AbiTypeValidator.IsCompatible("bool[2]", new[] { true, false }));
        Assert.IsTrue(AbiTypeValidator.IsCompatible("address[1]", new[]
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890")
        }));

        // Invalid fixed-size arrays
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8[3]", new byte[] { 1, 2 })); // Wrong length
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8[2]", new byte[] { 1, 2, 3 })); // Wrong length
        Assert.IsFalse(AbiTypeValidator.IsCompatible("bool[2]", new[] { "true", "false" })); // Wrong element type
    }

    [TestMethod]
    public void IsCompatible_NestedArrayTypes_ValidatesCorrectly()
    {
        // Valid nested arrays
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint8[][]", new[]
        {
            new byte[] { 1, 2 },
            new byte[] { 3, 4 }
        }));

        Assert.IsTrue(AbiTypeValidator.IsCompatible("bool[2][]", new[]
        {
            new[] { true, false },
            new[] { false, true }
        }));

        // Invalid nested arrays
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8[][]", new object[]
        {
            new byte[] { 1, 2 },
            new object[] { "not", "bytes" }
        }));

        Assert.IsFalse(AbiTypeValidator.IsCompatible("bool[2][]", new[]
        {
            new[] { true, false },
            new[] { true } // Wrong inner array length
        }));
    }

    [TestMethod]
    public void IsCompatible_EmptyArrays_ValidatesCorrectly()
    {
        // Empty dynamic arrays should be valid
        Assert.IsTrue(AbiTypeValidator.IsCompatible("uint8[]", Array.Empty<byte>()));
        Assert.IsTrue(AbiTypeValidator.IsCompatible("bool[]", Array.Empty<bool>()));

        // Empty fixed-size arrays should be invalid
        Assert.IsFalse(AbiTypeValidator.IsCompatible("uint8[1]", Array.Empty<byte>()));
        Assert.IsFalse(AbiTypeValidator.IsCompatible("bool[2]", Array.Empty<bool>()));
    }

    [TestMethod]
    public void IsCompatible_TupleTypes_ValidatesCorrectly()
    {
        // Simple tuple
        var person = (Name: "Alice", Age: BigInteger.Parse("25"), Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890"));
        Assert.IsTrue(AbiTypeValidator.IsCompatible("(string,uint256,address)", person));

        // Nested tuple
        var complexData = (
            Person: (Name: "Bob", Age: BigInteger.Parse("30")),
            Active: true,
            Addresses: new[] { new EthereumAddress("0x1234567890123456789012345678901234567890") }
        );
        Assert.IsTrue(AbiTypeValidator.IsCompatible("((string,uint256),bool,address[])", complexData));

        // Invalid tuples
        Assert.IsFalse(AbiTypeValidator.IsCompatible("(uint256,bool)", (123, "not a bool"))); // Wrong inner type
        Assert.IsFalse(AbiTypeValidator.IsCompatible("(uint256,bool)", (BigInteger.One))); // Wrong number of components
        Assert.IsFalse(AbiTypeValidator.IsCompatible("(uint256,bool)", "not a tuple")); // Not a tuple
    }

    [TestMethod]
    public void IsCompatible_ValidatesComplexGroupedParameters()
    {
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

        Assert.IsTrue(AbiTypeValidator.IsCompatible(permitType, permitData));

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

        Assert.IsFalse(AbiTypeValidator.IsCompatible(permitType, invalidPermitData));
    }

    [TestMethod]
    public void IsCompatible_PersonTuple_ValidatesCorrectly()
    {
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

        Assert.IsTrue(AbiTypeValidator.IsCompatible(personType, personData));

        // Test with invalid data
        var invalidPersonData = (
            Name: "Alice",
            Age: DateTime.Now, // Wrong type
            Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
        );

        Assert.IsFalse(AbiTypeValidator.IsCompatible(personType, invalidPersonData));
    }

    [TestMethod]
    public void ValidateParameters_WithTuples_ValidatesCorrectly()
    {
        var signature = FunctionSignature.Parse("setPersonData((string,uint256,address),bool)");

        var validParams = ValueTuple.Create(
            (
                Name: "Alice",
                Age: BigInteger.Parse("25"),
                Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
            ),
            true
        );

        Assert.IsTrue(signature.ValidateParameters(validParams));

        var invalidParams = ValueTuple.Create(
            (
                Name: "Alice",
                Age: DateTime.Now, // Wrong type
                Wallet: new EthereumAddress("0x1234567890123456789012345678901234567890")
            ),
            true
        );

        Assert.IsFalse(signature.ValidateParameters(invalidParams));
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