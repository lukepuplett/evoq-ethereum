using System;
using System.Linq;
using Evoq.Ethereum.Crypto;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Evoq.Ethereum.Tests.Crypto;

[TestClass]
public class KeccakHashTests
{
    [TestMethod]
    [DataRow(
        "transfer(address,uint256)",
        "a9059cbb")] // Known transfer() function selector
    [DataRow(
        "balanceOf(address)",
        "70a08231")] // Known balanceOf() function selector
    [DataRow(
        "approve(address,uint256)",
        "095ea7b3")] // Known approve() function selector
    public void GetFunctionSelector_ReturnsCorrectSelector(string input, string expectedHex)
    {
        // Arrange
        var inputBytes = System.Text.Encoding.UTF8.GetBytes(input);
        var expected = Convert.FromHexString(expectedHex);

        // Act
        var actual = KeccakHash.ComputeHash(inputBytes).Take(4).ToArray();

        // Assert
        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void KeccakHash_EmptyInput_ReturnsCorrectHash()
    {
        // Empty string Keccak-256 hash
        var expected = Convert.FromHexString(
            "c5d2460186f7233c927e7db2dcc703c0e500b653ca82273b7bfad8045d85a470");

        var actual = KeccakHash.ComputeHash(Array.Empty<byte>());

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void KeccakHash_SingleCharacter_ReturnsCorrectHash()
    {
        // Single character "A"
        var input = System.Text.Encoding.UTF8.GetBytes("A");
        var expected = Convert.FromHexString(
            "03783fac2efed8fbc9ad443e592ee30e61d65f471140c10ca155e937b435b760");

        var actual = KeccakHash.ComputeHash(input);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void KeccakHash_SmallString_ReturnsCorrectHash()
    {
        // String "asd"
        var input = System.Text.Encoding.UTF8.GetBytes("asd");
        var expected = Convert.FromHexString(
            "87c2d362de99f75a4f2755cdaaad2d11bf6cc65dc71356593c445535ff28f43d");

        var actual = KeccakHash.ComputeHash(input);

        CollectionAssert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void KeccakHash_KnownInput_ReturnsCorrectHash()
    {
        // Hash of "testing" from multiple implementations
        var input = System.Text.Encoding.UTF8.GetBytes("testing");
        var expected = Convert.FromHexString(
            "5f16f4c7f149ac4f9510d9cf8cf384038ad348b3bcdc01915f95de12df9d1b02");

        var actual = KeccakHash.ComputeHash(input);

        CollectionAssert.AreEqual(expected, actual);
    }
}
