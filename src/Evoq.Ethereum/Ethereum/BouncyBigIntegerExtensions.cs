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
    public static Hex ToHexStruct(this BCMath.BigInteger value, bool unsigned = true) =>
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

    /// <summary>
    /// Determines whether the first <see cref="BCMath.BigInteger"/> value is greater than the second.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>true if <paramref name="left"/> is greater than <paramref name="right"/>; otherwise, false.</returns>
    public static bool IsGreaterThan(this BCMath.BigInteger left, BCMath.BigInteger right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Determines whether the first <see cref="BCMath.BigInteger"/> value is less than the second.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>true if <paramref name="left"/> is less than <paramref name="right"/>; otherwise, false.</returns>
    public static bool IsLessThan(this BCMath.BigInteger left, BCMath.BigInteger right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Determines whether the first <see cref="BCMath.BigInteger"/> value is greater than or equal to the second.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>true if <paramref name="left"/> is greater than or equal to <paramref name="right"/>; otherwise, false.</returns>
    public static bool IsGreaterThanOrEqual(this BCMath.BigInteger left, BCMath.BigInteger right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Determines whether the first <see cref="BCMath.BigInteger"/> value is less than or equal to the second.
    /// </summary>
    /// <param name="left">The first value to compare.</param>
    /// <param name="right">The second value to compare.</param>
    /// <returns>true if <paramref name="left"/> is less than or equal to <paramref name="right"/>; otherwise, false.</returns>
    public static bool IsLessThanOrEqual(this BCMath.BigInteger left, BCMath.BigInteger right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Determines whether the <see cref="BCMath.BigInteger"/> value is zero.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>true if <paramref name="value"/> is zero; otherwise, false.</returns>
    public static bool IsZero(this BCMath.BigInteger value)
    {
        return value.CompareTo(BCMath.BigInteger.Zero) == 0;
    }

    /// <summary>
    /// Determines whether the <see cref="BCMath.BigInteger"/> value is positive (greater than zero).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>true if <paramref name="value"/> is positive; otherwise, false.</returns>
    public static bool IsPositive(this BCMath.BigInteger value)
    {
        return value.CompareTo(BCMath.BigInteger.Zero) > 0;
    }

    /// <summary>
    /// Determines whether the <see cref="BCMath.BigInteger"/> value is negative (less than zero).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>true if <paramref name="value"/> is negative; otherwise, false.</returns>
    public static bool IsNegative(this BCMath.BigInteger value)
    {
        return value.CompareTo(BCMath.BigInteger.Zero) < 0;
    }
}
