using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// A transaction receipt containing details about a completed Ethereum transaction.
/// </summary>
public class TransactionReceipt
{
    private static readonly HexParseOptions ParseOptions = HexParseOptions.AllowOddLength | HexParseOptions.AllowEmptyString;

    //

    /// <summary>
    /// Initializes a new instance of the TransactionReceipt class.
    /// </summary>
    internal TransactionReceipt() { }

    //

    /// <summary>
    /// Hash of the transaction (32 Bytes)
    /// </summary>
    public Hex TransactionHash { get; set; } = Hex.Empty;

    /// <summary>
    /// Index position of the transaction in the block
    /// </summary>
    public ulong TransactionIndex { get; set; }

    /// <summary>
    /// Hash of the block containing this transaction
    /// </summary>
    public Hex BlockHash { get; set; } = Hex.Empty;

    /// <summary>
    /// Number of the block containing this transaction
    /// </summary>
    public BigInteger BlockNumber { get; set; }

    /// <summary>
    /// Address of the sender
    /// </summary>
    public EthereumAddress From { get; set; } = EthereumAddress.Empty;

    /// <summary>
    /// Address of the receiver. Null when it's a contract creation transaction
    /// </summary>
    public EthereumAddress? To { get; set; }

    /// <summary>
    /// The total amount of gas used when this transaction was executed in the block
    /// </summary>
    public BigInteger CumulativeGasUsed { get; set; }

    /// <summary>
    /// The sum of the base fee and tip paid per unit of gas
    /// </summary>
    public EtherAmount EffectiveGasPrice { get; set; } = EtherAmount.Zero;

    /// <summary>
    /// The amount of gas used by this specific transaction alone
    /// </summary>
    public BigInteger GasUsed { get; set; }

    /// <summary>
    /// The contract address created, if the transaction was a contract creation
    /// </summary>
    public EthereumAddress ContractAddress { get; set; } = EthereumAddress.Empty;

    /// <summary>
    /// Array of log entries which this transaction generated
    /// </summary>
    public TransactionLog[] Logs { get; set; } = Array.Empty<TransactionLog>();

    /// <summary>
    /// Bloom filter for light clients to quickly retrieve related logs
    /// </summary>
    public Hex LogsBloom { get; set; } = Hex.Empty;

    /// <summary>
    /// Transaction type: 0 for legacy, 1 for access list types, 2 for dynamic fees
    /// </summary>
    public TransactionType Type { get; set; }

    /// <summary>
    /// Post-transaction state root (pre-Byzantium only)
    /// </summary>
    public Hex StateRoot { get; set; } = Hex.Empty;

    /// <summary>
    /// Indicates if the transaction was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// The amount of gas used for the blob transaction (EIP-4844)
    /// </summary>
    public BigInteger? BlobGasUsed { get; set; }

    /// <summary>
    /// The price of the blob transaction (EIP-4844)
    /// </summary>
    public EtherAmount? BlobGasPrice { get; set; }

    //

    internal static Hex ParseHex(string? hex) =>
        Hex.Parse(hex ?? string.Empty, ParseOptions);

    internal static EthereumAddress ParseAddress(string? hex) =>
        string.IsNullOrEmpty(hex) ? EthereumAddress.Empty : new EthereumAddress(ParseHex(hex));

    internal static ulong ParseUInt64(string? hex) =>
        ParseHex(hex).ToUInt64();

    internal static BigInteger ParseBigInteger(string? hex) =>
        ParseHex(hex).ToBigInteger();

    internal static EtherAmount ParseEtherAmount(string? hex) =>
        EtherAmount.FromWei(ParseHex(hex));

