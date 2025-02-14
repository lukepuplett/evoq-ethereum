namespace Evoq.Ethereum.ABI;

[TestClass]
public class SolidityTypesTests
{
    [TestMethod]
    [DataRow("uint8")]
    [DataRow("uint16")]
    [DataRow("uint256")]
    [DataRow("int8")]
    [DataRow("int256")]
    [DataRow("bytes1")]
    [DataRow("bytes32")]
    [DataRow("uint")]  // Shorthand for uint256
    [DataRow("int")]   // Shorthand for int256
    [DataRow("bytes")] // Dynamic bytes
    [DataRow("uint8[]")]
    [DataRow("bytes32[]")]
    [DataRow("uint256[5]")]
    [DataRow("uint256[][]")]     // Dynamic multi-dimensional array
    [DataRow("uint256[5][]")]    // Mixed fixed and dynamic array
    [DataRow("uint256[][5]")]    // Mixed dynamic and fixed array
    [DataRow("uint256[5][10]")]  // Fixed multi-dimensional array
    [DataRow("address[2][3]")]   // Fixed multi-dimensional array with address
    [DataRow("bool[1][]")]       // Mixed fixed and dynamic with bool
    public void IsValidType_ValidTypes_ReturnsTrue(string type)
    {
        Assert.IsTrue(SolidityTypes.IsValidType(type), $"Type '{type}' should be valid");
    }

    [TestMethod]
    [DataRow("uint0")]     // Too small
    [DataRow("uint7")]     // Not multiple of 8
    [DataRow("uint264")]   // Too large
    [DataRow("bytes0")]    // Too small
    [DataRow("bytes33")]   // Too large
    [DataRow("uint[0")]    // Invalid array syntax
    [DataRow("")]          // Empty string
    [DataRow("notAType")]  // Invalid type
    [DataRow("uint256[0][]")]    // Invalid fixed size 0
    [DataRow("uint256[-1][5]")]  // Negative fixed size
    [DataRow("uint256[5]]")]     // Invalid bracket syntax
    [DataRow("uint256[[5]]")]    // Invalid bracket syntax
    [DataRow("uint256[5][")]     // Incomplete bracket
    public void IsValidType_InvalidTypes_ReturnsFalse(string type)
    {
        Assert.IsFalse(SolidityTypes.IsValidType(type), $"Type '{type}' should be invalid");
    }

    [TestMethod]
    [DataRow("uint", "uint256")]
    [DataRow("int", "int256")]
    [DataRow("byte", "bytes1")]
    [DataRow("uint8", "uint8")]
    [DataRow("address", "address")]
    [DataRow("uint[5][10]", "uint256[5][10]")]
    [DataRow("uint[][]", "uint256[][]")]
    [DataRow("uint[5][]", "uint256[5][]")]
    public void GetCanonicalType_ReturnsNormalizedType(string input, string expected)
    {
        Assert.AreEqual(expected, SolidityTypes.GetCanonicalType(input));
    }
}