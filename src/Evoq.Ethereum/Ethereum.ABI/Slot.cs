using System;
using System.Linq;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents a 32-byte slot in the ABI encoding.
/// </summary>
public class Slot
{
    private byte[] data;

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
    public Slot(byte[] data, SlotCollection? pointsToFirst = null)
    {
        if (data == null)
        {
            throw new ArgumentNullException(nameof(data));
        }

        if (data.Length != Size)
        {
            throw new ArgumentException($"Slot data must be exactly {Size} bytes", nameof(data));
        }

        this.data = data;
        this.PointsTo = pointsToFirst;
    }

    /// <summary>
    /// Creates a new slot that points to the given slots.
    /// </summary>
    /// <param name="pointsToFirst">The slots that this slot points to (will point to the first slot).</param>
    public Slot(SlotCollection pointsToFirst)
    {
        this.data = new byte[Size];
        this.PointsTo = pointsToFirst;
    }

    //

    /// <summary>
    /// The slot that this slot points to.
    /// </summary>
    public SlotCollection? PointsTo { get; private set; }

    /// <summary>
    /// Whether the slot is a pointer.
    /// </summary>
    public bool IsPointer => this.PointsTo != null;

    /// <summary>
    /// The offset of the slot.
    /// </summary>
    public int Offset { get; private set; } = -1;

    //

    /// <summary>
    /// Gets the slot data.
    /// </summary>
    internal byte[] Data => this.data;

    //

    /// <summary>
    /// Returns a string representation of the slot.
    /// </summary>
    /// <returns>A string representation of the slot.</returns>
    public Hex ToHex()
    {
        return new Hex(this.data);
    }

    //

    /// <summary>
    /// Returns a string representation of the slot.
    /// </summary>
    /// <returns>A string representation of the slot.</returns>
    public override string ToString()
    {
        return this.ToHex().ToString();
    }

    //

    /// <summary>
    /// Sets the offset of the slot within its slot space.
    /// </summary>
    /// <remarks>
    /// The offset is the byte index of this slot in memory.
    /// </remarks>
    /// <param name="offset">The offset of the slot.</param>
    internal void SetOffset(int offset)
    {
        this.Offset = offset;
    }

    /// <summary>
    /// Encodes the offset that this slot points to into the slot data.
    /// </summary>
    internal void EncodePointer()
    {
        if (this.PointsTo != null && this.PointsTo.First() != null && this.PointsTo.First().Offset >= 0)
        {
            var offset = UintTypeEncoder.EncodeUint(256, this.PointsTo.First().Offset);
            this.data = offset;
        }
    }
}