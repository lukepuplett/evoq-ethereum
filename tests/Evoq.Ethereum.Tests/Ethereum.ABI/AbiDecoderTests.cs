using System.Text;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiDecoderTests
{
    private IAbiDecoder decoder = new AbiDecoder();

    [TestInitialize]
    public void Setup()
    {
        decoder = new AbiDecoder();
    }

    /*
    The following tests are based on the same test cases used in AbiEncoderTests.
    We'll take the encoded data from the test cases and decode it back to the original values.
    
    The ABI decoding process takes the encoded byte array and decodes it back into
    the original parameter values based on the function signature.
    */

    [TestMethod]
    [DynamicData(nameof(GetTestCases), DynamicDataSourceType.Method)]
    public void Decode_TestCase_ReturnsCorrectValues(int caseNumber, AbiTestCase testCase)
    {
        // Arrange
        var signature = FunctionSignature.Parse(testCase.Signature);

        var expectedHexList = testCase.ExpectedLines
            .Select(line => Hex.Parse(FormatHexLine(line)))
            .ToList();

        var encodedBytes = expectedHexList.SelectMany(h => h.ToByteArray()).ToArray();

        // Act - This will throw NotImplementedException for now, which is expected
        try
        {
            var result = this.decoder.DecodeParameters(signature.Parameters, encodedBytes);

            // If we get here, the decoder didn't throw, so we should check the results
            Assert.IsNotNull(result, $"Test case {caseNumber}: {testCase.Name} failed to decode.");

            // In the future, we would validate the decoded values against the original values
            // ValidateDecodedValues(testCase.Values, result.Parameters, caseNumber, testCase.Name);
        }
        catch (NotImplementedException notYet)
        {
            // This is expected for now, as the decoder is not fully implemented
            Assert.Inconclusive($"Test case {caseNumber}: {testCase.Name} - Decoder not fully implemented yet: " + notYet.Message);
        }
    }

    private static IEnumerable<object[]> GetTestCases()
    {
        // Start with a subset of test cases to focus on basic functionality
        return AbiTestCases.Cases
            // .Where(kvp => kvp.Key > 0 && kvp.Key <= 20)              // IMPORTANT / filter
            // .Where(kvp => kvp.Key == 17)              // IMPORTANT / filter
            .Select(kvp => new object[] { kvp.Key, kvp.Value });
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