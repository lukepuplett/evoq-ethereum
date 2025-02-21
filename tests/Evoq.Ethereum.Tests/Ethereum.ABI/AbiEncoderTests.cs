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
    public void Encode_JaggedStaticUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[4][2])");

        // uint8[4][2] / outer array contains two elements, each element is an array of four uint8
        //
        // [
        //     [10, 20, 30, 40], // first element of outer array
        //     [1, 2, 3, 4]     // second element of outer array
        // ]
        //
        // the first uint8[4] of the outer array is [10, 20, 30, 40]
        // the second uint8[4] of the outer array is [1, 2, 3, 4]
        //
        // these are layed out sequentially, 10, 20, 30, 40, then 1, 2, 3, 4

        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x000000000000000000000000000000000000000000000000000000000000000a  // element 0 of uint8[4] first element of outer array
            0x0000000000000000000000000000000000000000000000000000000000000014  // element 1 of uint8[4]
            0x000000000000000000000000000000000000000000000000000000000000001e  // element 2 of uint8[4]
            0x0000000000000000000000000000000000000000000000000000000000000028  // element 3 of uint8[4] 
            0x0000000000000000000000000000000000000000000000000000000000000001  // element 0 of uint8[4] second element of outer array
            0x0000000000000000000000000000000000000000000000000000000000000002  // element 1 of uint8[4]
            0x0000000000000000000000000000000000000000000000000000000000000003  // element 2 of uint8[4]
            0x0000000000000000000000000000000000000000000000000000000000000004  // element 3 of uint8[4]
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var first = new byte[] { 10, 20, 30, 40 };
        var second = new byte[] { 1, 2, 3, 4 };
        var array = new byte[][] { first, second };

        var result = this.encoder.EncodeParameters(parameters, array);

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_TripleJaggedStaticUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[3][2][1])");

        // uint8[3][2][1] / outer array contains one element, which is an array of two uint8[3]
        //
        // the first and only uint8[3][2] of the outer array is [[1, 2, 3], [1, 2, 3]]
        //
        // these are layed out sequentially, 1, 2, 3, 1, 2, 3
        //
        // NOTE - there is no "outer array" for the single element, obviously
        // NOTE - an array of length 1 is pretty weird as it isn't an array really

        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001  // element 0 of uint8[3][2] first element of outer array
            0x0000000000000000000000000000000000000000000000000000000000000002  // element 1 of uint8[3][2] 
            0x0000000000000000000000000000000000000000000000000000000000000003  // element 2 of uint8[3][2] 
            0x0000000000000000000000000000000000000000000000000000000000000001  // element 0 of uint8[3][2] second element of outer array
            0x0000000000000000000000000000000000000000000000000000000000000002  // element 1 of uint8[3][2] 
            0x0000000000000000000000000000000000000000000000000000000000000003  // element 2 of uint8[3][2] 
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var first = new byte[3] { 1, 2, 3 };
        var second = new byte[3] { 1, 2, 3 };
        var jagged = new byte[][] { first, second };
        var tripleJagged = new byte[][][] { jagged };

        var result = this.encoder.EncodeParameters(parameters, tripleJagged);

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_PointlessStaticTuple_ReturnsCorrectEncoding()
    {
        // Arrange

        // This signature has a single parameter account which is a tuple containing two uint256
        // values. The tuple is pointless as they could just be two separate parameters.
        //
        // This situation confuses the encoder so it must be forced into a ValueTuple.

        var signature = FunctionSignature.Parse("function foo((uint256 id, uint256 balance) account)");

        // static tuples are encoded like arrays: (3, 10)
        //
        // (
        //      3                   // id is 3, encodes to 0x..3
        //      10                  // balance is 10, encodes to 0x..a
        // )
        //
        // these are layed out sequentially, 3, 10

        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000003  // id is 3
            0x000000000000000000000000000000000000000000000000000000000000000a  // balance is 10
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        // this throws an ArgumentException
        Assert.ThrowsException<ArgumentException>(() => this.encoder.EncodeParameters(parameters, (3u, 10u)));

        // this works, notice the extra () around the values passed in
        var result = this.encoder.EncodeParameters(parameters, ValueTuple.Create((3u, 10u)));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_NestedStaticTuple_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(bool isActive, (uint256 id, uint256 balance) account)");

        // static tuples are encoded like arrays: true, (3, 10)
        //
        // 1                        // isActive true, encodes to 0x..1
        // (
        //      3                   // id is 3, encodes to 0x..3
        //      10                  // balance is 10, encodes to 0x..a
        // )
        //
        // these are layed out sequentially, 1, 3, 10

        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001  // isActive true
            0x0000000000000000000000000000000000000000000000000000000000000003  // id is 3
            0x000000000000000000000000000000000000000000000000000000000000000a  // balance is 10
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, (true, (3u, 10u)));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_TwoNestedStaticTuple_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse(
            "function foo((bool isActive, uint256 seenUnix) prof, (uint256 id, uint256 balance) account)");

        // static tuples are encoded like arrays: (1, 20), (3, 10)
        //
        // (
        //      1                   // isActive true, encodes to 0x..1
        //      20                  // seenUnix is 20, encodes to 0x..14
        // )
        // (
        //      3                   // id is 3, encodes to 0x..3
        //      10                  // balance is 10, encodes to 0x..a
        // )
        //
        // these are layed out sequentially, 1, 20, 3, 10

        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001  // isActive true
            0x0000000000000000000000000000000000000000000000000000000000000014  // seenUnix is 20
            0x0000000000000000000000000000000000000000000000000000000000000003  // id is 3
            0x000000000000000000000000000000000000000000000000000000000000000a  // balance is 10
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, ((true, 20u), (3u, 10u)));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    [TestMethod]
    public void Encode_DoubleNestedStaticTuple_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse(
            "function foo(((bool isActive, uint256 seenUnix) prof, uint256 id, uint256 balance) account)");

        Assert.AreEqual("function foo(((bool,uint256),uint256,uint256))", signature.ToString());

        // ((bool,uint256),uint256,uint256)

        // static tuples are encoded like arrays: ((true, 20), 3, 10)
        //
        // (
        //      (
        //          1                   // isActive true, encodes to 0x..1
        //          20                  // seenUnix is 20, encodes to 0x..14
        //      )
        //      3                   // id is 3, encodes to 0x..3
        //      10                  // balance is 10, encodes to 0x..a
        // )
        //
        // these are layed out sequentially, 1, 20, 3, 10

        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001  // isActive true
            0x0000000000000000000000000000000000000000000000000000000000000014  // seenUnix is 20
            0x0000000000000000000000000000000000000000000000000000000000000003  // id is 3
            0x000000000000000000000000000000000000000000000000000000000000000a  // balance is 10
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, ValueTuple.Create(((true, 20u), 3u, 10u)));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet);
    }

    // dynamic types

    [TestMethod]
    public void Encode_SimpleBytes_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(bytes)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000020  // pointer to length at offset 32
            0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) length of bytes
            0x0100000000000000000000000000000000000000000000000000000000000000  // (dyn) element 0, byte 01, right-padded to 32 bytes
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, new byte[] { 1 });

        var actualHexList = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        Assert.AreEqual(expectedHexList.Count, result.Count);
        CollectionAssert.AreEquivalent(expectedHexList, actualHexList, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void Encode_SimpleDynamicUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000020  // pointer to length at offset 32
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length
            0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) element 0
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) element 1
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, new byte[] { 1, 2 });

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        Assert.AreEqual(expectedHexList.Count, result.Count);
        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void Encode_MoreComplexDynamicUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[2][])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000020  // pointer to length at offset 32
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length, 2 fixed arrays, ABI type hints at length of each
            0x0000000000000000000000000000000000000000000000000000000000000001  // first fixed array, element 1 value of uint8[2]
            0x0000000000000000000000000000000000000000000000000000000000000002  // first fixed array, element 2 value of uint8[2]
            0x0000000000000000000000000000000000000000000000000000000000000003  // second fixed array, element 1 value of uint8[2]
            0x0000000000000000000000000000000000000000000000000000000000000004  // second fixed array, element 2 value of uint8[2]
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, new byte[][] { new byte[] { 1, 2 }, new byte[] { 3, 4 } });

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        Assert.AreEqual(expectedHexList.Count, result.Count);
        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void JaggedDynamicUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[][])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000020  // pointer to length at offset 32
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length, 2 dynamic arrays
            0x0000000000000000000000000000000000000000000000000000000000000060  // (dyn) pointer to first dynamic array, relative to offset 32, which is the start of the block for this dynamic array data, i.e. the length slot, 3x 32
            0x00000000000000000000000000000000000000000000000000000000000000C0  // (dyn) pointer to second dynamic array, relative to offset 32, which is the start of the block for this dynamic array data, i.e. the length slot, 6x 32
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length 2 of first dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) first dynamic array, element 1 value of uint8[]
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) first dynamic array, element 2 value of uint8[]
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length 2 of second dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) second dynamic array, element 1 value of uint8[]
            0x0000000000000000000000000000000000000000000000000000000000000004  // (dyn) second dynamic array, element 2 value of uint8[]
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, new byte[][] { new byte[] { 1, 2 }, new byte[] { 3, 4 } });

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        Assert.AreEqual(expectedHexList.Count, result.Count);
        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    //

    private static string FormatHexLine(string hex) => hex.Trim().Substring(0, 64 + 2);

    private static string FormatSlotBlock(ISet<Slot> slots)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Block:");
        foreach (var slot in slots)
        {
            sb.AppendLine(slot.ToString());
        }
        return sb.ToString();
    }
}