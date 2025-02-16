using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a uint type to its ABI binary representation.
/// </summary>
public class UintTypeEncoder : AbiTypeChecker, IAbiTypeEncoder
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UintTypeEncoder"/> class.
    /// </summary>
    public UintTypeEncoder()
        : base(new HashSet<string>
        {
            AbiTypeNames.IntegerTypes.Uint8,
            AbiTypeNames.IntegerTypes.Uint16,
            AbiTypeNames.IntegerTypes.Uint32,
            AbiTypeNames.IntegerTypes.Uint64,
            AbiTypeNames.IntegerTypes.Uint128,
            AbiTypeNames.IntegerTypes.Uint256,
        }, new HashSet<Type>
        {
            typeof(byte), typeof(ushort), typeof(uint), typeof(ulong),
            typeof(BigInteger)
        })
    {
    }

    /// <summary>
    /// Attempts to encode a uint type to its ABI binary representation.
    /// </summary>
    /// <param name="abiType">The ABI type to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoded">The encoded bytes if successful.</param>
    public bool TryEncode(string abiType, object value, out byte[] encoded)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType()))
        {
            return false;
        }

        //

        if (!AbiTypes.TryGetBits(abiType, out var bits))
        {
            return false;
        }

        // Check value fits within the target ABI type's bit width
        if (value is byte byteValue)
        {
            if (bits < 8) // value could be 8 bits but abiType is smaller
            {
                return false;
            }

            encoded = EncodeUint(bits, byteValue);
            return true;
        }

        if (value is ushort ushortValue)
        {
            if (bits < 16)
            {
                return false;
            }

            encoded = EncodeUint(bits, ushortValue);
            return true;
        }

        if (value is uint uintValue)
        {
            if (bits < 32)
            {
                return false;
            }

            encoded = EncodeUint(bits, uintValue);
            return true;
        }

        if (value is ulong ulongValue)
        {
            if (bits < 64)
            {
                return false;
            }

            encoded = EncodeUint(bits, ulongValue);
            return true;
        }

        if (value is BigInteger bigIntegerValue)
        {
            if (bigIntegerValue < 0)
            {
                return false;
            }

            if (bits < 256)
            {
                return false;
            }

            encoded = EncodeUint(bits, bigIntegerValue);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Encodes a uint as a 32-byte value.
    /// </summary>
    /// <param name="bits">The number of bits to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeUint(int bits, BigInteger value)
    {
        if (bits < 8 || bits > 256 || bits % 8 != 0)
        {
            throw new ArgumentException("Bits must be between 8 and 256 and a multiple of 8", nameof(bits));
        }

        if (value < 0)
        {
            throw new ArgumentException("Value cannot be negative", nameof(value));
        }

        var two = new BigInteger(2);
        var maxValue = BigInteger.Pow(two, bits) - 1;

        if (value > maxValue)
        {
            throw new ArgumentException($"Value too large for {bits} bits", nameof(value));
        }

        var result = new byte[32];
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        Buffer.BlockCopy(bytes, 0, result, 32 - bytes.Length, bytes.Length);

        return result;
    }
}