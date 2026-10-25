using System.Text.Json.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a transaction data returned by eth_getBlockByNumber RPC method.
/// </summary>
public class TransactionDataDto
{
    /// <summary>
    /// The hash of the block containing this transaction. Null when pending.
    /// </summary>
    [JsonPropertyName("blockHash")]
    public string BlockHash { get; set; } = string.Empty;

    /// <summary>
    /// The block number containing this transaction. Null when pending.
    /// </summary>
    [JsonPropertyName("blockNumber")]
    public string BlockNumber { get; set; } = string.Empty;

    /// <summary>
    /// The address the transaction is sent from.
    /// </summary>
    [JsonPropertyName("from")]
    public string From { get; set; } = string.Empty;

    /// <summary>
    /// The gas provided for transaction execution.
    /// </summary>
    [JsonPropertyName("gas")]
    public string Gas { get; set; } = string.Empty;

    /// <summary>
    /// The gas price in wei. Not present for Type 2 transactions.
    /// </summary>
    [JsonPropertyName("gasPrice")]
    public string? GasPrice { get; set; }

    /// <summary>
    /// The hash of the transaction.
    /// </summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>
    /// The contract code or data sent with the transaction.
    /// </summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>
    /// The number of transactions sent from the sender's address before this transaction.
    /// </summary>
    [JsonPropertyName("nonce")]
    public string Nonce { get; set; } = string.Empty;

    /// <summary>
    /// The recipient address of the transaction. Null for contract creation transactions.
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = string.Empty;

    /// <summary>
    /// The index of the transaction in the block. Null when pending.
    /// </summary>
    [JsonPropertyName("transactionIndex")]
    public string TransactionIndex { get; set; } = string.Empty;

    /// <summary>
    /// The value being transferred in wei.
    /// </summary>
    [JsonPropertyName("value")]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// The recovery identifier of the transaction signature.
    /// </summary>
    [JsonPropertyName("v")]
    public string V { get; set; } = string.Empty;

    /// <summary>
    /// The R component of the transaction signature.
    /// </summary>
    [JsonPropertyName("r")]
    public string R { get; set; } = string.Empty;

    /// <summary>
    /// The S component of the transaction signature.
    /// </summary>
    [JsonPropertyName("s")]
    public string S { get; set; } = string.Empty;

    /// <summary>
    /// The access list for gas optimization. Only present in Type 1 and Type 2 transactions.
    /// </summary>
    [JsonPropertyName("accessList")]
    public AccessListEntryDto[]? AccessList { get; set; }

    /// <summary>
    /// The transaction type. "0x0" for legacy, "0x1" for Type 1, "0x2" for Type 2.
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// The maximum total fee per gas. Only present in Type 2 transactions.
    /// </summary>
    [JsonPropertyName("maxFeePerGas")]
    public string? MaxFeePerGas { get; set; }

    /// <summary>
    /// The maximum priority fee per gas (miner tip). Only present in Type 2 transactions.
    /// </summary>
    [JsonPropertyName("maxPriorityFeePerGas")]
    public string? MaxPriorityFeePerGas { get; set; }

    /// <summary>
    /// The chain ID. Only present in Type 1 and Type 2 transactions.
    /// </summary>
    [JsonPropertyName("chainId")]
    public string? ChainId { get; set; }
}
