using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a uint type to its ABI binary representation.
/// </summary>
public class UintTypeEncoder : AbiCompatChecker, IAbiEncode
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

        if (!AbiTypes.TryGetMaxBitSize(abiType, out var maxBitsSize))
        {
            return false;
        }

        // check value fits within the target ABI type's bit width

        if (value is byte byteValue)
        {
            if (8 > maxBitsSize) // value could be 8 bits but abiType is smaller
            {
                return false;
            }

            encoded = EncodeUint(maxBitsSize, byteValue);
            return true;
        }

        if (value is ushort ushortValue)
        {
            if (16 > maxBitsSize)
            {
                return false;
            }

            encoded = EncodeUint(maxBitsSize, ushortValue);
            return true;
        }

        if (value is uint uintValue)
        {
            if (32 > maxBitsSize)
            {
                return false;
            }

            encoded = EncodeUint(maxBitsSize, uintValue);
            return true;
        }

        if (value is ulong ulongValue)
        {
            if (64 > maxBitsSize)
            {
                return false;
            }

            encoded = EncodeUint(maxBitsSize, ulongValue);
            return true;
        }

        if (value is BigInteger bigIntegerValue)
        {
            if (bigIntegerValue < 0)
            {
                return false;
            }

            if (256 > maxBitsSize)
            {
                return false;
            }

            encoded = EncodeUint(maxBitsSize, bigIntegerValue);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Encodes a uint as a 32-byte value.
    /// </summary>
    /// <param name="bits">The number of bits to encode, e.g. 8 or 256 etc.</param>
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