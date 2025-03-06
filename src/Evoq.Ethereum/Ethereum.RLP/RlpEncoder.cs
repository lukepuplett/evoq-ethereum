using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Evoq.Blockchain;
using Evoq.Ethereum.Crypto;
using Evoq.Ethereum.Transactions;
using Org.BouncyCastle.Math;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Encodes an object into RLP format.
/// </summary>
public class RlpEncoder : IRlpTransactionEncoder
{
    /// <summary>
    /// Encodes an EIP-1559 transaction into RLP format.
    /// EIP-1559 introduced a new transaction type (type 2) with a fee market mechanism.
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction.</returns>
    public byte[] Encode(TransactionEIP1559 tx)
    {
        // Check if To is null or an array of all zeros (indicating contract creation)
        bool toIsEmpty = tx.To == null || tx.To.All(b => b == 0);

        // Validate that the transaction has required fields
        // EIP-1559 transactions must have chainId, fee parameters, and gas limit
        if (
            tx.ChainId == 0 ||
            (tx.MaxPriorityFeePerGas.SignValue == 0 && tx.MaxFeePerGas.SignValue == 0) ||
            tx.GasLimit == 0)
        {
            throw new ArgumentException("Transaction cannot be empty or invalid.");
        }

        // [chain_id, nonce, max_priority_fee_per_gas, max_fee_per_gas, gas_limit, to, value, data, access_list, signature_y_parity, signature_r, signature_s]

        // For EIP-1559 transactions, we need to:
        // 1. Start with the transaction type byte (0x02)
        // 2. RLP encode the transaction fields as a list

        // Create the list of fields to encode
        // IMPORTANT: This order is mandatory and must not be changed
        // EIP-1559 defines this exact sequence for transaction encoding
        var fields = new List<object>
        {
            tx.ChainId,              // Chain ID is now a required field in the transaction itself
                                     // This is different from legacy transactions where chainId was optional
                                     // and encoded in the v value for EIP-155
            
            tx.Nonce,                // Transaction sequence number from the sender (prevents replay attacks)
            
            tx.MaxPriorityFeePerGas, // Max fee per gas the sender is willing to give to miners as a tip
                                     // This is a new field introduced in EIP-1559
                                     // Replaces the single gasPrice field from legacy transactions
            
            tx.MaxFeePerGas,         // Max total fee per gas the sender is willing to pay
                                     // This includes both the base fee (burned) and priority fee (to miners)
                                     // Another new field introduced in EIP-1559
            
            tx.GasLimit,             // Maximum gas units allowed for this transaction
                                     // Same as in legacy transactions
            
            tx.To!,                  // Recipient address (or empty for contract creation)
                                     // Same as in legacy transactions
            
            tx.Value,                // Amount of Ether to transfer
                                     // Same as in legacy transactions
            
            tx.Data,                 // Transaction payload data (or contract initialization code)
                                     // Same as in legacy transactions
            
            // Access list (EIP-2930 feature included in EIP-1559)
            // This allows specifying storage slots and addresses the transaction will access
            // to reduce gas costs for accessing them during execution
            EncodeAccessList(tx.AccessList)
        };

        // If the transaction is signed, include signature components
        if (tx.IsSigned(out var signature))
        {
            // Use the new methods from RsvSignature to get the appropriate y-parity value
            // This simplifies the logic and centralizes the signature format handling
            byte yParity = signature.GetYParity(tx);

            // Add y_parity (0 or 1) - this will be minimally encoded
            fields.Add(yParity);

            // R component
            fields.Add(signature.R);

            // S component
            fields.Add(signature.S);
        }
        // NOTE: Unlike legacy transactions, EIP-1559 does not need empty r and s placeholders
        // for unsigned transactions because the chain ID is always included in the transaction fields

        // RLP encode the fields
        // This creates the payload portion of the EIP-1559 transaction
        byte[] rlpEncoded = EncodeList(fields);

        // Prepend the transaction type byte (0x02 for EIP-1559)
        // EIP-1559 transactions are identified by this leading byte
        // This is different from legacy transactions which don't have a type prefix
        byte[] result = new byte[rlpEncoded.Length + 1];
        result[0] = 0x02;  // Type identifier for EIP-1559 transactions
        Array.Copy(rlpEncoded, 0, result, 1, rlpEncoded.Length);

        return result;
    }

