using System;
using System.Collections.Generic;
using System.Numerics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes an int type to its ABI binary representation.
/// </summary>
public class IntTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="IntTypeEncoder"/> class.
    /// </summary>
    public IntTypeEncoder()
        : base(new HashSet<string>
        {
            AbiTypeNames.IntegerTypes.Int,
            AbiTypeNames.IntegerTypes.Int8,
            AbiTypeNames.IntegerTypes.Int16,
            AbiTypeNames.IntegerTypes.Int32,
            AbiTypeNames.IntegerTypes.Int64,
            AbiTypeNames.IntegerTypes.Int128,
            AbiTypeNames.IntegerTypes.Int256
        }, new HashSet<Type>
        {
            typeof(sbyte), typeof(short), typeof(int), typeof(long),
            typeof(BigInteger)
        })
    {
    }

    //

    /// <summary>
    /// Attempts to encode an int type to its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    /// <returns>True if the value was encoded successfully, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the value is null.</exception>
    public bool TryEncode(string abiType, object value, out byte[] encoded, int length = 32)
    {
        if (value == null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        //

        if (!AbiTypes.TryGetBitsSize(abiType, out var maxAbiCapacity))
        {
            return false;
        }

        if (length == 0 || length < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (value is sbyte sbyteValue)
        {
            if (maxAbiCapacity < 8) // .NET value could be 8 bits but ABI type is smaller
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, sbyteValue, length);
            return true;
        }

        if (value is short shortValue)
        {
            if (maxAbiCapacity < 16)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, shortValue, length);
            return true;
        }

        if (value is Int32 intValue)
        {
            if (maxAbiCapacity < 32)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, intValue, length);
            return true;
        }

        if (value is Int64 longValue)
        {
            if (maxAbiCapacity < 64)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, longValue, length);
            return true;
        }

        if (value is BigInteger bigIntegerValue)
        {
            var two = new BigInteger(2);
            var minValue = BigInteger.Negate(BigInteger.Pow(two, maxAbiCapacity - 1));
            var maxValue = BigInteger.Pow(two, maxAbiCapacity - 1) - 1;

            if (bigIntegerValue < minValue || bigIntegerValue > maxValue)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, bigIntegerValue, length);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to decode an int from its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <param name="clrType">The CLR type to decode to.</param>
    /// <param name="decoded">The decoded value if successful.</param>
    /// <returns>True if the value was decoded successfully, false otherwise.</returns>
    public bool TryDecode(string abiType, byte[] data, Type clrType, out object? decoded)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        decoded = null;

        if (!this.IsCompatible(abiType, clrType, out var _))
        {
            return false;
        }

        if (!AbiTypes.TryGetBitsSize(abiType, out var abiBits))
        {
            return false;
        }

        var bigIntValue = DecodeInt(abiBits, data);

        if (clrType == typeof(sbyte))
        {
            if (bigIntValue < sbyte.MinValue || bigIntValue > sbyte.MaxValue)
            {
                return false;
            }
            decoded = (sbyte)bigIntValue;
            return true;
        }

        if (clrType == typeof(short))
        {
            if (bigIntValue < short.MinValue || bigIntValue > short.MaxValue)
            {
                return false;
            }
            decoded = (short)bigIntValue;
            return true;
        }

        if (clrType == typeof(int))
        {
            if (bigIntValue < int.MinValue || bigIntValue > int.MaxValue)
            {
                return false;
            }
            decoded = (int)bigIntValue;
            return true;
        }

        if (clrType == typeof(long))
        {
            if (bigIntValue < long.MinValue || bigIntValue > long.MaxValue)
            {
                return false;
            }
            decoded = (long)bigIntValue;
            return true;
        }

        if (clrType == typeof(BigInteger))
        {
            decoded = bigIntValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Encodes an int as a 32-byte value.
    /// </summary>
    /// <param name="bits">The number of bits to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="length">The length of the byte array to return or -1 for no padding.</param>
    /// <returns>The encoded value as a byte array.</returns>
    public static byte[] EncodeInt(int bits, BigInteger value, int length = 32)
    {
        if (length == 0 || length < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        if (bits < 8 || bits > 256 || bits % 8 != 0)
        {
            throw new ArgumentException("Bits must be between 8 and 256 and a multiple of 8", nameof(bits));
        }

        if (length == -1)
        {
            length = bits / 8;
        }

        var two = new BigInteger(2);
        var minValue = BigInteger.Negate(BigInteger.Pow(two, bits - 1));
        var maxValue = BigInteger.Pow(two, bits - 1) - 1;

        if (value < minValue || value > maxValue)
        {
            throw new ArgumentException($"Value outside range for {bits} bits", nameof(value));
        }

        var result = new byte[length];
        var bytes = value.ToByteArray(isUnsigned: false, isBigEndian: true);
        int copyLength = Math.Min(bytes.Length, length);
        int startIndex = length - copyLength;

        Buffer.BlockCopy(bytes, 0, result, startIndex, copyLength);

        // Sign-extend if negative and bytes don't fill the array
        if (value < 0 && startIndex > 0)
        {
            Array.Fill(result, (byte)0xFF, 0, startIndex);
        }

        return result;
    }

    /// <summary>
    /// Decodes an int from its ABI binary representation.
    /// </summary>
    /// <param name="bits">The number of bits to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded value.</returns>
    public static BigInteger DecodeInt(int bits, byte[] data)
    {
        if (bits < 8 || bits > 256 || bits % 8 != 0)
        {
            throw new ArgumentException("Bits must be between 8 and 256 and a multiple of 8", nameof(bits));
        }

        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length != 32)
        {
            throw new ArgumentException("Data must be exactly 32 bytes for ABI encoding", nameof(data));
        }

        // Convert the 32-byte array to a signed, big-endian BigInteger
        BigInteger value = new BigInteger(data, isUnsigned: false, isBigEndian: true);

        // Define the valid range for the specified bit size
        var two = new BigInteger(2);
        var minValue = BigInteger.Negate(BigInteger.Pow(two, bits - 1));
        var maxValue = BigInteger.Pow(two, bits - 1) - 1;

        // Check if the value fits within the specified bit size
        if (value < minValue || value > maxValue)
        {
            throw new ArgumentException($"Decoded value {value} is outside the range for {bits} bits", nameof(data));
        }

        return value;
    }

    /// <summary>
    /// Attempts to get the default CLR type for an int type.
    /// </summary>
    /// <param name="abiType">The ABI type to get the default CLR type for.</param>
    /// <param name="clrType">The default CLR type if successful.</param>
    /// <returns>True if the default CLR type was successfully retrieved, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (!AbiTypes.TryGetBitsSize(abiType, out var bits))
        {
            return false;
        }

        if (bits == 8)
        {
            clrType = typeof(sbyte);
            return true;
        }

        if (bits == 16)
        {
            clrType = typeof(short);
            return true;
        }

        if (bits == 32)
        {
            clrType = typeof(int);
            return true;
        }

        if (bits == 64)
        {
            clrType = typeof(long);
            return true;
        }

        if (bits == 128)
        {
            clrType = typeof(BigInteger);
            return true;
        }

        if (bits == 256)
        {
            clrType = typeof(BigInteger);
            return true;
        }

        return false;
    }
}