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
    /// <param name="staticSlots">The static slots.</param>
    public AbiEncodingResult(SlotCollection staticSlots)
    {
        this.staticData = new SlotSpace(staticSlots);
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
        this.SetOffsets();

        return this.GetSlotsInternal().ToHashSet();
    }

    /// <summary>
    /// Gets the combined static and dynamic data as a byte array.
    /// </summary>
    /// <returns>The combined static and dynamic data as a byte array.</returns>
    public byte[] GetBytes()
    {
        this.SetOffsets();

        return this.staticData.GetBytes().Concat(this.dynamicData.GetBytes()).ToArray();
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
