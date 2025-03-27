using System;
using System.Collections.Generic;
using System.Text;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a string type to its ABI binary representation.
/// </summary>
internal class StringTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringTypeEncoder"/> class.
    /// </summary>
    public StringTypeEncoder()
        : base(
            new HashSet<string> { "string", "bytes" },
            new HashSet<Type> { typeof(string) })
    {
    }

    //

    /// <summary>
    /// Attempts to encode a string type to its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    /// <param name="length">The length of the byte array to return or -1 for no padding.</param>
    /// <returns>True if the encoding was successful, false otherwise.</returns>
    public bool TryEncode(string abiType, object value, out byte[] encoded, int length = 32)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        //

        if (value is string str)
        {
            encoded = EncodeString(str, length);
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
    /// <param name="length">The length of the byte array to return or -1 for no padding.</param>
    /// <returns>The encoded string.</returns>
    public static byte[] EncodeString(string str, int length = 32)
    {
        if (str == null)
        {
            throw new ArgumentNullException(nameof(str));
        }

        if (length == 0 || length < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        byte[] stringBytes = Encoding.UTF8.GetBytes(str);

        if (length == -1)
        {
            return stringBytes;
        }

        if (stringBytes.Length == 0)
        {
            return new byte[length];
        }

        // Calculate how many bytes we need to add to make the length a multiple of 'length'
        var inputLength = stringBytes.Length;
        var remainder = inputLength % length;
        var padding = remainder == 0 ? 0 : length - remainder;

        byte[] result = new byte[inputLength + padding];
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

    /// <summary>
    /// Attempts to get the default CLR type for a string type.
    /// </summary>
    /// <param name="abiType">The ABI type to get the default CLR type for.</param>
    /// <param name="clrType">The default CLR type if successful.</param>
    /// <returns>True if the default CLR type was successfully retrieved, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (abiType != "string")
        {
            return false;
        }

        clrType = typeof(string);
        return true;
    }
}
