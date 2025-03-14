using System;
using System.Numerics;

namespace Evoq.Ethereum;

/// <summary>
/// Represents a unit of Ethereum currency.
/// </summary>
public enum EthereumUnit
{
    /// <summary>
    /// Wei - the smallest unit of Ethereum (1 Ether = 10^18 Wei).
    /// </summary>
    Wei,

    /// <summary>
    /// Ether - the main unit of Ethereum.
    /// </summary>
    Ether
}

/// <summary>
/// Represents an amount of Ethereum currency.
/// </summary>
public readonly struct EthereumAmount : IEquatable<EthereumAmount>, IComparable<EthereumAmount>
{
    // Constants for conversion
    private static readonly BigInteger WeiPerEther = BigInteger.Parse("1000000000000000000");
    private const decimal WeiToEtherFactor = 0.000000000000000001m; // 10^-18

    /// <summary>
    /// The amount in Wei (the smallest unit).
    /// </summary>
    public readonly BigInteger WeiValue { get; }

    /// <summary>
    /// The display unit for this amount.
    /// </summary>
    public readonly EthereumUnit DisplayUnit { get; }

    /// <summary>
    /// Creates a new instance of the EthereumAmount struct.
    /// </summary>
    /// <param name="weiValue">The amount in Wei.</param>
    /// <param name="displayUnit">The unit to use for display.</param>
    public EthereumAmount(BigInteger weiValue, EthereumUnit displayUnit)
    {
        if (!Enum.IsDefined(typeof(EthereumUnit), displayUnit))
        {
            throw new ArgumentException("Invalid Ethereum unit", nameof(displayUnit));
        }

        if (weiValue < 0)
        {
            throw new ArgumentException("Ethereum amounts cannot be negative", nameof(weiValue));
        }

        WeiValue = weiValue;
        DisplayUnit = displayUnit;
    }

    /// <summary>
    /// Creates a new amount in Wei.
    /// </summary>
    /// <param name="wei">The amount in Wei.</param>
    /// <returns>A new EthereumAmount with Wei as the display unit.</returns>
    public static EthereumAmount FromWei(BigInteger wei)
    {
        if (wei < 0)
        {
            throw new ArgumentException("Ethereum amounts cannot be negative", nameof(wei));
        }

        return new EthereumAmount(wei, EthereumUnit.Wei);
    }

    /// <summary>
    /// Creates a new amount in Ether.
    /// </summary>
    /// <param name="ether">The amount in Ether.</param>
    /// <returns>A new EthereumAmount with Ether as the display unit.</returns>
    public static EthereumAmount FromEther(decimal ether)
    {
        if (ether < 0)
        {
            throw new ArgumentException("Ethereum amounts cannot be negative", nameof(ether));
        }

        return new EthereumAmount(
            (BigInteger)(ether * 1_000_000_000_000_000_000m),
            EthereumUnit.Ether);
    }

    /// <summary>
    /// Gets the amount in Wei.
    /// </summary>
    /// <returns>The amount in Wei.</returns>
    public BigInteger ToWei()
    {
        return WeiValue;
    }

    /// <summary>
    /// Gets the amount in Ether.
    /// </summary>
    /// <returns>The amount in Ether.</returns>
    public decimal ToEther()
    {
        return (decimal)WeiValue * WeiToEtherFactor;
    }

    /// <summary>
    /// Gets the amount in the display unit.
    /// </summary>
    /// <returns>The amount in the display unit.</returns>
    public decimal GetDisplayValue()
    {
        return DisplayUnit switch
        {
            EthereumUnit.Wei => (decimal)WeiValue,
            EthereumUnit.Ether => ToEther(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Converts the amount to a different display unit.
    /// </summary>
    /// <param name="targetUnit">The target display unit.</param>
    /// <returns>A new EthereumAmount with the specified display unit.</returns>
    public EthereumAmount ConvertTo(EthereumUnit unit)
    {
        if (!Enum.IsDefined(typeof(EthereumUnit), unit))
        {
            throw new ArgumentException("Invalid Ethereum unit", nameof(unit));
        }

        return new EthereumAmount(WeiValue, unit);
    }

    /// <summary>
    /// Adds two EthereumAmount values.
    /// </summary>
    /// <param name="other">The other amount to add.</param>
    /// <returns>A new EthereumAmount representing the sum.</returns>
    public EthereumAmount Add(EthereumAmount other)
    {
        return new EthereumAmount(WeiValue + other.WeiValue, DisplayUnit);
    }

    /// <summary>
    /// Subtracts another EthereumAmount from this one.
    /// </summary>
    /// <param name="other">The amount to subtract.</param>
    /// <returns>A new EthereumAmount representing the difference.</returns>
    public EthereumAmount Subtract(EthereumAmount other)
    {
        var leftWei = ToWei();
        var rightWei = other.ToWei();

        if (leftWei < rightWei)
        {
            throw new InvalidOperationException("Cryptocurrency amounts cannot be negative");
        }

        return new EthereumAmount(leftWei - rightWei, DisplayUnit);
    }

    /// <summary>
    /// Returns a string representation of the amount.
    /// </summary>
    /// <returns>A string representation of the amount.</returns>
    public override string ToString()
    {
        return DisplayUnit switch
        {
            EthereumUnit.Wei => $"{WeiValue} Wei",
            EthereumUnit.Ether => $"{ToEther():0.##################} ETH",
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    /// <summary>
    /// Determines whether this instance is equal to another instance.
    /// </summary>
    /// <param name="other">The other instance.</param>
    /// <returns>True if the instances are equal, false otherwise.</returns>
    public bool Equals(EthereumAmount other)
    {
        return WeiValue == other.WeiValue;
    }

    /// <summary>
    /// Determines whether this instance is equal to another object.
    /// </summary>
    /// <param name="obj">The other object.</param>
    /// <returns>True if the objects are equal, false otherwise.</returns>
    public override bool Equals(object obj)
    {
        return obj is EthereumAmount other && Equals(other);
    }

    /// <summary>
    /// Gets a hash code for this instance.
    /// </summary>
    /// <returns>A hash code for this instance.</returns>
    public override int GetHashCode()
    {
        return WeiValue.GetHashCode();
    }

    /// <summary>
    /// Compares this instance to another instance.
    /// </summary>
    /// <param name="other">The other instance.</param>
    /// <returns>A value indicating the relative order of the instances.</returns>
    public int CompareTo(EthereumAmount other)
    {
        return WeiValue.CompareTo(other.WeiValue);
    }

    /// <summary>
    /// Determines whether two instances are equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True if the instances are equal, false otherwise.</returns>
    public static bool operator ==(EthereumAmount left, EthereumAmount right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Determines whether two instances are not equal.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True if the instances are not equal, false otherwise.</returns>
    public static bool operator !=(EthereumAmount left, EthereumAmount right)
    {
        return !left.Equals(right);
    }

    /// <summary>
    /// Determines whether one instance is less than another instance.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True if the first instance is less than the second instance, false otherwise.</returns>
    public static bool operator <(EthereumAmount left, EthereumAmount right)
    {
        return left.CompareTo(right) < 0;
    }

    /// <summary>
    /// Determines whether one instance is greater than another instance.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True if the first instance is greater than the second instance, false otherwise.</returns>
    public static bool operator >(EthereumAmount left, EthereumAmount right)
    {
        return left.CompareTo(right) > 0;
    }

    /// <summary>
    /// Determines whether one instance is less than or equal to another instance.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True if the first instance is less than or equal to the second instance, false otherwise.</returns>
    public static bool operator <=(EthereumAmount left, EthereumAmount right)
    {
        return left.CompareTo(right) <= 0;
    }

    /// <summary>
    /// Determines whether one instance is greater than or equal to another instance.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>True if the first instance is greater than or equal to the second instance, false otherwise.</returns>
    public static bool operator >=(EthereumAmount left, EthereumAmount right)
    {
        return left.CompareTo(right) >= 0;
    }

    /// <summary>
    /// Adds two EthereumAmount instances.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>The sum of the instances.</returns>
    public static EthereumAmount operator +(EthereumAmount left, EthereumAmount right)
    {
        return left.Add(right);
    }

    /// <summary>
    /// Subtracts one EthereumAmount instance from another.
    /// </summary>
    /// <param name="left">The first instance.</param>
    /// <param name="right">The second instance.</param>
    /// <returns>The difference between the instances.</returns>
    public static EthereumAmount operator -(EthereumAmount left, EthereumAmount right)
    {
        var leftWei = left.ToWei();
        var rightWei = right.ToWei();

        if (leftWei < rightWei)
        {
            throw new InvalidOperationException("Cryptocurrency amounts cannot be negative");
        }

        return new EthereumAmount(leftWei - rightWei, left.DisplayUnit);
    }

    /// <summary>
    /// Multiplies an EthereumAmount by a scalar value.
    /// </summary>
    /// <param name="amount">The amount to multiply.</param>
    /// <param name="multiplier">The multiplier.</param>
    /// <returns>A new EthereumAmount representing the product.</returns>
    public static EthereumAmount operator *(EthereumAmount amount, int multiplier)
    {
        return new EthereumAmount(amount.WeiValue * multiplier, amount.DisplayUnit);
    }

    /// <summary>
    /// Multiplies an EthereumAmount by a scalar value.
    /// </summary>
    /// <param name="multiplier">The multiplier.</param>
    /// <param name="amount">The amount to multiply.</param>
    /// <returns>A new EthereumAmount representing the product.</returns>
    public static EthereumAmount operator *(int multiplier, EthereumAmount amount)
    {
        return amount * multiplier;
    }

    /// <summary>
    /// Divides an EthereumAmount by a scalar value.
    /// </summary>
    /// <param name="amount">The amount to divide.</param>
    /// <param name="divisor">The divisor.</param>
    /// <returns>A new EthereumAmount representing the quotient.</returns>
    public static EthereumAmount operator /(EthereumAmount amount, int divisor)
    {
        if (divisor == 0)
            throw new DivideByZeroException();

        return new EthereumAmount(amount.WeiValue / divisor, amount.DisplayUnit);
    }

    /// <summary>
    /// Multiplies an EthereumAmount by a BigInteger value.
    /// </summary>
    /// <param name="amount">The amount to multiply.</param>
    /// <param name="multiplier">The BigInteger multiplier.</param>
    /// <returns>A new EthereumAmount representing the product.</returns>
    public static EthereumAmount operator *(EthereumAmount amount, BigInteger multiplier)
    {
        return new EthereumAmount(amount.WeiValue * multiplier, amount.DisplayUnit);
    }

    /// <summary>
    /// Multiplies an EthereumAmount by a BigInteger value.
    /// </summary>
    /// <param name="multiplier">The BigInteger multiplier.</param>
    /// <param name="amount">The amount to multiply.</param>
    /// <returns>A new EthereumAmount representing the product.</returns>
    public static EthereumAmount operator *(BigInteger multiplier, EthereumAmount amount)
    {
        return amount * multiplier;
    }

    public static EthereumAmount operator *(EthereumAmount amount, decimal factor)
    {
        if (factor < 0)
        {
            throw new ArgumentException("Cannot multiply by negative values", nameof(factor));
        }

        // Instead of scaling up and down, which can lose precision,
        // we'll work directly with the decimal conversion
        var etherValue = amount.ToEther() * factor;

        // Convert back to wei, maintaining maximum precision
        var weiValue = (BigInteger)(etherValue * 1_000_000_000_000_000_000m);

        return new EthereumAmount(weiValue, amount.DisplayUnit);
    }

    public static EthereumAmount operator /(EthereumAmount amount, decimal divisor)
    {
        if (divisor == 0)
        {
            throw new DivideByZeroException();
        }

        var newWeiValue = amount.WeiValue * 1_000_000_000_000_000_000 / (BigInteger)(divisor * 1_000_000_000_000_000_000m);
        return new EthereumAmount(newWeiValue, amount.DisplayUnit);
    }
}