    /// <summary>
    /// Encodes a legacy transaction into RLP format.
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <param name="chainId">The chain ID to use for EIP-155 replay protection when signing an unsigned transaction.</param>
    /// <returns>The RLP encoded transaction.</returns>
    public byte[] Encode(Transaction tx, ulong chainId = 0)
    {
        // Check if To is null or an array of all zeros (indicating contract creation)
        bool toIsEmpty = tx.To == null || tx.To.All(b => b == 0);

        // Validate that the transaction has at least some data
        // A completely empty transaction is invalid in Ethereum
        if (
            tx.Nonce == 0 &&
            tx.GasPrice.SignValue == 0 &&
            tx.GasLimit == 0 &&
            toIsEmpty &&
            tx.Value.SignValue == 0 &&
            tx.Data.Length == 0)
        {
            throw new ArgumentException("Transaction cannot be empty or invalid.");
        }

        // Create a list with the basic transaction fields in the order specified by Ethereum protocol
        // IMPORTANT: This order is mandatory and must not be changed
        // The Ethereum Yellow Paper defines this exact sequence for transaction encoding
        var fields = new List<object>
        {
            tx.Nonce,    // Transaction sequence number from the sender (prevents replay attacks)
            tx.GasPrice, // Price per gas unit the sender is willing to pay
            tx.GasLimit, // Maximum gas units allowed for this transaction
            tx.To!,      // Recipient address (or empty for contract creation)
            tx.Value,    // Amount of Ether to transfer
            tx.Data      // Transaction payload data (or contract initialization code)
        };

        // Handle signature components based on whether the transaction is signed or not
        if (tx.IsSigned(out var signature))
        {
            // For signed transactions, append the signature components (v, r, s)
            // NOTE: The order here is v, r, s (not r, s, v) as per Ethereum specification

            // Use the V value directly for legacy transactions
            // The RsvSignature struct already handles the correct format for V
            fields.Add(signature.V);

            // R component
            fields.Add(signature.R);

            // S component
            fields.Add(signature.S);
        }
        else if (chainId > 0)
        {
            // For unsigned transactions with EIP-155 replay protection
            // This is used when preparing a transaction for signing

            // Add the chain ID as the v value
            // This implements EIP-155 replay protection for unsigned transactions
            // When the transaction is signed, this chainId will be incorporated into the v value
            fields.Add(chainId);

            // Add empty placeholders for r and s
            // IMPORTANT: These empty placeholders are REQUIRED by EIP-155 specification
            // When calculating the transaction hash for signing, EIP-155 requires including:
            // 1. All transaction fields
            // 2. The chain ID (as v)
            // 3. Empty values for r and s
            // This ensures the signature is bound to a specific chain ID, preventing replay attacks
            fields.Add(new byte[0]); // r placeholder as empty byte array
            fields.Add(new byte[0]); // s placeholder as empty byte array
        }
        // NOTE: If the transaction is unsigned and chainId is 0, we don't add v, r, s fields
        // This is for legacy (pre-EIP-155) unsigned transactions
        // Such transactions are vulnerable to replay attacks across different chains

        // Encode the entire list of fields using RLP encoding
        // RLP (Recursive Length Prefix) is the standard serialization method in Ethereum
        return EncodeList(fields);
    }

    /// <summary>
    /// Encodes a transaction for signing.
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <param name="chainId">The chain ID to use for EIP-155 replay protection.</param>
    /// <returns>The RLP encoded transaction for signing.</returns>
    public byte[] EncodeForSigning(Transaction tx, ulong chainId = 0)
    {
        // Create a copy of the transaction without a signature
        var unsignedTx = new Transaction(
            tx.Nonce,
            tx.GasPrice,
            tx.GasLimit,
            tx.To,
            tx.Value,
            tx.Data,
            null // No signature
        );

        // Encode with chainId for EIP-155 replay protection
        return Encode(unsignedTx, chainId);
    }

