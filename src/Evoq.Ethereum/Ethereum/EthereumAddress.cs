using System;
using System.Linq;
using Evoq.Blockchain;
using Nethereum.Signer;

namespace Evoq.Ethereum;

/// <summary>
/// The checksum mode for Ethereum addresses.
/// </summary>
public enum EthereumAddressChecksum
{
    /// <summary>
    /// Do not check the checksum.
    /// </summary>
    DoNotCheck,
    /// <summary>
    /// Always check the checksum.
    /// </summary>
    AlwaysCheck,
    /// <summary>
    /// Detect and check the checksum.
    /// </summary>
    DetectAndCheck
}

/// <summary>
/// Represents an Ethereum address for a EOA or smart contract.
/// </summary>
public readonly struct EthereumAddress : IEquatable<EthereumAddress>, IByteArray
{
    /// <summary>
    /// The zero address.
    /// </summary>
    /// <remarks>
    /// The zero address is special in Ethereum and is not the same as an empty address
    /// which is not permitted.
    /// </remarks>
    public static readonly EthereumAddress Zero = new(new byte[20]);

    /// <summary>
    /// Represents an empty or uninitialized address.
    /// </summary>
    /// <remarks>
    /// This is different from the Zero address which has semantic meaning in Ethereum.
    /// Empty addresses are used to represent null or uninitialized states.
    /// </remarks>
    public static readonly EthereumAddress Empty = default;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="EthereumAddress"/> struct.
    /// </summary>
    /// <param name="address">The address to initialize with.</param>
    /// <exception cref="ArgumentNullException">Thrown if the address is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the address is empty or not 20 bytes.</exception>
    public EthereumAddress(Hex address)
    {
        if (address.Length == 0)
        {
            throw new ArgumentException("An empty address is not permitted. Use Empty or Zero instead.", nameof(address));
        }

        if (address.Length != 20)
        {
            throw new ArgumentException("Ethereum addresses must be 20 bytes.", nameof(address));
        }

        this.Address = address;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EthereumAddress"/> struct.
    /// </summary>
    /// <param name="address">The address to initialize with.</param>
    /// <exception cref="ArgumentNullException">Thrown if the address is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the address is empty or not 20 bytes.</exception>
    public EthereumAddress(byte[] address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (address.Length == 0)
        {
            throw new ArgumentException("An empty address is not permitted. Use Empty or Zero instead.", nameof(address));
        }

        bool isZero = address.Length == 1 && address[0] == 0;

        // Handle 32-byte padded addresses by taking last 20 bytes
        if (address.Length == 32)
        {
            // Verify first 12 bytes are zero
            if (!address.Take(12).All(b => b == 0))
                throw new ArgumentException("Padded address must start with 12 zero bytes.", nameof(address));

            address = address.Skip(12).Take(20).ToArray();
        }

        if (!isZero && address.Length != 20)
        {
            throw new ArgumentException("Ethereum addresses must be 20 bytes or a single zero byte.", nameof(address));
        }

        this.Address = new Hex(address);
    }

    //

    /// <summary>
    /// The underlying address.
    /// </summary>
    public Hex Address { get; } = Hex.Empty; // in the case of default(EthereumAddress), the Hex will be default(Hex)

    /// <summary>
    /// Whether the address is the zero address; returns false if the address is uninitialized or empty.
    /// </summary>
    public bool IsZero
    {
        get
        {
            if (this.Address == default)
            {
                return false; // default(EthereumAddress) is not zero, it's uninitialized and empty
            }

            return this.Address.IsZeroValue();
        }
    }

    /// <summary>
    /// Whether the address is empty; returns false if the address is uninitialized or zero.
    /// </summary>
    public bool IsEmpty => this.Address == default || this.Address.IsEmpty();

    /// <summary>
    /// Whether the address is valid; returns false if the address is uninitialized or zero.
    /// </summary>
    public bool HasValue => !this.IsEmpty && !this.IsZero;

    //

    /// <summary>
    /// Verifies if this address signed the given message.
    /// </summary>
    /// <param name="message">The message that was signed</param>
    /// <param name="signature">The signature to verify</param>
    /// <returns>True if this address signed the message, false otherwise</returns>
    public bool HasSigned(string message, string signature)
    {
        return VerifySignature(message, signature, this.ToString());
    }

    //

    /// <summary>
    /// Verifies if the given address signed the given message.
    /// </summary>
    /// <param name="message">The message that was signed</param>
    /// <param name="signature">The signature to verify</param>
    /// <param name="expectedAddress">The expected signer address in 0x format</param>
    /// <returns>True if the address signed the message, false otherwise</returns>
    public static bool VerifySignature(string message, string signature, string expectedAddress)
    {
        try
        {
            var signer = new EthereumMessageSigner();
            var recoveredAddress = signer.EncodeUTF8AndEcRecover(message, signature);
            return string.Equals(recoveredAddress, expectedAddress, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception)
        {
            // If signature is invalid or malformed, return false
            return false;
        }
    }

    /// <summary>
    /// Tries to parse a string as an Ethereum address.
    /// </summary>
    /// <param name="address">The address to parse.</param>
    /// <param name="result">The parsed Ethereum address.</param>
    /// <param name="checksum">Whether to validate the checksum</param>
    /// <returns>True if the address was parsed successfully, otherwise false.</returns>
    public static bool TryParse(string address, EthereumAddressChecksum checksum, out EthereumAddress result)
    {
        try
        {
            result = Parse(address, checksum);
            return true;
        }
        catch
        {
            result = Zero;
            return false;
        }
    }

    /// <summary>
    /// Parses a string as an Ethereum address.
    /// </summary>
    /// <param name="hex">The address to parse.</param>
    /// <param name="checksum">Whether to validate the checksum</param>
    /// <returns>The parsed Ethereum address.</returns>
    public static EthereumAddress Parse(
        string hex, EthereumAddressChecksum checksum = EthereumAddressChecksum.DetectAndCheck)
    {
        hex = hex.Trim();

        if (string.IsNullOrEmpty(hex))
        {
            throw new FormatException("Address cannot be empty.");
        }

        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
        {
            hex = hex[2..];
        }

        // Handle both standard (40 chars) and padded (64 chars) addresses
        if (hex.Length == 64)
        {
            // For padded addresses (32 bytes), verify the first 24 chars are zeros
            var prefix = hex[..24];
            if (!prefix.All(c => c == '0'))
                throw new FormatException("Padded address must start with 24 zeros.");

            // Extract the actual address from the padded format
            hex = hex[24..];
        }
        else if (hex.Length != 40)
        {
            throw new FormatException($"The Ethereum address must be 40 characters (got {hex.Length}).");
        }

        if (checksum == EthereumAddressChecksum.AlwaysCheck || isFormatted(hex))
        {
            var u = new Nethereum.Util.AddressUtil();
            if (!u.IsChecksumAddress(hex))
            {
                // string h = hex.Substring(0, 10)
                string h = hex;

                throw new FormatException(
                    $"The Ethereum address '{h}...' is not a checksum address.");
            }
        }

        // Use Hex.Parse for the final conversion
        return new EthereumAddress(Hex.Parse("0x" + hex));

        //

        static bool isFormatted(string hex)
        {
            // detect if it's a checksum address by looking for a mix of uppercase and lowercase letters
            var alphas = hex.Where(c => char.IsLetter(c)).ToArray();
            return alphas.Length > 0 && alphas.Any(c => char.IsUpper(c)) && alphas.Any(c => char.IsLower(c));
        }
    }

    //

    /// <summary>
    /// Returns the Ethereum address in EIP-55 checksum format. 
    /// </summary>
    public override string ToString()
    {
        return this.ToString(shortZero: false);
    }

    /// <summary>
    /// Returns the Ethereum address in EIP-55 checksum format.
    /// </summary>
    /// <param name="shortZero">Whether to return a short zero address when the address is zero</param>
    public string ToString(bool shortZero = false)
    {
        if (this.IsEmpty)
        {
            return "0x";
        }
        else if (this.Address.IsZeroValue())
        {
            if (shortZero)
            {
                return "0x0";
            }

            return "0x0000000000000000000000000000000000000000";
        }

        var bytes = this.Address.ToByteArray();
        var hex = BitConverter.ToString(bytes).Replace("-", "").ToLower();

        var addressUtil = new Nethereum.Util.AddressUtil();

        return addressUtil.ConvertToChecksumAddress("0x" + hex);
    }

    /// <summary>
    /// Returns the Ethereum address as a padded hex string with the specified total length.
    /// Common lengths are 40 (20 bytes, standard) and 64 (32 bytes, EVM storage).
    /// </summary>
    /// <param name="totalLength">The desired length of the hex string (excluding 0x prefix)</param>
    /// <returns>A padded hex string of the specified length (plus 2 for '0x' prefix)</returns>
    /// <exception cref="ArgumentException">Thrown when totalLength is less than the address length (40)</exception>
    public string ToPadded(int totalLength)
    {
        if (totalLength < 40)
        {
            throw new ArgumentException($"Total length must be at least 40 characters, got {totalLength}", nameof(totalLength));
        }

        if (this.IsEmpty)
        {
            throw new InvalidOperationException("Cannot convert an empty address to a padded string.");
        }

        if (this.IsZero)
        {
            return "0x" + new string('0', totalLength);
        }

        int paddingLength = totalLength - 40; // 40 is the standard address length
        return "0x" + new string('0', paddingLength) + ToString()[2..];
    }

    //

    /// <summary>
    /// Compares the current Ethereum address with another for equality.
    /// </summary>
    /// <param name="other">The Ethereum address to compare with.</param>
    /// <returns>True if the addresses are equal, otherwise false.</returns>
    public bool Equals(EthereumAddress other)
    {
        return this.Address.Equals(other.Address);
    }

    /// <summary>
    /// Compares the current Ethereum address with another for equality.
    /// </summary>
    /// <param name="obj">The object to compare with.</param>
    /// <returns>True if the addresses are equal, otherwise false.</returns>
    public override bool Equals(object? obj)
    {
        return obj is EthereumAddress address && this.Equals(address);
    }

    /// <summary>
    /// Returns the hash code for the current Ethereum address.
    /// </summary>
    /// <returns>The hash code for the current Ethereum address.</returns>
    public override int GetHashCode()
    {
        return this.Address.GetHashCode();
    }

    //

    /// <summary>
    /// Converts the Ethereum address to a byte array.
    /// </summary>
    /// <returns>The byte array.</returns>
    public byte[] ToByteArray()
    {
        return this.Address.ToByteArray();
    }

    //

    /// <summary>
    /// Compares two Ethereum addresses for equality.
    /// </summary>
    /// <param name="left">The left address to compare.</param>
    /// <param name="right">The right address to compare.</param>
    /// <returns>True if the addresses are equal, otherwise false.</returns>
    public static bool operator ==(EthereumAddress left, EthereumAddress right) => left.Equals(right);

    /// <summary>
    /// Compares two Ethereum addresses for inequality.
    /// </summary>
    /// <param name="left">The left address to compare.</param>
    /// <param name="right">The right address to compare.</param>
    /// <returns>True if the addresses are not equal, otherwise false.</returns>
    public static bool operator !=(EthereumAddress left, EthereumAddress right) => !left.Equals(right);

    /// <summary>
    /// Provides explicit string to EthereumAddress conversion.
    /// </summary>
    /// <param name="address">The string address to convert.</param>
    public static explicit operator EthereumAddress(string address) => Parse(address);

    /// <summary>
    /// Provides explicit Hex to EthereumAddress conversion.
    /// </summary>
    /// <param name="address">The Hex address to convert.</param>
    public static explicit operator EthereumAddress(Hex address) => new(address.ToByteArray());


}
