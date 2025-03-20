using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a boolean type to its ABI binary representation.
/// </summary>
internal class BoolTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoolTypeEncoder"/> class.
    /// </summary>
    public BoolTypeEncoder()
        : base(new HashSet<string> { AbiTypeNames.Bool }, new HashSet<Type> { typeof(bool) })
    {
    }

    //

    /// <summary>
    /// Attempts to encode a boolean type to its ABI binary representation.
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

        //

        if (value is bool boolValue)
        {
            encoded = EncodeBool(boolValue, length);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to decode a boolean from its ABI binary representation.
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

        if (clrType != typeof(bool))
        {
            return false;
        }

        //

        decoded = DecodeBool(data);

        return true;
    }

    //

    /// <summary>
    /// Encodes a boolean as a 32-byte value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeBool(bool value, int length = 32)
    {
        if (length == -1)
        {
            length = 1;
        }

        if (length == 0 || length < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var result = new byte[length];

        if (value)
        {
            result[length - 1] = 1;
        }

        return result;
    }

    /// <summary>
    /// Decodes a boolean from a 32-byte value.
    /// </summary>
    /// <param name="data">The 32-byte value to decode.</param>
    /// <returns>The decoded boolean.</returns>
    public static bool DecodeBool(byte[] data)
    {
        return data[31] == 1;
    }

    /// <summary>
    /// Attempts to get the default CLR type for a boolean type.
    /// </summary>
    /// <param name="abiType">The ABI type to get the default CLR type for.</param>
    /// <param name="clrType">The default CLR type if successful.</param>
    /// <returns>True if the default CLR type was successfully retrieved, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (abiType != AbiTypeNames.Bool)
        {
            return false;
        }

        clrType = typeof(bool);
        return true;
    }
}