using System;
using System.Diagnostics;
using System.Linq;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents a 32-byte slot in the ABI encoding.
/// </summary>
public class Slot
{
    private readonly Guid id = Guid.NewGuid();
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
    /// <param name="name">The name of the slot.</param>
    /// <param name="data">The 32-byte data.</param>
    /// <exception cref="ArgumentException">Thrown if data is not exactly 32 bytes.</exception>
    public Slot(string name, byte[] data)
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
        this.Name = name;
    }

    /// <summary>
    /// Creates a new slot with the given data.
    /// </summary>
    /// <param name="name">The name of the slot.</param>
    /// <param name="data">The 32-byte data.</param>
    /// <param name="pointsToFirst">The slot collection that this slot points to.</param>
    /// <exception cref="ArgumentException">Thrown if data is not exactly 32 bytes.</exception>
    [Obsolete]
    public Slot(string name, byte[] data, SlotCollection? pointsToFirst)
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
        this.Name = name;
    }

    /// <summary>
    /// Creates a new slot that points to the given slots.
    /// </summary>
    /// <param name="name">The name of the slot.</param>
    /// <param name="pointsToFirst">The slots that this slot points to (will point to the first slot).</param>
    /// <param name="relativeTo">The slot space that the pointer is relative to.</param>
    public Slot(string name, SlotCollection pointsToFirst, SlotCollection relativeTo)
    {
        this.data = new byte[Size];
        this.PointsTo = pointsToFirst;
        this.RelativeTo = relativeTo;
        this.Name = name;
    }

    //

    /// <summary>
    /// The name of the slot.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The slot that this slot points to.
    /// </summary>
    public SlotCollection? PointsTo { get; private set; }

    /// <summary>
    /// The slot that this slot is relative to.
    /// </summary>
    public SlotCollection? RelativeTo { get; private set; }

    /// <summary>
    /// Whether the slot is a pointer.
    /// </summary>
    public bool IsPointer => this.PointsTo != null && this.PointsTo.Count > 0;

    /// <summary>
    /// The offset of the slot within its container.
    /// </summary>
    public int Offset { get; private set; } = -1;

    /// <summary>
    /// The order of the slot within its container.
    /// </summary>
    public int Order { get; private set; } = -1;

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
        string idStr = this.id.ToString()[^4..];
        string pointsTo = this.IsPointer ? $", ptr: {this.PointsTo.First().id.ToString()[^4..]} " : "";

        return $"{this.ToHex()} (id: {idStr}, off: {this.Offset}, ord: {this.Order}{pointsTo} - {this.Name})";
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
    /// Sets the order of the slot within its container.
    /// </summary>
    /// <param name="order">The order of the slot.</param>
    internal void SetOrder(int order)
    {
        this.Order = order;
    }

    /// <summary>
    /// Encodes the offset that this slot points to into the slot data.
    /// </summary>
    internal void EncodePointer()
    {
        if (this.PointsTo != null && this.PointsTo.Count() > 0 && this.PointsTo.First().Offset >= 0)
        {
            var offset = UintTypeEncoder.EncodeUint(256, this.PointsTo.First().Offset);
            this.data = offset;
        }
    }

    /// <summary>
    /// Encodes the offset that this slot points to into the slot data.
    /// </summary>
    internal void EncodePointerOffset()
    {
        if (this.PointsTo != null && this.PointsTo.Count() > 0 && this.PointsTo.First().Offset >= 0)
        {
            Debug.Assert(this.RelativeTo != null, "RelativeTo is null");
            Debug.Assert(this.RelativeTo.Count > 0, "RelativeTo is empty");

            int startingPosition = this.RelativeTo?.First().Order ?? 0;
            int distance = this.PointsTo.First().Order - startingPosition;

            var offset = UintTypeEncoder.EncodeUint(256, distance * Size);
            this.data = offset;
        }
    }
}