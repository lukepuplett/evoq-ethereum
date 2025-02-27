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
    /// <returns>True if the value was encoded successfully, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the value is null.</exception>
    public bool TryEncode(string abiType, object value, out byte[] encoded)
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

        if (value is sbyte sbyteValue)
        {
            if (maxAbiCapacity < 8) // .NET value could be 8 bits but ABI type is smaller
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, sbyteValue);
            return true;
        }

        if (value is short shortValue)
        {
            if (maxAbiCapacity < 16)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, shortValue);
            return true;
        }

        if (value is Int32 intValue)
        {
            if (maxAbiCapacity < 32)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, intValue);
            return true;
        }

        if (value is Int64 longValue)
        {
            if (maxAbiCapacity < 64)
            {
                return false;
            }

            encoded = EncodeInt(maxAbiCapacity, longValue);
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

            encoded = EncodeInt(maxAbiCapacity, bigIntegerValue);
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
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeInt(int bits, BigInteger value)
    {
        if (bits < 8 || bits > 256 || bits % 8 != 0)
            throw new ArgumentException("Bits must be between 8 and 256 and a multiple of 8", nameof(bits));

        var two = new BigInteger(2);
        var minValue = BigInteger.Negate(BigInteger.Pow(two, bits - 1));
        var maxValue = BigInteger.Pow(two, bits - 1) - 1;

        if (value < minValue || value > maxValue)
            throw new ArgumentException($"Value outside range for {bits} bits", nameof(value));

        var result = new byte[32];
        var bytes = value.ToByteArray(isUnsigned: false, isBigEndian: true);
        int copyLength = Math.Min(bytes.Length, 32);
        int destOffset = 32 - copyLength;
        Buffer.BlockCopy(bytes, 0, result, destOffset, copyLength);

        // Sign-extend if negative and bytes don't fill the array
        if (value < 0 && destOffset > 0)
            Array.Fill(result, (byte)0xFF, 0, destOffset);

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
}