    /// <summary>
    /// Creates a TransactionReceipt from its DTO representation
    /// </summary>
    /// <param name="dto">The DTO representation of the transaction receipt.</param>
    /// <returns>The transaction receipt.</returns>
    internal static TransactionReceipt? FromDto(TransactionReceiptDto? dto)
    {
        if (dto == null) return null;

        return new TransactionReceipt
        {
            TransactionHash = ParseHex(dto.TransactionHashHex),
            TransactionIndex = ParseUInt64(dto.TransactionIndexHex),
            BlockHash = ParseHex(dto.BlockHashHex),
            BlockNumber = ParseBigInteger(dto.BlockNumberHex),
            From = ParseAddress(dto.FromAddressHex),
            To = ParseAddress(dto.ToAddressHex),
            CumulativeGasUsed = ParseBigInteger(dto.CumulativeGasUsedHex),
            EffectiveGasPrice = ParseEtherAmount(dto.EffectiveGasPriceHex),
            GasUsed = ParseBigInteger(dto.GasUsedHex),
            ContractAddress = ParseAddress(dto.ContractAddressHex),
            Logs = dto.Logs?.Select(TransactionLog.FromDto).ToArray() ?? Array.Empty<TransactionLog>(),
            LogsBloom = ParseHex(dto.LogsBloomHex),
            Type = (TransactionType)ParseUInt64(dto.TransactionTypeHex),
            StateRoot = ParseHex(dto.StateRootHex),
            Success = dto.StatusHex != null && ParseUInt64(dto.StatusHex) == 1,
            BlobGasUsed = dto.BlobGasUsedHex != null ? ParseBigInteger(dto.BlobGasUsedHex) : null,
            BlobGasPrice = dto.BlobGasPriceHex != null ? ParseEtherAmount(dto.BlobGasPriceHex) : null,
        };
    }

    /// <summary>
    /// Returns a string representation of the transaction receipt.
    /// </summary>
    /// <returns>A string representation of the transaction receipt.</returns>
    public override string ToString() => $"Tx {TransactionHash} Block {BlockNumber} {(Success ? "✓" : "✗")}";
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
    DynamicFee = 2
}


/// <summary>
/// Represents a log entry from an Ethereum transaction
/// </summary>
public class TransactionLog
{
    /// <summary>
    /// True when the log was removed due to a chain reorganization
    /// </summary>
    public bool Removed { get; set; }

    /// <summary>
    /// Index position of the log in the block
    /// </summary>
    public ulong LogIndex { get; set; }

    /// <summary>
    /// Index position of the transaction that created this log
    /// </summary>
    public ulong TransactionIndex { get; set; }

    /// <summary>
    /// Hash of the transaction that created this log
    /// </summary>
    public Hex TransactionHash { get; set; } = Hex.Empty;

    /// <summary>
    /// Hash of the block containing this log
    /// </summary>
    public Hex BlockHash { get; set; } = Hex.Empty;

    /// <summary>
    /// Number of the block containing this log
    /// </summary>
    public BigInteger BlockNumber { get; set; }

    /// <summary>
    /// Address of the contract that generated this log
    /// </summary>
    public EthereumAddress Address { get; set; } = EthereumAddress.Empty;

    /// <summary>
    /// Contains the non-indexed parameters of the log
    /// </summary>
    public Hex Data { get; set; } = Hex.Empty;

    /// <summary>
    /// Array of 0 to 4 32-byte indexed log parameters
    /// </summary>
    public IReadOnlyList<Hex> Topics { get; set; } = Array.Empty<Hex>();

    /// <summary>
    /// Creates a TransactionLog from its DTO representation
    /// </summary>
    internal static TransactionLog FromDto(LogDto dto)
    {
        return new TransactionLog
        {
            Removed = dto.Removed,
            LogIndex = TransactionReceipt.ParseUInt64(dto.LogIndexHex),
            TransactionIndex = TransactionReceipt.ParseUInt64(dto.TransactionIndexHex),
            TransactionHash = TransactionReceipt.ParseHex(dto.TransactionHashHex),
            BlockHash = TransactionReceipt.ParseHex(dto.BlockHashHex),
            BlockNumber = TransactionReceipt.ParseBigInteger(dto.BlockNumberHex),
            Address = TransactionReceipt.ParseAddress(dto.AddressHex),
            Data = TransactionReceipt.ParseHex(dto.DataHex),
            Topics = dto.TopicsHex.Select(TransactionReceipt.ParseHex).ToList()
        };
    }
}