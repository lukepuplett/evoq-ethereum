using System.Numerics;
using System.Text;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiEncoderTests
{
    private IAbiEncoder encoder = new AbiEncoderV2();

    [TestInitialize]
    public void Setup()
    {
        encoder = new AbiEncoderV2();
    }

    /*
    The following tests are loosely based on the examples in abi.md. The idea is to
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
    public void Encode_SimpleUint256_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint256)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        // var result = encoder.EncodeParameters(parameters, (1));
        var result = this.encoder.EncodeParameters(parameters, BigInteger.One);

        var actualHexList = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexList);
    }

    [TestMethod]
    public void Encode_SimpleBool_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(bool)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, true);

        var actualHexList = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexList);
    }

    [TestMethod]
    public void Encode_SimpleUint8_And_Uint256_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8, uint256)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001
            0x0000000000000000000000000000000000000000000000000000000000000001
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, ((byte)1, (uint)1));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_SimpleStaticUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[2])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000002  // length
            0x0000000000000000000000000000000000000000000000000000000000000001  // element 0    
            0x0000000000000000000000000000000000000000000000000000000000000002  // element 1
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, new byte[] { 1, 2 });

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_SimpleDynamicUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000020  // pointer to length at offset 32
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dynamic) length
            0x0000000000000000000000000000000000000000000000000000000000000001  // (dynamic) element 0
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dynamic) element 1
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, new byte[] { 1, 2 });

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatHexBlock(actualHexSet));
    }

    //

    private static string FormatHexLine(string hex) => hex.Trim().Substring(0, 64 + 2);

    private static string FormatHexBlock(IList<Hex> hexList)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Block:");
        foreach (var hex in hexList)
        {
            sb.AppendLine(hex.ToString());
        }
        return sb.ToString();
    }
}