using System.Numerics;
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

        AbiDecodingResult? result = null;

        // Act
        try
        {
            result = this.decoder.DecodeParameters(signature.Inputs, encodedBytes);
        }
        catch (Exception)
        {
            Console.WriteLine($"Test case {caseNumber}: {testCase.Name} failed to decode.");
            // Console.WriteLine(ex.Message);
            // Console.WriteLine(ex.StackTrace);

            throw;
        }

        // If we get here, the decoder didn't throw, so we should check the results
        Assert.IsNotNull(result, $"Test case {caseNumber}: {testCase.Name} failed to decode.");

        // In the future, we would validate the decoded values against the original values
        ValidateDecodedParameters(testCase.Values, result.Parameters, caseNumber, testCase.Name, "root");
    }

    //

    private static IEnumerable<object[]> GetTestCases()
    {
        // Start with a subset of test cases to focus on basic functionality
        return AbiEncoderDecoderTestCases.Cases
            // .Where(numberCase => numberCase.Key > 0 && numberCase.Key <= 20)              // IMPORTANT / filter
            // .Where(numberCase => numberCase.Key == 7)                                    // IMPORTANT / filter
            .Select(numberCase => new object[] { numberCase.Key, numberCase.Value });
    }

    private void ValidateDecodedParameters(
        IDictionary<string, object?> expectedValues, IReadOnlyList<AbiParam> decodedParameters, int caseNumber, string name, string paramName)
    {
        for (int i = 0; i < decodedParameters.Count; i++)
        {
            var p = decodedParameters[i];
            var expectedValue = expectedValues.Values.ToList()[i];

            // compare param and expected value

            assert(expectedValue, p, i);
        }

        //

        void assert(object? expectedValue, AbiParam p, int i)
        {
            var actualValue = p.Value;

            if (p.IsTupleStrict)
            {
                // we expect either a tuple, or a list for the expected value, and a list for the actual value

                if (ArrayComparer.TryArray(actualValue!, out var actualList))
                {
                    if (ArrayComparer.TryArray(expectedValue!, out var expectedValues))
                    {
                        ArrayComparer.AssertEqual(expectedValues!, actualList!, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}'", $"{paramName}.{p.Name}");
                    }
                    else
                    {
                        Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Expected value was not a tuple or list, got {expectedValue?.GetType()}");
                    }
                }
                else
                {
                    Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Actual value was not a tuple or list, got {actualValue?.GetType()}");
                }
            }
            else if (expectedValue is string[] expectedStrings && actualValue is byte[][] jagged)
            {
                string[] strings = jagged.Select(Encoding.UTF8.GetString).ToArray();

                ArrayComparer.AssertEqual(expectedStrings, strings, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}'", $"{paramName}.{p.Name}");
            }
            else if (p.IsArray)
            {
                if (ArrayComparer.TryArray(expectedValue!, out var expectedArray))
                {
                    if (actualValue is Array arrayValue)
                    {
                        // compare the arrays

                        ArrayComparer.AssertEqual(expectedArray!, arrayValue, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}'", $"{paramName}.{p.Name}");
                    }
                    else
                    {
                        Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Parameter value was not an array, got {actualValue?.GetType()}");
                    }
                }
                else
                {
                    Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Expected value was not an array, got {expectedValue?.GetType()}");
                }
            }
            else if (p.AbiType.StartsWith(AbiTypeNames.Bytes))
            {
                if (expectedValue is byte[] expectedBytes)
                {
                    if (actualValue is byte[] arrayValue)
                    {
                        // compare the arrays

                        ArrayComparer.AssertEqual(expectedBytes, arrayValue, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}'", $"{paramName}.{p.Name}");
                    }
                    else
                    {
                        Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Parameter value was not a byte array, got {actualValue?.GetType()}");
                    }
                }
                else if (expectedValue is string expectedString)
                {
                    if (actualValue is byte[] arrayValue)
                    {
                        // convert the byte array to a string

                        string result = Encoding.UTF8.GetString(arrayValue);

                        Assert.AreEqual(expectedString, result, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}'");
                    }
                    else
                    {
                        Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Parameter value was not a string, got {actualValue?.GetType()}");
                    }
                }
                else
                {
                    Assert.Fail($"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Expected value was not a byte array, got {expectedValue?.GetType()}");
                }
            }
            else if (expectedValue is UInt32 expectedUInt32 && actualValue is BigInteger actualBigInt)
            {
                var expectedBigInt = new BigInteger(expectedUInt32);

                Assert.AreEqual(expectedBigInt, actualBigInt, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Expected {expectedValue.GetType()}, got {actualValue?.GetType()}");
            }
            else
            {
                Assert.AreEqual(expectedValue, actualValue, $"Case {caseNumber}: {name}, Param: {i} '{paramName}.{p.Name}' - Expected {expectedValue?.GetType()}, got {actualValue?.GetType()}");
            }
        }
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
