using System.Collections.Generic;
using System.Linq;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// The result of encoding a set of parameters.
/// </summary>
public class AbiEncodingResult
{
    private readonly SlotCollection? slotCollection;
    private readonly byte[]? bytes;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    /// <param name="slots">The final slots.</param>
    internal AbiEncodingResult(SlotCollection slots)
    {
        this.slotCollection = slots;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncodingResult"/> class.
    /// </summary>
    /// <param name="bytes">The final bytes.</param>
    internal AbiEncodingResult(byte[] bytes)
    {
        this.bytes = bytes;
    }

    //

    /// <summary>
    /// Gets the combined static and dynamic data as a byte array.
    /// </summary>
    /// <returns>The combined static and dynamic data as a byte array.</returns>
    public byte[] ToByteArray()
    {
        if (this.TryGetSlots(out var slots))
        {
            return slots.SelectMany(slot => slot.Data).ToArray();
        }

        return this.bytes!;
    }

    /// <summary>
    /// Gets the hex representation of the encoding result.
    /// </summary>
    /// <returns>The hex representation of the encoding result.</returns>
    public Hex ToHexStruct()
    {
        return new Hex(this.ToByteArray());
    }

    /// <summary>
    /// Gets the slots.
    /// </summary>
    /// <returns>The slots.</returns>
    internal bool TryGetSlots(out IReadOnlyList<Slot>? slots)
    {
        if (this.slotCollection == null)
        {
            slots = null;
            return false;
        }

        this.UpdateOffsetsAndEncodePointers();

        slots = this.slotCollection.OrderBy(slot => slot.Order).ToList();
        return true;
    }

    //

    /// <summary>
    /// Sets the byte offset on every slot.
    /// </summary>
    /// <remarks>
    /// This method must be called after all the slots have been appended to the slot space
    /// and the full bytes are read.
    /// </remarks>
    private void UpdateOffsetsAndEncodePointers()
    {
        if (this.slotCollection == null)
        {
            return;
        }

        int offset = 0;
        int order = 0;

        foreach (var slot in this.slotCollection)
        {
            slot.SetOrder(order);
            order++;

            slot.SetOffset(offset);
            offset += Slot.Size;
        }

        // now we can encode the pointers

        foreach (var slot in this.slotCollection)
        {
            slot.EncodePointer();
            slot.EncodePointerOffset();
        }
    }
}
