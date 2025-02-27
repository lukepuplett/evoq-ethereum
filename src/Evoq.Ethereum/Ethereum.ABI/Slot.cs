using System;
using System.Collections.Generic;
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
    /// <param name="order">The order of the slot within its container.</param>
    /// <exception cref="ArgumentException">Thrown if data is not exactly 32 bytes.</exception>
    public Slot(string name, byte[] data, int order = -1)
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

        if (order >= 0)
        {
            this.Order = order;
            this.OffsetByte = order * Size;
        }
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
    public bool IsPointer => this.PointsTo != null;

    /// <summary>
    /// The offset of the slot within its container.
    /// </summary>
    public int OffsetByte { get; private set; } = -1;

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

        string pointsTo = "";
        if (this.IsPointer)
        {
            if (this.PointsTo.Any())
            {
                pointsTo = $", ptr: {this.PointsTo.First().id.ToString()[^4..]}";
            }
            else
            {
                pointsTo = $", ptr: EMPTY! CHECK SLOTS WERE ADDED TO THE CORRECT COLLECTION";
            }
        }

        string relTo = "";
        if (this.IsPointer)
        {
            if (this.RelativeTo.Any())
            {
                relTo = $", rel: {this.RelativeTo.First().id.ToString()[^4..]}";
            }
            else
            {
                relTo = $", rel: EMPTY! CHECK SLOTS WERE ADDED TO THE CORRECT COLLECTION";
            }
        }

        return $"{this.ToHex()} (id: {idStr}, off: {this.OffsetByte}, ord: {this.Order}{pointsTo}{relTo} - {this.Name})";
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
        this.OffsetByte = offset;
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
        if (this.PointsTo != null && this.PointsTo.Count() > 0 && this.PointsTo.First().OffsetByte >= 0)
        {
            var offset = UintTypeEncoder.EncodeUint(256, this.PointsTo.First().OffsetByte);
            this.data = offset;
        }
    }

    /// <summary>
    /// Decodes the pointer that this slot points to.
    /// </summary>
    /// <param name="relativeTo">The slot space that the pointer is relative to.</param>
    /// <param name="length">The length of the pointer, if known, else it'll be read from the first data slot.</param>
    /// <exception cref="NotImplementedException">Thrown if the pointer is not implemented.</exception>
    internal void DecodePointer(SlotCollection relativeTo, int? length)
    {
        if (this.IsPointer)
        {
            throw new InvalidOperationException("Slot is already a pointer");
        }

        this.OffsetByte = (int)UintTypeEncoder.DecodeUint(256, this.data);

        int skipTo = this.OffsetByte / Size;
        List<Slot> dataSlots;

        if (length.HasValue)
        {
            dataSlots = relativeTo.Skip(skipTo + 1).Take(length.Value).ToList();
        }
        else
        {
            // decode the length from the first data slot

            int len = (int)UintTypeEncoder.DecodeUint(256, relativeTo.Skip(skipTo).First().Data);

            dataSlots = relativeTo.Skip(skipTo).Take(len).ToList();
        }

        this.PointsTo = new SlotCollection(dataSlots);
    }

    /// <summary>
    /// Encodes the offset that this slot points to into the slot data.
    /// </summary>
    internal void EncodePointerOffset()
    {
        if (this.PointsTo != null)
        {
            // Debug.Assert(this.PointsTo.Count > 0, "PointsTo an empty SlotCollection. Check slots were added to the correct collection.");

            if (this.PointsTo.Count > 0)
            {
                // Debug.Assert(this.PointsTo.First().Offset >= 0, "PointsTo.First().Offset is negative");

                if (this.PointsTo.First().OffsetByte >= 0)
                {
                    Debug.Assert(this.RelativeTo != null, $"RelativeTo for slot {this.Name} is null");
                    Debug.Assert(this.RelativeTo.Count > 0, $"RelativeTo for slot {this.Name} is empty");

                    int startingPosition = this.RelativeTo?.First().Order ?? 0;
                    int distance = this.PointsTo.First().Order - startingPosition;

                    var offset = UintTypeEncoder.EncodeUint(256, distance * Size);
                    this.data = offset;
                }
            }
        }
    }
}