namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiParametersTests
{
    [TestMethod]
    public void SplitParams_WithSingleString_ReturnsCorrectTuples()
    {
        var parameterString = "(string)";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("string", firstResult?.AbiType, "Parameter should be of type 'string'");
        Assert.AreEqual(0, firstResult?.Position, "Parameter should be at position 0");
        Assert.AreEqual("", firstResult?.Name, "Parameter should have empty name");
        Assert.IsFalse(firstResult?.TryParseComponents(out var _), "Parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithSingleStringArray_ReturnsCorrectTuples()
    {
        var parameterString = "(string[2])";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("string[2]", firstResult?.AbiType, "Parameter should be of type 'string[2]'");
        Assert.AreEqual(0, firstResult?.Position, "Parameter should be at position 0");
        Assert.AreEqual("", firstResult?.Name, "Parameter should have empty name");
        Assert.IsFalse(firstResult?.TryParseComponents(out var _), "Parameter should not have components");
        Assert.AreEqual(1, firstResult?.ArrayLengths!.Count, "Parameter should have 1 array length");
        Assert.AreEqual(2, firstResult?.ArrayLengths![0], "Array length should be 2");
    }

    [TestMethod]
    public void SplitParams_WithTwoStrings_ReturnsCorrectTuples()
    {
        var parameterString = "(string,uint256)";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);
        var secondResult = result.ElementAtOrDefault(1);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");
        Assert.AreEqual("string", firstResult?.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, firstResult?.Position, "First parameter should be at position 0");
        Assert.AreEqual("", firstResult?.Name, "First parameter should have empty name");
        Assert.IsFalse(firstResult?.TryParseComponents(out var _), "First parameter should not have components");

        Assert.AreEqual("uint256", secondResult?.AbiType, "Second parameter should be of type 'uint256'");
        Assert.AreEqual(1, secondResult?.Position, "Second parameter should be at position 1");
        Assert.AreEqual("", secondResult?.Name, "Second parameter should have empty name");
        Assert.IsFalse(secondResult?.TryParseComponents(out var _), "Second parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithNamedTuple_ReturnsCorrectNamedTuple()
    {
        var parameterString = "((string name, uint256 value) item)";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("(string,uint256)", firstResult?.AbiType, "Parameter should be of type '(string,uint256)'");
        Assert.AreEqual(0, firstResult?.Position, "Parameter should be at position 0");
        Assert.AreEqual("item", firstResult?.Name, "Parameter should have name 'item'");
        Assert.IsTrue(firstResult!.TryParseComponents(out var components), "Parameter should have components");
        Assert.AreEqual(2, components!.Count, "Tuple should have exactly two components");

        var firstComponent = components!.ElementAtOrDefault(0);
        Assert.AreEqual("string", firstComponent?.AbiType, "First component should be of type 'string'");
        Assert.AreEqual(0, firstComponent?.Position, "First component should be at position 0");
        Assert.AreEqual("name", firstComponent?.Name, "First component should have name 'name'");
    }

    [TestMethod]
    public void SplitParams_WithNamedParam_ReturnsCorrectTuples()
    {
        var parameterString = "(string name)";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");
        Assert.AreEqual("string", firstResult!.AbiType, "Parameter should be of type 'string'");
        Assert.AreEqual(0, firstResult.Position, "Parameter should be at position 0");
        Assert.AreEqual("name", firstResult.Name, "Parameter should have name 'name'");
        Assert.IsFalse(firstResult.TryParseComponents(out var _), "Parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithBadlyFormedNamedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(   (  string  name ,  uint256  age  )  data  ,  bool  enabled )";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");
        Assert.AreEqual("(string,uint256)", firstResult!.AbiType, "First parameter should be of type '(string,uint256)'");
        Assert.AreEqual("data", firstResult.Name, "First parameter should have name 'data'");
    }

    [TestMethod]
    public void SplitParams_WithNamedParams_ReturnsCorrectTuples()
    {
        var parameterString = "(string name, uint256 value)";
        var result = AbiParameters.Parse(parameterString);

        var firstResult = result.ElementAtOrDefault(0);
        var secondResult = result.ElementAtOrDefault(1);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");
        Assert.AreEqual("string", firstResult!.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, firstResult.Position, "First parameter should be at position 0");
        Assert.AreEqual("name", firstResult.Name, "First parameter should have name 'name'");
        Assert.IsFalse(firstResult.TryParseComponents(out var _), "First parameter should not have components");

        Assert.AreEqual("uint256", secondResult!.AbiType, "Second parameter should be of type 'uint256'");
        Assert.AreEqual(1, secondResult.Position, "Second parameter should be at position 1");
        Assert.AreEqual("value", secondResult.Name, "Second parameter should have name 'value'");
        Assert.IsFalse(secondResult.TryParseComponents(out var _), "Second parameter should not have components");
    }

    [TestMethod]
    public void SplitParams_WithNestedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(string name, (uint256 value, bool valid) ticket)";
        var result = AbiParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        var nameParam = result[0];
        Assert.AreEqual("string", nameParam.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, nameParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("name", nameParam.Name, "First parameter should have name 'name'");
        Assert.IsFalse(nameParam.TryParseComponents(out var _), "First parameter should not have components");

        var ticketParam = result[1];
        Assert.AreEqual("(uint256,bool)", ticketParam.AbiType, "Second parameter should be of type '(uint256,bool)'");
        Assert.AreEqual(1, ticketParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("ticket", ticketParam.Name, "Second parameter should have name 'ticket'");
        Assert.IsTrue(ticketParam.TryParseComponents(out var ticketComponents), "Second parameter should have components");
        Assert.AreEqual(2, ticketComponents!.Count, "Tuple should have exactly two components");

        var valueParam = ticketComponents[0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First tuple component should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First tuple component should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First tuple component should have name 'value'");
        Assert.IsFalse(valueParam.TryParseComponents(out var _), "First tuple component should not have components");

        var validParam = ticketComponents[1];
        Assert.AreEqual("bool", validParam.AbiType, "Second tuple component should be of type 'bool'");
        Assert.AreEqual(1, validParam.Position, "Second tuple component should be at position 1");
        Assert.AreEqual("valid", validParam.Name, "Second tuple component should have name 'valid'");
        Assert.IsFalse(validParam.TryParseComponents(out var _), "Second tuple component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithUnnamedNestedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(string,(uint256,bool))";
        var result = AbiParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        var stringParam = result[0];
        Assert.AreEqual("string", stringParam.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, stringParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("", stringParam.Name, "First parameter should have empty name");
        Assert.IsFalse(stringParam.TryParseComponents(out var _), "First parameter should not have components");

        var tupleParam = result[1];
        Assert.AreEqual("(uint256,bool)", tupleParam.AbiType, "Second parameter should be of type '(uint256,bool)'");
        Assert.AreEqual(1, tupleParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("", tupleParam.Name, "Second parameter should have empty name");
        Assert.IsTrue(tupleParam.TryParseComponents(out var tupleComponents), "Second parameter should have components");
        Assert.AreEqual(2, tupleComponents!.Count, "Tuple should have exactly two components");

        var uintParam = tupleComponents[0];
        Assert.AreEqual("uint256", uintParam.AbiType, "First tuple component should be of type 'uint256'");
        Assert.AreEqual(0, uintParam.Position, "First tuple component should be at position 0");
        Assert.AreEqual("", uintParam.Name, "First tuple component should have empty name");
        Assert.IsFalse(uintParam.TryParseComponents(out var _), "First tuple component should not have components");

        var boolParam = tupleComponents[1];
        Assert.AreEqual("bool", boolParam.AbiType, "Second tuple component should be of type 'bool'");
        Assert.AreEqual(1, boolParam.Position, "Second tuple component should be at position 1");
        Assert.AreEqual("", boolParam.Name, "Second tuple component should have empty name");
        Assert.IsFalse(boolParam.TryParseComponents(out var _), "Second tuple component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithNestedTupleOnly_ReturnsCorrectTuples()
    {
        var parameterString = "(uint256 value, (bool valid, address owner) details)";
        var result = AbiParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        // First parameter (uint256 value)
        var valueParam = result[0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First parameter should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First parameter should have name 'value'");
        Assert.IsFalse(valueParam.TryParseComponents(out var _), "First parameter should not have components");

        // Second parameter (the nested tuple named 'details')
        var detailsParam = result[1];
        Assert.AreEqual("(bool,address)", detailsParam.AbiType, "Second parameter should be of type '(bool,address)'");
        Assert.AreEqual(1, detailsParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("details", detailsParam.Name, "Second parameter should have name 'details'");
        Assert.IsTrue(detailsParam.TryParseComponents(out var detailsComponents), "Second parameter should have components");
        Assert.AreEqual(2, detailsComponents!.Count, "Details tuple should have exactly two components");

        // First component of details (bool valid)
        var validParam = detailsComponents[0];
        Assert.AreEqual("bool", validParam.AbiType, "First details component should be of type 'bool'");
        Assert.AreEqual(0, validParam.Position, "First details component should be at position 0");
        Assert.AreEqual("valid", validParam.Name, "First details component should have name 'valid'");
        Assert.IsFalse(validParam.TryParseComponents(out var _), "First details component should not have components");

        // Second component of details (address owner)
        var ownerParam = detailsComponents[1];
        Assert.AreEqual("address", ownerParam.AbiType, "Second details component should be of type 'address'");
        Assert.AreEqual(1, ownerParam.Position, "Second details component should be at position 1");
        Assert.AreEqual("owner", ownerParam.Name, "Second details component should have name 'owner'");
        Assert.IsFalse(ownerParam.TryParseComponents(out var _), "Second details component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithDoubleNestedTuple_ReturnsCorrectTuples()
    {
        var parameterString = "(string name, (uint256 value, (bool valid, address owner) details) ticket)";
        var result = AbiParameters.Parse(parameterString);

        Assert.AreEqual(2, result.Count, "Should have exactly two parameters");

        // First parameter (string name)
        var nameParam = result[0];
        Assert.AreEqual("string", nameParam.AbiType, "First parameter should be of type 'string'");
        Assert.AreEqual(0, nameParam.Position, "First parameter should be at position 0");
        Assert.AreEqual("name", nameParam.Name, "First parameter should have name 'name'");
        Assert.IsFalse(nameParam.TryParseComponents(out var _), "First parameter should not have components");

        // Second parameter (the outer tuple named 'ticket')
        var ticketParam = result[1];
        Assert.AreEqual("(uint256,(bool,address))", ticketParam.AbiType, "Second parameter should be of type '(uint256,(bool,address))'");
        Assert.AreEqual(1, ticketParam.Position, "Second parameter should be at position 1");
        Assert.AreEqual("ticket", ticketParam.Name, "Second parameter should have name 'ticket'");
        Assert.IsTrue(ticketParam.TryParseComponents(out var ticketComponents), "Second parameter should have components");
        Assert.AreEqual(2, ticketComponents!.Count, "Ticket tuple should have exactly two components");

        // First component of ticket (uint256 value)
        var valueParam = ticketComponents[0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First ticket component should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First ticket component should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First ticket component should have name 'value'");
        Assert.IsFalse(valueParam.TryParseComponents(out var _), "First ticket component should not have components");

        // Second component of ticket (the inner tuple named 'details')
        var detailsParam = ticketComponents[1];
        Assert.AreEqual("(bool,address)", detailsParam.AbiType, "Second ticket component should be of type '(bool,address)'");
        Assert.AreEqual(1, detailsParam.Position, "Second ticket component should be at position 1");
        Assert.AreEqual("details", detailsParam.Name, "Second ticket component should have name 'details'");
        Assert.IsTrue(detailsParam.TryParseComponents(out var detailsComponents), "Details tuple should have components");
        Assert.AreEqual(2, detailsComponents!.Count, "Details tuple should have exactly two components");

        // First component of details (bool valid)
        var validParam = detailsComponents[0];
        Assert.AreEqual("bool", validParam.AbiType, "First details component should be of type 'bool'");
        Assert.AreEqual(0, validParam.Position, "First details component should be at position 0");
        Assert.AreEqual("valid", validParam.Name, "First details component should have name 'valid'");
        Assert.IsFalse(validParam.TryParseComponents(out var _), "First details component should not have components");

        // Second component of details (address owner)
        var ownerParam = detailsComponents[1];
        Assert.AreEqual("address", ownerParam.AbiType, "Second details component should be of type 'address'");
        Assert.AreEqual(1, ownerParam.Position, "Second details component should be at position 1");
        Assert.AreEqual("owner", ownerParam.Name, "Second details component should have name 'owner'");
        Assert.IsFalse(ownerParam.TryParseComponents(out var _), "Second details component should not have components");
    }

    [TestMethod]
    public void SplitParams_WithTupleArray_ReturnsCorrectTuples()
    {
        var parameterString = "((uint256 value, bool valid)[] items)";
        var result = AbiParameters.Parse(parameterString);

        Assert.AreEqual(1, result.Count, "Should have exactly one parameter");

        var arrayParam = result[0];
        Assert.AreEqual(1, arrayParam.ArrayLengths!.Count, "Parameter should have one array dimension");
        Assert.AreEqual(-1, arrayParam.ArrayLengths![0], "Array dimension should be dynamic length");
        Assert.AreEqual("(uint256,bool)[]", arrayParam.AbiType, "Parameter should be single array of tuple type");
        Assert.AreEqual(0, arrayParam.Position, "Parameter should be at position 0");
        Assert.AreEqual("items", arrayParam.Name, "Parameter should have name 'items'");
        Assert.IsTrue(arrayParam.TryParseComponents(out var arrayComponents), "Parameter should have components");
        Assert.AreEqual(2, arrayComponents!.Count, "Tuple should have exactly two components");

        var valueParam = arrayComponents[0];
        Assert.AreEqual("uint256", valueParam.AbiType, "First component should be of type 'uint256'");
        Assert.AreEqual(0, valueParam.Position, "First component should be at position 0");
        Assert.AreEqual("value", valueParam.Name, "First component should have name 'value'");
        Assert.IsFalse(valueParam.TryParseComponents(out var _), "First component should not have components");

        var validParam = arrayComponents[1];
        Assert.AreEqual("bool", validParam.AbiType, "Second component should be of type 'bool'");
        Assert.AreEqual(1, validParam.Position, "Second component should be at position 1");
        Assert.AreEqual("valid", validParam.Name, "Second component should have name 'valid'");
        Assert.IsFalse(validParam.TryParseComponents(out var _), "Second component should not have components");
    }

    [TestMethod]
    public void ToString_WithSingleString_ReturnsCorrectFormat()
    {
        var parameterString = "(string)";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format single parameter correctly");
    }

    [TestMethod]
    public void ToString_WithTwoStrings_ReturnsCorrectFormat()
    {
        var parameterString = "(string, uint256)";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format two parameters correctly");
    }

    [TestMethod]
    public void ToString_WithNamedParams_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, uint256 value)";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format named parameters correctly");
    }

    [TestMethod]
    public void ToString_WithNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, (uint256 value, bool valid) ticket)";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format nested tuple correctly");
    }

    [TestMethod]
    public void ToString_WithUnnamedNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string, (uint256, bool))";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format unnamed nested tuple correctly");
    }

    [TestMethod]
    public void ToString_WithDoubleNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, (uint256 value, (bool valid, address owner) details) ticket)";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        Assert.AreEqual(parameterString, result, "Should format double nested tuple correctly");
    }

    [TestMethod]
    public void ToString_WithTupleArray_ReturnsCorrectFormat()
    {
        var parameterString = "((uint256 value, bool valid)[] items)";
        var parameters = AbiParameters.Parse(parameterString);
        var result = parameters.ToString();

        var firstParam = parameters.ElementAtOrDefault(0);

        Assert.IsNotNull(firstParam, "Parameter should not be null");
        Assert.AreEqual("items", firstParam!.Name, "Parameter should have name 'items'");
        Assert.AreEqual(parameterString, result, "Should format tuple array correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithSingleString_ReturnsCorrectFormat()
    {
        var parameterString = "(string)";
        var parameters = AbiParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("(string)", resultWithoutNames, "Should format single parameter without names correctly");
        Assert.AreEqual("(string)", resultWithNames, "Should format single parameter with names correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithNamedParams_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, uint256 value)";
        var parameters = AbiParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("(string,uint256)", resultWithoutNames, "Should format parameters without names correctly");
        Assert.AreEqual("(string name,uint256 value)", resultWithNames, "Should format parameters with names correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithNestedTuple_ReturnsCorrectFormat()
    {
        var parameterString = "(string name, (uint256 value, bool valid) ticket)";
        var parameters = AbiParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("(string,(uint256,bool))", resultWithoutNames, "Should format nested tuple without names correctly");
        Assert.AreEqual("(string name,(uint256 value,bool valid) ticket)", resultWithNames, "Should format nested tuple with names correctly");
    }

    [TestMethod]
    public void GetCanonicalType_WithTupleArray_ReturnsCorrectFormat()
    {
        var parameterString = "((uint256 value, bool valid)[] items)";
        var parameters = AbiParameters.Parse(parameterString);

        var resultWithoutNames = parameters.GetCanonicalType(includeNames: false);
        var resultWithNames = parameters.GetCanonicalType(includeNames: true);

        Assert.AreEqual("((uint256,bool)[])", resultWithoutNames, "Should format tuple array without names correctly");
        Assert.AreEqual("((uint256 value,bool valid)[] items)", resultWithNames, "Should format tuple array with names correctly");
    }
}