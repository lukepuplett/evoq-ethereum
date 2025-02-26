using System.Numerics;
using System.Text;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

[TestClass]
public class AbiEncoderTests_Original
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
    public void No1_Encode_SimpleUint256_ReturnsCorrectEncoding()
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
    public void No2_Encode_SimpleBool_ReturnsCorrectEncoding()
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
    public void No3_Encode_SimpleUint8_And_Uint256_ReturnsCorrectEncoding()
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
    public void No4_Encode_SimpleStaticUint8Array_ReturnsCorrectEncoding()
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

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No5_Encode_JaggedStaticUint8Array_ReturnsCorrectEncoding()
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
    public void No6_Encode_TripleJaggedStaticUint8Array_ReturnsCorrectEncoding()
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
    public void No7_Encode_PointlessStaticTuple_ReturnsCorrectEncoding()
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
    public void No8_Encode_NestedStaticTuple_ReturnsCorrectEncoding()
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
    public void No9_Encode_TwoNestedStaticTuple_ReturnsCorrectEncoding()
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
    public void No10_Encode_DoubleNestedStaticTuple_ReturnsCorrectEncoding()
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
    public void No11_Encode_SimpleBytes_ReturnsCorrectEncoding()
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

        CollectionAssert.AreEquivalent(expectedHexList, actualHexList, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No12_Encode_SimpleDynamicUint8Array_ReturnsCorrectEncoding()
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

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No13_Encode_MoreComplexDynamicUint8Array_ReturnsCorrectEncoding()
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

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No14_JaggedDynamicUint8Array_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint8[][])"); // [[1,2],[3,4]]
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000020  // pointer to length at offset 32
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length, 2 dynamic arrays
            0x0000000000000000000000000000000000000000000000000000000000000040  // (dyn) pointer to first dynamic array
            0x00000000000000000000000000000000000000000000000000000000000000a0  // (dyn) pointer to second dynamic array
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

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No15_NestedDynamicTuple_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(bool isActive, (string id, uint256 balance) account)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001  // .isActive = true
            0x0000000000000000000000000000000000000000000000000000000000000040  // .account = offset to tail of dynamic tuple 'account'
            0x0000000000000000000000000000000000000000000000000000000000000040  // .account.id = offset to tail of string 'id'
            0x0000000000000000000000000000000000000000000000000000000000000009  // .account.balance = uint256 value (9)
            0x0000000000000000000000000000000000000000000000000000000000000003  // .account.id.length = string length
            0x6162630000000000000000000000000000000000000000000000000000000000  // .account.id.data = string data ("abc" padded)
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, (true, ("abc", BigInteger.Parse("9"))));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No16_DoubleNestedDynamicTuple_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(bool isActive, ((string id, string name) user, uint256 balance) account)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000001  // isActive true
            0x0000000000000000000000000000000000000000000000000000000000000040  // offset to outer dynamic tuple 'account'
            0x0000000000000000000000000000000000000000000000000000000000000040  // (dyn) offset to inner dynamic tuple 'user'
            0x0000000000000000000000000000000000000000000000000000000000000009  // (dyn) uint256 value (9)
            0x0000000000000000000000000000000000000000000000000000000000000040  // (dyn) inner tuple: offset to string id
            0x0000000000000000000000000000000000000000000000000000000000000080  // (dyn) inner tuple: offset to string name
            0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) length of string id
            0x6100000000000000000000000000000000000000000000000000000000000000  // (dyn) string data ("a" padded)
            0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) length of string name
            0x6162630000000000000000000000000000000000000000000000000000000000  // (dyn) string data ("abc" padded)
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, (true, (("a", "abc"), 9u)));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    // examples from the formal spec

    [TestMethod]
    public void No17_Example_FromFormalSpec_FixedArrayOfBytes3_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function bar(bytes3[2])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x6162630000000000000000000000000000000000000000000000000000000000  // first value of bytes3
            0x6465660000000000000000000000000000000000000000000000000000000000  // second value of bytes3
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        // encoding of 'abc' is 0x616263
        // encoding of 'def' is 0x646566

        var result = this.encoder.EncodeParameters(parameters, new string[2] { "abc", "def" });

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No18_Example_FromFormalSpec_TwoSimpleValues_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function baz(uint256 x,bool y)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000045  // first value
            0x0000000000000000000000000000000000000000000000000000000000000001  // second value
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, (69u, true));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No19_Example_FromFormalSpec_ThreeValuesLastIsDynamicArray_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function sam(bytes,bool,uint[])");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000060  // offset to start of data part of bytes value
            0x0000000000000000000000000000000000000000000000000000000000000001  // second value
            0x00000000000000000000000000000000000000000000000000000000000000a0  // offset to start of data part of uint[] value
            0x0000000000000000000000000000000000000000000000000000000000000004  // (dyn) length of bytes value
            0x6461766500000000000000000000000000000000000000000000000000000000  // (dyn) data of the bytes value
            0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) length of first dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000001  // (dyn) first element of first dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) second element of first dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000003  // (dyn) third element of first dynamic array
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, ("dave", true, new uint[] { 1, 2, 3 }));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    [TestMethod]
    public void No20_Example_FromFormalSpec_FourMixedValues_ReturnsCorrectEncoding()
    {
        // Arrange

        var signature = FunctionSignature.Parse("function foo(uint256,uint32[],bytes10,bytes)");
        var parameters = new EvmParameters(signature.Parameters);

        var expectedRawHex = """
            0x0000000000000000000000000000000000000000000000000000000000000123  // uint256 value of 0x123
            0x0000000000000000000000000000000000000000000000000000000000000080  // offset to start of data part of uint32[]
            0x3132333435363738393000000000000000000000000000000000000000000000  // bytes10 value of "1234567890"
            0x00000000000000000000000000000000000000000000000000000000000000e0  // offset to start of data part of bytes value
            0x0000000000000000000000000000000000000000000000000000000000000002  // (dyn) length of first dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000456  // (dyn) first element of first dynamic array
            0x0000000000000000000000000000000000000000000000000000000000000789  // (dyn) second element of first dynamic array
            0x000000000000000000000000000000000000000000000000000000000000000d  // (dyn) length of the bytes value
            0x48656c6c6f2c20776f726c642100000000000000000000000000000000000000  // (dyn) data of the bytes value
            """;

        var lines = expectedRawHex.Split(Environment.NewLine);
        var expectedHexList = lines.Select(line => Hex.Parse(FormatHexLine(line))).ToList();

        // Act

        var result = this.encoder.EncodeParameters(parameters, (0x123u, new uint[] { 0x456u, 0x789u }, "1234567890", "Hello, world!"));

        var actualHexSet = result.GetSlots().Select(slot => slot.ToHex()).ToList();

        // Assert

        CollectionAssert.AreEquivalent(expectedHexList, actualHexSet, FormatSlotBlock(result.GetSlots()));
    }

    //

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