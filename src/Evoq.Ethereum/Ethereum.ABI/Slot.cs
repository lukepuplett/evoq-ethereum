using System;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents a 32-byte slot in the ABI encoding.
/// </summary>
public class Slot : IEquatable<Slot>
{
    private readonly byte[] data;

    /// <summary>
    /// The size of a slot in bytes.
    /// </summary>
    public const int Size = 32;

    //

    /// <summary>
    /// Creates a new slot with no data.
    /// </summary>
    public Slot()
    {
        this.data = new byte[Size];
    }

    /// <summary>
    /// Creates a new slot with the given data.
    /// </summary>
    /// <param name="data">The 32-byte data.</param>
    /// <param name="pointsToFirst">The slot that this slot points to.</param>
    /// <exception cref="ArgumentException">Thrown if data is not exactly 32 bytes.</exception>
    public Slot(byte[] data, Slots? pointsToFirst = null)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (data.Length != Size)
            throw new ArgumentException($"Slot data must be exactly {Size} bytes", nameof(data));

        this.data = data;
        this.PointsTo = pointsToFirst;
    }

    /// <summary>
    /// Creates a new slot that points to the given slots.
    /// </summary>
    /// <param name="pointsToFirst">The slots that this slot points to (will point to the first slot).</param>
    public Slot(Slots pointsToFirst)
    {
        this.data = new byte[Size];
        this.PointsTo = pointsToFirst;
    }

    //

    /// <summary>
    /// The slot that this slot points to.
    /// </summary>
    public Slots? PointsTo { get; internal set; }

    /// <summary>
    /// Gets the slot data.
    /// </summary>
    internal byte[] Data => this.data;

    //

    /// <summary>
    /// Returns true if the two slots are equal.
    /// </summary>
    /// <param name="other">The other slot.</param>
    /// <returns>True if the two slots are equal.</returns>
    public bool Equals(Slot other)
    {
        return this.data.AsSpan().SequenceEqual(other.data);
    }

    /// <summary>
    /// Returns true if the slot is equal to the object.
    /// </summary>
    /// <param name="obj">The object.</param>
    /// <returns>True if the slot is equal to the object.</returns> 
    public override bool Equals(object? obj)
    {
        return obj is Slot slot && Equals(slot);
    }

    /// <summary>
    /// Returns the hash code of the slot.
    /// </summary>
    /// <returns>The hash code of the slot.</returns>
    public override int GetHashCode()
    {
        return this.data.AsSpan().GetHashCode();
    }

    //

    /// <summary>
    /// Returns true if the two slots are equal.
    /// </summary>
    /// <param name="left">The left slot.</param>
    /// <param name="right">The right slot.</param>
    /// <returns>True if the two slots are equal.</returns>
    public static bool operator ==(Slot left, Slot right)
    {
        return left.Equals(right);
    }

    /// <summary>
    /// Returns true if the two slots are not equal.
    /// </summary>
    /// <param name="left">The left slot.</param>
    /// <param name="right">The right slot.</param>
    /// <returns>True if the two slots are not equal.</returns>
    public static bool operator !=(Slot left, Slot right)
    {
        return !left.Equals(right);
    }
}