using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/*

// recursive reader

- Use the ABI to guide the reader via EvmParam graph.
- Read all slots into a collection.
- The modes of reading, determinable by the ABI type information on the EvmParam, are:

	• Static value, dynamic value (ptr), static tuple, static array, dynamic tuple (ptr), dynamic array (ptr)
	• From binary into the EvmParamValue via TryFindStaticSlotDecoder(…).
	
// reading a static array

! Problem - This requires C# code like `byte[a][b][c] = value;` which is problematic because it would mean
            writing a switch for all possible CLR types, and all the possible dimensions.

- Consider an alternative approach where we read directly into the array's structures by rolling the indexes
- If we have an array of uint8[2][3][4] then it has 2*3*4 = 24 elements
- Its inner has 3*4 = 12 elements
- And its innermost has 4 elements
- We can create an array of 3 counters, or write positions, one for each array dimension
- We can start at 0,0,0 and increment the first counter reaches >3 then reset it to 0 and increment the next counter
- We continue until the last counter has reached its limit
- This reads direct and avoids complex nesting

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

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var slot = slots[i];

            if (parameter.IsDynamic)
            {
                // expect a pointer to the next slot, though we cannot check IsPointer on
                // the slot because the slot doesn't yet know, i.e. it has no PointsTo
                // information

                // the first pointer is relative to the root slots, and points to a slot that
                // contains the data for the parameter

                slot.DecodePointer(relativeTo: slots);
            }
        }

        throw new NotImplementedException();
    }

    //

    private void DecodeStaticParameter(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        if (parameter.IsArray)
        {
            // static array

            DecodeStaticArray(parameter, slot, slots);
        }
        else if (parameter.IsTuple)
        {
            // static tuple

            DecodeStaticTuple(parameter, slot, slots);
        }
        else
        {
            // static value

            DecodeSingleSlotStaticValue(parameter, slot);
        }
    }

    private void DecodeSingleSlotStaticValue(AbiParam parameter, Slot slot)
    {
        // just decode the value

        parameter.Value = this.DecodeStaticSlot(parameter.AbiType, parameter.ClrType, slot);
    }

    private void DecodeStaticArray(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        if (!AbiTypes.TryGetArrayDimensions(parameter.AbiType, out var dimensions))
        {
            throw new InvalidOperationException($"Failed to get dimensions for {parameter.AbiType}");
        }

        if (!AbiTypes.TryGetArrayMultiLength(parameter.AbiType, out var multiLength))
        {
            throw new InvalidOperationException($"Failed to get length for {parameter.AbiType}");
        }

        if (!parameter.ClrType.IsArray)
        {
            throw new InvalidOperationException($"Expected array type, got {parameter.ClrType}");
        }

        // define function to decode the array (see note, top)

        (Array Array, int Skip) getArray(string abiType, Type clrType, int skip, IList<object?> values)
        {
            if (!AbiTypes.TryGetOuterArrayLength(abiType, out var length))
            {
                throw new InvalidOperationException($"Failed to get length for {abiType}");
            }

            if (AbiTypes.TryGetArrayInnerType(abiType, out var innerType) &&
                AbiTypes.IsArray(innerType!))
            {
                var array = Array.CreateInstance(clrType.GetElementType(), length);

                for (int i = 0; i < length; i++)
                {
                    var inner = getArray(innerType!, clrType.GetElementType(), skip, values);

                    skip = inner.Skip;

                    array.SetValue(inner.Array, i);
                }

                return (array, skip);
            }
            else
            {
                var array = Array.CreateInstance(clrType, length);

                // fill the array

                int c = 0;
                foreach (var value in values.Skip(skip).Take(length))
                {
                    array.SetValue(value, c++);
                }

                // return the array

                return (array, skip + length);
            }
        }

        // the first slot should be the first element

        var subSlots = slots.Skip(slot).Take(multiLength).ToList();
        var subValues = subSlots.Select(s => this.DecodeStaticSlot(parameter.AbiType, parameter.ClrType.GetBaseElementType(), s)).ToList();

        var (array, _) = getArray(parameter.AbiType, parameter.ClrType, 0, subValues);

        parameter.Value = array;
    }

    private void DecodeStaticTuple(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        // all values are in contiguous slots, so we can just stick them in their parameters

        int c = 0;
        parameter.DeepVisit(_ => c++);

        var subSlots = slots.Skip(slot).Take(c).ToList();

        c = 0;
        parameter.DeepVisit((param) =>
        {
            var subSlot = slots.Skip(slot).First();
            if (param.IsArray)
            {
                DecodeStaticArray(param, subSlot, slots);
            }
            else
            {
                param.Value = this.DecodeStaticSlot(param.AbiType, param.ClrType, subSlot);
            }
        });

        // ^ if this deep visit idea fails, then we can check IsTuple and recurse DecodeStaticTuple
    }

    private void DecodeDynamicParameter(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        if (parameter.IsArray)
        {
            DecodeDynamicArray(parameter, slot, slots);
        }
        else if (parameter.IsTuple)
        {
            DecodeDynamicTuple(parameter, slot, slots);
        }
        else
        {
            DecodeDynamicValue(parameter, slot, slots);
        }
    }

    private void DecodeDynamicValue(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        // a dynamic value has its pointer and then the length and then the data slots

        var subSlots = slots.Skip(slot).ToList();
        var lengthSlot = subSlots[0];
        var length = (int)this.DecodeStaticSlot(AbiTypeNames.IntegerTypes.Uint, typeof(int), lengthSlot)!;
        var dataSlots = subSlots.Skip(1).Take(length).ToList();

        //

        if (parameter.AbiType == AbiTypeNames.Bytes)
        {
            parameter.Value = SlotsToBytes(dataSlots, length);
        }
        else if (parameter.AbiType == AbiTypeNames.String)
        {
            parameter.Value = this.DecodeString(parameter.AbiType, parameter.ClrType, dataSlots, length);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported dynamic type {parameter.AbiType}");
        }
    }

    private void DecodeDynamicArray(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        throw new NotImplementedException();
    }

    private void DecodeDynamicTuple(AbiParam parameter, Slot slot, SlotCollection slots)
    {
        throw new NotImplementedException();
    }

    //

    private object? DecodeString(string abiType, Type clrType, IList<Slot> slots, int length)
    {
        if (length == 0)
        {
            return string.Empty;
        }

        var bytes = SlotsToBytes(slots, length);

        foreach (var decoder in this.dynamicTypeDecoders)
        {
            if (decoder.TryDecode(abiType, bytes, clrType, out var value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Failed to decode {abiType} -> CLR type {clrType}");
    }

    private object? DecodeStaticSlot(string abiType, Type clrType, Slot slot)
    {
        if (slot.IsNull)
        {
            return null;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {abiType}");
        }

        foreach (var staticDecoder in this.staticTypeDecoders)
        {
            if (staticDecoder.TryDecode(canonicalType, slot.Data, clrType, out var value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"Failed to decode ABI type {abiType} -> CLR type {clrType}");
    }

    private static byte[] SlotsToBytes(IList<Slot> slots, int length)
    {
        bool hasRemainder = (length % 32 != 0);
        int slotsLength = length / 32 + (hasRemainder ? 1 : 0);
        var bytes = new byte[length];

        for (int i = 0; i < slotsLength; i++)
        {
            int bytesToCopy = (i == slotsLength - 1 && hasRemainder) ? length % 32 : 32;

            Array.Copy(slots[i].Data, 0, bytes, i * 32, bytesToCopy);
        }

        return bytes;
    }

    private static string Name(EncodingContext context, string name)
    {
        return $"{context}.{name}";
    }
}

