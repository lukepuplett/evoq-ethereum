using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
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
public class AbiDecoder : IAbiDecoder
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
        var allSlots = SlotCollection.FromBytes("slot", data);

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var slot = allSlots[i];

            if (parameter.IsDynamic)
            {
                // expect a pointer to the next slot, though we cannot check IsPointer on
                // the slot because the slot doesn't yet know, i.e. it has no PointsTo
                // information

                // the first pointer is relative to the root slots, and points to a slot that
                // contains the data for the parameter

                slot.DecodePointer(relativeTo: allSlots);

                // then what?

                this.DecodeDynamicParameter(parameter, slot, allSlots);
            }
            else
            {
                this.DecodeStaticParameter(parameter, slot, allSlots);
            }
        }

        return new AbiDecodingResult();
    }

    // routers

    private void DecodeStaticParameter(AbiParam parameter, Slot valueSlot, SlotCollection allSlots)
    {
        // check for array first, because it may be an array of tuples

        if (parameter.IsArray)
        {
            // array of static, fixed-length values in sequential slots

            DecodeStaticArray(parameter, valueSlot, allSlots);
        }
        else if (parameter.IsTuple)
        {
            // tuple of static, fixed-length values in sequential slots

            DecodeStaticTuple(parameter, valueSlot, allSlots);
        }
        else
        {
            // static value

            DecodeSingleSlotStaticValue(parameter, valueSlot);
        }
    }

    private void DecodeDynamicParameter(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        // check for array first, because it may be an array of tuples

        if (parameter.IsArray)
        {
            // slot points to data

            DecodeDynamicArray(parameter, pointer, allSlots);
        }
        else if (parameter.IsTuple)
        {
            // slot points to data

            DecodeDynamicTuple(parameter, pointer, allSlots);
        }
        else
        {
            // slot points to data

            DecodeDynamicValue(parameter, pointer, allSlots);
        }
    }

    //

    private void DecodeSingleSlotStaticValue(AbiParam parameter, Slot valueSlot)
    {
        // just decode the value

        parameter.Value = this.DecodeStaticSlot(parameter.AbiType, parameter.ClrType, valueSlot);
    }

    private void DecodeStaticArray(AbiParam parameter, Slot firstValueSlot, SlotCollection allSlots)
    {
        if (!AbiTypes.TryGetArrayDimensions(parameter.AbiType, out var dimensions))
        {
            throw new InvalidOperationException($"Failed to get dimensions for {parameter.AbiType}");
        }

        if (!AbiTypes.TryGetArrayBaseType(parameter.AbiType, out var baseType))
        {
            throw new InvalidOperationException($"Failed to get base type for {parameter.AbiType}");
        }

        if (!AbiTypes.TryGetArrayMultiLength(parameter.AbiType, out var multiLength))
        {
            throw new InvalidOperationException($"Failed to get length for {parameter.AbiType}");
        }

        if (!parameter.ClrType.IsArray)
        {
            throw new InvalidOperationException($"Expected array type, got {parameter.ClrType}");
        }

        // for static arrays, all the values are in contiguous slots, so we can just decode them
        // into the array, or array of arrays, etc.

        // define recursive function to decode the array (see note, top)

        (Array Array, int Skip) getArray(string abiType, Type clrType, int skip, IList<object?> values)
        {
            if (!AbiTypes.TryGetArrayOuterLength(abiType, out var length))
            {
                throw new InvalidOperationException($"Failed to get length for {abiType}");
            }

            if (AbiTypes.TryGetArrayInnerType(abiType, out var innerType) &&
                AbiTypes.IsArray(innerType!))
            {
                // inner type is itself an array, so we create an array to hold arrays of that type

                var arrayOfArrays = Array.CreateInstance(clrType.GetElementType(), length);

                for (int i = 0; i < length; i++)
                {
                    var inner = getArray(innerType!, clrType.GetElementType(), skip, values); // recurse

                    skip = inner.Skip;

                    arrayOfArrays.SetValue(inner.Array, i);
                }

                return (arrayOfArrays, skip);
            }
            else if (AbiTypes.IsTuple(innerType!))
            {
                throw new NotImplementedException("Static array of tuples not implemented");
            }
            else
            {
                // inner type is a non-array type, so we create an array of that type
                // and fill it with the non-array values

                var array = Array.CreateInstance(clrType.GetElementType(), length);

                // fill the array

                int c = 0;
                foreach (var value in values.Skip(skip).Take(length))
                {
                    array.SetValue(value, c++);
                }

                // return the filled array and a skip value that will be used by the recursive caller
                // above to skip over the array values that have already been decoded

                return (array, skip + length);
            }
        }

        // the first slot should be the first element and all the values are contiguous

        var subSlots = allSlots.SkipTo(firstValueSlot).Take(multiLength).ToList();
        List<object?> subValues;

        if (parameter.IsTuple)
        {
            // array of static tuples, these are also laid out in contiguous slots

            subValues = new();

            throw new NotImplementedException("Static array of tuples not implemented");
        }
        else
        {
            // array of single slot values, laid out contiguously, so we can just decode them
            // all into a list

            subValues = subSlots
                .Select(s => this.DecodeStaticSlot(baseType!, parameter.ClrType.GetBaseElementType(), s))
                .ToList();
        }

        var (array, _) = getArray(parameter.AbiType, parameter.ClrType, 0, subValues);

        parameter.Value = array;
    }

    private void DecodeStaticTuple(AbiParam parameter, Slot firstValueSlot, SlotCollection allSlots)
    {
        // all values are in contiguous slots, including any in inner static tuples
        // so we can just iterate over the slots and stick them in their parameters

        // however, static arrays are also laid out in contiguous slots, so once we've read an array
        // we need to fast forward through its slots to catch up to where we are in the tuple

        int skip = 0;
        var valueSlot = firstValueSlot;
        parameter.DeepVisit((param) =>
        {
            if (skip == 0) // when this is positive, we are skipping over an array
            {
                if (param.IsArray)
                {
                    this.DecodeStaticArray(param, valueSlot, allSlots);

                    // fast forward through the array slots to catch up to where we are in the tuple

                    if (!AbiTypes.TryGetArrayMultiLength(param.AbiType, out var multiLength))
                    {
                        throw new InvalidOperationException($"Failed to get length for {param.AbiType}");
                    }

                    skip = multiLength;
                }
                else if (param.IsTuple)
                {
                    // ignore the outer tuple param; its inner params will come around next time
                }
                else
                {
                    param.Value = this.DecodeStaticSlot(param.AbiType, param.ClrType, valueSlot);
                }
            }
            else
            {
                skip--;
            }

            // try to advance to the next slot

            if (allSlots.TrySkipPast(valueSlot, out var beyond))
            {
                valueSlot = beyond.First();
            }
        });

        // ^ if this deep visit idea fails, then we can check IsTuple and recurse DecodeStaticTuple
    }

    // dyn

    private void DecodeDynamicValue(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        // a dynamic value has its pointer and then the length and then the data slots, e.g. string, bytes, etc.

        parameter.Value = this.DecodeDynamicSlot(parameter.AbiType, parameter.ClrType, pointer, allSlots);
    }

    private void DecodeDynamicArray(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        // for variable length arrays the length is in the first slot, else it's in the ABI type

        if (!AbiTypes.TryGetArrayOuterLength(parameter.AbiType, out var length))
        {
            throw new InvalidOperationException($"Failed to get length for {parameter.AbiType}");
        }

        if (!AbiTypes.TryGetArrayDimensions(parameter.AbiType, out var dimensions))
        {
            throw new InvalidOperationException($"Failed to get dimensions for {parameter.AbiType}");
        }

        if (!AbiTypes.TryGetArrayBaseType(parameter.AbiType, out var baseType))
        {
            throw new InvalidOperationException($"Failed to get base type for {parameter.AbiType}");
        }

        if (!AbiTypes.TryGetArrayInnerType(parameter.AbiType, out var innerType))
        {
            throw new InvalidOperationException($"Failed to get inner type for {parameter.AbiType}");
        }

        var isDynamicLength = length == -1;
        var isDynamicBase = AbiTypes.IsDynamic(baseType!);
        var baseClrType = parameter.ClrType.GetBaseElementType();

        var subSlots = allSlots.SkipToPoint(pointer);
        SlotCollection dataSlots = subSlots;

        if (isDynamicLength)
        {
            var lengthSlot = subSlots[0];
            length = this.DecodeLength(lengthSlot);

            dataSlots = subSlots.SkipPast(lengthSlot);
        }

        // we have the length and the data slots

        // reminder of examples; uint8[], string[2][4], uint8[2][][4], uint8[][3], bytes[], (bool, bool)[], (bool, bool)[][2]

        // there's always a slot per element, be it a static value or a pointer to the next level of dynamic data

        // if the inner type is dynamic, because it either has a [] or is a string, bytes, or dynamic tuple
        // then it has a pointer        

        if (dimensions!.Count > 1)
        {
            // e.g. string[2][4], uint8[2][][4], uint8[][3], (bool, bool)[][2]

            // outer array, so create an array, then fill it with its decoded (array) elements

            var array = Array.CreateInstance(parameter.ClrType.GetElementType(), length);
            // var data = dataSlots.GetEnumerator();

            for (int i = 0; i < length; i++)
            {
                // var s = data.Current;
                var s = dataSlots[i];

                if (isDynamicBase || isDynamicLength)
                {
                    s.DecodePointer(dataSlots);

                    var p = new AbiParam(0, $"{parameter.Name}.stunt", innerType!, components: parameter.Components);
                    this.DecodeDynamicArray(p, s, allSlots); // recurse

                    array.SetValue(p.Value, i); // stick the value from our stunt parameter into the array
                }
                else
                {
                    var p = new AbiParam(0, $"{parameter.Name}.stunt", innerType!, components: parameter.Components);
                    this.DecodeStaticArray(p, s, allSlots); // recurse

                    array.SetValue(p.Value, i);
                }

                // if (!data.MoveNext())
                // {
                //     throw new ArgumentException("Unexpected end of data", nameof(allSlots));
                // }
            }

            parameter.Value = array;
        }
        else
        {
            // e.g. uint8[], bytes[], (bool, bool)[]

            // innermost array, so create the array and fill it with its decoded elements

            var array = Array.CreateInstance(baseClrType, length);
            // var data = dataSlots.GetEnumerator();

            for (int i = 0; i < length; i++)
            {
                // decode the elements into the array

                // var s = data.Current;
                var s = dataSlots[i];

                if (isDynamicBase)
                {
                    s.DecodePointer(dataSlots);

                    if (parameter.IsTuple)
                    {
                        // make a stunt parameter for the tuple and decode into it

                        var p = new AbiParam(0, $"{parameter.Name}.stunt", baseType!, components: parameter.Components);
                        this.DecodeDynamicTuple(p, s, allSlots);

                        array.SetValue(p.Value, i);
                    }
                    else
                    {
                        var obj = this.DecodeDynamicSlot(baseType!, baseClrType, s, allSlots);
                        array.SetValue(obj, i);
                    }
                }
                else
                {
                    if (parameter.IsTuple)
                    {
                        // make a stunt parameter for the tuple and decode into it

                        var p = new AbiParam(0, $"{parameter.Name}.stunt", baseType!, components: parameter.Components);
                        this.DecodeStaticTuple(p, s, allSlots);

                        array.SetValue(p.Value, i);
                    }
                    else
                    {
                        var obj = this.DecodeStaticSlot(baseType!, baseClrType, s);
                        array.SetValue(obj, i);
                    }
                }

                // if (data.MoveNext())
                // {
                //     throw new ArgumentException("Unexpected end of data", nameof(allSlots));
                // }
            }

            parameter.Value = array;
        }
    }

    private void DecodeDynamicTuple(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        if (!parameter.IsTuple)
        {
            throw new InvalidOperationException($"Expected tuple type, got {parameter.AbiType}");
        }

        if (!parameter.IsDynamic)
        {
            throw new InvalidOperationException($"Expected dynamic tuple, got {parameter.AbiType}");
        }

        // fixed length, size information in the tuple, read forward through the slots
        // which will either be static values or pointers to the next level of dynamic data

        int length = parameter.Components!.Count;
        var tupleSlots = allSlots.SkipToPoint(pointer);

        for (int i = 0; i < length; i++)
        {
            var component = parameter.Components[i];
            var s = tupleSlots[i];

            if (component.IsDynamic)
            {
                s.DecodePointer(tupleSlots);

                this.DecodeDynamicParameter(component, s, allSlots);
            }
            else
            {
                this.DecodeStaticParameter(component, s, allSlots);
            }
        }
    }

    //

    private object? DecodeString(string abiType, Type clrType, IList<Slot> slots, int length)
    {
        if (length == 0)
        {
            return string.Empty;
        }

        var bytes = SlotsToBytes(slots, length);

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {abiType}");
        }

        foreach (var decoder in this.dynamicTypeDecoders)
        {
            if (decoder.TryDecode(canonicalType, bytes, clrType, out var value))
            {
                return value;
            }
        }

        throw new InvalidOperationException($"No decoder found for ABI type {abiType} -> CLR type {clrType}");
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

        throw new InvalidOperationException($"No decoder found for ABI type {abiType} -> CLR type {clrType}");
    }

    private object? DecodeDynamicSlot(string abiType, Type clrType, Slot pointer, SlotCollection allSlots)
    {
        // a dynamic value has its pointer and then the length and then the data slots, e.g. string, bytes, etc.

        var subSlots = allSlots.SkipToPoint(pointer);
        var lengthSlot = subSlots[0];
        int length = this.DecodeLength(lengthSlot);
        var dataSlots = subSlots.Skip(1).Take(length).ToList();

        //

        if (abiType == AbiTypeNames.Bytes)
        {
            return SlotsToBytes(dataSlots, length);
        }
        else if (abiType == AbiTypeNames.String)
        {
            return this.DecodeString(abiType, clrType, dataSlots, length);
        }
        else
        {
            throw new InvalidOperationException($"Unsupported dynamic type {abiType}");
        }
    }

    private int DecodeLength(Slot lengthSlot)
    {
        var bigLength = (BigInteger)this.DecodeStaticSlot(AbiTypeNames.IntegerTypes.Uint, typeof(BigInteger), lengthSlot)!;
        var length = (int)bigLength;
        return length;
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

