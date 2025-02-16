using System.Numerics;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class EvmParamTests
{
    [TestMethod]
    public void Constructor_WithBasicTypes_SetsPropertiesCorrectly()
    {
        var param = new EvmParam(0, "amount", "uint256");

        Assert.AreEqual("uint256", param.AbiType, "Type should be set correctly");
        Assert.AreEqual("amount", param.Name, "Name should be set correctly");
        Assert.AreEqual(0, param.Position, "Position should be set correctly");
        Assert.IsNull(param.Components, "Components should be null for basic types");
    }

    [TestMethod]
    public void Constructor_WithTupleComponents_SetsPropertiesCorrectly()
    {
        var components = new List<EvmParam>
        {
            new(0, "valid", "bool"),
            new(1, "owner", "address")
        };

        var param = new EvmParam(0, "details", components);

        Assert.AreEqual("(bool,address)", param.AbiType, "Type should be derived from components");
        Assert.AreEqual("details", param.Name, "Name should be set correctly");
        Assert.AreEqual(0, param.Position, "Position should be set correctly");
        Assert.IsNotNull(param.Components, "Components should not be null for tuple types");
        Assert.AreEqual(2, param.Components!.Count, "Components count should match");
    }

    [TestMethod]
    public void Constructor_WithTupleType_ThrowsArgumentException()
    {
        Assert.ThrowsException<ArgumentException>(() =>
            new EvmParam(0, "details", "(bool,address)"),
            "Should throw when trying to create tuple type without components");
    }

    [TestMethod]
    public void Constructor_WithMismatchedComponents_ThrowsArgumentException()
    {
        var components = new List<EvmParam>
        {
            new(0, "valid", "bool"),
            new(1, "owner", "address")
        };

        Assert.ThrowsException<ArgumentException>(() =>
            new EvmParam(0, "details", "(uint256,bool)", null, components),
            "Should throw when components don't match specified type");
    }

    [TestMethod]
    public void Constructor_WithNullComponents_CreatesEmptyList()
    {
        var param = new EvmParam(0, "amount", "uint256");
        Assert.IsNull(param.Components, "Components should be null for basic types");
    }

    [TestMethod]
    public void Constructor_WithEmptyComponents_CreatesEmptyList()
    {
        var param = new EvmParam(0, "amount", new List<EvmParam>());
        Assert.IsNotNull(param.Components, "Components should not be null when passed empty list");
        Assert.AreEqual(0, param.Components!.Count, "Components should be empty when passed empty list");
        Assert.AreEqual("()", param.AbiType, "Type should be empty tuple");
    }

    [TestMethod]
    public void Constructor_WithDynamicLengthArray_SetsPropertiesCorrectly()
    {
        var arrayLengths = new[] { -1 };
        var param = new EvmParam(0, "values", "uint256", arrayLengths);

        Assert.AreEqual("uint256[]", param.AbiType, "Type should include array length");
        Assert.AreEqual("values", param.Name, "Name should be set correctly");
        Assert.AreEqual(0, param.Position, "Position should be set correctly");
        Assert.IsNull(param.Components, "Components should be null for basic types");
        Assert.IsNotNull(param.ArrayLengths, "ArrayLengths should not be null");
        Assert.AreEqual(1, param.ArrayLengths!.Count, "Should have one array dimension");
        Assert.AreEqual(-1, param.ArrayLengths![0], "First array dimension length should be -1");
    }

    [TestMethod]
    public void Constructor_WithFixedLengthArray_SetsPropertiesCorrectly()
    {
        var arrayLengths = new[] { 16, 2 };
        var param = new EvmParam(0, "values", "uint256", arrayLengths);

        Assert.AreEqual("uint256[16][2]", param.AbiType, "Type should include array length");
        Assert.AreEqual("values", param.Name, "Name should be set correctly");
        Assert.AreEqual(0, param.Position, "Position should be set correctly");
        Assert.IsNull(param.Components, "Components should be null for basic types");
        Assert.IsNotNull(param.ArrayLengths, "ArrayLengths should not be null");
        Assert.AreEqual(2, param.ArrayLengths!.Count, "Should have two array dimensions");
        Assert.AreEqual(16, param.ArrayLengths![0], "First array dimension length should be 16");
        Assert.AreEqual(2, param.ArrayLengths![1], "Second array dimension length should be 2");
    }

    [TestMethod]
    public void ToString_WithBasicType_ReturnsTypeOnly()
    {
        var param = new EvmParam(0, "", "uint256");
        Assert.AreEqual("uint256", param.ToString(), "Should return type only when no name is present");
    }

    [TestMethod]
    public void ToString_WithNamedBasicType_ReturnsTypeAndName()
    {
        var param = new EvmParam(0, "amount", "uint256");
        Assert.AreEqual("uint256 amount", param.ToString(), "Should return type and name when name is present");
    }

    [TestMethod]
    public void ToString_WithUnnamedTuple_ReturnsTupleType()
    {
        var components = new List<EvmParam>
        {
            new(0, "", "bool"),
            new(1, "", "address")
        };

        var param = new EvmParam(0, "", components);
        Assert.AreEqual("(bool, address)", param.ToString(), "Should return tuple type when no names are present");
    }

    [TestMethod]
    public void ToString_WithNamedTuple_ReturnsTupleTypeAndName()
    {
        var components = new List<EvmParam>
        {
            new(0, "valid", "bool"),
            new(1, "owner", "address")
        };

        var param = new EvmParam(0, "details", components);
        Assert.AreEqual("(bool valid, address owner) details", param.ToString(), "Should return tuple type with component names and tuple name");
    }

    [TestMethod]
    [DataRow("uint")]        // Valid - defaults to uint256
    [DataRow("uint256")]     // Valid
    [DataRow("address")]     // Valid
    [DataRow("bool")]        // Valid
    [DataRow("bytes32")]     // Valid
    public void Constructor_WithValidSolidityType_DoesNotThrow(string type)
    {
        // Act & Assert
        var param = new EvmParam(0, "param", type);
        Assert.AreEqual(type, param.AbiType);
    }

    [TestMethod]
    [DataRow("uint257")]     // Invalid - uint size too large
    [DataRow("uint7")]       // Invalid - uint size not multiple of 8
    [DataRow("bytes33")]     // Invalid - bytes size too large
    [DataRow("bytes0")]      // Invalid - bytes size too small
    [DataRow("notatype")]    // Invalid - unknown type
    [DataRow("string8")]     // Invalid - string doesn't take size
    public void Constructor_WithInvalidSolidityType_ThrowsArgumentException(string type)
    {
        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            new EvmParam(0, "param", type));

        StringAssert.Contains(ex.Message, "Invalid Solidity");
    }

    [TestMethod]
    [DataRow("uint256[]")]       // Should be passed as arrayLengths
    [DataRow("address[5]")]      // Should be passed as arrayLengths
    [DataRow("bool[][5]")]       // Should be passed as arrayLengths
    public void Constructor_WithArrayInType_ThrowsArgumentException(string type)
    {
        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            new EvmParam(0, "param", type));

        StringAssert.Contains(ex.Message, "must be a single type");
    }

    [TestMethod]
    public void Constructor_WithValidTypeAndArrayLengths_CreatesValidParam()
    {
        // Arrange
        var arrayLengths = new[] { 5, -1 }; // Fixed size 5, then dynamic size

        // Act
        var param = new EvmParam(0, "param", "uint256", arrayLengths);

        // Assert
        Assert.AreEqual("uint256[5][]", param.AbiType);
    }

    [TestMethod]
    public void Constructor_WithInvalidTypeAndValidArrayLengths_ThrowsArgumentException()
    {
        // Arrange
        var arrayLengths = new[] { 5 };

        // Act & Assert
        var ex = Assert.ThrowsException<ArgumentException>(() =>
            new EvmParam(0, "param", "uint257", arrayLengths));

        StringAssert.Contains(ex.Message, "Invalid Solidity");
    }

    [TestMethod]
    public void ValidateValue_WithInvalidBasicType_ThrowsAbiValidationException()
    {
        var param = new EvmParam(0, "amount", "uint256");

        var ex = Assert.ThrowsException<AbiValidationException>(() =>
            param.ValidateValue("not a number"));

        Assert.AreEqual("uint256", ex.ExpectedType);
        Assert.AreEqual("not a number", ex.ValueProvided);
        Assert.AreEqual(typeof(string), ex.TypeProvided);
        Assert.AreEqual("Value of type System.String is not compatible with parameter type uint256: incompatible type", ex.Message);
        Assert.AreEqual("param-0 (amount)", ex.ValidationPath);
    }

    [TestMethod]
    public void ValidateValue_WithInvalidNestedType_ThrowsAbiValidationException()
    {
        var components = new List<EvmParam>
        {
            new(0, "user", new List<EvmParam>
            {
                new(0, "name", "string"),
                new(1, "balance", "uint256")
            }),
            new(1, "active", "bool")
        };

        var param = new EvmParam(0, "data", components);
        var value = (("John", "not a number"), true);

        var ex = Assert.ThrowsException<AbiValidationException>(() =>
            param.ValidateValue(value));

        Assert.AreEqual("uint256", ex.ExpectedType);
        Assert.AreEqual("not a number", ex.ValueProvided);
        Assert.AreEqual(typeof(string), ex.TypeProvided);
        Assert.AreEqual("Value of type System.String is not compatible with parameter type uint256: incompatible type", ex.Message);
        Assert.AreEqual("param-0 (data) -> param-0 (user) -> param-1 (balance)", ex.ValidationPath);
    }

    [TestMethod]
    public void ValidateValue_WithWrongTupleLength_ThrowsAbiValidationException()
    {
        var components = new List<EvmParam>
        {
            new(0, "name", "string"),
            new(1, "age", "uint256")
        };

        var param = new EvmParam(0, "person", components);
        var value = ValueTuple.Create("John");

        var ex = Assert.ThrowsException<AbiValidationException>(() =>
            param.ValidateValue(value));

        // Check all properties of the exception
        Assert.AreEqual("(string,uint256)", ex.ExpectedType);
        Assert.AreEqual(value, ex.ValueProvided);
        Assert.AreEqual(typeof(ValueTuple<string>), ex.TypeProvided);
        Assert.AreEqual("Value of type System.ValueTuple`1[System.String] is not compatible with parameter type (string,uint256): expected tuple of length 2", ex.Message);
        Assert.AreEqual("param-0 (person)", ex.ValidationPath);
    }

    [TestMethod]
    public void ValidateValue_ReturnsCorrectVisitCount()
    {
        // Test single param (count = 1)
        var sig = FunctionSignature.Parse("transfer(uint256 amount)");
        var param = sig.Parameters[0];
        var visitCount = param.ValidateValue(BigInteger.One);
        Assert.AreEqual(1, visitCount, "Single parameter should have visit count of 1");

        // Test simple tuple (count = 2)
        sig = FunctionSignature.Parse("process((bool valid, uint256 amount) data)");
        param = sig.Parameters[0];
        visitCount = param.ValidateValue((true, BigInteger.One));
        Assert.AreEqual(2, visitCount, "Simple tuple should have visit count of 2");
    }

    [TestMethod]
    public void DeepCount_WithComplexSignature_ReturnsCorrectCount()
    {
        var sig = FunctionSignature.Parse("process((bool active, uint256 balance) user, bool valid, uint256 amount)");
        var count = sig.Parameters.DeepCount();
        Assert.AreEqual(4, count, $"Should return 4 parameters; {sig.Parameters.GetCanonicalType()}");
    }

    [TestMethod]
    public void DeepCount_WithMoreComplexSig_ReturnsCorrectCount()
    {
        var sig = FunctionSignature.Parse("process(((bool active, uint256 balance) account, (string name, uint8 age) profile) user, bool valid, uint256 amount)");
        var count = sig.Parameters.DeepCount();
        Assert.AreEqual(6, count, $"Should return 6 parameters; {sig.Parameters.GetCanonicalType()}");
    }
}
