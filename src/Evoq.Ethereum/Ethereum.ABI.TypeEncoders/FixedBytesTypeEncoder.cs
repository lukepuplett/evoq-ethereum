using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a bytes type to its ABI binary representation.
/// </summary>
internal class FixedBytesTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
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
        }, new HashSet<Type> { typeof(byte[]), typeof(byte), typeof(sbyte), typeof(char), typeof(string), typeof(Hex) })
    {
    }

    //

    /// <summary>
    /// Attempts to encode a bytes type to its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    public bool TryEncode(string abiType, object value, out byte[] encoded, int length = 32)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        if (!AbiTypes.TryGetBytesSize(abiType, out var maxBytesSize))
        {
            if (value is byte[] bytes)
            {
                if (bytes.Length > maxBytesSize)
                {
                    return false;
                }

                encoded = EncodeBytes(bytes, length);
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

            encoded = EncodeBytes(new byte[] { byteValue }, length);
            return true;
        }

        if (value is sbyte sbyteValue)
        {
            if (1 > maxBytesSize) // sbyte is always 1 byte
            {
                return false;
            }

            encoded = EncodeBytes(new byte[] { (byte)sbyteValue }, length);
            return true;
        }

        if (value is char charValue)
        {
            if (2 > maxBytesSize) // char is always 2 bytes
            {
                return false;
            }

            encoded = EncodeBytes(new byte[] { (byte)charValue }, length);
            return true;
        }

        if (value is string stringValue)
        {
            var bytes = Encoding.UTF8.GetBytes(stringValue);

            if (bytes.Length > maxBytesSize)
            {
                return false;
            }

            encoded = EncodeBytes(bytes, length);
            return true;
        }

        if (value is Hex hexValue)
        {
            if (hexValue.Length > maxBytesSize)
            {
                return false;
            }

            encoded = EncodeBytes(hexValue.ToByteArray(), length);
            return true;
        }

        //

        return false;
    }

    /// <summary>
    /// Attempts to decode a bytes type from its ABI binary representation into the specified CLR type.
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

        if (!AbiTypes.TryGetBytesSize(abiType, out var abiSize))
        {
            return false;
        }

        //

        var decodedBytes = DecodeBytes(data, abiSize);

        if (clrType == typeof(byte[]))
        {
            // all ABI types are fixed sizes and will fit into a byte array, so we can just decode the bytes

            decoded = decodedBytes;
            return true;
        }

        if (clrType == typeof(byte))
        {
            if (abiSize > 1) // risk of overflow
            {
                return false;
            }

            decoded = decodedBytes[0];
            return true;
        }

        if (clrType == typeof(sbyte))
        {
            if (abiSize > 1) // risk of overflow
            {
                return false;
            }

            decoded = (sbyte)decodedBytes[0];
            return true;
        }

        if (clrType == typeof(char))
        {
            if (abiSize > 2) // risk of overflow
            {
                return false;
            }

            decoded = (char)decodedBytes[0];
            return true;
        }

        if (clrType == typeof(string))
        {
            // all ABI types are fixed sizes and will fit into a string, so we can just decode the bytes

            decoded = Encoding.UTF8.GetString(decodedBytes).Trim('\0');
            return true;
        }

        if (clrType == typeof(Hex))
        {
            // all ABI types are fixed sizes and will fit into a Hex, so we can just decode the bytes

            decoded = new Hex(decodedBytes);
            return true;
        }

        return false;
    }

    //

    /// <summary>
    /// Encodes a bytes array to its ABI binary representation.
    /// </summary>
    /// <remarks>
    /// Bytes are encoded with right-padding to the specified width. So a byte16 padded to 32 bytes
    /// will return a 32-byte array with the last 16 bytes being 0. A byte16 padded to 16 bytes will
    /// return a 16-byte array. A byte16 padded to 8 bytes will throw an exception because the bytes
    /// are too large to fit. Using -1 for the padToWidth parameter will return the bytes as is, without
    /// padding, so a byte16 will return a 16-byte array.
    /// </remarks>
    /// <param name="bytes">The bytes to encode.</param>
    /// <param name="length">The length of byte array to return, right-padded with 0s. Use -1 for no padding.</param>
    /// <returns>The encoded bytes, padded to the specified width.</returns>
    public static byte[] EncodeBytes(byte[] bytes, int length = 32)
    {
        if (bytes == null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }

        if (length == -1)
        {
            return bytes;
        }

        var result = new byte[length];
        Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);

        Debug.Assert(result.Length == length);

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

    /// <summary>
    /// Attempts to get the default CLR type for a fixed bytes type.
    /// </summary>
    /// <param name="abiType">The ABI type to get the default CLR type for.</param>
    /// <param name="clrType">The default CLR type if successful.</param>
    /// <returns>True if the default CLR type was successfully retrieved, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (abiType == "byte")
        {
            clrType = typeof(byte);
            return true;
        }

        if (!AbiTypes.TryGetBytesSize(abiType, out var size))
        {
            return false;
        }

        if (size == 1)
        {
            clrType = typeof(byte);
            return true;
        }

        clrType = typeof(byte[]);
        return true;
    }
}