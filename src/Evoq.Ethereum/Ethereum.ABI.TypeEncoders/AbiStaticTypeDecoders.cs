using System.Collections.Generic;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// A collection of the built-in ABI type decoders for types that are not dynamic.
/// </summary>
public class AbiStaticTypeDecoders : System.Collections.ObjectModel.ReadOnlyCollection<IAbiDecode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiStaticTypeDecoders"/> class.
    /// </summary>
    public AbiStaticTypeDecoders()
        : base(new List<IAbiDecode>
        {
            new UintTypeEncoder(),
            new IntTypeEncoder(),
            new AddressTypeEncoder(),
            new BoolTypeEncoder(),
            new FixedBytesTypeEncoder()
        })
    {
    }
}
