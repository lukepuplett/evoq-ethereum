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
    /// Skips the slot and returns a new collection of the remaining slots.
    /// </summary>
    /// <param name="slot">The slot to skip.</param>
    /// <returns>A new collection of the remaining slots.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the slot is not found in the collection.</exception>
    public SlotCollection Skip(Slot slot)
    {
        var index = this.IndexOf(slot);

        if (index == -1)
        {
            throw new InvalidOperationException("Slot not found in collection");
        }

        return new SlotCollection(this.Skip(index).ToList());
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
