using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// The result of encoding a set of parameters.
/// </summary>
public class AbiEncodingResult
{
    private readonly SlotSpace staticData = new();
    private readonly SlotSpace dynamicData = new();
    private readonly SlotCollection final = new(capacity: 8);

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    /// <param name="head">The head.</param>
    public AbiEncodingResult(SlotSpace head)
    {
        this.staticData = head;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    /// <param name="slots">The final slots.</param>
    /// <param name="skipFirstSlot">
    /// Whether to skip the first slot; the encoder creates a root tuple for all the level 0 parameters.
    /// </param>
    public AbiEncodingResult(SlotCollection slots, bool skipFirstSlot = true)
    {
        if (skipFirstSlot)
        {
            this.final = new SlotCollection(slots.Skip(1).ToList());
        }
        else
        {
            this.final = slots;
        }
    }

    // classic v0 constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    public AbiEncodingResult(int _ = 0) // TODO: remove this
    {
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

    //

    /// <summary>
    /// Gets the slots of the slot space.
    /// </summary>
    /// <returns>The slots of the slot space.</returns>
    public ISet<Slot> GetSlots()
    {
        this.UpdateOffsetsAndEncodePointers();

        return this.GetSlotsInternal().ToHashSet();
    }

    /// <summary>
    /// Gets the combined static and dynamic data as a byte array.
    /// </summary>
    /// <returns>The combined static and dynamic data as a byte array.</returns>
    public byte[] GetBytes()
    {
        this.UpdateOffsetsAndEncodePointers();

        return this.staticData.GetBytes().Concat(this.dynamicData.GetBytes()).ToArray();
    }

    //

    private IEnumerable<Slot> GetSlotsInternal()
    {
        foreach (var slot in this.final)
        {
            yield return slot;
        }

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
    /// Sets the byte offset on every slot.
    /// </summary>
    /// <remarks>
    /// Each slot is 32 bytes, so the second slot has offset 32, the third has offset 64, etc.
    /// This method must be called after all the slots have been appended to the slot space
    /// and the full bytes are read.
    /// </remarks>
    private void UpdateOffsetsAndEncodePointers()
    {
        int offset = 0;
        int order = 0;

        foreach (var slot in this.GetSlotsInternal())
        {
            slot.SetOrder(order);
            order++;

            slot.SetOffset(offset);
            offset += Slot.Size;
        }

        // now we can encode the pointers

        foreach (var slot in this.GetSlotsInternal())
        {
            slot.EncodePointer();           // TODO / remove
            slot.EncodePointerOffset();
        }
    }
}
