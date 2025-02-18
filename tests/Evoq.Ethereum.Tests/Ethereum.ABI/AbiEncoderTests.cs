using System.Numerics;
using Evoq.Blockchain;
using Nethereum.RPC;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiEncoderTests
{
    private AbiEncoder encoder;

    [TestInitialize]
    public void Setup()
    {
        encoder = new AbiEncoder();
    }

    [TestMethod]
    public void EncodeUint256_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint256)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexSet = lines.Select(line => Hex.Parse(line));

        // Act

        // var result = encoder.EncodeParameters(parameters, (1));
        var result = this.encoder.EncodeParameters(parameters, BigInteger.One);

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToHashSet();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexSet.ToList(), actualHexSet.ToList());
    }
}