using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// A collection of slots.
/// </summary>
public class SlotCollection : System.Collections.ObjectModel.Collection<Slot>
{
    //

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotCollection"/> class.
    /// </summary>
    /// <param name="capacity">The capacity of the slots.</param>
    public SlotCollection(int capacity) : base(new List<Slot>(capacity))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotCollection"/> class.
    /// </summary>
    /// <param name="slots">Some slots to add.</param>
    public SlotCollection(IList<Slot> slots) : base(slots)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotCollection"/> class.
    /// </summary>
    /// <param name="slot">A slot to add.</param>
    public SlotCollection(Slot slot) : base(new List<Slot> { slot })
    {
    }

    //

    /// <summary>
    /// Adds a range of slots to the collection.
    /// </summary>
    /// <param name="slots">The slots to add.</param>
    public void AddRange(IEnumerable<Slot> slots)
    {
        foreach (var slot in slots)
        {
            this.Add(slot);
        }
    }

    /// <summary>
    /// Skips past the slot pointed to by the pointer and returns a new collection of the remaining slots.
    /// </summary>
    /// <param name="pointer">The pointer to skip.</param>
    /// <returns>A new collection of the remaining slots.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the pointer is not found in the collection.</exception>
    public SlotCollection SkipToPoint(Slot pointer)
    {
        if (pointer.IsPointer)
        {
            return this.SkipTo(pointer.PointsTo!);
        }

        throw new ArgumentException("Slot passed in is not a pointer", nameof(pointer));
    }

    /// <summary>
    /// Skips to the slot and returns a new collection including the slot and the following slots.
    /// </summary>
    /// <param name="slot">The slot to skip to.</param>
    /// <returns>A new collection of the slot and the following slots.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the slot is not found in the collection.</exception>
    public SlotCollection SkipTo(Slot slot)
    {
        var index = this.IndexOf(slot);

        if (index < 0)
        {
            throw new InvalidOperationException("Slot not found in collection");
        }

        if (index == 0)
        {
            return new SlotCollection(this);
        }

        return new SlotCollection(this.Skip(index).ToList());
    }

    /// <summary>
    /// Skips the slot and returns a new collection of the remaining slots.
    /// </summary>
    /// <param name="slot">The slot to skip.</param>
    /// <returns>A new collection of the remaining slots.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the slot is not found in the collection.</exception>
    public SlotCollection SkipPast(Slot slot)
    {
        var index = this.IndexOf(slot);

        if (index == -1)
        {
            throw new InvalidOperationException("Slot not found in collection");
        }

        if (index + 1 == this.Count)
        {
            throw new InvalidOperationException("Cannot skip past the last element");
        }

        return new SlotCollection(this.Skip(index + 1).ToList());
    }

    /// <summary>
    /// Skips past the slot and returns a new collection of the remaining slots.
    /// </summary>
    /// <param name="slot">The slot to skip past.</param>
    /// <param name="slots">The new collection of the remaining slots.</param>
    /// <returns>True if the slot was found and skipped past; otherwise, false.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the slot is not found in the collection.</exception>
    public bool TrySkipPast(Slot slot, out SlotCollection? slots)
    {
        var index = this.IndexOf(slot);

        if (index == -1)
        {
            throw new InvalidOperationException("Slot not found in collection");
        }

        if (index + 1 == this.Count)
        {
            slots = default;
            return false;
        }

        slots = new SlotCollection(this.Skip(index + 1).ToList());
        return true;
    }

    //

    /// <summary>
    /// Returns a string representation of the slot collection.
    /// </summary>
    /// <returns>A string representation of the slot collection.</returns>
    public override string ToString()
    {
        if (this.Count == 0)
        {
            return "[]";
        }

        return string.Join(Environment.NewLine, this.Select(slot => slot.ToString()));
    }

    //

    /// <summary>
    /// Reads a byte array into a slot collection.
    /// </summary>
    /// <param name="slotNamePrefix">The prefix of the slots.</param>
    /// <param name="paddedBytes">The padded bytes to read.</param>
    /// <returns>The slot collection.</returns>
    public static SlotCollection FromBytes(string slotNamePrefix, byte[] paddedBytes)
    {
        bool hasRemainingBytes = paddedBytes.Length % 32 != 0;

        Debug.Assert(!hasRemainingBytes, "Has remaining bytes; bytes expected to be a multiple of 32");

        var slots = new SlotCollection(capacity: paddedBytes.Length / 32 + (hasRemainingBytes ? 1 : 0));

        for (int i = 0; i < paddedBytes.Length; i += 32)
        {
            var chunk = new byte[32];
            var count = Math.Min(32, paddedBytes.Length - i);
            Buffer.BlockCopy(paddedBytes, i, chunk, 0, count);

            slots.Add(new Slot($"{slotNamePrefix}_{i}", chunk, i));
        }

        return slots;
    }
}
