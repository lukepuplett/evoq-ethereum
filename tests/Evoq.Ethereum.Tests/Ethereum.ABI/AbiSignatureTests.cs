using System.Numerics;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiSignatureTests
{
    private readonly AbiTypeValidator validator;

    public AbiSignatureTests()
    {
        this.validator = new AbiTypeValidator();
    }

    [TestMethod]
    public void Constructor_WithNoInputsAndOutputs_CreatesCorrectSignature()
    {
        var signature = new AbiSignature(AbiItemType.Function, "noParams");

        Assert.AreEqual("noParams()", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("()", signature.GetCanonicalOutputsSignature());
        Assert.AreEqual(0, signature.Inputs.Count);
        Assert.AreEqual(0, signature.Outputs.Count);
    }

    [TestMethod]
    public void Constructor_WithNoInputsButOutput_CreatesCorrectSignature()
    {
        var signature = new AbiSignature(AbiItemType.Function, "getValue", "", "uint256");

        Assert.AreEqual("getValue()", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(uint256)", signature.GetCanonicalOutputsSignature());
        Assert.AreEqual(0, signature.Inputs.Count);
        Assert.AreEqual(1, signature.Outputs.Count);
        Assert.AreEqual("uint256", signature.Outputs[0].AbiType);
    }

    [TestMethod]
    public void Constructor_WithNameAndTypes_CreatesCorrectSignature()
    {
        var signature = new AbiSignature(AbiItemType.Function, "transfer", "address recipient, uint256 amount");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void Constructor_NormalizesTypes()
    {
        var signature = new AbiSignature(AbiItemType.Function, "example", "uint foo, int bar, byte[] data");
        Assert.AreEqual("example(uint256,int256,bytes1[])", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void Constructor_HandlesArrayTypes()
    {
        var signature = new AbiSignature(AbiItemType.Function, "example", "uint256[] numbers, address[3] addresses");
        Assert.AreEqual("example(uint256[],address[3])", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void Constructor_WithComplexTypes_CreatesCorrectSignature()
    {
        // Tuple parameter
        var signature = new AbiSignature(AbiItemType.Function, "setPerson", "(string name, uint256 age, address wallet)");
        Assert.AreEqual("setPerson(string,uint256,address)", signature.GetCanonicalInputsSignature());

        // Multiple tuples with arrays
        signature = new AbiSignature(AbiItemType.Function, "complexOp", "(address[] accounts, uint256 value)[], bool enabled");
        Assert.AreEqual("complexOp((address[],uint256)[],bool)", signature.GetCanonicalInputsSignature());

        // Nested tuples
        signature = new AbiSignature(AbiItemType.Function, "nestedData", "(string name, (uint256 x, uint256 y)[] points)");
        Assert.AreEqual("nestedData(string,(uint256,uint256)[])", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void Constructor_WithNamedBasicParameters_StripsNames()
    {
        var signature = new AbiSignature(AbiItemType.Function, "transfer", "address recipient, uint256 amount");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void Constructor_WithNamedTupleParameters_StripsNames()
    {
        var signature = new AbiSignature(AbiItemType.Function, "setPerson", "(string userName, uint256 userAge, address userWallet) person");
        Assert.AreEqual("setPerson((string,uint256,address))", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void Constructor_WithEmptyName_ThrowsException()
    {
        _ = new AbiSignature(AbiItemType.Function, "", "address,uint256");
    }

    [TestMethod]
    public void Constructor_WithWhitespace_NormalizesCorrectly()
    {
        var signature = new AbiSignature(AbiItemType.Function, "complex", "  (  string  name ,  uint256  age  )  data  ,  bool  enabled  ");
        Assert.AreEqual("complex((string,uint256),bool)", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void FromString_WithFullSignature_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "transfer(address,uint256) returns (bool)");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(bool)", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithFullSignatureWithNames_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "transfer(address recipient, uint256 amount)");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
    }

    [TestMethod]
    public void FromString_WithTupleParameter_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPerson((string,uint256,address))");
        var types = signature.GetInputParameterTypes();

        Assert.AreEqual("setPerson", signature.GetCanonicalInputsSignature()[..signature.GetCanonicalInputsSignature().IndexOf('(')]);
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithTupleParameterWithNames_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPerson((string userName, uint256 userAge, address userWallet))");
        var types = signature.GetInputParameterTypes();

        Assert.AreEqual("setPerson", signature.GetCanonicalInputsSignature()[..signature.GetCanonicalInputsSignature().IndexOf('(')]);
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithNestedTupleParameter_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPerson(((string,string),uint256,address))");
        var types = signature.GetInputParameterTypes();

        Assert.AreEqual("setPerson", signature.GetCanonicalInputsSignature()[..signature.GetCanonicalInputsSignature().IndexOf('(')]);
        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("((string,string),uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithTupleAndOtherParameters_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPersonAndActive((string,uint256,address),bool)");
        var types = signature.GetInputParameterTypes();

        Assert.AreEqual(2, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
        Assert.AreEqual("bool", types[1]);
    }

    [TestMethod]
    public void FromString_WithNestedTuples_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "complexFunction((uint256,(address,bool)[]))");
        var types = signature.GetInputParameterTypes();

        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(uint256,(address,bool)[])", types[0]);
    }

    [TestMethod]
    [DataRow("transfer(address,uint256)", "a9059cbb")]
    [DataRow("balanceOf(address)", "70a08231")]
    [DataRow("approve(address,uint256)", "095ea7b3")]
    [DataRow("getValue()", "20965255")]
    public void GetSelector_ReturnsCorrectBytes(string fullSignature, string expectedHex)
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, fullSignature);
        var selector = signature.GetSelector();
        CollectionAssert.AreEqual(Convert.FromHexString(expectedHex), selector.ToByteArray());
    }

    [TestMethod]
    public void ValidateParameters_WithValidValues_ReturnsTrue()
    {
        string m;

        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Function, "transfer(address,uint256)");
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
        var signature = AbiSignature.Parse(AbiItemType.Function, "transfer(address,uint256)");

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
        var signature = AbiSignature.Parse(AbiItemType.Function, "complexFunction(bytes32,uint8[],bool)");
        var values = new List<object?>
        {
            new byte[32],
            new byte[] { 1, 2, 3 },
            true
        };

        var withTooLarge = new List<object?> { new byte[33], values[1], values[2] };

        // Act & Assert
        Assert.IsTrue(signature.ValidateParameters(this.validator, values, out m), m);

        // Should fail when bytes32 array is too large
        Assert.IsTrue(signature.ValidateParameters(this.validator, withTooLarge, out m), m);
        Assert.IsFalse(signature.ValidateParameters(this.validator, withTooLarge, out m, tryEncoding: true), m);
    }

    [TestMethod]
    public void GetParameterTypes_WithTuple_ReturnsCorrectTypes()
    {
        // Single tuple parameter
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPerson((string,uint256,address))");
        var types = signature.GetInputParameterTypes();

        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void GetParameterTypes_WithTupleAndOtherTypes_ReturnsCorrectTypes()
    {
        // Tuple and bool parameters
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPersonData((string,uint256,address),bool)");
        var types = signature.GetInputParameterTypes();

        Assert.IsTrue(types.Any());
        Assert.AreEqual("(string,uint256,address)", types[0]);
        Assert.AreEqual(2, types.Length);
        Assert.AreEqual("bool", types[1]);
    }

    [TestMethod]
    public void FromString_WithSimpleReturnType_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "transfer(address,uint256) returns (bool)");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(bool)", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithMultipleReturnTypes_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getDetails(address) returns (string,uint256,bool)");
        Assert.AreEqual("getDetails(address)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(string,uint256,bool)", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithTupleReturnType_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getPerson(uint256) returns ((string,uint256,address))");
        Assert.AreEqual("getPerson(uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("((string,uint256,address))", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithNestedTupleReturnType_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getComplex(bytes32) returns ((string,uint256,(bool,address)))");
        Assert.AreEqual("getComplex(bytes32)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("((string,uint256,(bool,address)))", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithArrayReturnType_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getList(uint256) returns (address[])");
        Assert.AreEqual("getList(uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(address[])", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithTupleArrayReturnType_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getPersons(bool) returns ((string,uint256,address)[])");
        Assert.AreEqual("getPersons(bool)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("((string,uint256,address)[])", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithComplexReturnTypes_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getEverything() returns (uint256,(bool,string)[],(address,bytes32)[3])");
        Assert.AreEqual("getEverything()", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(uint256,(bool,string)[],(address,bytes32)[3])", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithEmptyInput_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getEverything()");
        Assert.AreEqual("getEverything()", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("()", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithNamedReturnParameters_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getValues(address) returns (uint256 balance, bool active)");
        Assert.AreEqual("getValues(address)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(uint256,bool)", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithNamedTupleReturnParameters_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getUserData(address) returns ((string name, uint256 age, bool active) userData)");
        Assert.AreEqual("getUserData(address)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("((string,uint256,bool))", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FromString_WithRealWorldExample_ParsesCorrectly()
    {
        // Based on the getAttestation function from EAS
        var signature = AbiSignature.Parse(AbiItemType.Function, "getAttestation(bytes32) returns ((bytes32,bytes32,uint64,uint64,uint64,bytes32,address,address,bool,bytes))");
        Assert.AreEqual("getAttestation(bytes32)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("((bytes32,bytes32,uint64,uint64,uint64,bytes32,address,address,bool,bytes))", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void Constructor_WithInputsAndOutputs_CreatesCorrectSignature()
    {
        var signature = new AbiSignature(AbiItemType.Function, "transfer", "address,uint256", "bool");
        Assert.AreEqual("transfer(address,uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(bool)", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void Constructor_WithComplexInputsAndOutputs_CreatesCorrectSignature()
    {
        var signature = new AbiSignature(
            AbiItemType.Function,
            "complexFunction",
            "address,(uint256,bytes32),((bool,(string,uint8))[]))",
            "(bytes32,uint64,address)");

        Assert.AreEqual("complexFunction(address,(uint256,bytes32),((bool,(string,uint8))[]))", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(bytes32,uint64,address)", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void GetOutputParameterTypes_WithSimpleTypes_ReturnsCorrectTypes()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getValues() returns (uint256,bool,address)");
        var types = signature.GetOutputParameterTypes();

        Assert.AreEqual(3, types.Length);
        Assert.AreEqual("uint256", types[0]);
        Assert.AreEqual("bool", types[1]);
        Assert.AreEqual("address", types[2]);
    }

    [TestMethod]
    public void GetOutputParameterTypes_WithTupleType_ReturnsCorrectTypes()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getPerson() returns ((string,uint256,address))");
        var types = signature.GetOutputParameterTypes();

        Assert.AreEqual(1, types.Length);
        Assert.AreEqual("(string,uint256,address)", types[0]);
    }

    [TestMethod]
    public void FromString_WithEmptyInputAndComplexReturnTypes_ParsesCorrectly()
    {
        var signature = AbiSignature.Parse(AbiItemType.Function, "getEverything() returns (uint256,(bool,string)[],(address,bytes32)[3])");
        Assert.AreEqual("getEverything()", signature.GetCanonicalInputsSignature());
        Assert.AreEqual("(uint256,(bool,string)[],(address,bytes32)[3])", signature.GetCanonicalOutputsSignature());
    }

    [TestMethod]
    public void FunctionSignature_CapturesParameterNames_ForBasicTypes()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Function, "transfer(address recipient, uint256 amount)");

        // Act
        var inputs = signature.Inputs;

        // Assert
        Assert.AreEqual(2, inputs.Count);
        Assert.AreEqual("recipient", inputs[0].Name);
        Assert.AreEqual("amount", inputs[1].Name);
        Assert.AreEqual("address", inputs[0].AbiType);
        Assert.AreEqual("uint256", inputs[1].AbiType);
    }

    [TestMethod]
    public void FunctionSignature_CapturesParameterNames_ForTupleTypes()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Function, "setPerson((string name, uint256 age, address wallet) person)");

        // Act
        var inputs = signature.Inputs;

        // Assert
        Assert.AreEqual(1, inputs.Count);
        Assert.AreEqual("person", inputs[0].Name);
        Assert.AreEqual("(string,uint256,address)", inputs[0].AbiType);

        // Verify we can access the tuple components
        Assert.IsTrue(inputs[0].TryParseComponents(out var components));
        Assert.IsNotNull(components);
        Assert.AreEqual(3, components.Count);
        Assert.AreEqual("name", components[0].Name);
        Assert.AreEqual("age", components[1].Name);
        Assert.AreEqual("wallet", components[2].Name);
    }

    [TestMethod]
    public void FunctionSignature_CapturesParameterNames_ForOutputParameters()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Function, "getValues(address account) returns (uint256 balance, bool active)");

        // Act
        var outputs = signature.Outputs;

        // Assert
        Assert.IsNotNull(outputs);
        Assert.AreEqual(2, outputs.Count);
        Assert.AreEqual("balance", outputs[0].Name);
        Assert.AreEqual("active", outputs[1].Name);
        Assert.AreEqual("uint256", outputs[0].AbiType);
        Assert.AreEqual("bool", outputs[1].AbiType);
    }

    [TestMethod]
    public void FunctionSignature_CapturesParameterNames_ForNestedTuples()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Function, "complexFunction((uint256 id, (string firstName, string lastName) fullName) userData)");

        // Act
        var inputs = signature.Inputs;

        // Assert
        Assert.AreEqual(1, inputs.Count);
        Assert.AreEqual("userData", inputs[0].Name);

        // Verify outer tuple components
        Assert.IsTrue(inputs[0].TryParseComponents(out var outerComponents));
        Assert.IsNotNull(outerComponents);
        Assert.AreEqual(2, outerComponents.Count);
        Assert.AreEqual("id", outerComponents[0].Name);
        Assert.AreEqual("fullName", outerComponents[1].Name);

        // Verify inner tuple components
        Assert.IsTrue(outerComponents[1].TryParseComponents(out var innerComponents));
        Assert.IsNotNull(innerComponents);
        Assert.AreEqual(2, innerComponents.Count);
        Assert.AreEqual("firstName", innerComponents[0].Name);
        Assert.AreEqual("lastName", innerComponents[1].Name);
    }

    [TestMethod]
    public void FunctionSignature_CapturesParameterNames_ForArraysWithNames()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Function, "batchTransfer(address[] recipients, uint256[] amounts)");

        // Act
        var inputs = signature.Inputs;

        // Assert
        Assert.AreEqual(2, inputs.Count);
        Assert.AreEqual("recipients", inputs[0].Name);
        Assert.AreEqual("amounts", inputs[1].Name);
        Assert.AreEqual("address[]", inputs[0].AbiType);
        Assert.AreEqual("uint256[]", inputs[1].AbiType);
    }

    [TestMethod]
    public void EventSignature_SimpleEvent_ParsesCorrectly()
    {
        // Arrange & Act
        var signature = AbiSignature.Parse(AbiItemType.Event, "Transfer(address indexed from, address indexed to, uint256 value)");

        // Assert
        Assert.AreEqual("Transfer(address,address,uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual(3, signature.Inputs.Count);
        Assert.IsTrue(signature.Inputs[0].IsIndexed);
        Assert.IsTrue(signature.Inputs[1].IsIndexed);
        Assert.IsFalse(signature.Inputs[2].IsIndexed);
    }

    [TestMethod]
    public void EventSignature_WithoutIndexed_ParsesCorrectly()
    {
        // Arrange & Act
        var signature = AbiSignature.Parse(AbiItemType.Event, "ValueUpdated(uint256 oldValue, uint256 newValue)");

        // Assert
        Assert.AreEqual("ValueUpdated(uint256,uint256)", signature.GetCanonicalInputsSignature());
        Assert.AreEqual(2, signature.Inputs.Count);
        Assert.IsFalse(signature.Inputs[0].IsIndexed);
        Assert.IsFalse(signature.Inputs[1].IsIndexed);
    }

    [TestMethod]
    public void EventSignature_WithTupleParameter_ParsesCorrectly()
    {
        // Arrange & Act
        var signature = AbiSignature.Parse(AbiItemType.Event, "PersonUpdated((string name, uint256 age, address wallet) indexed person)");

        // Assert
        Assert.AreEqual("PersonUpdated((string,uint256,address))", signature.GetCanonicalInputsSignature());
        Assert.AreEqual(1, signature.Inputs.Count);
        Assert.IsTrue(signature.Inputs[0].IsIndexed);
        Assert.IsTrue(signature.Inputs[0].TryParseComponents(out var components));
        Assert.AreEqual(3, components!.Count);
    }

    [TestMethod]
    public void EventSignature_WithComplexTypes_ParsesCorrectly()
    {
        // Arrange & Act
        var signature = AbiSignature.Parse(AbiItemType.Event, "ComplexUpdate(bytes32 indexed id, (uint256 value, bool active)[] data)");

        // Assert
        Assert.AreEqual("ComplexUpdate(bytes32,(uint256,bool)[])", signature.GetCanonicalInputsSignature());
        Assert.AreEqual(2, signature.Inputs.Count);
        Assert.IsTrue(signature.Inputs[0].IsIndexed);
        Assert.IsFalse(signature.Inputs[1].IsIndexed);
    }

    [TestMethod]
    public void GetSignatureHash_ForEvent_ReturnsCorrectHash()
    {
        // Common ERC20 Transfer event
        var signature = AbiSignature.Parse(AbiItemType.Event, "Transfer(address indexed from, address indexed to, uint256 value)");
        var expectedHash = "0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef";

        var selector = signature.GetSignatureHash();
        Assert.AreEqual(expectedHash[2..], Convert.ToHexString(selector.ToByteArray()).ToLower());
    }

    [TestMethod]
    public void EventSignature_WithMaxIndexedParameters_ParsesCorrectly()
    {
        // Solidity allows up to 3 indexed parameters for events
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "TripleIndex(address indexed one, bytes32 indexed two, uint256 indexed three, string four)");

        Assert.AreEqual(4, signature.Inputs.Count);
        Assert.IsTrue(signature.Inputs[0].IsIndexed);
        Assert.IsTrue(signature.Inputs[1].IsIndexed);
        Assert.IsTrue(signature.Inputs[2].IsIndexed);
        Assert.IsFalse(signature.Inputs[3].IsIndexed);
    }

    [TestMethod]
    public void EventSignature_WithAnonymousFlag_ParsesCorrectly()
    {
        // Arrange & Act
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "AnonymousEvent(address indexed sender, uint256 value) anonymous");

        // Assert
        Assert.AreEqual("AnonymousEvent(address,uint256)", signature.GetCanonicalInputsSignature());
        Assert.IsTrue(signature.IsAnonymous);
        Assert.AreEqual(2, signature.Inputs.Count);
        Assert.IsTrue(signature.Inputs[0].IsIndexed);
        Assert.IsFalse(signature.Inputs[1].IsIndexed);
    }

    [TestMethod]
    public void EventSignature_RealWorldExamples_ParseCorrectly()
    {
        // ERC721 Transfer event
        var transfer = AbiSignature.Parse(AbiItemType.Event,
            "Transfer(address indexed from, address indexed to, uint256 indexed tokenId)");
        Assert.AreEqual("Transfer(address,address,uint256)", transfer.GetCanonicalInputsSignature());
        Assert.IsTrue(transfer.Inputs.All(i => i.IsIndexed));

        // ERC1155 TransferSingle event
        var transferSingle = AbiSignature.Parse(AbiItemType.Event,
            "TransferSingle(address indexed operator, address indexed from, address indexed to, uint256 id, uint256 value)");
        Assert.AreEqual("TransferSingle(address,address,address,uint256,uint256)",
            transferSingle.GetCanonicalInputsSignature());
        Assert.AreEqual(3, transferSingle.Inputs.Count(i => i.IsIndexed));
    }

    [TestMethod]
    public void EventSignature_WithIndexedTuple_ParsesCorrectly()
    {
        // Arrange & Act
        // This is valid - indexing the entire tuple
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "PersonEvent((string name, address wallet, uint256 age) indexed person)");

        // Assert
        Assert.AreEqual("PersonEvent((string,address,uint256))", signature.GetCanonicalInputsSignature());
        Assert.AreEqual(1, signature.Inputs.Count);
        Assert.IsTrue(signature.Inputs[0].IsIndexed);
    }

    [TestMethod]
    public void EventSignature_IdentifiesTopicAndDataParameters()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "Transfer(address indexed from, address indexed to, uint256 value)");

        // Act
        var topicParams = signature.Inputs.Where(p => p.IsIndexed).ToList();
        var dataParams = signature.Inputs.Where(p => !p.IsIndexed).ToList();

        // Assert
        Assert.AreEqual(2, topicParams.Count);
        Assert.AreEqual("from", topicParams[0].Name);
        Assert.AreEqual("to", topicParams[1].Name);

        Assert.AreEqual(1, dataParams.Count);
        Assert.AreEqual("value", dataParams[0].Name);
    }

    [TestMethod]
    public void EventSignature_WithAnonymousEvent_HasNoSignatureTopicButStillHasIndexedTopics()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "AnonymousTransfer(address indexed from, uint256 value) anonymous");

        // Act
        var topicParams = signature.Inputs.Where(p => p.IsIndexed).ToList();
        var dataParams = signature.Inputs.Where(p => !p.IsIndexed).ToList();

        // Assert
        Assert.IsTrue(signature.IsAnonymous); // No topics[0]
        Assert.AreEqual(1, topicParams.Count); // But still has indexed param in topics[1]
        Assert.AreEqual("from", topicParams[0].Name);

        Assert.AreEqual(1, dataParams.Count);
        Assert.AreEqual("value", dataParams[0].Name);
    }

    [TestMethod]
    public void EventSignature_WithIndexedTuple_CountsAsOneTopicSlot()
    {
        // Arrange
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "ComplexEvent((uint256 id, address owner) indexed data, address indexed user, uint256 value)");

        // Act
        var topicParams = signature.Inputs.Where(p => p.IsIndexed).ToList();
        var dataParams = signature.Inputs.Where(p => !p.IsIndexed).ToList();

        // Assert
        Assert.AreEqual(2, topicParams.Count); // Tuple counts as one topic
        Assert.AreEqual("data", topicParams[0].Name);
        Assert.AreEqual("user", topicParams[1].Name);

        Assert.AreEqual(1, dataParams.Count);
        Assert.AreEqual("value", dataParams[0].Name);
    }

    [TestMethod]
    public void EventSignature_WithMixedIndexedParameters_SetsIsIndexedCorrectly()
    {
        // Arrange & Act
        var signature = AbiSignature.Parse(AbiItemType.Event,
            "SchemaRegistered(bytes32 indexed uid, address indexed registerer, (bytes32 uid, address resolver, bool revocable, string schema) schema)");

        // Assert
        Assert.AreEqual(3, signature.Inputs.Count, "Should have exactly 3 parameters");

        // First parameter (indexed bytes32)
        Assert.AreEqual("uid", signature.Inputs[0].Name, "First parameter should be named 'uid'");
        Assert.AreEqual("bytes32", signature.Inputs[0].AbiType, "First parameter should be bytes32");
        Assert.IsTrue(signature.Inputs[0].IsIndexed, "First parameter should be indexed");

        // Second parameter (indexed address)
        Assert.AreEqual("registerer", signature.Inputs[1].Name, "Second parameter should be named 'registerer'");
        Assert.AreEqual("address", signature.Inputs[1].AbiType, "Second parameter should be address");
        Assert.IsTrue(signature.Inputs[1].IsIndexed, "Second parameter should be indexed");

        // Third parameter (non-indexed tuple)
        Assert.AreEqual("schema", signature.Inputs[2].Name, "Third parameter should be named 'schema'");
        Assert.IsFalse(signature.Inputs[2].IsIndexed, "Tuple parameter should not be indexed");

        // Verify tuple components
        Assert.IsTrue(signature.Inputs[2].TryParseComponents(out var components), "Should be able to parse tuple components");
        Assert.IsNotNull(components);
        Assert.AreEqual(4, components.Count, "Tuple should have 4 components");

        // Verify none of the tuple components are marked as indexed
        foreach (var component in components)
        {
            Assert.IsFalse(component.IsIndexed, $"Tuple component {component.Name} should not be indexed");
        }

        // Verify canonical signature is correct
        Assert.AreEqual(
            "SchemaRegistered(bytes32,address,(bytes32,address,bool,string))",
            signature.GetCanonicalInputsSignature(),
            "Canonical signature should match expected format");
    }
}