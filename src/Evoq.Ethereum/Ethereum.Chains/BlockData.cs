using System;
using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Represents a block with transaction data.
/// </summary>
/// <typeparam name="T">The type of the transaction data.</typeparam>
public class BlockData<T>
{
    /// <summary>   
    /// The block number.
    /// </summary>
    public BigInteger Number { get; set; }

    /// <summary>
    /// The block hash. 
    /// </summary>
    public Hex? Hash { get; set; }

    /// <summary>
    /// The parent block hash.
    /// </summary>
    public Hex? ParentHash { get; set; }

    /// <summary>
    /// The block nonce.
    /// </summary>
    public Hex? Nonce { get; set; }

    /// <summary>
    /// The SHA3 of the uncles data in the block.
    /// </summary>
    public Hex? Sha3Uncles { get; set; }

    /// <summary>
    /// The bloom filter for the logs of the block.
    /// </summary>
    public Hex? LogsBloom { get; set; }

    /// <summary>
    /// The root of the transaction trie of the block.
    /// </summary>
    public Hex? TransactionsRoot { get; set; }

    /// <summary>
    /// The root of the final state trie of the block.
    /// </summary>
    public Hex? StateRoot { get; set; }

    /// <summary>
    /// The root of the receipts trie of the block.
    /// </summary>
    public Hex? ReceiptsRoot { get; set; }

    /// <summary>
    /// The address of the beneficiary to whom the mining rewards were given.
    /// </summary>
    public EthereumAddress? Miner { get; set; }

    /// <summary>
    /// The difficulty for this block.
    /// </summary>
    public BigInteger Difficulty { get; set; }

    /// <summary>
    /// The total difficulty of the chain until this block.
    /// </summary>
    public BigInteger TotalDifficulty { get; set; }

    /// <summary>
    /// The "extra data" field of this block.
    /// </summary>
    public Hex? ExtraData { get; set; }

    /// <summary>
    /// The size of this block in bytes.
    /// </summary>
    public BigInteger Size { get; set; }

    /// <summary>
    /// The maximum gas allowed in this block.
    /// </summary>
    public BigInteger GasLimit { get; set; }

    /// <summary>
    /// The total used gas by all transactions in this block.
    /// </summary>
    public BigInteger GasUsed { get; set; }

    /// <summary>
    /// The block timestamp.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>
    /// Array of uncle hashes.
    /// </summary>
    public Hex[]? Uncles { get; set; } = Array.Empty<Hex>();

    /// <summary>
    /// The transactions.
    /// </summary>
    public T[]? Transactions { get; set; } = Array.Empty<T>();

    /// <summary>
    /// The base fee per gas (EIP-1559).
    /// </summary>
    public BigInteger BaseFeePerGas { get; set; }

    /// <summary>
    /// Withdrawals in the block (EIP-4895).
    /// </summary>
    public Withdrawal[]? Withdrawals { get; set; } = Array.Empty<Withdrawal>();

    /// <summary>
    /// The root of the withdrawal trie of the block (EIP-4895).
    /// </summary>
    public Hex? WithdrawalsRoot { get; set; }

    /// <summary>
    /// The parent beacon block root (post-Merge).
    /// </summary>
    public Hex? ParentBeaconBlockRoot { get; set; }

