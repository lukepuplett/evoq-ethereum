using System;
using System.Collections.Generic;
using System.Linq;

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
    public int LastDynamicSlotIndex => staticSlotCount + dynamicData.Count();

    /// <summary>
    /// The dynamic offset which is the byte index of the next dynamic slot.
    /// </summary>
    public int NextDynamicOffsetIndex => this.LastDynamicSlotIndex * Slot.Size;

    //

    /// <summary>
    /// Gets the combined static and dynamic data as a byte array.
    /// </summary>
    /// <returns>The combined static and dynamic data as a byte array.</returns>
    public byte[] GetBytes()
    {
        if (this.staticData.Count() != this.staticSlotCount)
            throw new InvalidOperationException($"Expected {this.staticSlotCount} static slots but got {this.staticData.Count()}");

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
    public void AppendDynamic(Slots slots)
    {
        if (slots.Count == 0)
            throw new ArgumentException("Cannot add empty dynamic data");

        var offsetSlot = new Slot(AbiEncoder.EncodeUint(256, this.NextDynamicOffsetIndex), pointsToFirst: slots);
        this.AppendStatic(offsetSlot);

        this.dynamicData.Append(slots);
    }
}