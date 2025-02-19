namespace Evoq.Ethereum.ABI;

[TestClass]
public class EvmParametersTests
{
    [TestMethod]
    public void SplitParams_WithSingleString_ReturnsCorrectTuples()
    {
        var parameterString = "(string)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("string", result[0].AbiType, "Parameter should be of type 'string'");
        Assert.AreEqual(0, result[0].Position, "Parameter should be at position 0");
        Assert.AreEqual("", result[0].Name, "Parameter should have empty name");
        Assert.IsNull(result[0].Components, "Parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithSingleStringArray_ReturnsCorrectTuples()
    {
        var parameterString = "(string[2])";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("string[2]", result[0].AbiType, "Parameter should be of type 'string[2]'");
        Assert.AreEqual(0, result[0].Position, "Parameter should be at position 0");
        Assert.AreEqual("", result[0].Name, "Parameter should have empty name");
        Assert.IsNull(result[0].Components, "Parameter should not have components");
        Assert.AreEqual(1, result[0].ArrayLengths!.Count, "Parameter should have 1 array length");
        Assert.AreEqual(2, result[0].ArrayLengths![0], "Array length should be 2");
    }

    [TestMethod]
    public void SplitParams_WithTwoStrings_ReturnsCorrectTuples()
    {
        var parameterString = "(string,uint256)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");
        Assert.AreEqual("string", result[0].AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, result[0].Position, "First parameter should be at position 0");
        Assert.AreEqual("", result[0].Name, "First parameter should have empty name");
        Assert.IsNull(result[0].Components, "First parameter should not have components");

        Assert.AreEqual("uint256", result[1].AbiType, "Second parameter should be of type 'uint256'");
        Assert.AreEqual(1, result[1].Position, "Second parameter should be at position 1");
        Assert.AreEqual("", result[1].Name, "Second parameter should have empty name");
        Assert.IsNull(result[1].Components, "Second parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithNamedTuple_ReturnsCorrectNamedTuple()
    {
        var parameterString = "((string name, uint256 value) item)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("(string,uint256)", result[0].AbiType, "Parameter should be of type '(string,uint256)'");
        Assert.AreEqual(0, result[0].Position, "Parameter should be at position 0");
        Assert.AreEqual("item", result[0].Name, "Parameter should have name 'item'");
        Assert.IsTrue(result[0].Components != null, "Parameter should have components");
        Assert.AreEqual(2, result[0].Components!.Count, "Tuple should have exactly two components");

        Assert.AreEqual("string", result[0].Components![0].AbiType, "First component should be of type 'string'");
        Assert.AreEqual(0, result[0].Components![0].Position, "First component should be at position 0");
        Assert.AreEqual("name", result[0].Components![0].Name, "First component should have name 'name'");

    }

    [TestMethod]
    public void SplitParams_WithNamedParam_ReturnsCorrectTuples()
    {
        var parameterString = "(string name)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("string", result[0].AbiType, "Parameter should be of type 'string'");
        Assert.AreEqual(0, result[0].Position, "Parameter should be at position 0");
        Assert.AreEqual("name", result[0].Name, "Parameter should have name 'name'");
        Assert.IsNull(result[0].Components, "Parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithBadlyFormedNamedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(   (  string  name ,  uint256  age  )  data  ,  bool  enabled )";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");
        Assert.AreEqual("(string,uint256)", result[0].AbiType, "First parameter should be of type '(string,uint256)'");
        Assert.AreEqual("data", result[0].Name, "First parameter should have name 'data'");

    }

    [TestMethod]
    public void SplitParams_WithNamedParams_ReturnsCorrectTuples()
    {
        var parameterString = "(string name, uint256 value)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");
        Assert.AreEqual("string", result[0].AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, result[0].Position, "First parameter should be at position 0");
        Assert.AreEqual("name", result[0].Name, "First parameter should have name 'name'");
        Assert.IsNull(result[0].Components, "First parameter should not have components");

        Assert.AreEqual("uint256", result[1].AbiType, "Second parameter should be of type 'uint256'");
        Assert.AreEqual(1, result[1].Position, "Second parameter should be at position 1");
        Assert.AreEqual("value", result[1].Name, "Second parameter should have name 'value'");
        Assert.IsNull(result[1].Components, "Second parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithNestedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(string name, (uint256 value, bool valid) ticket)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        var nameParam = result[0];
        Assert.AreEqual("string", nameParam.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, nameParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("name", nameParam.Name, "First parameter should have name 'name'");
        Assert.IsNull(nameParam.Components, "First parameter should not have components");

        var ticketParam = result[1];
        Assert.AreEqual("(uint256,bool)", ticketParam.AbiType, "Second parameter should be of type '(uint256,bool)'");
        Assert.AreEqual(1, ticketParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("ticket", ticketParam.Name, "Second parameter should have name 'ticket'");
        Assert.IsTrue(ticketParam.Components != null, "Second parameter should have components");
        Assert.AreEqual(2, ticketParam.Components!.Count, "Tuple should have exactly two components");

        var valueParam = ticketParam.Components![0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First tuple component should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First tuple component should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First tuple component should have name 'value'");
        Assert.IsNull(valueParam.Components, "First tuple component should not have components");

        var validParam = ticketParam.Components![1];
        Assert.AreEqual("bool", validParam.AbiType, "Second tuple component should be of type 'bool'");
        Assert.AreEqual(1, validParam.Position, "Second tuple component should be at position 1");
        Assert.AreEqual("valid", validParam.Name, "Second tuple component should have name 'valid'");
        Assert.IsNull(validParam.Components, "Second tuple component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithUnnamedNestedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(string,(uint256,bool))";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        var stringParam = result[0];
        Assert.AreEqual("string", stringParam.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, stringParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("", stringParam.Name, "First parameter should have empty name");
        Assert.IsNull(stringParam.Components, "First parameter should not have components");

        var tupleParam = result[1];
        Assert.AreEqual("(uint256,bool)", tupleParam.AbiType, "Second parameter should be of type '(uint256,bool)'");
        Assert.AreEqual(1, tupleParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("", tupleParam.Name, "Second parameter should have empty name");
        Assert.IsTrue(tupleParam.Components != null, "Second parameter should have components");
        Assert.AreEqual(2, tupleParam.Components!.Count, "Tuple should have exactly two components");

        var uintParam = tupleParam.Components![0];
        Assert.AreEqual("uint256", uintParam.AbiType, "First tuple component should be of type 'uint256'");
        Assert.AreEqual(0, uintParam.Position, "First tuple component should be at position 0");
        Assert.AreEqual("", uintParam.Name, "First tuple component should have empty name");
        Assert.IsNull(uintParam.Components, "First tuple component should not have components");

        var boolParam = tupleParam.Components![1];
        Assert.AreEqual("bool", boolParam.AbiType, "Second tuple component should be of type 'bool'");
        Assert.AreEqual(1, boolParam.Position, "Second tuple component should be at position 1");
        Assert.AreEqual("", boolParam.Name, "Second tuple component should have empty name");
        Assert.IsNull(boolParam.Components, "Second tuple component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithNestedTupleOnly_ReturnsCorrectTuples()
    {
        var parameterString = "(uint256 value, (bool valid, address owner) details)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        // First parameter (uint256 value)
        var valueParam = result[0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First parameter should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First parameter should have name 'value'");
        Assert.IsNull(valueParam.Components, "First parameter should not have components");

        // Second parameter (the nested tuple named 'details')
        var detailsParam = result[1];
        Assert.AreEqual("(bool,address)", detailsParam.AbiType, "Second parameter should be of type '(bool,address)'");
        Assert.AreEqual(1, detailsParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("details", detailsParam.Name, "Second parameter should have name 'details'");
        Assert.IsTrue(detailsParam.Components != null, "Second parameter should have components");
        Assert.AreEqual(2, detailsParam.Components!.Count, "Details tuple should have exactly two components");

        // First component of details (bool valid)
        var validParam = detailsParam.Components![0];
        Assert.AreEqual("bool", validParam.AbiType, "First details component should be of type 'bool'");
        Assert.AreEqual(0, validParam.Position, "First details component should be at position 0");
        Assert.AreEqual("valid", validParam.Name, "First details component should have name 'valid'");
        Assert.IsNull(validParam.Components, "First details component should not have components");

        // Second component of details (address owner)
        var ownerParam = detailsParam.Components![1];
        Assert.AreEqual("address", ownerParam.AbiType, "Second details component should be of type 'address'");
        Assert.AreEqual(1, ownerParam.Position, "Second details component should be at position 1");
        Assert.AreEqual("owner", ownerParam.Name, "Second details component should have name 'owner'");
        Assert.IsNull(ownerParam.Components, "Second details component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithDoubleNestedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(string name, (uint256 value, (bool valid, address owner) details) ticket)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        // First parameter (string name)
        var nameParam = result[0];
        Assert.AreEqual("string", nameParam.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, nameParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("name", nameParam.Name, "First parameter should have name 'name'");
        Assert.IsNull(nameParam.Components, "First parameter should not have components");

        // Second parameter (the outer tuple named 'ticket')
        var ticketParam = result[1];
        Assert.AreEqual("(uint256,(bool,address))", ticketParam.AbiType, "Second parameter should be of type '(uint256,(bool,address))'");
        Assert.AreEqual(1, ticketParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("ticket", ticketParam.Name, "Second parameter should have name 'ticket'");
        Assert.IsTrue(ticketParam.Components != null, "Second parameter should have components");
        Assert.AreEqual(2, ticketParam.Components!.Count, "Ticket tuple should have exactly two components");

        // First component of ticket (uint256 value)
        var valueParam = ticketParam.Components![0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First ticket component should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First ticket component should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First ticket component should have name 'value'");
        Assert.IsNull(valueParam.Components, "First ticket component should not have components");

        // Second component of ticket (the inner tuple named 'details')
        var detailsParam = ticketParam.Components![1];
        Assert.AreEqual("(bool,address)", detailsParam.AbiType, "Second ticket component should be of type '(bool,address)'");
        Assert.AreEqual(1, detailsParam.Position, "Second ticket component should be at position 1");
        Assert.AreEqual("details", detailsParam.Name, "Second ticket component should have name 'details'");
        Assert.IsTrue(detailsParam.Components != null, "Details tuple should have components");
        Assert.AreEqual(2, detailsParam.Components!.Count, "Details tuple should have exactly two components");

        // First component of details (bool valid)
        var validParam = detailsParam.Components![0];
        Assert.AreEqual("bool", validParam.AbiType, "First details component should be of type 'bool'");
        Assert.AreEqual(0, validParam.Position, "First details component should be at position 0");
        Assert.AreEqual("valid", validParam.Name, "First details component should have name 'valid'");
        Assert.IsNull(validParam.Components, "First details component should not have components");

        // Second component of details (address owner)
        var ownerParam = detailsParam.Components![1];
        Assert.AreEqual("address", ownerParam.AbiType, "Second details component should be of type 'address'");
        Assert.AreEqual(1, ownerParam.Position, "Second details component should be at position 1");
        Assert.AreEqual("owner", ownerParam.Name, "Second details component should have name 'owner'");
        Assert.IsNull(ownerParam.Components, "Second details component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithTupleArray_ReturnsCorrectTuples()
    {
        var parameterString = "((uint256 value, bool valid)[] items)";
        var result = EvmParameters.Parse(parameterString);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");

        var arrayParam = result[0];
        Assert.AreEqual(1, arrayParam.ArrayLengths!.Count, "Parameter should have one array dimension");
        Assert.AreEqual(-1, arrayParam.ArrayLengths![0], "Array dimension should be dynamic length");
        Assert.AreEqual("(uint256,bool)[]", arrayParam.AbiType, "Parameter should be single array of tuple type");
        Assert.AreEqual(0, arrayParam.Position, "Parameter should be at position 0");
        Assert.AreEqual("items", arrayParam.Name, "Parameter should have name 'items'");
        Assert.IsTrue(arrayParam.Components != null, "Parameter should have components");
        Assert.AreEqual(2, arrayParam.Components!.Count, "Tuple should have exactly two components");

        var valueParam = arrayParam.Components![0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First component should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First component should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First component should have name 'value'");
        Assert.IsNull(valueParam.Components, "First component should not have components");

        var validParam = arrayParam.Components![1];
        Assert.AreEqual("bool", validParam.AbiType, "Second component should be of type 'bool'");
        Assert.AreEqual(1, validParam.Position, "Second component should be at position 1");
        Assert.AreEqual("valid", validParam.Name, "Second component should have name 'valid'");
        Assert.IsNull(validParam.Components, "Second component should not have components");
    }

    [TestMethod]
    public void ToString_WithSingleString_ReturnsCorrectFormat()
    {
        var parameterString = "(string)";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format single parameter correctly");
    }

    [TestMethod]
    public void ToString_WithTwoStrings_ReturnsCorrectFormat()
    {
        var parameterString = "(string, uint256)";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format two parameters correctly");
    }

    [TestMethod]
    public void ToString_WithNamedParams_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, uint256 value)";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format named parameters correctly");
    }

    [TestMethod]
    public void ToString_WithNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, (uint256 value, bool valid) ticket)";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format nested tuple correctly");
    }

    [TestMethod]
    public void ToString_WithUnnamedNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string, (uint256, bool))";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format unnamed nested tuple correctly");
    }

    [TestMethod]
    public void ToString_WithDoubleNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, (uint256 value, (bool valid, address owner) details) ticket)";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format double nested tuple correctly");
    }

    [TestMethod]
    public void ToString_WithTupleArray_ReturnsCorrectFormat()
    {
        var parameterString = "((uint256 value, bool valid)[] items)";
        var parameters = EvmParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual("items", parameters[0].Name, "Parameter should have name 'items'");
        Assert.AreEqual(parameterString, result, "Should format tuple array correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithSingleString_ReturnsCorrectFormat()
    {
        var parameterString = "(string)";
        var parameters = EvmParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("(string)", resultWithoutNames, "Should format single parameter without names correctly");
        Assert.AreEqual("(string)", resultWithNames, "Should format single parameter with names correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithNamedParams_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, uint256 value)";
        var parameters = EvmParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("(string,uint256)", resultWithoutNames, "Should format parameters without names correctly");
        Assert.AreEqual("(string name,uint256 value)", resultWithNames, "Should format parameters with names correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, (uint256 value, bool valid) ticket)";
        var parameters = EvmParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("(string,(uint256,bool))", resultWithoutNames, "Should format nested tuple without names correctly");
        Assert.AreEqual("(string name,(uint256 value,bool valid) ticket)", resultWithNames, "Should format nested tuple with names correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithTupleArray_ReturnsCorrectFormat()
    {
        var parameterString = "((uint256 value, bool valid)[] items)";
        var parameters = EvmParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("((uint256,bool)[])", resultWithoutNames, "Should format tuple array without names correctly");
        Assert.AreEqual("((uint256 value,bool valid)[] items)", resultWithNames, "Should format tuple array with names correctly");
    }
}