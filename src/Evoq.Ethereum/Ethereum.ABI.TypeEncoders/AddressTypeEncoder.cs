using System;
using System.Collections.Generic;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Encodes an address type to its ABI binary representation.
/// </summary>
internal class AddressTypeEncoder : AbiCompatChecker, IAbiEncode, IAbiDecode
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
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    public bool TryEncode(string abiType, object value, out byte[] encoded, int length = 32)
    {
        encoded = Array.Empty<byte>();

        if (!this.IsCompatible(abiType, value.GetType(), out var _))
        {
            return false;
        }

        //

        if (value is EthereumAddress address)
        {
            encoded = EncodeAddress(address, length);
            return true;
        }

        if (value is string addr)
        {
            try
            {
                Hex h = Hex.Parse(addr);
                encoded = EncodeAddress(new EthereumAddress(h), length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        if (value is Hex hex)
        {
            encoded = EncodeAddress(new EthereumAddress(hex), length);
            return true;
        }

        if (value is byte[] bytes)
        {
            try
            {
                encoded = EncodeAddress(new EthereumAddress(bytes), length);
                return true;
            }
            catch
            {
                return false;
            }

        }

        return false;
    }

    /// <summary>
    /// Attempts to decode an address type from its ABI binary representation.
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

        if (abiType != AbiTypeNames.Address)
        {
            decoded = null;
            return false;
        }

        //

        var address = DecodeAddress(data);

        if (clrType == typeof(EthereumAddress))
        {
            decoded = address;
            return true;
        }

        if (clrType == typeof(string))
        {
            decoded = address.ToString();
            return true;
        }

        if (clrType == typeof(Hex))
        {
            decoded = new Hex(address.ToByteArray());
            return true;
        }

        if (clrType == typeof(byte[]))
        {
            decoded = address.ToByteArray();
            return true;
        }

        return false;
    }

    //

    /// <summary>
    /// Encodes an address as a 32-byte value.
    /// </summary>
    /// <param name="address">The address to encode.</param>
    /// <param name="length">The length of the bytes to encode or -1 for no padding.</param>
    /// <returns>The encoded address, padded to the specified length.</returns>
    public static byte[] EncodeAddress(EthereumAddress address, int length = 32)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        var addressBytes = address.ToByteArray();

        if (length == -1)
        {
            length = addressBytes.Length;
        }

        var result = new byte[length];
        var startIndex = length - addressBytes.Length;

        Buffer.BlockCopy(addressBytes, 0, result, startIndex, addressBytes.Length);

        return result;
    }

    /// <summary>
    /// Decodes an address from a 32-byte value.
    /// </summary>
    /// <param name="data">The 32-byte value to decode.</param>
    /// <returns>The decoded address.</returns>
    public static EthereumAddress DecodeAddress(byte[] data)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length != 32)
        {
            throw new ArgumentException("Address must be 32 bytes", nameof(data));
        }

        var addressBytes = new byte[20];
        Buffer.BlockCopy(data, 12, addressBytes, 0, 20);

        return new EthereumAddress(addressBytes);
    }

    /// <summary>
    /// Attempts to get the default CLR type for an address type.
    /// </summary>
    /// <param name="abiType">The ABI type to get the default CLR type for.</param>
    /// <param name="clrType">The default CLR type if successful.</param>
    /// <returns>True if the default CLR type was successfully retrieved, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (abiType != AbiTypeNames.Address)
        {
            return false;
        }

        clrType = typeof(EthereumAddress);
        return true;
    }
}