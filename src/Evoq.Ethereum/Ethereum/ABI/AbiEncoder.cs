using System;
using System.Numerics;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Provides methods for encoding values according to the Ethereum ABI specification.
/// </summary>
public static class AbiEncoder
{
    /// <summary>
    /// Encodes an address as a 32-byte value.
    /// </summary>
    /// <param name="address">The address to encode.</param>
    /// <returns>The encoded address, padded to 32 bytes.</returns>
    public static byte[] EncodeAddress(EthereumAddress address)
    {
        if (address == null)
            throw new ArgumentNullException(nameof(address));

        var result = new byte[32];
        var addressBytes = address.ToByteArray();
        if (addressBytes.Length != 20)
            throw new ArgumentException("Address must be 20 bytes", nameof(address));
        Buffer.BlockCopy(addressBytes, 0, result, 12, 20);
        return result;
    }

    /// <summary>
    /// Encodes a uint256 as a 32-byte value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeUint256(BigInteger value)
    {
        if (value < 0)
            throw new ArgumentException("Value cannot be negative", nameof(value));
        if (value > BigInteger.Pow(2, 256) - 1)
            throw new ArgumentException("Value too large for uint256", nameof(value));

        var result = new byte[32];
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        Buffer.BlockCopy(bytes, 0, result, 32 - bytes.Length, bytes.Length); // Right-align
        return result;
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
        return result;
    }
}