using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// A collection of slots that are encoded into the tail of a dynamic value.
/// </summary>
public class TailSlotCollection : SlotCollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TailSlotCollection"/> class.
    /// </summary>
    public TailSlotCollection(int capacity) : base(capacity)
    {
    }
}

/// <summary>
/// A collection of slots that hold the head of a value.
/// </summary>
public class HeadSlotCollection : SlotCollection
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HeadSlotCollection"/> class.
    /// </summary>
    public HeadSlotCollection(int capacity) : base(capacity)
    {
    }

    //

    /// <summary>
    /// Creates a pointer slot.
    /// </summary>
    /// <param name="name">The name of the pointer slot.</param>
    /// <param name="tail">The tail of the pointer slot.</param>
    /// <returns>A pointer slot.</returns>
    internal Slot AddPointer(string name, out TailSlotCollection tail)
    {
        tail = new TailSlotCollection(8);

        var pointerSlot = new Slot(name, tail, this);

        this.Add(pointerSlot);

        return pointerSlot;
    }
}

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

    // /// <summary>
    // /// Reads a byte array into a slot collection.
    // /// </summary>
    // /// <param name="bytes">The bytes to read.</param>
    // /// <returns>The slot collection.</returns>
    // public static SlotCollection FromBytes(byte[] bytes)
    // {
    //     if (bytes.Length % 32 != 0)
    //     {
    //         throw new ArgumentException("Bytes must be a multiple of 32", nameof(bytes));
    //     }

    //     var slots = new SlotCollection(bytes.Length / 32);

    //     for (int i = 0; i < bytes.Length; i += 32)
    //     {
    //         slots.Add(new Slot($"slot_{i}", bytes));
    //     }

    //     return slots;
    // }
}
