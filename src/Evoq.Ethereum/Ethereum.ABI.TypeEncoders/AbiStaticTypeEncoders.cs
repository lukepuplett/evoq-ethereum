using System.Collections.Generic;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// A collection of the built-in ABI type encoders for types that are not dynamic.
/// </summary>
public class AbiStaticTypeEncoders : System.Collections.ObjectModel.ReadOnlyCollection<IAbiEncode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiStaticTypeEncoders"/> class.
    /// </summary>
    public AbiStaticTypeEncoders()
        : base(new List<IAbiEncode>
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
