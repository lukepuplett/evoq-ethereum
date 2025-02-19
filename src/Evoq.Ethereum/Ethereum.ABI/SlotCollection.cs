using System.Collections.Generic;

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
}
