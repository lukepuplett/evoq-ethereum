using System;
using System.Collections.Generic;
using System.Numerics;
using Evoq.Blockchain;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.Contracts;
using Evoq.Ethereum.JsonRPC;
using Evoq.Ethereum.Transactions;

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
    /// Converts a <see cref="Hex"/> to a <see cref="ulong"/>.
    /// </summary>
    /// <param name="hex">The hex to convert.</param>
    /// <returns>The ulong.</returns>
    public static ulong ToUInt64(this Hex hex)
    {
        return (ulong)hex.ToBigInteger();
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
        return value.Value.ToHexString(trimLeadingZeroDigits: true);
    }

    /// <summary>
    /// Converts a <see cref="Hex"/> to a hex string for JSON RPC.
    /// </summary>
    /// <param name="hex">The hex to convert.</param>
    /// <returns>The hex string.</returns>
    public static string ToHexStringForJsonRpc(this Hex hex) => hex.ToString(trimLeadingZeroDigits: true);

    /// <summary>
    /// Converts an <see cref="EtherAmount"/> to a hex string for JSON RPC.
    /// </summary>
    /// <param name="amount">The amount to convert.</param>
    /// <returns>The hex string.</returns>
    public static string ToHexStringForJsonRpc(this EtherAmount amount) => amount.ToHexStruct().ToString(trimLeadingZeroDigits: true);

    /// <summary>
    /// Converts a <see cref="ulong"/> to a hex string for JSON RPC.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The hex string.</returns>
    public static string ToHexStringForJsonRpc(this ulong value) => value.NumberToHexStruct().ToString(trimLeadingZeroDigits: true);

    /// <summary>
    /// Converts a <see cref="DateTime"/> to a Unix timestamp.
    /// </summary>
    /// <param name="dateTime">The date time to convert.</param>
    /// <returns>The Unix timestamp.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the DateTime is not in UTC or is before Unix epoch.</exception>
    public static ulong ToUnixTimestamp(this DateTime dateTime)
    {
        if (dateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime must be in UTC", nameof(dateTime));
        }

        if (dateTime < new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc))
        {
            throw new ArgumentException("DateTime must not be before Unix epoch (1970-01-01 00:00:00 UTC)");
        }

        return dateTime.ToDateTimeOffset(TimeSpan.Zero).ToUnixTimestamp();
    }

    /// <summary>
    /// Converts a <see cref="DateTimeOffset"/> to a Unix timestamp.
    /// </summary>
    /// <param name="dateTimeOffset">The date time offset to convert.</param>
    /// <returns>The Unix timestamp.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the DateTimeOffset is not in UTC or is before Unix epoch.</exception>
    public static ulong ToUnixTimestamp(this DateTimeOffset dateTimeOffset)
    {
        if (dateTimeOffset.Offset != TimeSpan.Zero)
        {
            throw new ArgumentException("DateTimeOffset must be in UTC", nameof(dateTimeOffset));
        }

        if (dateTimeOffset < new DateTimeOffset(1970, 1, 1, 0, 0, 0, TimeSpan.Zero))
        {
            throw new ArgumentException("DateTimeOffset must not be before Unix epoch (1970-01-01 00:00:00 UTC)", nameof(dateTimeOffset));
        }

        return (ulong)dateTimeOffset.ToUnixTimeSeconds();
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
    /// Converts a <see cref="BigInteger"/> to an <see cref="EtherAmount"/> in Wei.
    /// </summary>
    /// <param name="wei">The amount in Wei as a BigInteger.</param>
    /// <returns>An EthereumAmount representing the specified amount of Wei.</returns>
    /// <remarks>
    /// Wei is the smallest denomination of Ether (1 Ether = 10^18 Wei).
    /// This method interprets the BigInteger value directly as Wei without any conversion.
    /// </remarks>
    public static EtherAmount ToWeiAmount(this BigInteger wei)
    {
        return new EtherAmount(wei, EthereumUnit.Wei);
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to an <see cref="EtherAmount"/> in Ether.
    /// </summary>
    /// <param name="ether">The amount in Ether as a BigInteger.</param>
    /// <returns>An EthereumAmount representing the specified amount of Ether.</returns>
    /// <remarks>
    /// Ether is the main unit of the Ethereum cryptocurrency.
    /// This method interprets the BigInteger value directly as Ether without any conversion.
    /// Note that since BigInteger is an integer type, this represents whole Ether units.
    /// For fractional Ether amounts, consider using the FromEther method with a decimal value.
    /// </remarks>
    public static EtherAmount ToEtherAmount(this BigInteger ether)
    {
        return new EtherAmount(ether, EthereumUnit.Ether);
    }

    /// <summary>
    /// Converts a <see cref="BigInteger"/> to an <see cref="EtherAmount"/> in the specified unit.
    /// </summary>
    /// <param name="amount">The amount as a BigInteger.</param>
    /// <param name="unit">The Ethereum unit that the amount represents (Wei, Gwei, or Ether).</param>
    /// <returns>An EthereumAmount representing the specified amount in the given unit.</returns>
    /// <remarks>
    /// This method interprets the BigInteger value directly as the specified unit without any conversion.
    /// For example, if amount=5 and unit=EthereumUnit.Ether, this returns an EthereumAmount representing 5 Ether.
    /// </remarks>
    public static EtherAmount ToEthereumAmount(this BigInteger amount, EthereumUnit unit)
    {
        return new EtherAmount(amount, unit);
    }

    /// <summary>
    /// Tries to read event logs from a transaction receipt.
    /// </summary>
    /// <param name="receipt">The transaction receipt.</param>
    /// <param name="contract">The contract.</param>
    /// <param name="eventName">The name of the event to read.</param>
    /// <param name="indexed">The indexed parameters of the event.</param>
    /// <param name="data">The data parameters of the event.</param>
    public static bool TryReadEventLogs(
        this TransactionReceipt receipt,
        Contract contract,
        string eventName,
        out IReadOnlyDictionary<string, object?>? indexed,
        out IReadOnlyDictionary<string, object?>? data)
    {
        return contract.TryReadEventLogsFromReceipt(receipt, eventName, out indexed, out data);
    }

    /// <summary>
    /// Creates a chain from an endpoint.
    /// </summary>
    /// <param name="endpoint">The endpoint.</param>
    /// <returns>The chain.</returns>
    public static Chain CreateChain(this Endpoint endpoint)
    {
        var chainId = ulong.Parse(ChainNames.GetChainId(endpoint.NetworkName));

        return Chain.CreateDefault(chainId, new Uri(endpoint.URL), endpoint.LoggerFactory);
    }
}
