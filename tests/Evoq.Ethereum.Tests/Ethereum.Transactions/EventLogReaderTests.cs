using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;

namespace Evoq.Ethereum.Transactions;

[TestClass]
public class EventLogReaderTests
{
    private readonly IAbiDecoder decoder = new AbiDecoder();
    private readonly EventLogReader reader;

    public EventLogReaderTests()
    {
        reader = new EventLogReader(decoder);
    }

    [TestMethod]
    public void TryRead_EmptyLogs_ReturnsFalse()
    {
        // Arrange
        var receipt = new TransactionReceipt
        {
            Logs = Array.Empty<TransactionLog>()
        };
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "Transfer(address indexed from,address indexed to,uint256 value)");

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(indexed);
        Assert.IsNull(data);
    }

    [TestMethod]
    public void TryRead_Transfer_DecodesCorrectly()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "Transfer(address indexed from,address indexed to,uint256 value)");

        // Real Transfer event data:
        // from: 0x742d35Cc6634C0532925a3b844Bc454e4438f44e
        // to: 0x742d35Cc6634C0532925a3b844Bc454e4438f44e
        // value: 1000000000000000000 (1 ETH)

        var log = new TransactionLog
        {
            Topics = new[]
            {
                // keccak256("Transfer(address,address,uint256)")
                Hex.Parse("0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef"),
                // from address padded
                Hex.Parse("0x000000000000000000000000742d35cc6634c0532925a3b844bc454e4438f44e"),
                // to address padded
                Hex.Parse("0x000000000000000000000000742d35cc6634c0532925a3b844bc454e4438f44e")
            },
            // value encoded
            Data = Hex.Parse("0x0000000000000000000000000000000000000000000000000de0b6b3a7640000")
        };

        var receipt = new TransactionReceipt { Logs = new[] { log } };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(indexed);
        Assert.IsNotNull(data);

        var fromAddress = (EthereumAddress)indexed!["from"]!;
        var toAddress = (EthereumAddress)indexed!["to"]!;
        var value = (BigInteger)data!["value"]!;

        Assert.AreEqual("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", fromAddress.ToString());
        Assert.AreEqual("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", toAddress.ToString());
        Assert.AreEqual(BigInteger.Parse("1000000000000000000"), value); // 1 ETH
    }

    [TestMethod]
    public void TryRead_Approval_DecodesCorrectly()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "Approval(address indexed owner,address indexed spender,uint256 value)");

        // Real Approval event data:
        // owner: 0x742d35Cc6634C0532925a3b844Bc454e4438f44e
        // spender: 0x11111112542D85B3EF69AE05771c2dCCff4fAa26
        // value: 115792089237316195423570985008687907853269984665640564039457584007913129639935 (max uint256)

        var log = new TransactionLog
        {
            Topics = new[]
            {
                // keccak256("Approval(address,address,uint256)")
                Hex.Parse("0x8c5be1e5ebec7d5bd14f71427d1e84f3dd0314c0f7b2291e5b200ac8c7c3b925"),
                // owner address padded
                Hex.Parse("0x000000000000000000000000742d35cc6634c0532925a3b844bc454e4438f44e"),
                // spender address padded
                Hex.Parse("0x00000000000000000000000011111112542d85b3ef69ae05771c2dccff4faa26")
            },
            // max uint256 value encoded
            Data = Hex.Parse("0xffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff")
        };

        var receipt = new TransactionReceipt { Logs = new[] { log } };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(indexed);
        Assert.IsNotNull(data);

        var owner = (EthereumAddress)indexed!["owner"]!;
        var spender = (EthereumAddress)indexed!["spender"]!;
        var value = (BigInteger)data!["value"]!;

        Assert.AreEqual("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", owner.ToString());
        Assert.AreEqual("0x11111112542D85B3EF69AE05771c2dCCff4fAa26", spender.ToString());
        Assert.AreEqual(BigInteger.Parse("115792089237316195423570985008687907853269984665640564039457584007913129639935"), value);
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void TryRead_NonEventSignature_ThrowsArgumentException()
    {
        // Arrange
        var receipt = new TransactionReceipt();
        var functionSignature = AbiSignature.Parse(AbiItemType.Function, "transfer(address to,uint256 value)");

        // Act
        reader.TryRead(receipt, functionSignature, out _, out _);

        // Assert is handled by ExpectedException
    }

    [TestMethod]
    public void TryRead_URI_DecodesCorrectly()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "URI(string value,uint256 indexed id)");

        // Real URI event data:
        // value: "ipfs://QmeSjSinHpPnmXmspMjwiXyN6zS4E9zccariGR3jxcaWtq/",
        // id: 42

        var log = new TransactionLog
        {
            Topics = new[]
            {
                // keccak256("URI(string,uint256)")
                Hex.Parse("0x6bb7ff708619ba0610cba295a58592e0451dee2622938c8755667688daf3529b"),
                // tokenId padded
                Hex.Parse("0x000000000000000000000000000000000000000000000000000000000000002a")
            },
            // encoded string data (offset + length + value padded)
            Data = Hex.Parse(
                "0x" +
                "0000000000000000000000000000000000000000000000000000000000000020" + // offset
                "0000000000000000000000000000000000000000000000000000000000000037" + // length (55)
                "697066733a2f2f516d65536a53696e4870506e6d586d73704d6a776958794e36" + // "ipfs://QmeSjSinHpPnmXmspMjwiXyN6"
                "7a5334453977636361726947523378636157747100000000000000000000000000"  // "zS4E9zccariGR3jxcaWtq" + padding
            )
        };

        var receipt = new TransactionReceipt { Logs = new[] { log } };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(indexed);
        Assert.IsNotNull(data);

        var id = (BigInteger)indexed!["id"]!;
        var value = (string)data!["value"]!;

        Assert.AreEqual(42, id);
        Assert.AreEqual("ipfs://QmeSjSinHpPnmXmspMjwiXyN6zS4E9zccariGR3jxcaWtq/", value);
    }

    [TestMethod]
    public void TryRead_AnonymousEvent_DecodesCorrectly()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "MyEvent(uint256 indexed value) anonymous");

        // Anonymous event with indexed value: 123
        var log = new TransactionLog
        {
            Topics = new[]
            {
                // value padded (no event signature hash because anonymous)
                Hex.Parse("0x000000000000000000000000000000000000000000000000000000000000007b")
            },
            Data = Hex.Empty // no non-indexed params
        };

        var receipt = new TransactionReceipt { Logs = new[] { log } };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(indexed);
        Assert.IsNotNull(data);

        var value = (BigInteger)indexed!["value"]!;
        Assert.AreEqual(123, value);
        Assert.AreEqual(0, data!.Count); // no non-indexed params
    }
}