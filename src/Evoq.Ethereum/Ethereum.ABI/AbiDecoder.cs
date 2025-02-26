using System.Collections.Generic;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

record class DecodingContext() { }

public class AbiDecoder
{
    private readonly IReadOnlyList<IAbiDecode> staticTypeDecoders;
    private readonly IReadOnlyList<IAbiDecode> dynamicTypeDecoders;

    //

    public AbiDecoder()
    {
        // this.staticTypeDecoders = new AbiStaticTypeDecoders();
    }
}
