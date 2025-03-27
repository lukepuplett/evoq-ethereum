using System.Collections.Generic;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// A collection of the built-in ABI type encoders for types that are dynamic.
/// </summary>
public class AbiDynamicTypeEncoders : System.Collections.ObjectModel.ReadOnlyCollection<IAbiEncode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDynamicTypeEncoders"/> class.
    /// </summary>
    public AbiDynamicTypeEncoders()
        : base(new List<IAbiEncode>
        {
            new BytesTypeEncoder(),
            new StringTypeEncoder()
        })
    {
    }
}

/// <summary>
/// A collection of the built-in ABI type decoders for types that are dynamic.
/// </summary>
public class AbiDynamicTypeDecoders : System.Collections.ObjectModel.ReadOnlyCollection<IAbiDecode>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDynamicTypeDecoders"/> class.
    /// </summary>
    public AbiDynamicTypeDecoders()
        : base(new List<IAbiDecode>
        {
            new BytesTypeEncoder(),
            new StringTypeEncoder()
        })
    {
    }
}
