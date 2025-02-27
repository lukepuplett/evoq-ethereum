using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/*

// recursive reader

- Use the ABI to guide the reader via EvmParam graph.
- Read all slots into a collection.
- The modes of reading, determinable by the ABI type information on the EvmParam, are:

	• Static value, dynamic value (ptr), static tuple, static array, dynamic tuple (ptr), dynamic array (ptr)
	• From binary into the EvmParamValue via TryFindStaticSlotDecoder(…).
	
*/

record class DecodingContext() { }

/// <summary>
/// A new and improved ABI decoder that uses a more efficient decoding scheme.
/// </summary>
public class AbiDecoder
{
    private readonly IReadOnlyList<IAbiDecode> staticTypeDecoders;
    private readonly IReadOnlyList<IAbiDecode> dynamicTypeDecoders;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDecoder"/> class.
    /// </summary>
    public AbiDecoder()
    {
        this.staticTypeDecoders = new AbiStaticTypeDecoders();
        this.dynamicTypeDecoders = new AbiDynamicTypeDecoders();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDecoder"/> class.
    /// </summary>
    /// <param name="staticTypeDecoders">The static type decoders.</param>
    /// <param name="dynamicTypeDecoders">The dynamic type decoders.</param>
    public AbiDecoder(IReadOnlyList<IAbiDecode> staticTypeDecoders, IReadOnlyList<IAbiDecode> dynamicTypeDecoders)
    {
        this.staticTypeDecoders = staticTypeDecoders;
        this.dynamicTypeDecoders = dynamicTypeDecoders;
    }

    //

    /// <summary>
    /// Decodes the parameters.
    /// </summary>
    /// <param name="parameters">The parameters to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded parameters.</returns>
    public AbiDecodingResult DecodeParameters(AbiParameters parameters, byte[] data)
    {
        var slots = SlotCollection.FromBytes("slot", data);

        // iterate over the parameters to work out which slots are parameters

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var slot = slots[i];

            if (parameter.IsDynamic)
            {
                // expect a pointer to the next slot, though we cannot check IsPointer on
                // the slot because the slot doesn't yet know, i.e. it has no PointsTo
                // information

                /*

                The Slots are read with order and offsets set.

                TODO / set the PointsTo and RelativeTo information on the slots. These are
                SlotCollection instances, so we need to work out which slots can be placed
                into some new collections for these. This will depend on information from
                the current parameter, i.e. whether it is dynamic etc.

                */

                // the first pointer is relative to the root slots, and points to a slot that
                // contains the data for the parameter

                slot.DecodePointer(relativeTo: slots);
            }
        }

        throw new NotImplementedException();
    }

    //

    private void DecodeSingleSlotStaticValue(AbiParam parameter, Slot slot)
    {
        // just decode the value


    }

    private void DecodeStaticParameter(AbiParam parameter, Slot slot)
    {
        if (parameter.IsArray)
        {
            // TODO
        }
        else if (parameter.IsTuple)
        {
            // TODO
        }
        else
        {
            // TODO
        }
    }
    private void DecodeDynamicParameter(AbiParam parameter, Slot slot)
    {

    }

    //

    private bool TryFindStaticSlotDecoder(string abiType, Slot slot, out Func<object?>? decoder)
    {
        if (slot.IsNull)
        {
            decoder = () => null;
            return true;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {abiType}");
        }

        // foreach (var staticDecoder in this.staticTypeDecoders)
        // {
        //     if (staticDecoder.TryDecode(canonicalType, slot.Data, parameter.ClrType, out var value))
        //     {
        //         decoder = () => value;
        //         return true;
        //     }
        // }

        decoder = null;
        return false;
    }

    private static SlotCollection BytesToSlots(EncodingContext context, byte[] paddedBytes)
    {
        return SlotCollection.FromBytes(Name(context, "chunk"), paddedBytes);
    }

    private static string Name(EncodingContext context, string name)
    {
        return $"{context}.{name}";
    }
}

