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
    public Slots(int capacity) : base(new List<Slot>(capacity))
    {
    }
}
