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

    [TestMethod]
    public void TryRead_MultipleLogsInReceipt_FindsCorrectOne()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "Transfer(address indexed from,address indexed to,uint256 value)");

        var logs = new[]
        {
            new TransactionLog // Some other event
            {
                Topics = new[] { Hex.Parse("0x1234567890123456789012345678901234567890123456789012345678901234") }
            },
            new TransactionLog // Our Transfer event
            {
                Topics = new[]
                {
                    Hex.Parse("0xddf252ad1be2c89b69c2b068fc378daa952ba7f163c4a11628f55a4df523b3ef"),
                    Hex.Parse("0x000000000000000000000000742d35cc6634c0532925a3b844bc454e4438f44e"),
                    Hex.Parse("0x000000000000000000000000742d35cc6634c0532925a3b844bc454e4438f44e")
                },
                Data = Hex.Parse("0x0000000000000000000000000000000000000000000000000de0b6b3a7640000")
            },
            new TransactionLog // Another different event
            {
                Topics = new[] { Hex.Parse("0x9876543210987654321098765432109876543210987654321098765432109876") }
            }
        };

        var receipt = new TransactionReceipt { Logs = logs };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(indexed);
        var fromAddress = (EthereumAddress)indexed!["from"]!;
        Assert.AreEqual("0x742d35Cc6634C0532925a3b844Bc454e4438f44e", fromAddress.ToString());
    }

    [TestMethod]
    public void TryRead_MultipleAnonymousLogsInReceipt_FindsFirstMatchingTopicCount()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "MyEvent(uint256 indexed value) anonymous");

        var logs = new[]
        {
            new TransactionLog // Event with 2 topics (wrong)
            {
                Topics = new[]
                {
                    Hex.Parse("0x0000000000000000000000000000000000000000000000000000000000000001"),
                    Hex.Parse("0x0000000000000000000000000000000000000000000000000000000000000002")
                }
            },
            new TransactionLog // Our anonymous event with 1 topic
            {
                Topics = new[]
                {
                    Hex.Parse("0x000000000000000000000000000000000000000000000000000000000000007b")
                }
            },
            new TransactionLog // Another event with 1 topic (we should get the first one)
            {
                Topics = new[]
                {
                    Hex.Parse("0x0000000000000000000000000000000000000000000000000000000000000064")
                }
            }
        };

        var receipt = new TransactionReceipt { Logs = logs };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result);
        Assert.IsNotNull(indexed);
        var value = (BigInteger)indexed!["value"]!;
        Assert.AreEqual(123, value); // We got the first matching log (0x7b = 123)
    }

    [TestMethod]
    public void TryRead_NoMatchingLogs_ReturnsFalse()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event, "Transfer(address indexed from,address indexed to,uint256 value)");

        var logs = new[]
        {
            new TransactionLog // Different event
            {
                Topics = new[] { Hex.Parse("0x1234567890123456789012345678901234567890123456789012345678901234") }
            },
            new TransactionLog // Different event
            {
                Topics = new[] { Hex.Parse("0x9876543210987654321098765432109876543210987654321098765432109876") }
            }
        };

        var receipt = new TransactionReceipt { Logs = logs };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsFalse(result);
        Assert.IsNull(indexed);
        Assert.IsNull(data);
    }

    [TestMethod]
    public void TryRead_SchemaRegistered_DecodesCorrectly()
    {
        // Arrange
        var eventSignature = AbiSignature.Parse(AbiItemType.Event,
            "Registered(bytes32 indexed uid, address indexed registerer, tuple(bytes32 uid, address resolver, bool revocable, string schema) schema)");

        var log = new TransactionLog
        {
            Topics = new[]
            {
                // Event signature hash
                Hex.Parse("0xd0b86852e21f9e5fa4bc3b0cff9757ffe243d50c4b43968a42202153d651ea5e"),
                // indexed uid
                Hex.Parse("0x8af15e65888f2e3b487e536a4922e277dcfe85b4b18187b0cf9afdb802ba6bb6"),
                // indexed registerer
                Hex.Parse("0x000000000000000000000000f39fd6e51aad88f6f4ce6ab8827279cfffb92266")
            },
            // The schema tuple data
            Data = Hex.Parse("0x00000000000000000000000000000000000000000000000000000000000000208af15e65888f2e3b487e536a4922e277dcfe85b4b18187b0cf9afdb802ba6bb6000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000010000000000000000000000000000000000000000000000000000000000000080000000000000000000000000000000000000000000000000000000000000000c626f6f6c20697348756d616e0000000000000000000000000000000000000000")
        };

        var receipt = new TransactionReceipt { Logs = new[] { log } };

        // Act
        bool result = reader.TryRead(receipt, eventSignature, out var indexed, out var data);

        // Assert
        Assert.IsTrue(result, "Should successfully decode the event");
        Assert.IsNotNull(indexed, "Indexed parameters should not be null");
        Assert.IsNotNull(data, "Data parameters should not be null");

        // Check indexed parameters
        Assert.AreEqual(2, indexed!.Count, "Should have 2 indexed parameters");

        var indexedUid = (byte[])indexed["uid"]!;
        Assert.AreEqual(
            "0x8af15e65888f2e3b487e536a4922e277dcfe85b4b18187b0cf9afdb802ba6bb6",
            Hex.FromBytes(indexedUid).ToString(),
            "Indexed uid does not match");

        var registerer = (EthereumAddress)indexed["registerer"]!;
        Assert.AreEqual(
            "0xf39Fd6e51aad88F6F4ce6aB8827279cffFb92266",
            registerer.ToString(),
            "Registerer address does not match");

        // Check schema tuple in data
        Assert.AreEqual(1, data!.Count, "Should have 1 data parameter (the schema tuple)");
        var schemaTuple = (IDictionary<string, object?>)data["schema"]!;

        // Check schema tuple fields
        var schemaUid = (byte[])schemaTuple["uid"]!;
        Assert.AreEqual(
            "0x8af15e65888f2e3b487e536a4922e277dcfe85b4b18187b0cf9afdb802ba6bb6",
            Hex.FromBytes(schemaUid).ToString(),
            "Schema uid does not match");

        var resolver = (EthereumAddress)schemaTuple["resolver"]!;
        Assert.AreEqual(EthereumAddress.Zero.ToString(), resolver.ToString(), "Resolver should be zero address");

        Assert.IsTrue((bool)schemaTuple["revocable"]!, "Schema should be revocable");
        Assert.AreEqual("bool isHuman", schemaTuple["schema"], "Schema string does not match");
    }
}