using System;
using System.Collections.Generic;
using System.Diagnostics;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a bytes type to its ABI binary representation.
/// </summary>
internal class BytesTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BytesTypeEncoder"/> class.
    /// </summary>
    public BytesTypeEncoder()
        : base(
            new HashSet<string> { "bytes" },
            new HashSet<Type> { typeof(byte[]), typeof(Hex) })
    { }

    //

    /// <summary>
    /// Attempts to encode a CLR object to its ABI binary representation as a bytes type.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    /// <returns>True if the encoding was successful, false otherwise.</returns>
    public bool TryEncode(string abiType, object value, out byte[] encoded, int length = 32)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        if (value is byte[] bytes)
        {
            encoded = EncodeBytes(bytes, length);
            return true;
        }

        if (value is Hex hexValue)
        {
            encoded = EncodeBytes(hexValue.ToByteArray(), length);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to decode a bytes type from its ABI binary representation.
    /// </summary>
    /// <remarks>
    /// This method expects the data to be unpadded, i.e. the data was read up
    /// to the length of the bytes as specified in the encoded data.
    /// </remarks>
    /// <param name="abiType">The ABI type to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <param name="clrType">The CLR type to decode to.</param>
    /// <param name="decoded">The decoded value if successful.</param>
    /// <returns>True if the decoding was successful, false otherwise.</returns>
    public bool TryDecode(string abiType, byte[] data, Type clrType, out object? decoded)
    {
        decoded = null;

        if (!this.IsCompatible(abiType, clrType, out var _))
        {
            return false;
        }

        if (clrType == typeof(byte[]))
        {
            decoded = DecodeBytes(data);
            return true;
        }

        if (clrType == typeof(Hex))
        {
            decoded = new Hex(data);
            return true;
        }

        return false;
    }

    //

    /// <summary>
    /// Encodes a byte array to a byte array padded to 32 bytes or returns empty for null.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    /// <returns>The encoded bytes, padded to a multiple of 32 bytes.</returns>
    public static byte[] EncodeBytes(byte[] bytes, int length = 32)
    {
        if (bytes == null)
        {
            return Array.Empty<byte>();
        }

        byte[] result;

        if (length == -1)
        {
            // No padding for packed encoding
            result = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
            return result;
        }

        // Calculate how many bytes we need to add to make the length a multiple of 'length'
        var inputLength = bytes.Length;
        var remainder = inputLength % length;
        var padding = remainder == 0 ? 0 : length - remainder;

        result = new byte[inputLength + padding];
        Buffer.BlockCopy(bytes, 0, result, 0, inputLength);

        Debug.Assert(result.Length % length == 0, "Result length should be a multiple of the specified length");

        return result;
    }

    /// <summary>
    /// Decodes a byte array from its ABI binary representation.
    /// </summary>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded byte array.</returns>
    public static byte[] DecodeBytes(byte[] data)
    {
        if (data == null)
        {
            return Array.Empty<byte>();
        }

        byte[] result = new byte[data.Length];
        Buffer.BlockCopy(data, 0, result, 0, data.Length);
        return result;
    }

    /// <summary>
    /// Attempts to get the default CLR type for a bytes type.
    /// </summary>
    /// <param name="abiType">The ABI type to get the default CLR type for.</param>
    /// <param name="clrType">The default CLR type if successful.</param>
    /// <returns>True if the default CLR type was successfully retrieved, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (abiType != "bytes")
        {
            return false;
        }

        clrType = typeof(byte[]);
        return true;
    }
}
