using System;
using System.Collections.Generic;
using System.Text;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a string type to its ABI binary representation.
/// </summary>
public class StringTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringTypeEncoder"/> class.
    /// </summary>
    public StringTypeEncoder()
        : base(new HashSet<string> { "string", "bytes" }, new HashSet<Type> { typeof(string) })
    {
    }

    //

    /// <summary>
    /// Attempts to encode a string type to its ABI binary representation.
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

        //

        if (value is string str)
        {
            encoded = EncodeString(str);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to decode a string from its ABI binary representation.
    /// </summary>
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

        //

        if (clrType == typeof(string))
        {
            decoded = DecodeString(data);
            return true;
        }

        return false;
    }

    //

    /// <summary>
    /// Encodes a string to its ABI binary representation.
    /// </summary>
    /// <param name="str">The string to encode.</param>
    /// <returns>The encoded string.</returns>
    public static byte[] EncodeString(string str)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));

        byte[] stringBytes = Encoding.UTF8.GetBytes(str);
        int paddedLength = ((stringBytes.Length + 31) / 32) * 32;
        byte[] result = new byte[paddedLength];
        Array.Copy(stringBytes, 0, result, 0, stringBytes.Length);

        return result;
    }

    /// <summary>
    /// Decodes a string from its ABI binary representation.
    /// </summary>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded string.</returns>
    public static string DecodeString(byte[] data)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));

        return Encoding.UTF8.GetString(data).Trim('\0');
    }
}
