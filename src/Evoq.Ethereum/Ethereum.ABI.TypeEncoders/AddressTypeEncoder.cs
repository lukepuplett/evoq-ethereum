using System;
using System.Collections.Generic;
using System.Diagnostics;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes an address type to its ABI binary representation.
/// </summary>
public class AddressTypeEncoder : AbiCompatChecker, IAbiEncode
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddressTypeEncoder"/> class.
    /// </summary>
    public AddressTypeEncoder()
        : base(
            new HashSet<string> { AbiTypeNames.Address },
            new HashSet<Type> {
                typeof(string),
                typeof(byte[]),
                typeof(EthereumAddress),
                typeof(Hex)
            })
    {
    }

    /// <summary>
    /// Attempts to encode an address type to its ABI binary representation.
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

        if (value is EthereumAddress address)
        {
            encoded = EncodeAddress(address);
            return true;
        }

        if (value is string addr)
        {
            try
            {
                Hex h = Hex.Parse(addr);
                encoded = EncodeAddress(new EthereumAddress(h));
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (value is Hex hex)
        {
            encoded = EncodeAddress(new EthereumAddress(hex));
            return true;
        }

        if (value is byte[] bytes)
        {
            try
            {
                encoded = EncodeAddress(new EthereumAddress(bytes));
                return true;
            }
            catch
            {
                return false;
            }

        }

        return false;
    }

    //

    /// <summary>
    /// Encodes an address as a 32-byte value.
    /// </summary>
    /// <param name="address">The address to encode.</param>
    /// <returns>The encoded address, padded to 32 bytes.</returns>
    public static byte[] EncodeAddress(EthereumAddress address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        var result = new byte[32];
        var addressBytes = address.ToByteArray();

        if (addressBytes.Length != 20)
            throw new ArgumentException("Address must be 20 bytes", nameof(address));

        Buffer.BlockCopy(addressBytes, 0, result, 12, 20);

        Debug.Assert(result.Length == 32);

        return result;
    }
}