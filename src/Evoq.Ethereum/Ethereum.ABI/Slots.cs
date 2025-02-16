using System.Collections.Generic;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// A collection of slots.
/// </summary>
public class Slots : System.Collections.ObjectModel.Collection<Slot>
{
    //

    /// <summary>
    /// Initializes a new instance of the <see cref="Slots"/> class.
    /// </summary>
    /// <param name="capacity">The capacity of the slots.</param>
    public Slots(int capacity) : base(new List<Slot>(capacity))
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slots"/> class.
    /// </summary>
    /// <param name="slots">Some slots to add.</param>
    public Slots(IList<Slot> slots) : base(slots)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="Slots"/> class.
    /// </summary>
    /// <param name="slot">A slot to add.</param>
    public Slots(Slot slot) : base(new List<Slot> { slot })
    {
    }
}
