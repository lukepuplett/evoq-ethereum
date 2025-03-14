using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.JsonRPC;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Represents transaction data with proper .NET types.
/// </summary>
public class TransactionData
{
    /// <summary>
    /// The hash of the block where this transaction was included.
    /// </summary>
    public Hex? BlockHash { get; set; }

    /// <summary>
    /// The block number where this transaction was included.
    /// </summary>
    public BigInteger BlockNumber { get; set; }

    /// <summary>
    /// The sender address.
    /// </summary>
    public EthereumAddress? From { get; set; }

    /// <summary>
    /// The gas limit provided by the sender.
    /// </summary>
    public BigInteger Gas { get; set; }

    /// <summary>
    /// The gas price provided by the sender in wei.
    /// </summary>
    public BigInteger GasPrice { get; set; }

    /// <summary>
    /// The hash of the transaction.
    /// </summary>
    public Hex? Hash { get; set; }

    /// <summary>
    /// The data sent along with the transaction.
    /// </summary>
    public Hex? Input { get; set; }

    /// <summary>
    /// The number of transactions made by the sender prior to this one.
    /// </summary>
    public BigInteger Nonce { get; set; }

    /// <summary>
    /// The recipient address (null when it's a contract creation transaction).
    /// </summary>
    public EthereumAddress? To { get; set; }

    /// <summary>
    /// The index of the transaction in the block.
    /// </summary>
    public BigInteger TransactionIndex { get; set; }

    /// <summary>
    /// The value transferred in wei.
    /// </summary>
    public BigInteger Value { get; set; }

    /// <summary>
    /// The ECDSA recovery ID.
    /// </summary>
    public Hex? V { get; set; }

    /// <summary>
    /// The ECDSA signature r.
    /// </summary>
    public Hex? R { get; set; }

    /// <summary>
    /// The ECDSA signature s.
    /// </summary>
    public Hex? S { get; set; }

    /// <summary>
    /// The transaction type (0 = legacy, 1 = EIP-2930, 2 = EIP-1559).
    /// </summary>
    public BigInteger Type { get; set; }

    /// <summary>
    /// The maximum fee per gas (EIP-1559 transactions).
    /// </summary>
    public BigInteger MaxFeePerGas { get; set; }

    /// <summary>
    /// The maximum priority fee per gas (EIP-1559 transactions).
    /// </summary>
    public BigInteger MaxPriorityFeePerGas { get; set; }

    /// <summary>
    /// The chain ID specified in the transaction.
    /// </summary>
    public BigInteger ChainId { get; set; }

    /// <summary>
    /// The access list (EIP-2930 and EIP-1559 transactions).
    /// </summary>
    public AccessList? AccessList { get; set; }

    /// <summary>
    /// Creates a TransactionData instance from a TransactionDataDto.
    /// </summary>
    /// <param name="dto">The DTO to convert from.</param>
    /// <returns>A new TransactionData instance.</returns>
    public static TransactionData? FromDto(TransactionDataDto? dto)
    {
        if (dto == null)
            return null;

        var result = new TransactionData
        {
            BlockHash = TryParseHex(dto.BlockHash),
            BlockNumber = string.IsNullOrEmpty(dto.BlockNumber) ? BigInteger.Zero : Hex.Parse(dto.BlockNumber).ToBigInteger(),
            From = string.IsNullOrEmpty(dto.From) ? null : new EthereumAddress(dto.From),
            Gas = string.IsNullOrEmpty(dto.Gas) ? BigInteger.Zero : Hex.Parse(dto.Gas).ToBigInteger(),
            GasPrice = string.IsNullOrEmpty(dto.GasPrice) ? BigInteger.Zero : Hex.Parse(dto.GasPrice).ToBigInteger(),
            Hash = TryParseHex(dto.Hash),
            Input = TryParseHex(dto.Input),
            Nonce = string.IsNullOrEmpty(dto.Nonce) ? BigInteger.Zero : Hex.Parse(dto.Nonce).ToBigInteger(),
            To = string.IsNullOrEmpty(dto.To) ? null : new EthereumAddress(dto.To),
            TransactionIndex = string.IsNullOrEmpty(dto.TransactionIndex) ? BigInteger.Zero : Hex.Parse(dto.TransactionIndex).ToBigInteger(),
            Value = string.IsNullOrEmpty(dto.Value) ? BigInteger.Zero : Hex.Parse(dto.Value).ToBigInteger(),
            V = TryParseHex(dto.V),
            R = TryParseHex(dto.R),
            S = TryParseHex(dto.S),
            Type = string.IsNullOrEmpty(dto.Type) ? BigInteger.Zero : Hex.Parse(dto.Type).ToBigInteger(),
            MaxFeePerGas = string.IsNullOrEmpty(dto.MaxFeePerGas) ? BigInteger.Zero : Hex.Parse(dto.MaxFeePerGas).ToBigInteger(),
            MaxPriorityFeePerGas = string.IsNullOrEmpty(dto.MaxPriorityFeePerGas) ? BigInteger.Zero : Hex.Parse(dto.MaxPriorityFeePerGas).ToBigInteger(),
            ChainId = string.IsNullOrEmpty(dto.ChainId) ? BigInteger.Zero : Hex.Parse(dto.ChainId).ToBigInteger()
        };

        // Convert access list if present
        if (dto.AccessList != null)
        {
            result.AccessList = new AccessList
            {
                Entries = new AccessListEntry[dto.AccessList.AccessList?.Length ?? 0]
            };

            if (dto.AccessList.AccessList != null)
            {
                for (int i = 0; i < dto.AccessList.AccessList.Length; i++)
                {
                    var entry = dto.AccessList.AccessList[i];
                    var storageKeys = new Hex[entry.StorageKeys?.Length ?? 0];

                    if (entry.StorageKeys != null)
                    {
                        for (int j = 0; j < entry.StorageKeys.Length; j++)
                        {
                            storageKeys[j] = Hex.Parse(entry.StorageKeys[j]);
                        }
                    }

                    result.AccessList.Entries[i] = new AccessListEntry
                    {
                        Address = string.IsNullOrEmpty(entry.Address) ? null : new EthereumAddress(entry.Address),
                        StorageKeys = storageKeys
                    };
                }
            }
        }

        return result;
    }

    //

    private static Hex? TryParseHex(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        return Hex.Parse(value);
    }
}

/// <summary>
/// Represents an access list for EIP-2930 transactions.
/// </summary>
public class AccessList
{
    /// <summary>
    /// The entries in the access list.
    /// </summary>
    public AccessListEntry[]? Entries { get; set; }
}

/// <summary>
/// Represents an entry in an access list.
/// </summary>
public class AccessListEntry
{
    /// <summary>
    /// The address being accessed.
    /// </summary>
    public EthereumAddress? Address { get; set; }

    /// <summary>
    /// The storage keys being accessed.
    /// </summary>
    public Hex[]? StorageKeys { get; set; }
}
