using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// The result of encoding a set of parameters.
/// </summary>
public class AbiEncodingResult
{
    private readonly SlotCollection final = new(capacity: 8);

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    /// <param name="slots">The final slots.</param>
    public AbiEncodingResult(SlotCollection slots)
    {
        this.final = slots;
    }

    //

    /// <summary>
    /// Gets the number of slots in the encoding result.
    /// </summary>
    public int Count => this.final.Count;

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

        return this.final.SelectMany(slot => slot.Data).ToArray();
    }

    //

    private IEnumerable<Slot> GetSlotsInternal()
    {
        foreach (var slot in this.final)
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
