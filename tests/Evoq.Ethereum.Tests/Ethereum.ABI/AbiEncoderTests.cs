using System.Numerics;
using System.Text;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiEncoderTests
{
    private IAbiEncoder encoder = new AbiEncoder();

    [TestInitialize]
    public void Setup()
    {
        encoder = new AbiEncoder();
    }

    /*
    The following tests are based on the examples in abi.md. The idea is to
    make the tests as easily visualizable as possible.

    The final encoded bytes will be a single byte array but here we show those bytes
    as a list of 32-byte hex strings for easier authoring and debugging.

    The ABI encoding is based around two areas of memory. The first area is the static
    area and the second is the dynamic area. The static area is the first N bytes of the
    encoded data and the dynamic area is the remaining bytes.

    The static area contains the encoded bytes for the ABI types that have a fixed size
    like uint256, int256, address, bool, etc. which all fit into a 32-byte slot.

    The dynamic area contains the encoded bytes for the ABI types that have a variable
    size like strings, bytes, arrays, structs, etc.

    When the encoder first encounters a dynamic type, it will encode the dynamic type
    in the dynamic area and add an offset, or pointer, slot to the static area which
    contains the offset of start of the encoded value in the dynamic area. The offset
    is relative to the start of the whole encoded data.

    If a dynamic type contains a fixed size type, it will be encoded in the exact same
    way as it would be in the static area, except it will be written to the dynamic area.
    */

    [TestMethod]
    [DynamicData(nameof(GetTestCases), DynamicDataSourceType.Method)]
    public void Encode_TestCase_ReturnsCorrectEncoding(int caseNumber, AbiTestCase testCase)
    {
        // Arrange
        var signature = FunctionSignature.Parse(testCase.Signature);
        var parameters = new AbiParameters(signature.Parameters);

        var expectedHexList = testCase.ExpectedLines
            .Select(line => Hex.Parse(FormatHexLine(line)))
            .ToList();

        // Act
        var result = this.encoder.EncodeParameters(parameters, testCase.Values);
        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert
        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet,
            $"Test case {caseNumber}: {testCase.Name} failed.\n{FormatSlotBlock(result.GetSlots())}");
    }

    private static IEnumerable<object[]> GetTestCases()
    {
        return AbiTestCases.Cases.Select(kvp => new object[] { kvp.Key, kvp.Value });
    }

    private static string FormatHexLine(string hex) => hex.Trim().Substring(0, 64 + 2);

    private static string FormatSlotBlock(IReadOnlyList<Slot> slots)
    {
        var sb = new StringBuilder();
        sb.AppendLine();
        sb.AppendLine("Block:");
        foreach (var slot in slots)
        {
            sb.AppendLine(slot.ToString());
        }
        return sb.ToString();
    }
}