using System;
using System.Collections.Generic;
using System.Linq;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// The result of encoding a set of parameters.
/// </summary>
public class AbiEncodingResult
{
    private readonly SlotSpace staticData;
    private readonly SlotSpace dynamicData = new();
    private readonly int staticSlotCount;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    /// <param name="staticSlotCount">The number of static slots.</param>
    public AbiEncodingResult(int staticSlotCount)
    {
        this.staticSlotCount = staticSlotCount;
        this.staticData = new SlotSpace();
    }

    //

    // For testing and inspection

    /// <summary>
    /// The static data. Each array must be exactly 32 bytes.
    /// </summary>
    public SlotSpace StaticData => staticData;

    /// <summary>
    /// The dynamic data. Arrays can be any length but will be padded to 32-byte alignment.
    /// </summary>
    public SlotSpace DynamicData => dynamicData;

    /// <summary>
    /// The slot index of the last dynamic slot.
    /// </summary>
    public int CurrentDynamicSlotIndex => staticSlotCount + dynamicData.Count();

    /// <summary>
    /// The dynamic offset which is the byte index of the next dynamic slot.
    /// </summary>
    public int CurrentDynamicOffset => this.CurrentDynamicSlotIndex * Slot.Size;

    // ^ the end of the first slot is index 31, so the next dynamic slot is index 32
    // ^ 1 static slots with 0 dynamic slots should have a current dynamic offset of 31 + 1 (32 bytes ~ 1 slot)
    // ^ 2 static slots with 0 dynamic slots should have a current dynamic offset of 63 + 1 (64 bytes ~ 2 slots)
    // ^ 1 static slot with 1 dynamic slot should have a current dynamic offset of 31 + 32 + 1 (64 bytes ~ 2 slots)
    // ^ 1 static slot with 2 dynamic slots should have a current dynamic offset of 31 + 64 + 1 (96 bytes ~ 3 slots)

    //

    /// <summary>
    /// Gets the slots of the slot space.
    /// </summary>
    /// <returns>The slots of the slot space.</returns>
    public IEnumerable<Slot> GetSlots()
    {
        this.SetOffsets();

        return this.GetSlotsInternal();
    }

    /// <summary>
    /// Gets the combined static and dynamic data as a byte array.
    /// </summary>
    /// <returns>The combined static and dynamic data as a byte array.</returns>
    public byte[] GetBytes()
    {
        if (this.staticData.Count() != this.staticSlotCount)
        {
            throw new InvalidOperationException($"Expected {this.staticSlotCount} static slots but got {this.staticData.Count()}");
        }

        this.SetOffsets();

        return this.staticData.GetBytes().Concat(this.dynamicData.GetBytes()).ToArray();
    }

    //

    /// <summary>
    /// Adds static data.
    /// </summary>
    /// <param name="slot">The data to add.</param>
    /// <returns>The slot index of the added data.</returns>
    public void AppendStatic(Slot slot)
    {
        if (this.staticData.Count() >= this.staticSlotCount)
            throw new InvalidOperationException($"Cannot add more than {this.staticSlotCount} static slots");

        this.staticData.Append(slot);
    }

    /// <summary>
    /// Adds dynamic data and adds an offset slot to the added static section.
    /// </summary>
    /// <param name="slots">The array of 32-byte slots to add, each must be 32 bytes.</param>
    /// <returns>The slot index of the added data.</returns>
    public void AppendDynamic(SlotCollection slots)
    {
        var pointerSlot = new Slot(UintTypeEncoder.EncodeUint(256, this.CurrentDynamicOffset), pointsToFirst: slots);
        this.AppendStatic(pointerSlot);

        this.dynamicData.Append(slots);
    }

    //

    private IEnumerable<Slot> GetSlotsInternal()
    {
        foreach (var slot in this.staticData.GetSlots())
        {
            yield return slot;
        }

        foreach (var slot in this.dynamicData.GetSlots())
        {
            yield return slot;
        }
    }

    /// <summary>
    /// Sets the byte offset on every slot in the slot space where slot zero has offset zero.
    /// </summary>
    /// <remarks>
    /// Each slot is 32 bytes, so the second slot has offset 32, the third has offset 64, etc.
    /// This method must be called after all the slots have been appended to the slot space
    /// and the full bytes are read.
    /// </remarks>
    private void SetOffsets()
    {
        int offset = 0;
        foreach (var slot in this.GetSlotsInternal())
        {
            slot.SetOffset(offset);
            offset += Slot.Size;
        }

        // now we can encode the pointers

        foreach (var slot in this.GetSlotsInternal())
        {
            slot.EncodePointer();
        }
    }
}
