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
    private readonly List<SlotCollection> slotCollections = new(8);

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotSpace"/> class.
    /// </summary>
    public SlotSpace()
        : this(new List<SlotCollection>(capacity: 8))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotSpace"/> class.
    /// </summary>
    /// <param name="slots">The slots to initialize the slot space with.</param>
    public SlotSpace(SlotCollection slots)
        : this(new List<SlotCollection>(capacity: 8) { slots })
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SlotSpace"/> class.
    /// </summary>
    /// <param name="slotCollectionList">The space to initialize the slot space with.</param>
    private SlotSpace(IReadOnlyList<SlotCollection> slotCollectionList)
    {
        if (slotCollectionList == null)
        {
            throw new ArgumentNullException(nameof(slotCollectionList));
        }

        if (slotCollectionList.Any(sc => sc == null || sc.Any(slot => slot == null)))
        {
            throw new ArgumentException("Space cannot contain any null sets of slots", nameof(slotCollectionList));
        }

        this.slotCollections = new(slotCollectionList);
    }

    //

    /// <summary>
    /// Appends a set of reserved slots for an array to the slot space.
    /// </summary>
    /// <param name="length">The length of the array.</param>
    /// <returns>
    /// Returns a set of empty slots into which either static values can be written or,
    /// if dynamic, pointers to each dynamic value. Each dynamic value should be encoded
    /// into the returned <see cref="SlotSpace"/>.
    /// </returns>
    public (SlotSpace ElementsSpace, SlotSpace DynamicValuesSpace) AppendReservedArray(int length)
    {
        // Array ABI encoding recap:
        //
        // A dynamic array is encoded as a 32-byte slot containing a pointer to
        // 'the value'; the value is a set of slots and the pointer points to
        // the first one in the set, which is the length.
        //
        // The encoded array is itself encoded as a length slot followed by the
        // slots of the elements, each of which will either be the static value
        // or, if dynamic, a pointer to 'the value' (as above), but will always
        // be one slot per element.

        //

        // create the slot for the array length

        var lengthSlot = new Slot(UintTypeEncoder.EncodeUint(256, length));

        // create empty slots for each of the elements

        var elementsSlots = new SlotCollection(capacity: length);
        for (int i = 0; i < length; i++)
        {
            elementsSlots.Add(new Slot());
        }

        // create an empty space beyond the last element slot into which
        // any dynamic data for the array can be encoded

        var dynamicSlots = new SlotCollection(capacity: 8);

        // append it all to this space

        this.Append(lengthSlot);
        this.Append(elementsSlots);
        this.Append(dynamicSlots);

        // wrap the slots for the elements in a space, and wrap the slots for 
        // the values in a space and return them

        var elementsSpace = new SlotSpace(elementsSlots); // wrapped in a space
        var valuesSpace = new SlotSpace(dynamicSlots); // wrapped in a space

        return (elementsSpace, valuesSpace);
    }

    /// <summary>
    /// Appends a set of reserved slots for arbitrary bytes to the slot space.
    /// </summary>
    /// <param name="length">The length of the bytes.</param>
    public (Slot LengthSlot, SlotSpace DynamicValueSpace) AppendReservedBytes(int length)
    {
        // compute the capacity needed

        var hasRemainingBytes = length % Slot.Size != 0;
        var slotsNeededForBytes = (length / Slot.Size) + (hasRemainingBytes ? 1 : 0);

        // create the length slot

        var lengthSlot = new Slot(UintTypeEncoder.EncodeUint(256, length));

        // create the slots for the bytes

        var bytesSlots = new SlotCollection(capacity: slotsNeededForBytes);

        // append the length slot and the bytes slots to the slot space

        this.Append(lengthSlot);
        this.Append(bytesSlots);

        // wrap the bytes slots in a space and return it

        var valueSpace = new SlotSpace(bytesSlots);

        return (lengthSlot, valueSpace);
    }

    //

    /// <summary>
    /// Appends a set of reserved slots for an array to the slot space.
    /// </summary>
    /// <param name="length">The length of the array.</param>
    /// <returns>The length and pointer slots, where each offset points to the slots for the element data, and the elements slots themselves.</returns>
    // <returns>The length and pointer slots and a new slot space for the elements. The pointer point to the elements in the elements space.</returns>
    public (SlotCollection LengthAndPointers, IReadOnlyList<SlotCollection> Elements) AppendReservedArrayV1(int length)
    {
        // IMPORTANT / this assumes that the array is dynamic since it creates
        // pointer slots

        // Array ABI encoding recap:
        //
        // A dynamic array is encoded as a 32-byte slot containing a pointer to
        // 'the value'; the value is a set of slots and the pointer points to
        // the first one in the set, which is the length.
        //
        // The encoded array is itself encoded as a length slot followed by the
        // slots of the elements, each of which will either be the static value
        // or, if dynamic, a pointer to 'the value' (as above), but will always
        // be one slot per element.

        //

        // create a list of empty slots for each of the elements;
        // each element will have its own set of empty, elastic slots

        var elementsSlots = new List<SlotCollection>();

        // create a set of slots for the length and pointer slots

        var lengthSlot = new Slot(UintTypeEncoder.EncodeUint(256, length));
        var lengthAndElements = new SlotCollection(capacity: 4)
        {
            // add the array length
            lengthSlot,
        };

        // append the length and pointer slots to the slot space

        this.Append(lengthAndElements);

        // for each element in the array, 

        for (int i = 0; i < length; i++)
        {
            // create empty set of elastic slots for the element
            // and append them to this slot space

            var elementSlots = new SlotCollection(capacity: 8);
            this.Append(elementSlots);

            // then add these empty slots to the list of slots we're
            // constructing for all the elements

            elementsSlots.Add(elementSlots);

            // add a pointer slot to the length and pointer slots;
            // this pointer slot points to the first slot of the element slots

            var pointerSlot = new Slot(UintTypeEncoder.EncodeUint(256, i), pointsToFirst: elementSlots);
            lengthAndElements.Add(pointerSlot);
        }

        return (lengthAndElements, elementsSlots);
    }

    /// <summary>
    /// Appends a set of reserved slots for arbitrary bytes to the slot space.
    /// </summary>
    /// <param name="length">The length of the bytes.</param>
    /// <returns>The slots for the bytes with the length slot at the beginning.</returns>
    public SlotCollection AppendReservedBytesV1(int length)
    {
        var hasRemainingBytes = length % Slot.Size != 0;
        var slotsNeededForBytes = (length / Slot.Size) + (hasRemainingBytes ? 1 : 0);

        var lengthAndSlots = new SlotCollection(capacity: slotsNeededForBytes + 1) // +1 for the length slot
        {
            new Slot(UintTypeEncoder.EncodeUint(256, length))
        };

        this.Append(lengthAndSlots);

        return lengthAndSlots;
    }

    /// <summary>
    /// Gets the total number of slots in the slot space.
    /// </summary>
    /// <returns>The total number of slots in the slot space.</returns>
    public int Count() => this.slotCollections.Sum(sc => sc.Count);

    /// <summary>
    /// Appends a slot to the last slots in the slot space.
    /// </summary>
    /// <param name="slot">The slot to append.</param>
    public void Append(Slot slot)
    {
        if (this.slotCollections.Count == 0)
        {
            this.slotCollections.Add(new SlotCollection(capacity: 8));
        }

        this.slotCollections.Last().Add(slot);
    }

    /// <summary>
    /// Appends a collection of slots to the slot space.
    /// </summary>
    /// <param name="slots">The slots to append.</param>
    public void Append(SlotCollection slots)
    {
        this.slotCollections.Add(slots);
    }

    /// <summary>
    /// Gets the bytes of the slot space.
    /// </summary>
    /// <returns>The bytes of the slot space.</returns>
    public byte[] GetBytes()
    {
        return this.slotCollections.SelectMany(sc => sc.SelectMany(slot => slot.Data)).ToArray();
    }

    /// <summary>
    /// Gets the slots of the slot space.
    /// </summary>
    /// <returns>The slots of the slot space.</returns>
    public IEnumerable<Slot> GetSlots()
    {
        foreach (var slots in this.slotCollections)
        {
            foreach (var slot in slots)
            {
                yield return slot;
            }
        }
    }

    /// <summary>
    /// Gets the first slot of the slot space.
    /// </summary>
    /// <returns>The first slot of the slot space.</returns>
    internal SlotCollection GetFirstSlotCollection()
    {
        if (this.slotCollections.Count == 0)
        {
            throw new InvalidOperationException("Slot space is empty");
        }

        return this.slotCollections.First();
    }
}