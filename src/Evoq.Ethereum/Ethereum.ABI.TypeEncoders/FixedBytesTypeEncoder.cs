using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a bytes type to its ABI binary representation.
/// </summary>
public class FixedBytesTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FixedBytesTypeEncoder"/> class.
    /// </summary>
    public FixedBytesTypeEncoder()
        : base(new HashSet<string>
        {
            AbiTypeNames.FixedByteArrays.Bytes1,
            AbiTypeNames.FixedByteArrays.Bytes2,
            AbiTypeNames.FixedByteArrays.Bytes3,
            AbiTypeNames.FixedByteArrays.Bytes4,
            AbiTypeNames.FixedByteArrays.Bytes5,
            AbiTypeNames.FixedByteArrays.Bytes6,
            AbiTypeNames.FixedByteArrays.Bytes7,
            AbiTypeNames.FixedByteArrays.Bytes8,
            AbiTypeNames.FixedByteArrays.Bytes9,
            AbiTypeNames.FixedByteArrays.Bytes10,
            AbiTypeNames.FixedByteArrays.Bytes11,
            AbiTypeNames.FixedByteArrays.Bytes12,
            AbiTypeNames.FixedByteArrays.Bytes13,
            AbiTypeNames.FixedByteArrays.Bytes14,
            AbiTypeNames.FixedByteArrays.Bytes15,
            AbiTypeNames.FixedByteArrays.Bytes16,
            AbiTypeNames.FixedByteArrays.Bytes17,
            AbiTypeNames.FixedByteArrays.Bytes18,
            AbiTypeNames.FixedByteArrays.Bytes19,
            AbiTypeNames.FixedByteArrays.Bytes20,
            AbiTypeNames.FixedByteArrays.Bytes21,
            AbiTypeNames.FixedByteArrays.Bytes22,
            AbiTypeNames.FixedByteArrays.Bytes23,
            AbiTypeNames.FixedByteArrays.Bytes24,
            AbiTypeNames.FixedByteArrays.Bytes25,
            AbiTypeNames.FixedByteArrays.Bytes26,
            AbiTypeNames.FixedByteArrays.Bytes27,
            AbiTypeNames.FixedByteArrays.Bytes28,
            AbiTypeNames.FixedByteArrays.Bytes29,
            AbiTypeNames.FixedByteArrays.Bytes30,
            AbiTypeNames.FixedByteArrays.Bytes31,
            AbiTypeNames.FixedByteArrays.Bytes32
        }, new HashSet<Type> { typeof(byte[]), typeof(byte), typeof(sbyte), typeof(char), typeof(string) })
    {
    }

    //

    /// <summary>
    /// Attempts to encode a bytes type to its ABI binary representation.
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

        if (!AbiTypes.TryGetMaxBytesSize(abiType, out var maxBytesSize))
        {
            if (value is byte[] bytes)
            {
                if (bytes.Length > maxBytesSize)
                {
                    return false;
                }

                encoded = EncodeBytes(bytes);
                return true;
            }

            throw new InvalidOperationException($"Invalid value type: {value.GetType()}");
        }

        // check value fits within the target ABI type's bit width

        if (value is byte byteValue)
        {
            if (1 > maxBytesSize) // byte is always 1 byte
            {
                return false;
            }

            encoded = EncodeBytes(new byte[] { byteValue });
            return true;
        }

        if (value is sbyte sbyteValue)
        {
            if (1 > maxBytesSize) // sbyte is always 1 byte
            {
                return false;
            }

            encoded = EncodeBytes(new byte[] { (byte)sbyteValue });
            return true;
        }

        if (value is char charValue)
        {
            if (2 > maxBytesSize) // char is always 2 bytes
            {
                return false;
            }

            encoded = EncodeBytes(new byte[] { (byte)charValue });
            return true;
        }

        if (value is string stringValue)
        {
            var bytes = Encoding.UTF8.GetBytes(stringValue);

            if (bytes.Length > maxBytesSize)
            {
                return false;
            }

            encoded = EncodeBytes(bytes);
            return true;
        }

        //

        return false;
    }

    /// <summary>
    /// Attempts to decode a bytes type from its ABI binary representation. 
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

        if (clrType != typeof(byte[]))
        {
            return false;
        }

        if (!AbiTypes.TryGetMaxBytesSize(abiType, out var maxBytesSize))
        {
            return false;
        }

        //

        var bytes = DecodeBytes(data, maxBytesSize);

        if (clrType == typeof(byte[]))
        {
            // all ABI types are fixed sizes and will fit into a byte array, so we can just decode the bytes

            decoded = bytes;
            return true;
        }

        if (clrType == typeof(byte))
        {
            if (maxBytesSize > 1) // risk of overflow
            {
                return false;
            }

            decoded = bytes[0];
            return true;
        }

        if (clrType == typeof(sbyte))
        {
            if (maxBytesSize > 1) // risk of overflow
            {
                return false;
            }

            decoded = (sbyte)bytes[0];
            return true;
        }

        if (clrType == typeof(char))
        {
            if (maxBytesSize > 2) // risk of overflow
            {
                return false;
            }

            decoded = (char)bytes[0];
            return true;
        }

        if (clrType == typeof(string))
        {
            if (maxBytesSize > bytes.Length)
            {
                return false;
            }

            decoded = Encoding.UTF8.GetString(bytes).Trim('\0');
            return true;
        }

        return false;
    }

    //

    /// <summary>
    /// Encodes a bytes array as a 32-byte value.
    /// </summary>
    /// <remarks>
    /// Bytes are encoded with right-padding to 32 bytes.
    /// </remarks>
    /// <param name="bytes">The bytes to encode.</param>
    /// <returns>The encoded bytes, padded to 32 bytes.</returns>
    public static byte[] EncodeBytes(byte[] bytes)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        var result = new byte[32];
        Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);

        Debug.Assert(result.Length == 32);

        return result;
    }

    /// <summary>
    /// Decodes a bytes array from its ABI binary representation.
    /// </summary>
    /// <param name="data">The data to decode.</param>
    /// <param name="length">The length of the bytes to decode.</param>
    /// <returns>The decoded bytes.</returns>
    public static byte[] DecodeBytes(byte[] data, int length)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (length < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var result = new byte[length];
        Buffer.BlockCopy(data, 0, result, 0, length);

        return result;
    }
}