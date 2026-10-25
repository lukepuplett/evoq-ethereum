using System.Numerics;
using System.Text.Json;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.JsonRPC;

namespace Evoq.Ethereum.Tests.Ethereum.JsonRPC;

/// <summary>
/// Tests that a block as returned by a real node (camelCase JSON, minimal-length hex quantities)
/// survives deserialization with default serializer options, as used by the JSON-RPC client,
/// and conversion to the domain types.
/// </summary>
[TestClass]
public class BlockDataDeserializationTests
{
    private const string Hash32 = "0x" + "ab" + "00000000000000000000000000000000000000000000000000000000000000";
    private const string Address = "0x1111111111111111111111111111111111111111";

    // Quantities are deliberately odd-length (e.g. 0x5, 0x2) as real nodes return them.
    private static readonly string BlockJson = $$"""
    {
      "number": "0x5",
      "hash": "{{Hash32}}",
      "parentHash": "{{Hash32}}",
      "nonce": "0x0000000000000000",
      "sha3Uncles": "{{Hash32}}",
      "logsBloom": "0x00",
      "transactionsRoot": "{{Hash32}}",
      "stateRoot": "{{Hash32}}",
      "receiptsRoot": "{{Hash32}}",
      "miner": "{{Address}}",
      "difficulty": "0x0",
      "totalDifficulty": "0x0",
      "extraData": "0x",
      "size": "0x2ab",
      "gasLimit": "0x1c9c380",
      "gasUsed": "0x5208",
      "timestamp": "0x65f0c2a0",
      "baseFeePerGas": "0x7",
      "uncles": [],
      "withdrawalsRoot": "{{Hash32}}",
      "withdrawals": [{ "index": "0x1", "validatorIndex": "0x2", "address": "{{Address}}", "amount": "0x3" }],
      "transactions": [{
        "blockHash": "{{Hash32}}",
        "blockNumber": "0x5",
        "from": "{{Address}}",
        "gas": "0x5208",
        "hash": "{{Hash32}}",
        "input": "0x",
        "nonce": "0x1",
        "to": "{{Address}}",
        "transactionIndex": "0x0",
        "value": "0x0",
        "type": "0x2",
        "accessList": [],
        "chainId": "0x14a34",
        "maxFeePerGas": "0x3b9aca07",
        "maxPriorityFeePerGas": "0x1",
        "gasPrice": "0x8",
        "v": "0x1",
        "r": "0x01",
        "s": "0x02"
      }]
    }
    """;

    private static BlockData<TransactionData> ParseBlock()
    {
        var dto = JsonSerializer.Deserialize<BlockDataDto<TransactionDataDto>>(BlockJson, new JsonSerializerOptions());

        return BlockData<TransactionData>.FromDto(dto, t => TransactionData.FromDto(t)!)!;
    }

    [TestMethod]
    public void FromDto_NodeBlock_ParsesBlockQuantities()
    {
        var block = ParseBlock();

        Assert.AreEqual(new BigInteger(5), block.Number, "Block number 0x5 should be read from the 'number' field.");
        Assert.AreEqual(new BigInteger(7), block.BaseFeePerGas, "Base fee 0x7 should be read from 'baseFeePerGas'.");
        Assert.AreEqual(new BigInteger(0x5208), block.GasUsed, "Gas used should be read from 'gasUsed'.");
        Assert.AreEqual(new BigInteger(0x1c9c380), block.GasLimit, "Gas limit should be read from 'gasLimit'.");
        Assert.AreEqual(new BigInteger(0x2ab), block.Size, "Size should be read from 'size'.");
    }

    [TestMethod]
    public void FromDto_NodeBlock_ParsesTimestamp()
    {
        var block = ParseBlock();

        Assert.AreEqual(0x65f0c2a0L, block.Timestamp.ToUnixTimeSeconds(), "Timestamp should be read from 'timestamp'.");
    }

    [TestMethod]
    public void FromDto_NodeBlock_ParsesWithdrawals()
    {
        var block = ParseBlock();

        Assert.AreEqual(1, block.Withdrawals.Length, "The withdrawal should be read from 'withdrawals'.");
        Assert.AreEqual(new BigInteger(3), block.Withdrawals[0].Amount, "Withdrawal amount 0x3 should be parsed.");
        Assert.AreEqual(new BigInteger(2), block.Withdrawals[0].ValidatorIndex, "Withdrawal validator index 0x2 should be parsed.");
    }

    [TestMethod]
    public void FromDto_NodeBlock_ParsesType2Transaction()
    {
        var tx = ParseBlock().Transactions.Single();

        Assert.AreEqual(new BigInteger(2), tx.Type, "Transaction type 0x2 should be read from 'type'.");
        Assert.AreEqual(new BigInteger(0x14a34), tx.ChainId, "Chain id should be read from 'chainId'.");
        Assert.AreEqual(new BigInteger(0x3b9aca07), tx.MaxFeePerGas, "Max fee should be read from 'maxFeePerGas'.");
        Assert.AreEqual(new BigInteger(1), tx.MaxPriorityFeePerGas, "Max priority fee should be read from 'maxPriorityFeePerGas'.");
        Assert.AreEqual(new BigInteger(1), tx.Nonce, "Nonce 0x1 should be read from 'nonce'.");
    }

    [TestMethod]
    public void FromDto_NodeBlock_EmptyAccessListArrayDoesNotThrow()
    {
        var tx = ParseBlock().Transactions.Single();

        Assert.IsNotNull(tx.AccessList, "A transaction with an empty 'accessList' array should have an empty access list.");
        Assert.AreEqual(0, tx.AccessList!.Entries.Length, "The access list should have no entries.");
    }
}
