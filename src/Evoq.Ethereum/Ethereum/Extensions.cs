using System;
using Evoq.Blockchain;
using BCMath = Org.BouncyCastle.Math;
using NMath = System.Numerics;

namespace Evoq.Ethereum;

/// <summary>
/// Extension methods for the <see cref="BCMath.BigInteger"/> type.
/// </summary>
public static class BouncyBigIntegerExtensions
{
    /// <summary>
    /// Converts a <see cref="BCMath.BigInteger"/> to a <see cref="Hex"/> value.
    /// </summary>
    /// <remarks>
    /// The BouncyCastle BigInteger class has important characteristics to consider when converting to bytes:
    /// <list type="bullet">
    /// <item>
    /// <description>It represents both positive and negative integers, with sign information included in the default byte representation.</description>
    /// </item>
    /// <item>
    /// <description>When using <c>ToByteArray()</c>, the result includes sign information. For positive numbers whose highest bit is set (≥ 128), an extra leading zero byte is added to avoid misinterpreting the number as negative.</description>
    /// </item>
    /// <item>
    /// <description>When using <c>ToByteArrayUnsigned()</c>, the result excludes the leading zero byte for positive numbers with the high bit set, producing a more compact representation.</description>
    /// </item>
    /// <item>
    /// <description>For negative numbers, both <c>ToByteArray()</c> and <c>ToByteArrayUnsigned()</c> return the two's complement representation, not just the magnitude.</description>
    /// </item>
    /// <item>
    /// <description>Both methods produce byte arrays in big-endian order (most significant byte first), unlike .NET's BigInteger which uses little-endian.</description>
    /// </item>
    /// </list>
    /// </remarks>
    /// <param name="value">The <see cref="BCMath.BigInteger"/> value to convert.</param>
    /// <param name="unsigned">
    /// If true (default), uses <c>ToByteArrayUnsigned()</c> which:
    /// <list type="bullet">
    /// <item><description>For positive numbers: Omits the leading zero byte for numbers with the high bit set</description></item>
    /// <item><description>For negative numbers: Still returns the two's complement representation</description></item>
    /// </list>
    /// If false, uses <c>ToByteArray()</c> which:
    /// <list type="bullet">
    /// <item><description>For positive numbers: Adds a leading zero byte for numbers with the high bit set</description></item>
    /// <item><description>For negative numbers: Returns the two's complement representation</description></item>
    /// </list>
    /// </param>
    /// <returns>A <see cref="Hex"/> value representing the <see cref="BCMath.BigInteger"/>.</returns>
    public static Hex ToHex(this BCMath.BigInteger value, bool unsigned = true) =>
        new Hex(unsigned ? value.ToByteArrayUnsigned() : value.ToByteArray());

    /// <summary>
    /// Converts a BouncyCastle <see cref="BCMath.BigInteger"/> to a .NET <see cref="NMath.BigInteger"/>.
    /// </summary>
    /// <remarks>
    /// This conversion handles the endianness difference between the two implementations:
    /// <list type="bullet">
    /// <item>
    /// <description>BouncyCastle's BigInteger uses big-endian byte ordering (most significant byte first)</description>
    /// </item>
    /// <item>
    /// <description>.NET's BigInteger uses little-endian byte ordering (least significant byte first)</description>
    /// </item>
    /// </list>
    /// The sign of the number is preserved in the conversion. For negative numbers, the method first reverses the bytes
    /// to handle the endianness difference, then applies the negative sign to the resulting .NET BigInteger.
    /// </remarks>
    /// <param name="value">The BouncyCastle <see cref="BCMath.BigInteger"/> to convert.</param>
    /// <returns>A .NET <see cref="NMath.BigInteger"/> with the same value and sign.</returns>
    public static NMath.BigInteger ToBigNumerics(this BCMath.BigInteger value)
    {
        // Get the bytes in big-endian format
        byte[] bytes = value.ToByteArray();

        // Reverse the bytes to convert from big-endian to little-endian
        Array.Reverse(bytes);

        // Create a new .NET BigInteger with the reversed bytes
        // If the original value is negative, we need to set the sign
        if (value.SignValue < 0)
        {
            return -new NMath.BigInteger(bytes);
        }

        return new NMath.BigInteger(bytes);
    }
}

/// <summary>
/// Extension methods for the <see cref="NMath.BigInteger"/> type.
/// </summary>
public static class NMathBigIntegerExtensions
{
    /// <summary>
    /// Converts a .NET <see cref="NMath.BigInteger"/> to a BouncyCastle <see cref="BCMath.BigInteger"/>.
    /// </summary>
    /// <remarks>
    /// This conversion handles the endianness difference between the two implementations:
    /// <list type="bullet">
    /// <item>
    /// <description>.NET's BigInteger uses little-endian byte ordering (least significant byte first)</description>
    /// </item>
    /// <item>
    /// <description>BouncyCastle's BigInteger uses big-endian byte ordering (most significant byte first)</description>
    /// </item>
    /// </list>
    /// The sign of the number is preserved in the conversion. For negative numbers, the method:
    /// <list type="number">
    /// <item><description>Gets the absolute value of the number</description></item>
    /// <item><description>Converts it to a byte array (in little-endian format)</description></item>
    /// <item><description>Reverses the bytes to get big-endian format</description></item>
    /// <item><description>Creates a new BouncyCastle BigInteger with the correct sign</description></item>
    /// </list>
    /// </remarks>
    /// <param name="value">The .NET <see cref="NMath.BigInteger"/> to convert.</param>
    /// <returns>A BouncyCastle <see cref="BCMath.BigInteger"/> with the same value and sign.</returns>
    public static BCMath.BigInteger ToBigBouncy(this NMath.BigInteger value)
    {
        // For negative numbers, get the absolute value first
        bool isNegative = value < 0;
        var absValue = NMath.BigInteger.Abs(value);

        // Get the bytes in little-endian format from the absolute value
        byte[] bytes = absValue.ToByteArray();

        // Reverse the bytes to convert from little-endian to big-endian
        Array.Reverse(bytes);

        // Create a new BouncyCastle BigInteger with the reversed bytes and correct sign
        return new BCMath.BigInteger(isNegative ? -1 : 1, bytes);
    }
}
