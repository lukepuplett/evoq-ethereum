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
        var signature = AbiSignature.Parse(AbiItemType.Function, testCase.Signature);

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

    [TestMethod]
    public void Decode_SchemaRegistryEvent_Data()
    {
        // Arrange
        var data = "0x00000000000000000000000000000000000000000000000000000000000000208af15e65888f2e3b487e536a4922e277dcfe85b4b18187b0cf9afdb802ba6bb6000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000c626f6f6c20697348756d616e0000000000000000000000000000000000000000";

        // This represents the non-indexed parameters of the Registered event
        var signature = AbiSignature.Parse(AbiItemType.Event, "schema((bytes32 uid,address resolver,bool revocable,string schema))");

        // Act
        var result = decoder.DecodeParameters(signature.Inputs, Hex.Parse(data).ToByteArray());

        // Assert
        Assert.IsNotNull(result, "Decoding result should not be null");
        Assert.AreEqual(1, result.Parameters.Count, "Should have exactly one parameter (the schema tuple)");

        // Get the schema tuple
        var schemaTuple = result.Parameters[0].Value as IDictionary<string, object?>;
        Assert.IsNotNull(schemaTuple, "Schema tuple should decode to a dictionary");

        // Check uid
        Assert.IsTrue(schemaTuple.TryGetValue("uid", out var uidObj), "Schema tuple should contain 'uid' field");
        var uid = uidObj as byte[];
        Assert.IsNotNull(uid, "uid should be a byte array");
        Assert.AreEqual(
            "0x8af15e65888f2e3b487e536a4922e277dcfe85b4b18187b0cf9afdb802ba6bb6",
            Hex.FromBytes(uid).ToString(),
            "uid value does not match expected bytes32");

        // Check resolver
        Assert.IsTrue(schemaTuple.TryGetValue("resolver", out var resolverObj), "Schema tuple should contain 'resolver' field");
        var resolver = (EthereumAddress)resolverObj!;
        Assert.AreEqual(EthereumAddress.Zero, resolver, "resolver should be zero address");

        // Check revocable
        Assert.IsTrue(schemaTuple.TryGetValue("revocable", out var revocableObj), "Schema tuple should contain 'revocable' field");
        Assert.IsNotNull(revocableObj, "revocable value should not be null");
        Assert.IsTrue((bool)revocableObj!, "revocable should be true");

        // Check schema string
        Assert.IsTrue(schemaTuple.TryGetValue("schema", out var schemaObj), "Schema tuple should contain 'schema' field");
        Assert.IsNotNull(schemaObj, "schema string should not be null");
        Assert.AreEqual("bool isHuman", schemaObj, "schema string does not match expected value");
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
