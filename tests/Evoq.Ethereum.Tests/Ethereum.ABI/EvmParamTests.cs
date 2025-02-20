using System.Numerics;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class EvmParamTests
{
    private readonly AbiTypeValidator validator;

    public EvmParamTests()
    {
        this.validator = new AbiEncoderV2().Validator;
    }

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
        Assert.ThrowsException<ArgumentException>(() =>
            new EvmParam(0, "amount", new List<EvmParam>()));
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
    public void ValidateValue_WithInvalidBasicType_ThrowsAbiValidationException()
    {
        var param = new EvmParam(0, "amount", "uint256");

        var ex = Assert.ThrowsException<AbiValidationException>(() =>
            param.ValidateValue(this.validator, "not a number"));

        Assert.AreEqual("uint256", ex.ExpectedType);
        Assert.AreEqual("not a number", ex.ValueProvided);
        Assert.AreEqual(typeof(string), ex.TypeProvided);
        Assert.IsTrue(ex.Message.StartsWith("Value of type System.String is not compatible with parameter type uint256: incompatible type"));
        Assert.AreEqual("param-0 (amount)", ex.ValidationPath);
    }

    [TestMethod]
    public void ValidateValue_WithInvalidNestedType_ThrowsAbiValidationException()
    {
        // "((string name, uint256 balance) user, bool active) data"
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
        var value = (("John", "not a number"), true); // wrong type for balance

        Assert.AreEqual("uint256", components[0].Components![1].AbiType);
        Assert.AreEqual(typeof(string), value.Item1.Item1.GetType(), "name should be a string");
        Assert.AreEqual(typeof(string), value.Item1.Item2.GetType(), "balance should be a string"); // wrong type
        Assert.AreEqual(typeof(bool), value.Item2.GetType(), "active should be a bool");

        var ex = Assert.ThrowsException<AbiValidationException>(() =>
            param.ValidateValue(this.validator, value));

        Assert.IsTrue(ex.Message.StartsWith("Value of type System.String is not compatible with parameter type uint256: incompatible type"));
        Assert.AreEqual("param-0 (data) -> param-0 (user) -> param-1 (balance)", ex.ValidationPath);
        Assert.AreEqual("uint256", ex.ExpectedType);
        Assert.AreEqual("not a number", ex.ValueProvided);
        Assert.AreEqual(typeof(string), ex.TypeProvided);
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
            param.ValidateValue(this.validator, value));

        // Check all properties of the exception
        Assert.AreEqual("(string,uint256)", ex.ExpectedType);
        Assert.AreEqual(value, ex.ValueProvided);
        Assert.AreEqual(typeof(ValueTuple<string>), ex.TypeProvided);
        Assert.IsTrue(ex.Message.StartsWith("Value of type System.ValueTuple`1[System.String] is not compatible with parameter type (string,uint256): expected tuple of length 2"));
        Assert.AreEqual("param-0 (person)", ex.ValidationPath);
    }

    [TestMethod]
    public void ValidateValue_ReturnsCorrectVisitCount()
    {
        // Test single param (count = 1)
        var sig = FunctionSignature.Parse("transfer(uint256 amount)");
        var param = sig.Parameters[0];
        var visitCount = param.ValidateValue(this.validator, BigInteger.One);
        Assert.AreEqual(1, visitCount, "Single parameter should have visit count of 1");

        // Test simple tuple (count = 2)
        sig = FunctionSignature.Parse("process((bool valid, uint256 amount) data)");
        param = sig.Parameters[0];
        visitCount = param.ValidateValue(this.validator, (true, BigInteger.One));
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

    [TestMethod]
    [DataRow("string", true)]                // Basic dynamic type
    [DataRow("bytes", true)]                 // Basic dynamic type
    [DataRow("uint256", false)]             // Basic static type
    [DataRow("bool", false)]                // Basic static type
    [DataRow("address", false)]             // Basic static type
    [DataRow("bytes32", false)]             // Fixed bytes (static)
    public void IsDynamic_BasicTypes_ReturnsExpectedResult(string type, bool expectedDynamic)
    {
        var param = new EvmParam(0, "", type);
        Assert.AreEqual(expectedDynamic, param.IsDynamic);
    }

    [TestMethod]
    [DataRow("uint256[]", true)]            // Dynamic array
    [DataRow("bool[2]", false)]             // Fixed array of static type
    [DataRow("string[2]", true)]            // Fixed array of dynamic type
    [DataRow("bytes32[][]", true)]          // Nested dynamic array
    [DataRow("address[2][3]", false)]       // Fixed nested array
    public void IsDynamic_ArrayTypes_ReturnsExpectedResult(string type, bool expectedDynamic)
    {
        var param = new EvmParam(0, "", type);
        Assert.AreEqual(expectedDynamic, param.IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_TupleWithAllStaticComponents_ReturnsFalse()
    {
        var components = new List<EvmParam>
        {
            new EvmParam(0, "value", "uint256"),
            new EvmParam(1, "flag", "bool"),
            new EvmParam(2, "addr", "address")
        };
        var param = new EvmParam(0, "tuple", components);

        Assert.IsFalse(param.IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_TupleWithDynamicComponent_ReturnsTrue()
    {
        var components = new List<EvmParam>
        {
            new EvmParam(0, "value", "uint256"),
            new EvmParam(1, "name", "string"),  // Dynamic component
            new EvmParam(2, "flag", "bool")
        };
        var param = new EvmParam(0, "tuple", components);

        Assert.IsTrue(param.IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_NestedTupleWithAllStaticComponents_ReturnsFalse()
    {
        var innerComponents = new List<EvmParam>
        {
            new EvmParam(0, "value", "uint256"),
            new EvmParam(1, "flag", "bool")
        };
        var outerComponents = new List<EvmParam>
        {
            new EvmParam(0, "data", innerComponents),
            new EvmParam(1, "addr", "address")
        };
        var param = new EvmParam(0, "nested", outerComponents);

        Assert.IsFalse(param.IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_NestedTupleWithDynamicComponent_ReturnsTrue()
    {
        var innerComponents = new List<EvmParam>
        {
            new EvmParam(0, "value", "uint256"),
            new EvmParam(1, "name", "string")  // Dynamic component
        };
        var outerComponents = new List<EvmParam>
        {
            new EvmParam(0, "data", innerComponents),
            new EvmParam(1, "addr", "address")
        };
        var param = new EvmParam(0, "nested", outerComponents);

        Assert.IsTrue(param.IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_VeryNestedTupleWithDynamicComponent_ReturnsTrue()
    {
        var parameters = EvmParameters.Parse("(((uint8 age, (string first, string last) name) profile, uint256 id, bool active) user)");

        Assert.IsTrue(parameters.First().IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_DisjointedNestedTuple_ReturnsTrue()
    {
        // "disjointed" because the tuple within the tuple is via an array

        // logs is a tuple with two components, entries and count
        // entries is an array of tuples with two components, eventId and eventType
        // count is a uint256

        var parameters = EvmParameters.Parse("(((uint256 eventId, uint8 eventType)[] entries, uint256 count) logs)");

        Assert.AreEqual("logs", parameters.First().Name);
        Assert.IsNotNull(parameters.First().Components);
        Assert.IsFalse(parameters.First().IsArray);                         // logs is not an array
        Assert.AreEqual("entries", parameters.First().Components!.First().Name); // entries is the first component
        Assert.IsTrue(parameters.First().Components!.First().IsArray);           // entries is an array
    }

    [TestMethod]
    public void IsDynamic_TupleWithDynamicArrayComponent_ReturnsTrue()
    {
        var components = new List<EvmParam>
        {
            new EvmParam(0, "value", "uint256"),
            new EvmParam(1, "array", "uint256[]"),  // Dynamic array
            new EvmParam(2, "flag", "bool")
        };
        var param = new EvmParam(0, "tuple", components);

        Assert.IsTrue(param.IsDynamic);
    }

    [TestMethod]
    public void IsDynamic_TupleWithFixedArrayOfDynamicType_ReturnsTrue()
    {
        var components = new List<EvmParam>
        {
            new EvmParam(0, "value", "uint256"),
            new EvmParam(1, "names", "string[2]"),  // Fixed array of dynamic type
            new EvmParam(2, "flag", "bool")
        };
        var param = new EvmParam(0, "tuple", components);

        Assert.IsTrue(param.IsDynamic);
    }
}
