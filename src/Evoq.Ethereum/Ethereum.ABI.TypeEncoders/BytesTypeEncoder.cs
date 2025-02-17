using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a bytes type to its ABI binary representation.
/// </summary>
public class BytesTypeEncoder : AbiCompatChecker, IAbiEncode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BytesTypeEncoder"/> class.
    /// </summary>
    public BytesTypeEncoder()
        : base(new HashSet<string> { "bytes" }, new HashSet<Type> { typeof(byte[]) }) { }

    /// <summary>
    /// Attempts to encode a bytes type to its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    /// <returns>True if the encoding was successful, false otherwise.</returns>
    public bool TryEncode(string abiType, object value, out byte[] encoded)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        if (value is byte[] bytes)
        {
            encoded = EncodeBytes(bytes);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Encodes a byte array to a byte array padded to 32 bytes or returns empty for null.
    /// </summary>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The encoded bytes, padded to a multiple of 32 bytes.</returns>
    public static byte[] EncodeBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            return Array.Empty<byte>();
        }

        // we're returning the full length bytes but
        // we just need to pad the input until the
        // length is a multiple of 32

        var inputLength = bytes.Length;
        var padding = (32 - (inputLength % 32)) % 32;

        var result = new byte[inputLength + padding];
        Buffer.BlockCopy(bytes, 0, result, 0, inputLength);

        Debug.Assert(result.Length == inputLength + padding);

        return result;
    }
}
