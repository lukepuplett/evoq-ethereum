using System;
using System.Text.Json.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A transaction receipt containing details about a completed Ethereum transaction.
/// </summary>
public class TransactionReceiptDto
{
    /// <summary>
    /// Hash of the transaction (32 Bytes)
    /// </summary>
    [JsonPropertyName("transactionHash")]
    public string TransactionHashHex { get; set; } = string.Empty;

    /// <summary>
    /// Integer of the transaction's index position in the block
    /// </summary>
    [JsonPropertyName("transactionIndex")]
    public string TransactionIndexHex { get; set; } = "0x0";

    /// <summary>
    /// Hash of the block where this transaction was included (32 Bytes)
    /// </summary>
    [JsonPropertyName("blockHash")]
    public string BlockHashHex { get; set; } = string.Empty;

    /// <summary>
    /// Number of the block where this transaction was included
    /// </summary>
    [JsonPropertyName("blockNumber")]
    public string BlockNumberHex { get; set; } = "0x0";

    /// <summary>
    /// Address of the sender (20 Bytes)
    /// </summary>
    [JsonPropertyName("from")]
    public string FromAddressHex { get; set; } = string.Empty;

    /// <summary>
    /// Address of the receiver (20 Bytes). null when it's a contract creation transaction
    /// </summary>
    [JsonPropertyName("to")]
    public string? ToAddressHex { get; set; }

    /// <summary>
    /// The total amount of gas used when this transaction was executed in the block
    /// </summary>
    [JsonPropertyName("cumulativeGasUsed")]
    public string CumulativeGasUsedHex { get; set; } = "0x0";

    /// <summary>
    /// The sum of the base fee and tip paid per unit of gas
    /// </summary>
    [JsonPropertyName("effectiveGasPrice")]
    public string EffectiveGasPriceHex { get; set; } = "0x0";

    /// <summary>
    /// The amount of gas used by this specific transaction alone
    /// </summary>
    [JsonPropertyName("gasUsed")]
    public string GasUsedHex { get; set; } = "0x0";

    /// <summary>
    /// The contract address created, if the transaction was a contract creation, otherwise null
    /// </summary>
    [JsonPropertyName("contractAddress")]
    public string? ContractAddressHex { get; set; }

    /// <summary>
    /// Array of log objects which this transaction generated
    /// </summary>
    [JsonPropertyName("logs")]
    public LogDto[] Logs { get; set; } = Array.Empty<LogDto>();

    /// <summary>
    /// Bloom filter for light clients to quickly retrieve related logs (256 Bytes)
    /// </summary>
    [JsonPropertyName("logsBloom")]
    public string LogsBloomHex { get; set; } = string.Empty;

    /// <summary>
    /// Integer of the transaction type: 0x0 for legacy transactions, 0x1 for access list types, 0x2 for dynamic fees
    /// </summary>
    [JsonPropertyName("type")]
    public string TransactionTypeHex { get; set; } = "0x0";

    /// <summary>
    /// 32 bytes of post-transaction stateroot (pre Byzantium)
    /// </summary>
    [JsonPropertyName("root")]
    public string? StateRootHex { get; set; }

    /// <summary>
    /// Transaction status: 1 for success or 0 for failure
    /// </summary>
    [JsonPropertyName("status")]
    public string? StatusHex { get; set; }

    // proto dank sharding EIP 4844

    /// <summary>
    /// The amount of gas used for the blob transaction
    /// </summary>
    [JsonPropertyName("blobGasUsed")]
    public string? BlobGasUsedHex { get; set; }

    /// <summary>
    /// The price of the blob transaction
    /// </summary>
    [JsonPropertyName("blobGasPrice")]
    public string? BlobGasPriceHex { get; set; }
}

/// <summary>
/// Represents a log entry from an Ethereum transaction receipt.
/// </summary>
public class LogDto
{
    /// <summary>
    /// True when the log was removed due to a chain reorganization. False if it's a valid log.
    /// </summary>
    [JsonPropertyName("removed")]
    public bool Removed { get; set; }

    /// <summary>
    /// Integer of the log index position in the block
    /// </summary>
    [JsonPropertyName("logIndex")]
    public string LogIndexHex { get; set; } = "0x0";

    /// <summary>
    /// Integer of the transactions index position the log was created from
    /// </summary>
    [JsonPropertyName("transactionIndex")]
    public string TransactionIndexHex { get; set; } = "0x0";

    /// <summary>
    /// Hash of the transaction this log was created from (32 Bytes)
    /// </summary>
    [JsonPropertyName("transactionHash")]
    public string TransactionHashHex { get; set; } = string.Empty;

    /// <summary>
    /// Hash of the block where this log was in (32 Bytes)
    /// </summary>
    [JsonPropertyName("blockHash")]
    public string BlockHashHex { get; set; } = string.Empty;

    /// <summary>
    /// The block number where this log was in
    /// </summary>
    [JsonPropertyName("blockNumber")]
    public string BlockNumberHex { get; set; } = "0x0";

    /// <summary>
    /// Address from which this log originated (20 Bytes)
    /// </summary>
    [JsonPropertyName("address")]
    public string AddressHex { get; set; } = string.Empty;

    /// <summary>
    /// Contains the non-indexed parameters of the log
    /// </summary>
    [JsonPropertyName("data")]
    public string DataHex { get; set; } = string.Empty;

    /// <summary>
    /// Array of 0 to 4 32 Bytes DATA of indexed log parameters
    /// </summary>
    [JsonPropertyName("topics")]
    public string[] TopicsHex { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents the type of an Ethereum transaction
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Legacy transaction type (pre-EIP-2718)
    /// </summary>
    Legacy = 0,

    /// <summary>
    /// Access list transaction type (EIP-2930)
    /// </summary>
    AccessList = 1,

    /// <summary>
    /// Dynamic fee transaction type (EIP-1559)
    /// </summary>
    DynamicFee = 2,

    /// <summary>
    /// Blob transaction type (EIP-4844)
    /// </summary>
    Blob = 3
}