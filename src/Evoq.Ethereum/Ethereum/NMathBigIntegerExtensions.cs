using System;
using BCMath = Org.BouncyCastle.Math;
using NMath = System.Numerics;

namespace Evoq.Ethereum;

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
