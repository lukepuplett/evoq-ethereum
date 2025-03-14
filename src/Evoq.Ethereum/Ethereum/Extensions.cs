using System.Numerics;
using Evoq.Blockchain;

namespace Evoq.Ethereum;

/// <summary>
/// Extension methods for the Ethereum namespace.
/// </summary>
public static class Extensions
{
    /// <summary>
    /// Converts a <see cref="Hex"/> to a <see cref="BigInteger"/>.
    /// </summary>
    /// <param name="hex">The hex to convert.</param>
    /// <returns>The big integer.</returns>
    public static BigInteger ToBigInteger(this Hex hex)
    {
        return hex.ToBigInteger(HexSignedness.Unsigned, HexEndianness.BigEndian);
    }

    /// <summary>
    /// Converts a <see cref="ulong"/> to a big endian hex string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The hex string.</returns>
    public static Hex NumberToHexStruct(this ulong value)
    {
        var bigInt = (BigInteger)value;

        return Hex.FromBigInteger(bigInt, HexEndianness.BigEndian);
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to a big-endian hex string.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="trimLeadingZeroDigits">Whether to trim leading zero digits from the hex string.</param>
    /// <returns>The hex string.</returns>
    public static string ToHexString(this BigInteger value, bool trimLeadingZeroDigits = false)
    {
        // For the uint256 max value and other large positive numbers with the high bit set,
        // Hex.FromBigInteger may add a leading zero byte to preserve the sign.
        // We need to trim this leading zero byte for Ethereum compatibility.
        var hex = Hex.FromBigInteger(value, HexEndianness.BigEndian);

        // If the first byte is 0 and there are more bytes, and the second byte has its high bit set (>= 0x80),
        // then this is a case where a leading zero was added to preserve the sign of a positive number.
        // For Ethereum compatibility, we should remove this leading zero.
        byte[] bytes = hex.ToByteArray();
        if (bytes.Length > 1 && bytes[0] == 0 && (bytes[1] & 0x80) != 0)
        {
            // Create a new Hex without the leading zero byte
            hex = new Hex(bytes[1..]);
        }

        return hex.ToString(trimLeadingZeroDigits);
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to a hex string for JSON RPC.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The hex string.</returns>
    public static string ToHexStringForJsonRpc(this BigInteger? value)
    {
        if (!value.HasValue)
        {
            return "0x0";
        }

        // Use the same logic as ToHexString to handle leading zero bytes
        return value.Value.ToHexString(true);
    }

    /// <summary>
    /// Tries to parse a hex string.
    /// </summary>
    /// <param name="hexString">The hex string to parse.</param>
    /// <param name="options">The options to use for parsing.</param>
    /// <param name="hex">The parsed hex value.</param>
    /// <returns>True if the hex string was parsed successfully, false otherwise.</returns>
    public static bool TryParseHex(this string hexString, HexParseOptions options, out Hex? hex)
    {
        try
        {
            hex = Hex.Parse(hexString, options);
            return true;
        }
        catch
        {
            hex = null;
            return false;
        }
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to an <see cref="EthereumAmount"/> in Wei.
    /// </summary>
    /// <param name="wei">The amount in Wei as a BigInteger.</param>
    /// <returns>An EthereumAmount representing the specified amount of Wei.</returns>
    /// <remarks>
    /// Wei is the smallest denomination of Ether (1 Ether = 10^18 Wei).
    /// This method interprets the BigInteger value directly as Wei without any conversion.
    /// </remarks>
    public static EthereumAmount ToWeiAmount(this BigInteger wei)
    {
        return new EthereumAmount(wei, EthereumUnit.Wei);
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to an <see cref="EthereumAmount"/> in Ether.
    /// </summary>
    /// <param name="ether">The amount in Ether as a BigInteger.</param>
    /// <returns>An EthereumAmount representing the specified amount of Ether.</returns>
    /// <remarks>
    /// Ether is the main unit of the Ethereum cryptocurrency.
    /// This method interprets the BigInteger value directly as Ether without any conversion.
    /// Note that since BigInteger is an integer type, this represents whole Ether units.
    /// For fractional Ether amounts, consider using the FromEther method with a decimal value.
    /// </remarks>
    public static EthereumAmount ToEtherAmount(this BigInteger ether)
    {
        return new EthereumAmount(ether, EthereumUnit.Ether);
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to an <see cref="EthereumAmount"/> in the specified unit.
    /// </summary>
    /// <param name="amount">The amount as a BigInteger.</param>
    /// <param name="unit">The Ethereum unit that the amount represents (Wei, Gwei, or Ether).</param>
    /// <returns>An EthereumAmount representing the specified amount in the given unit.</returns>
    /// <remarks>
    /// This method interprets the BigInteger value directly as the specified unit without any conversion.
    /// For example, if amount=5 and unit=EthereumUnit.Ether, this returns an EthereumAmount representing 5 Ether.
    /// </remarks>
    public static EthereumAmount ToEthereumAmount(this BigInteger amount, EthereumUnit unit)
    {
        return new EthereumAmount(amount, unit);
    }
}
