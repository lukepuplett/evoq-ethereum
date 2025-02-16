using System;
using System.Collections.Generic;
using System.Linq;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represent a contiguous region of slot space.
/// </summary>
public class SlotSpace
{
    private readonly List<Slots> space = new();

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotSpace"/> class.
    /// </summary>
    public SlotSpace()
        : this(new List<Slots>(capacity: 8))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotSpace"/> class.
    /// </summary>
    /// <param name="slots">The slots to initialize the slot space with.</param>
    public SlotSpace(Slots slots)
        : this(new List<Slots>(capacity: 8) { slots })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotSpace"/> class.
    /// </summary>
    /// <param name="space">The space to initialize the slot space with.</param>
    public SlotSpace(IReadOnlyList<Slots> space)
    {
        if (space == null)
        {
            throw new ArgumentNullException(nameof(space));
        }

        if (space.Any(slots => slots == null))
        {
            throw new ArgumentException("Space cannot contain any null sets of slots", nameof(space));
        }

        if (!space.Any())
        {
            throw new ArgumentException("Space must contain at least one set of slots", nameof(space));
        }

        this.space = new List<Slots>(space);
    }

    //

    /// <summary>
    /// Appends a set of reserved slots for an array to the slot space.
    /// </summary>
    /// <param name="length">The length of the array.</param>
    /// <returns>The length and pointer slots, where each offset points to the slots for the element data, and the elements slots themselves.</returns>
    // <returns>The length and pointer slots and a new slot space for the elements. The pointer point to the elements in the elements space.</returns>
    public (Slots lengthAndPointers, IReadOnlyList<Slots> elements) AppendReservedArray(int length)
    {
        // for an array we need slots to hold the array length and slots for the pointer
        // to the elements in the array, and then a slot space for the element data

        // ^ change of design; we return the length and offsets and the elements slots
        // so that the caller can decide what to do with the elements slots

        var elementsSpace = new SlotSpace(); // potentially remove
        var elementsSlots = new List<Slots>();

        var lengthAndpointer = new Slots(capacity: 4)
        {
            new Slot(UintTypeEncoder.EncodeUint(256, length)) // add the array length
        };

        this.Append(lengthAndpointer);

        for (int i = 0; i < length; i++)
        {
            var elementSlots = new Slots(capacity: 8);

            this.Append(elementSlots);
            elementsSpace.Append(elementSlots); // potentially remove
            elementsSlots.Add(elementSlots);

            var pointerlot = new Slot(UintTypeEncoder.EncodeUint(256, i), pointsToFirst: elementSlots);
            lengthAndpointer.Add(pointerlot);
        }

        return (lengthAndpointer, elementsSlots);
    }

    /// <summary>
    /// Appends a set of reserved slots for a string to the slot space.
    /// </summary>
    /// <param name="length">The length of the string.</param>
    /// <returns>The slots for the string with the length slot at the beginning.</returns>
    public Slots AppendReservedString(int length)
    {
        var stringSlots = new Slots(capacity: (Slot.Size * length) + 1)
        {
            new Slot(UintTypeEncoder.EncodeUint(256, length))
        };

        this.Append(stringSlots);

        return stringSlots;
    }

    /// <summary>
    /// Gets the total number of slots in the slot space.
    /// </summary>
    /// <returns>The total number of slots in the slot space.</returns>
    public int Count() => this.space.Sum(x => x.Count);

    /// <summary>
    /// Appends a slot to the last slots in the slot space.
    /// </summary>
    /// <param name="slot">The slot to append.</param>
    public void Append(Slot slot)
    {
        this.space.Last().Add(slot);
    }

    /// <summary>
    /// Appends a collection of slots to the slot space.
    /// </summary>
    /// <param name="slots">The slots to append.</param>
    public void Append(Slots slots)
    {
        this.space.Add(slots);
    }

    /// <summary>
    /// Gets the bytes of the slot space.
    /// </summary>
    /// <returns>The bytes of the slot space.</returns>
    public byte[] GetBytes()
    {
        return this.space.SelectMany(x => x.SelectMany(y => y.Data)).ToArray();
    }
}