    /// <summary>
    /// Creates a BlockData instance from a BlockDataDto.
    /// </summary>
    /// <param name="dto">The DTO to convert from.</param>
    /// <param name="convertTransaction">Function to convert transaction data.</param>
    /// <returns>A new BlockData instance.</returns>
    public static BlockData<T>? FromDto<TDto>(BlockDataDto<TDto>? dto, Func<TDto, T> convertTransaction)
    {
        if (dto == null)
            return null;

        var result = new BlockData<T>
        {
            Number = string.IsNullOrEmpty(dto.Number) ? BigInteger.Zero : Hex.Parse(dto.Number).ToBigInteger(),
            Hash = string.IsNullOrEmpty(dto.Hash) ? null : Hex.Parse(dto.Hash),
            ParentHash = string.IsNullOrEmpty(dto.ParentHash) ? null : Hex.Parse(dto.ParentHash),
            Nonce = string.IsNullOrEmpty(dto.Nonce) ? null : Hex.Parse(dto.Nonce),
            Sha3Uncles = string.IsNullOrEmpty(dto.Sha3Uncles) ? null : Hex.Parse(dto.Sha3Uncles),
            LogsBloom = string.IsNullOrEmpty(dto.LogsBloom) ? null : Hex.Parse(dto.LogsBloom),
            TransactionsRoot = string.IsNullOrEmpty(dto.TransactionsRoot) ? null : Hex.Parse(dto.TransactionsRoot),
            StateRoot = string.IsNullOrEmpty(dto.StateRoot) ? null : Hex.Parse(dto.StateRoot),
            ReceiptsRoot = string.IsNullOrEmpty(dto.ReceiptsRoot) ? null : Hex.Parse(dto.ReceiptsRoot),
            Miner = string.IsNullOrEmpty(dto.Miner) ? null : new EthereumAddress(dto.Miner),
            Difficulty = string.IsNullOrEmpty(dto.Difficulty) ? BigInteger.Zero : Hex.Parse(dto.Difficulty).ToBigInteger(),
            TotalDifficulty = string.IsNullOrEmpty(dto.TotalDifficulty) ? BigInteger.Zero : Hex.Parse(dto.TotalDifficulty).ToBigInteger(),
            ExtraData = string.IsNullOrEmpty(dto.ExtraData) ? null : Hex.Parse(dto.ExtraData),
            Size = string.IsNullOrEmpty(dto.Size) ? BigInteger.Zero : Hex.Parse(dto.Size).ToBigInteger(),
            GasLimit = string.IsNullOrEmpty(dto.GasLimit) ? BigInteger.Zero : Hex.Parse(dto.GasLimit).ToBigInteger(),
            GasUsed = string.IsNullOrEmpty(dto.GasUsed) ? BigInteger.Zero : Hex.Parse(dto.GasUsed).ToBigInteger(),
            BaseFeePerGas = string.IsNullOrEmpty(dto.BaseFeePerGas) ? BigInteger.Zero : Hex.Parse(dto.BaseFeePerGas).ToBigInteger(),
            WithdrawalsRoot = string.IsNullOrEmpty(dto.WithdrawalsRoot) ? null : Hex.Parse(dto.WithdrawalsRoot),
            ParentBeaconBlockRoot = string.IsNullOrEmpty(dto.ParentBeaconBlockRoot) ? null : Hex.Parse(dto.ParentBeaconBlockRoot)
        };

        // Convert timestamp from Unix seconds to DateTimeOffset
        if (!string.IsNullOrEmpty(dto.Timestamp))
        {
            var unixTimestamp = Hex.Parse(dto.Timestamp).ToBigInteger();
            result.Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)unixTimestamp);
        }

        // Convert uncle hashes
        if (dto.Uncles != null)
        {
            result.Uncles = new Hex[dto.Uncles.Length];
            for (int i = 0; i < dto.Uncles.Length; i++)
            {
                result.Uncles[i] = Hex.Parse(dto.Uncles[i]);
            }
        }

        // Convert transactions using the provided conversion function
        if (dto.Transactions != null)
        {
            result.Transactions = new T[dto.Transactions.Length];
            for (int i = 0; i < dto.Transactions.Length; i++)
            {
                result.Transactions[i] = convertTransaction(dto.Transactions[i]);
            }
        }

        // Convert withdrawals
        if (dto.Withdrawals != null)
        {
            result.Withdrawals = new Withdrawal[dto.Withdrawals.Length];
            for (int i = 0; i < dto.Withdrawals.Length; i++)
            {
                var w = dto.Withdrawals[i];
                result.Withdrawals[i] = new Withdrawal
                {
                    Index = string.IsNullOrEmpty(w.Index) ? BigInteger.Zero : Hex.Parse(w.Index).ToBigInteger(),
                    ValidatorIndex = string.IsNullOrEmpty(w.ValidatorIndex) ? BigInteger.Zero : Hex.Parse(w.ValidatorIndex).ToBigInteger(),
                    Address = string.IsNullOrEmpty(w.Address) ? null : new EthereumAddress(w.Address),
                    Amount = string.IsNullOrEmpty(w.Amount) ? BigInteger.Zero : Hex.Parse(w.Amount).ToBigInteger()
                };
            }
        }

        return result;
    }
}

/// <summary>
/// Represents a withdrawal in a post-Shanghai block.
/// </summary>
public class Withdrawal
{
    /// <summary>
    /// The index of the withdrawal.
    /// </summary>
    public BigInteger Index { get; set; }

    /// <summary>
    /// The validator index.
    /// </summary>
    public BigInteger ValidatorIndex { get; set; }

    /// <summary>
    /// The address receiving the withdrawal.
    /// </summary>
    public EthereumAddress? Address { get; set; }

    /// <summary>
    /// The amount of the withdrawal in Gwei.
    /// </summary>
    public BigInteger Amount { get; set; }
}