    /// <summary>
    /// Encodes a transaction for signing (excludes signature components).
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction for signing.</returns>
    public byte[] EncodeForSigning(TransactionEIP1559 tx)
    {
        // Create a copy of the transaction without a signature
        var unsignedTx = new TransactionEIP1559(
            tx.ChainId,
            tx.Nonce,
            tx.MaxPriorityFeePerGas,
            tx.MaxFeePerGas,
            tx.GasLimit,
            tx.To,
            tx.Value,
            tx.Data,
            tx.AccessList,
            null // No signature
        );

        return Encode(unsignedTx);
    }

    /// <summary>
    /// Encodes a string into RLP format.
    /// </summary>
    /// <param name="str">The string to encode.</param>
    /// <returns>The RLP encoded string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the string is null.</exception>
    public byte[] Encode(string str)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        return Encode(Encoding.UTF8.GetBytes(str));
    }

    /// <summary>
    /// Encodes a byte array into RLP format.
    /// </summary>
    /// <param name="bytes">The byte array to encode.</param>
    /// <returns>The RLP encoded byte array.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the byte array is null.</exception>
    public byte[] Encode(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        return EncodeBytes(bytes);
    }

    /// <summary>
    /// Encodes a list of objects into RLP format.
    /// </summary>
    /// <param name="list">The list to encode.</param>
    /// <returns>The RLP encoded list.</returns>
    public byte[] Encode(List<object> list)
    {
        return EncodeList(list);
    }

    /// <summary>
    /// Encodes a single byte into RLP format.
    /// </summary>
    /// <param name="b">The byte to encode.</param>
    /// <returns>The RLP encoded byte.</returns>
    public byte[] Encode(byte b)
    {
        return new byte[] { b };
    }

    /// <summary>
    /// Encodes an unsigned long into RLP format.
    /// </summary>
    /// <param name="value">The unsigned long to encode.</param>
    /// <returns>The RLP encoded unsigned long.</returns>
    public byte[] Encode(ulong value)
    {
        if (value == 0)
        {
            return new byte[] { 0x80 }; // RLP encoding for empty byte array (zero value)
        }

        // Convert ulong to big-endian byte array
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }

        // Remove leading zeros
        int startIndex = 0;
        while (startIndex < bytes.Length && bytes[startIndex] == 0)
        {
            startIndex++;
        }

        byte[] result = bytes[startIndex..];

        return EncodeBytes(result);
    }

    /// <summary>
    /// Encodes a BigInteger into RLP format.
    /// </summary>
    /// <param name="value">The BigInteger to encode.</param>
    /// <returns>The RLP encoded BigInteger.</returns>
    public byte[] Encode(BigInteger value)
    {
        // Convert the BigInteger to a byte array
        byte[] bytes = value.ToByteArray();

        // If the value is zero, return the RLP encoding for zero
        if (bytes.Length == 0 || (bytes.Length == 1 && bytes[0] == 0))
        {
            return new byte[] { 0x80 };  // Return [0x80] for zero
        }

        // Bouncy Castle's BigInteger.ToByteArray() returns big-endian format
        // but may include a leading zero for positive numbers where the high bit is set
        // We need to remove this leading zero if present
        if (bytes.Length > 1 && bytes[0] == 0 && (bytes[1] & 0x80) != 0)
        {
            bytes = bytes.Skip(1).ToArray();
        }

        return EncodeBytes(bytes);
    }

    /// <summary>
    /// Encodes a byte array representing a number in big-endian format into RLP format.
    /// This can be used for encoding numbers with custom representations (e.g., negative numbers as two's complement).
    /// </summary>
    /// <param name="numberBytes">The byte array representing the number in big-endian format.</param>
    /// <returns>The RLP encoded number.</returns>
    public byte[] EncodeNumber(byte[] numberBytes)
    {
        if (numberBytes == null)
        {
            throw new ArgumentNullException(nameof(numberBytes));
        }

        // If the number is zero (empty byte array or all zeros), encode as empty string
        if (numberBytes.Length == 0 || numberBytes.All(b => b == 0))
        {
            return new byte[] { 0x80 };
        }

        // Remove leading zeros
        numberBytes = numberBytes.SkipWhile((b, i) => b == 0 && i < numberBytes.Length - 1).ToArray();

        return EncodeBytes(numberBytes);
    }

    /// <summary>
    /// Encodes an object into RLP format.
    /// </summary>
    /// <param name="item">The object to encode.</param>
    /// <returns>The RLP encoded object.</returns>
    /// <exception cref="ArgumentException">Thrown if the object is not supported.</exception>
    public byte[] Encode(object item)
    {
        return item switch
        {
            byte[] byteArray => Encode(byteArray),
            List<object> list => Encode(list),
            object[] array => Encode(array.ToList()),
            List<List<object>> nestedList => Encode(nestedList.Cast<object>().ToList()),
            byte b => Encode(b),
            ulong ulongValue => Encode(ulongValue),
            BigInteger bigInt => Encode(bigInt),
            string str => Encode(str),
            Transaction tx => Encode(tx),
            TransactionEIP1559 tx1559 => Encode(tx1559),
            Hex hex => Encode(hex.ToByteArray()),
            _ => throw new ArgumentException($"Unsupported type: {item?.GetType().Name ?? "null"}")
        };
    }

    /// <summary>
    /// Encodes an access list into RLP format.
    /// </summary>
    /// <param name="accessList">The access list to encode.</param>
    /// <returns>The RLP encoded access list.</returns>
    private List<List<object>> EncodeAccessList(AccessListItem[] accessList)
    {
        /*

        RLP encoding of an access list:

        [
            [address1, [storageKey1_1, storageKey1_2, ...]],
            [address2, [storageKey2_1, storageKey2_2, ...]],
            ...
        ]

        */

        var result = new List<List<object>>();

        if (accessList == null || accessList.Length == 0)
        {
            return result;
        }

        foreach (var item in accessList)
        {
            var storageKeys = new List<object>();
            foreach (var key in item.StorageKeys)
            {
                storageKeys.Add(key);
            }

            result.Add(new List<object> { item.Address, storageKeys });
        }

        return result;
    }

    private byte[] EncodeBytes(byte[] data)
    {
        if (data.Length == 1 && data[0] <= 0x7F)
        {
            return data; // Single byte case
        }

        if (data.Length <= 55)
        {
            byte pre = (byte)(0x80 + data.Length);
            return new[] { pre }.Concat(data).ToArray();
        }

        byte[] lengthBytes = ToBigEndianBytes(data.Length);
        byte prefix = (byte)(0xB7 + lengthBytes.Length);

        return new[] { prefix }.Concat(lengthBytes).Concat(data).ToArray();
    }

    private byte[] EncodeList(List<object> items)
    {
        var encodedItems = items
            .Select(this.Encode)
            .Aggregate(new byte[0], (acc, next) => acc.Concat(next).ToArray());

        if (encodedItems.Length <= 55)
        {
            byte pre = (byte)(0xC0 + encodedItems.Length);
            return new[] { pre }.Concat(encodedItems).ToArray();
        }

        byte[] lengthBytes = ToBigEndianBytes(encodedItems.Length);
        byte prefix = (byte)(0xF7 + lengthBytes.Length);

        return new[] { prefix }.Concat(lengthBytes).Concat(encodedItems).ToArray();
    }

    private static byte[] ToBigEndianBytes(int value)
    {
        byte[] bytes = BitConverter.GetBytes(value);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(bytes);
        }
        // Remove leading zeros to get the minimum representation
        return bytes.SkipWhile(b => b == 0).ToArray();
    }
}
