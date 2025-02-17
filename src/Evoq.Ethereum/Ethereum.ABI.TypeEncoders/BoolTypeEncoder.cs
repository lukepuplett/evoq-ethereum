using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a boolean type to its ABI binary representation.
/// </summary>
public class BoolTypeEncoder : AbiCompatChecker, IAbiEncode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BoolTypeEncoder"/> class.
    /// </summary>
    public BoolTypeEncoder()
        : base(new HashSet<string> { AbiTypeNames.Bool }, new HashSet<Type> { typeof(bool) })
    {
    }

    /// <summary>
    /// Attempts to encode a boolean type to its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    public bool TryEncode(string abiType, object value, out byte[] encoded)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        //

        if (value is bool boolValue)
        {
            encoded = EncodeBool(boolValue);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Encodes a boolean as a 32-byte value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeBool(bool value)
    {
        var result = new byte[32];
        if (value)
            result[31] = 1;

        Debug.Assert(result.Length == 32);

        return result;
    }
}