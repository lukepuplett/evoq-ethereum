using System;
using System.Collections.Generic;
using System.Linq;
using Evoq.Blockchain;
using Evoq.Ethereum.RLP;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.RLP;

[TestClass]
public class RlpEncoderCaseTests
{
    private RlpEncoder encoder = new RlpEncoder();

    [TestInitialize]
    public void Setup()
    {
        encoder = new RlpEncoder();
    }

    //

    [TestMethod]
    [DynamicData(nameof(GetTestCases), DynamicDataSourceType.Method)]
    public void Encode_TestCase_ReturnsCorrectEncoding(int caseNumber, RlpTestCase testCase)
    {
        // Arrange
        var expectedBytes = Hex.Parse(testCase.ExpectedHex).ToByteArray();

        try
        {
            // Act
            byte[] actualBytes = encoder.Encode(testCase.Value);

            // Assert
            CollectionAssert.AreEqual(expectedBytes, actualBytes,
                $"Test case {caseNumber} ({testCase.Name}) failed. Expected: {testCase.ExpectedHex}, Actual: {new Hex(actualBytes)}");
        }
        catch (Exception ex)
        {
            // Output detailed information about the failing test case
            Console.WriteLine($"Exception in test case {caseNumber} ({testCase.Name}):");
            Console.WriteLine($"Description: {testCase.Description}");
            Console.WriteLine($"Value type: {testCase.Value?.GetType().FullName ?? "null"}");
            Console.WriteLine($"Value: {testCase.Value}");
            Console.WriteLine($"Expected hex: {testCase.ExpectedHex}");
            Console.WriteLine($"Exception: {ex.GetType().Name}: {ex.Message}");

            // Rethrow to fail the test
            throw;
        }
    }

    //

    private static IEnumerable<object[]> GetTestCases()
    {
        foreach (var kvp in RlpTestCases.Cases)
        {
            yield return new object[] { kvp.Key, kvp.Value };
        }
    }
}