using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;

namespace Evoq.Ethereum.RLP;

/// <summary>
/// Encodes an object into RLP format.
/// </summary>
public class RlpEncoder
{
    /// <summary>
    /// Encodes a transaction into RLP format.
    /// </summary>
    /// <param name="tx">The transaction to encode.</param>
    /// <returns>The RLP encoded transaction.</returns>
    public byte[] Encode(Transaction tx)
    {
        // Check if To is null or an array of all zeros
        bool toIsEmpty = tx.To == null || tx.To.All(b => b == 0);

        if (
            tx.Nonce == 0 &&
            tx.GasPrice == 0 &&
            tx.GasLimit == 0 &&
            toIsEmpty &&
            tx.Value == 0 &&
            tx.Data.Length == 0 &&
            tx.V == 0 &&
            tx.R == 0 &&
            tx.S == 0)
        {
            throw new ArgumentException("Transaction cannot be empty or invalid.");
        }

        var fields = new List<object>
        {
            tx.Nonce,
            tx.GasPrice,
            tx.GasLimit,
            tx.To,
            tx.Value,
            tx.Data,
            tx.V,
            tx.R,
            tx.S
        };
        return Encode(fields); // Calls Encode(List<object>)
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
    /// <exception cref="ArgumentException">Thrown when the BigInteger is negative.</exception>
    public byte[] Encode(BigInteger value)
    {
        if (value < 0)
        {
            throw new ArgumentException("RLP encoding doesn't support negative numbers");
        }

        if (value == 0)
        {
            return new byte[] { 0 };  // Single byte 0 should be encoded as-is
        }

        byte[] bytes = value.ToByteArray();

        // BigInteger.ToByteArray() returns little-endian with a possible sign byte
        // Convert to big-endian and remove unnecessary leading zeroes
        bytes = bytes.Reverse().SkipWhile((b, i) => b == 0 && i < bytes.Length - 1).ToArray();

        return EncodeBytes(bytes);
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
            byte b => Encode(b),
            ulong ulongValue => Encode(ulongValue),
            BigInteger bigInt => Encode(bigInt),
            string str => Encode(str), // <-- Add this line to handle strings
            _ => throw new ArgumentException($"Unsupported type: {item?.GetType().Name ?? "null"}")
        };
    }

    //

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
        var bytes = BitConverter.GetBytes(value).Reverse().ToArray();
        int leadingZeros = bytes.TakeWhile(b => b == 0).Count();
        return bytes.Skip(leadingZeros).ToArray(); // Remove leading zeros
    }
}
