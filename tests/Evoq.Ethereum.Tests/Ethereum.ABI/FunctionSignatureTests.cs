using System.Numerics;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class FunctionSignatureTests
{
    private readonly AbiTypeValidator validator;

    public FunctionSignatureTests()
    {
        this.validator = new AbiEncoder().Validator;
    }

    [TestMethod]
    public void Constructor_WithNameAndTypes_CreatesCorrectSignature()
    {
        var signature = new FunctionSignature("transfer", "address recipient, uint256 amount");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void Constructor_NormalizesTypes()
    {
        var signature = new FunctionSignature("example", "uint foo, int bar, byte[] data");
        Assert.AreEqual("example(uint,int,byte[])", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void Constructor_HandlesArrayTypes()
    {
        var signature = new FunctionSignature("example", "uint256[] numbers, address[3] addresses");
        Assert.AreEqual("example(uint256[],address[3])", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void Constructor_WithComplexTypes_CreatesCorrectSignature()
    {
        // Tuple parameter
        var signature = new FunctionSignature("setPerson", "(string name, uint256 age, address wallet)");
        Assert.AreEqual("setPerson(string,uint256,address)", signature.GetCanonicalSignature());

        // Multiple tuples with arrays
        signature = new FunctionSignature("complexOp", "(address[] accounts, uint256 value)[], bool enabled");
        Assert.AreEqual("complexOp((address[],uint256)[],bool)", signature.GetCanonicalSignature());

        // Nested tuples
        signature = new FunctionSignature("nestedData", "(string name, (uint256 x, uint256 y)[] points)");
        Assert.AreEqual("nestedData(string,(uint256,uint256)[])", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void Constructor_WithNamedBasicParameters_StripsNames()
    {
        var signature = new FunctionSignature("transfer", "address recipient, uint256 amount");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void Constructor_WithNamedTupleParameters_StripsNames()
    {
        var signature = new FunctionSignature("setPerson", "(string userName, uint256 userAge, address userWallet) person");
        Assert.AreEqual("setPerson((string,uint256,address))", signature.GetCanonicalSignature());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithEmptyName_ThrowsException()
    {
        _ = new FunctionSignature("", "address,uint256");
    }

    [TestMethod]
    public void Constructor_WithWhitespace_NormalizesCorrectly()
    {
        var signature = new FunctionSignature("complex", "  (  string  name ,  uint256  age  )  data  ,  bool  enabled  ");
        Assert.AreEqual("complex((string,uint256),bool)", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void FromString_WithFullSignature_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("transfer(address,uint256)");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void FromString_WithFullSignatureWithNames_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("transfer(address recipient, uint256 amount)");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalSignature());
    }

    [TestMethod]
    public void FromString_WithTupleParameter_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("setPerson((string,uint256,address))");
        var types = signature.GetParameterTypes();

        Assert.AreEqual("setPerson", signature.GetCanonicalSignature()[..signature.GetCanonicalSignature().IndexOf('(')]);
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithTupleParameterWithNames_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("setPerson((string userName, uint256 userAge, address userWallet))");
        var types = signature.GetParameterTypes();

        Assert.AreEqual("setPerson", signature.GetCanonicalSignature()[..signature.GetCanonicalSignature().IndexOf('(')]);
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithNestedTupleParameter_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("setPerson(((string,string),uint256,address))");
        var types = signature.GetParameterTypes();

        Assert.AreEqual("setPerson", signature.GetCanonicalSignature()[..signature.GetCanonicalSignature().IndexOf('(')]);
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("((string,string),uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithTupleAndOtherParameters_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("setPersonAndActive((string,uint256,address),bool)");
        var types = signature.GetParameterTypes();

        Assert.AreEqual(2, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
        Assert.AreEqual("bool", types[1]);
    }

    [TestMethod]
    public void FromString_WithNestedTuples_ParsesCorrectly()
    {
        var signature = FunctionSignature.Parse("complexFunction((uint256,(address,bool)[]))");
        var types = signature.GetParameterTypes();

        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(uint256,(address,bool)[])", types[0]);
    }

    [TestMethod]
    [DataRow("transfer(address,uint256)", "a9059cbb")]
    [DataRow("balanceOf(address)", "70a08231")]
    [DataRow("approve(address,uint256)", "095ea7b3")]
    public void GetSelector_ReturnsCorrectBytes(string fullSignature, string expectedHex)
    {
        var signature = FunctionSignature.Parse(fullSignature);
        var selector = signature.GetSelector();
        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), selector);
    }

    [TestMethod]
    public void ValidateParameters_WithValidValues_ReturnsTrue()
    {
        string m;

        // Arrange
        var signature = FunctionSignature.Parse("transfer(address,uint256)");
        var values = new List<object?>
        {
            new EthereumAddress("0x1234567890123456789012345678901234567890"),
            BigInteger.Parse("1000000000000000000")
        };

        // Act & Assert
        Assert.IsTrue(signature.ValidateParameters(this.validator, values, out m), m);
    }

    [TestMethod]
    public void ValidateParameters_WithInvalidTypes_ReturnsFalse()
    {
        string m;

        // Arrange
        var signature = FunctionSignature.Parse("transfer(address,uint256)");

        // Act & Assert
        Assert.IsFalse(signature.ValidateParameters(this.validator, new List<object?> { "not an address", 123 }, out m), m); // Wrong types
        Assert.IsFalse(signature.ValidateParameters(this.validator, new List<object?> { new EthereumAddress("0x1234567890123456789012345678901234567890") }, out m), m); // Missing parameter
        Assert.IsFalse(signature.ValidateParameters(this.validator, new List<object?> { null, BigInteger.One }, out m), m); // Null not allowed
    }

    [TestMethod]
    public void ValidateParameters_WithComplexTypes_ValidatesCorrectly()
    {
        string m;

        // Arrange
        var signature = FunctionSignature.Parse("complexFunction(bytes32,uint8[],bool)");
        var values = new List<object?>
        {
            new byte[32],
            new byte[] { 1, 2, 3 },
            true
        };

        var with31 = new List<object?> { new byte[31], values[1], values[2] };

        // Act & Assert
        Assert.IsTrue(signature.ValidateParameters(this.validator, values, out m), m);

        // OK if simple type check but not if tryEncoding
        Assert.IsTrue(signature.ValidateParameters(this.validator, with31, out m), m);
        Assert.IsFalse(signature.ValidateParameters(this.validator, with31, out m, tryEncoding: true), m);
    }

    [TestMethod]
    public void GetParameterTypes_WithTuple_ReturnsCorrectTypes()
    {
        // Single tuple parameter
        var signature = FunctionSignature.Parse("setPerson((string,uint256,address))");
        var types = signature.GetParameterTypes();

        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void GetParameterTypes_WithTupleAndOtherTypes_ReturnsCorrectTypes()
    {
        // Tuple and bool parameters
        var signature = FunctionSignature.Parse("setPersonData((string,uint256,address),bool)");
        var types = signature.GetParameterTypes();

        Assert.IsTrue(types.Any());
        Assert.AreEqual("(string,uint256,address)", types[0]);
        Assert.AreEqual(2, types.Length);
        Assert.AreEqual("bool", types[1]);
    }
}