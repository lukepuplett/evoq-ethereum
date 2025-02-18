using System;
using System.Collections.Generic;
using System.Numerics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes an int type to its ABI binary representation.
/// </summary>
public class IntTypeEncoder : AbiCompatChecker, IAbiEncode
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

        if (!AbiTypes.TryGetBits(abiType, out var bits))
        {
            return false;
        }

        if (value is sbyte sbyteValue)
        {
            if (bits < 8) // value could be 8 bits but abiType is smaller
            {
                return false;
            }

            encoded = EncodeInt(bits, sbyteValue);
            return true;
        }

        if (value is short shortValue)
        {
            if (bits < 16)
            {
                return false;
            }

            encoded = EncodeInt(bits, shortValue);
            return true;
        }

        if (value is Int32 intValue)
        {
            if (bits < 32)
            {
                return false;
            }

            encoded = EncodeInt(bits, intValue);
            return true;
        }

        if (value is Int64 longValue)
        {
            if (bits < 64)
            {
                return false;
            }

            encoded = EncodeInt(bits, longValue);
            return true;
        }

        if (value is BigInteger bigIntegerValue)
        {
            if (bits < 256)
            {
                return false;
            }

            encoded = EncodeInt(bits, bigIntegerValue);
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
        {
            throw new ArgumentException("Bits must be between 8 and 256 and a multiple of 8", nameof(bits));
        }

        var two = new BigInteger(2);
        var minValue = BigInteger.Negate(BigInteger.Pow(two, bits - 1));
        var maxValue = BigInteger.Pow(two, bits - 1) - 1;

        if (value < minValue || value > maxValue)
        {
            throw new ArgumentException($"Value outside range for {bits} bits", nameof(value));
        }

        var result = new byte[32];
        var bytes = value.ToByteArray(isUnsigned: false, isBigEndian: true);
        Buffer.BlockCopy(bytes, 0, result, 32 - bytes.Length, bytes.Length);

        return result;
    }
}