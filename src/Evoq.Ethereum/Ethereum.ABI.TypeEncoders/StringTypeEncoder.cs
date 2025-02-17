using System;
using System.Collections.Generic;
using System.Text;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a string type to its ABI binary representation.
/// </summary>
public class StringTypeEncoder : AbiCompatChecker, IAbiEncode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StringTypeEncoder"/> class.
    /// </summary>
    public StringTypeEncoder()
        : base(new HashSet<string> { "string" }, new HashSet<Type> { typeof(string) })
    {
    }

    /// <summary>
    /// Attempts to encode a string type to its ABI binary representation.
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

        if (value is string str)
        {
            encoded = EncodeString(str);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Encodes a string to its ABI binary representation.
    /// </summary>
    /// <param name="str">The string to encode.</param>
    /// <returns>The encoded string.</returns>
    public static byte[] EncodeString(string str)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));

        return Encoding.UTF8.GetBytes(str);
    }
}
