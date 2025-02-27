using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes a uint type to its ABI binary representation.
/// </summary>
public class UintTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
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

    //

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

        if (!AbiTypes.TryGetBitsSize(abiType, out var maxAbiCapacity))
        {
            return false;
        }

        // check value fits within the target ABI type's bit width

        if (value is byte byteValue)
        {
            if (8 > maxAbiCapacity) // value could be 8 bits but abiType is smaller
            {
                return false;
            }

            encoded = EncodeUint(maxAbiCapacity, byteValue);
            return true;
        }

        if (value is ushort ushortValue)
        {
            if (16 > maxAbiCapacity)
            {
                return false;
            }

            encoded = EncodeUint(maxAbiCapacity, ushortValue);
            return true;
        }

        if (value is uint uintValue)
        {
            if (32 > maxAbiCapacity)
            {
                return false;
            }

            encoded = EncodeUint(maxAbiCapacity, uintValue);
            return true;
        }

        if (value is ulong ulongValue)
        {
            if (64 > maxAbiCapacity)
            {
                return false;
            }

            encoded = EncodeUint(maxAbiCapacity, ulongValue);
            return true;
        }

        if (value is BigInteger bigIntegerValue)
        {
            if (bigIntegerValue < 0)
            {
                return false;
            }

            // Check if the value fits within the specified bit size
            var two = new BigInteger(2);
            var maxValue = BigInteger.Pow(two, maxAbiCapacity) - 1;

            if (bigIntegerValue > maxValue)
            {
                return false;
            }

            encoded = EncodeUint(maxAbiCapacity, bigIntegerValue);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to decode a uint from its ABI binary representation.
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

        var bigIntValue = DecodeUint(abiBits, data);

        if (clrType == typeof(byte))
        {
            if (bigIntValue > byte.MaxValue)
            {
                return false;
            }
            decoded = (byte)bigIntValue;
            return true;
        }

        if (clrType == typeof(ushort))
        {
            if (bigIntValue > ushort.MaxValue)
            {
                return false;
            }
            decoded = (ushort)bigIntValue;
            return true;
        }

        if (clrType == typeof(uint))
        {
            if (bigIntValue > uint.MaxValue)
            {
                return false;
            }
            decoded = (uint)bigIntValue;
            return true;
        }

        if (clrType == typeof(ulong))
        {
            if (bigIntValue > ulong.MaxValue)
            {
                return false;
            }
            decoded = (ulong)bigIntValue;
            return true;
        }

        if (clrType == typeof(BigInteger))
        {
            decoded = bigIntValue;
            return true;
        }

        return false;
    }

    //

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

    /// <summary>
    /// Decodes a uint from its ABI binary representation.
    /// </summary>
    /// <param name="bits">The number of bits to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded value.</returns>
    public static BigInteger DecodeUint(int bits, byte[] data)
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

        BigInteger value = new BigInteger(data, isUnsigned: true, isBigEndian: true);

        var two = new BigInteger(2);
        var maxValue = BigInteger.Pow(two, bits) - 1;

        if (value < 0 || value > maxValue)
        {
            throw new ArgumentException($"Decoded value {value} is outside the range for {bits} bits", nameof(data));
        }

        return value;
    }

    //

    /// <summary>
    /// Attempts to get the default CLR type for a uint type.
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
            clrType = typeof(byte);
            return true;
        }

        if (bits == 16)
        {
            clrType = typeof(ushort);
            return true;
        }

        if (bits == 32)
        {
            clrType = typeof(uint);
            return true;
        }

        if (bits == 64)
        {
            clrType = typeof(ulong);
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