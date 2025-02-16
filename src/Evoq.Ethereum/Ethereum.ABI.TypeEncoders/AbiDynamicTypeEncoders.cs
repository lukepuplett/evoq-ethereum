using System.Collections.Generic;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// A collection of the built-in ABI type encoders for types that are dynamic.
/// </summary>
public class AbiDynamicTypeEncoders : System.Collections.ObjectModel.ReadOnlyCollection<IAbiTypeEncoder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDynamicTypeEncoders"/> class.
    /// </summary>
    public AbiDynamicTypeEncoders()
        : base(new List<IAbiTypeEncoder>
        {
            new StringTypeEncoder(),
            new BytesTypeEncoder()
        })
    {
    }
}
