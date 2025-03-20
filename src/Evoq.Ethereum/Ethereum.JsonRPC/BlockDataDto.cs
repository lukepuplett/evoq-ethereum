using System;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents block data returned by eth_getBlockByNumber RPC method.
/// </summary>
/// <typeparam name="T">The type of the transactions array. Can be either string (hashes only) or TransactionDataDto (full objects).</typeparam>
public class BlockDataDto<T>
{
    /// <summary>The block number. Hex-encoded.</summary>
    public string Number { get; set; } = string.Empty;

    /// <summary>The block's hash. 32 bytes.</summary>
    public string Hash { get; set; } = string.Empty;

    /// <summary>The hash of the parent block.</summary>
    public string ParentHash { get; set; } = string.Empty;

    /// <summary>The block's proof-of-work nonce. Empty after The Merge.</summary>
    public string Nonce { get; set; } = string.Empty;

    /// <summary>The SHA3 hash of the uncles data in the block.</summary>
    public string Sha3Uncles { get; set; } = string.Empty;

    /// <summary>The bloom filter for the logs of the block.</summary>
    public string LogsBloom { get; set; } = string.Empty;

    /// <summary>The root of the transaction trie of the block.</summary>
    public string TransactionsRoot { get; set; } = string.Empty;

    /// <summary>The root of the final state trie of the block.</summary>
    public string StateRoot { get; set; } = string.Empty;

    /// <summary>The root of the receipts trie of the block.</summary>
    public string ReceiptsRoot { get; set; } = string.Empty;

    /// <summary>The address of the beneficiary to whom the mining rewards were given.</summary>
    public string Miner { get; set; } = string.Empty;

    /// <summary>The difficulty of this block. Zero after The Merge.</summary>
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>Total difficulty of the chain until this block. Zero after The Merge.</summary>
    public string TotalDifficulty { get; set; } = string.Empty;

    /// <summary>The "extra data" field of this block.</summary>
    public string ExtraData { get; set; } = string.Empty;

    /// <summary>The size of this block in bytes.</summary>
    public string Size { get; set; } = string.Empty;

    /// <summary>The maximum gas allowed in this block.</summary>
    public string GasLimit { get; set; } = string.Empty;

    /// <summary>The total used gas by all transactions in this block.</summary>
    public string GasUsed { get; set; } = string.Empty;

    /// <summary>The unix timestamp for when the block was collated.</summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>Array of uncle hashes.</summary>
    public string[] Uncles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Array of transaction objects, or hashes depending on the T parameter.
    /// If T is string, contains transaction hashes. If T is TransactionDataDto, contains full transaction objects.
    /// </summary>
    public T[] Transactions { get; set; } = Array.Empty<T>();

    /// <summary>
    /// The base fee per gas in this block. Only present after the London fork (EIP-1559).
    /// </summary>
    public string BaseFeePerGas { get; set; } = string.Empty;

    /// <summary>
    /// Array of withdrawal objects in this block. Only present after the Shanghai fork (EIP-4895).
    /// </summary>
    public WithdrawalDto[] Withdrawals { get; set; } = Array.Empty<WithdrawalDto>();

    /// <summary>
    /// The root hash of the withdrawal trie. Only present after the Shanghai fork (EIP-4895).
    /// </summary>
    public string WithdrawalsRoot { get; set; } = string.Empty;

    /// <summary>
    /// The hash of the parent beacon block. Only present after The Merge.
    /// </summary>
    public string ParentBeaconBlockRoot { get; set; } = string.Empty;
}

/// <summary>
/// Represents a withdrawal in a post-Shanghai block.
/// </summary>
public class WithdrawalDto
{
    /// <summary>
    /// A monotonically increasing index value for the withdrawal.
    /// </summary>
    public string Index { get; set; } = string.Empty;

    /// <summary>
    /// Index of the validator that the withdrawal corresponds to.
    /// </summary>
    public string ValidatorIndex { get; set; } = string.Empty;

    /// <summary>
    /// The address that the withdrawal was sent to.
    /// </summary>
    public string Address { get; set; } = string.Empty;

    /// <summary>
    /// The withdrawal amount in Gwei.
    /// </summary>
    public string Amount { get; set; } = string.Empty;
}