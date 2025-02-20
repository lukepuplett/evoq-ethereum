using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// A new and improved ABI encoder that uses a more efficient encoding scheme.
/// </summary>
public class AbiEncoderV2 : IAbiEncoder
{
    //

    private readonly IReadOnlyList<IAbiEncode> staticTypeEncoders;
    private readonly IReadOnlyList<IAbiEncode> dynamicTypeEncoders;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoderV2"/> class.
    /// </summary>
    public AbiEncoderV2()
    {
        this.staticTypeEncoders = new AbiStaticTypeEncoders();
        this.dynamicTypeEncoders = new AbiDynamicTypeEncoders();

        this.Validator = new AbiTypeValidator(staticTypeEncoders, dynamicTypeEncoders);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoderV2"/> class.
    /// </summary>
    /// <param name="staticTypeEncoders">The static type encoders.</param>
    /// <param name="dynamicTypeEncoders">The dynamic type encoders.</param>
    public AbiEncoderV2(IReadOnlyList<IAbiEncode> staticTypeEncoders, IReadOnlyList<IAbiEncode> dynamicTypeEncoders)
    {
        this.staticTypeEncoders = staticTypeEncoders;
        this.dynamicTypeEncoders = dynamicTypeEncoders;

        this.Validator = new AbiTypeValidator(staticTypeEncoders, dynamicTypeEncoders);
    }

    //

    /// <summary>
    /// Gets the validator for the encoder.
    /// </summary>
    public AbiTypeValidator Validator { get; }

    //

    /// <summary>
    /// Encodes the parameters.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="values">The values to encode.</param>
    /// <returns>The encoded parameters.</returns>
    /// <exception cref="ArgumentException">Thrown if the number of values does not match the number of parameters.</exception>
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values)
    {
        var slotCount = parameters.Count;

        if (slotCount != values.Length)
        {
            throw new ArgumentException($"Expected {slotCount} values but got {values.Length}");
        }

        var rootParam = new EvmParam(0, "root", parameters);

        var root = new SlotCollection(capacity: slotCount * 8);

        this.EncodeValue(rootParam.AbiType, values, root);

        return new AbiEncodingResult(root);
    }

    // /// <summary>
    // /// Encodes the parameters.
    // /// </summary>
    // /// <param name="parameters">The parameters to encode.</param>
    // /// <param name="values">The values to encode.</param>
    // /// <returns>The encoded parameters.</returns>
    // /// <exception cref="ArgumentException">Thrown if the number of values does not match the number of parameters.</exception>
    // public AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values)
    // {
    //     var slotCount = parameters.Count;

    //     if (slotCount != values.Length)
    //     {
    //         throw new ArgumentException($"Expected {slotCount} values but got {values.Length}");
    //     }

    //     var gather = new SlotCollection(capacity: slotCount * 8);

    //     for (int i = 0; i < slotCount; i++)
    //     {
    //         var parameter = parameters[i];
    //         var value = values[i];

    //         var headAndTail = this.EncodeParameterValue(parameter, value);

    //         gather.AddRange(headAndTail);
    //     }

    //     //

    //     var result = new AbiEncodingResult(gather);

    //     return result;
    // }

    // privates

    // single slot value

    private void EncodeSingleSlotStaticValue(string abiType, object value, SlotCollection destination)
    {
        // e.g. bool or uint256, but not tuples like (bool,uint256)

        if (AbiTypes.IsTuple(abiType))
        {
            throw new ArgumentException($"The type {abiType} is a tuple, not a single slot static value");
        }

        if (!this.TryFindStaticSlotEncoder(abiType, value, out var encoder))
        {
            throw NotImplemented(abiType, value.GetType().ToString());
        }

        destination.Add(encoder!(value));
    }

    // single slot static iterables

    private void EncodeArrayOfSingleSlotValues(string abiType, object value, SlotCollection destination)
    {
        // e.g. bool[2][2] or uint256[2], but not tuples like (bool,uint256)[4]

        // all elements are directly encoded into the destination but no count of elements

        if (AbiTypes.IsTuple(abiType))
        {
            throw new ArgumentException($"The type {abiType} is a tuple, not an array");
        }

        if (!AbiTypes.TryRemoveOuterArrayDimension(abiType, out var innerType))
        {
            throw new ArgumentException($"The type {abiType} is not an array");
        }

        var array = value as Array;
        if (array == null)
        {
            throw new ArgumentException($"The value is not an array");
        }

        // encode each element directly into the destination

        for (int i = 0; i < array.Length; i++)
        {
            var element = array.GetValue(i);

            this.EncodeValue(innerType!, element, destination); // send back into the router
        }
    }

    private void EncodeArrayOfSingleSlotTuples(string abiType, object value, SlotCollection destination)
    {
        // e.g. (bool,uint256)[4] or (bool,uint256)[4][2], but not dynamic arrays of tuples like (bool,string)[4] or (bool,bool)[]

        // all elements are directly encoded into the destination with the count of elements
        // at the start of the destination

        if (AbiTypes.IsDynamic(abiType))
        {
            throw new ArgumentException($"The type {abiType} is not an static array of single slot tuples");
        }

        if (!AbiTypes.TryRemoveOuterArrayDimension(abiType, out var innerType))
        {
            throw new ArgumentException($"The type {abiType} is not a static array of single slot tuples");
        }

        var array = value as Array;
        if (array == null)
        {
            throw new ArgumentException($"The value is not an array");
        }

        // encode each element directly into the destination

        for (int i = 0; i < array.Length; i++)
        {
            var element = array.GetValue(i);

            this.EncodeSingleSlotStaticValue(innerType!, element, destination);
        }
    }

    private void EncodeTupleOfSingleSlotValues(string abiType, ITuple values, SlotCollection destination)
    {
        // e.g. (bool, uint256) or (bool, (uint256, uint256))

        // we simple iterate over the components

        // all components are directly encoded into the destination, one after the other

        // TODO / guard against wrong types

        var evmParams = EvmParameters.Parse(abiType);

        for (int i = 0; i < evmParams.Count; i++)
        {
            var parameter = evmParams[i];
            var value = values[i];

            this.EncodeValue(parameter.AbiType, value, destination); // send back into the router
        }
    }

    // dynamic value

    private void EncodeDynamicValue(string abiType, object value, SlotCollection heads, SlotCollection tail)
    {
        // e.g. string or bytes, but not tuples like (string,bytes) or arrays like string[] or bytes[]

        if (AbiTypes.IsTuple(abiType))
        {
            throw new ArgumentException($"The type {abiType} is a tuple, not a single slot dynamic value");
        }

        if (AbiTypes.IsArray(abiType))
        {
            throw new ArgumentException($"The type {abiType} is a tuple, not a single slot dynamic value");
        }

        if (!this.TryFindDynamicBytesEncoder(abiType, value, out var encoder))
        {
            throw NotImplemented(abiType, value.GetType().ToString());
        }

        // encode the value into the tail and add the offset pointer to the head

        var bytes = encoder!(value);
        var lengthSlot = new Slot(UintTypeEncoder.EncodeUint(256, bytes.Length));
        var dataSlots = this.BytesToSlots(bytes);

        var pointerSlot = new Slot(pointsToFirst: dataSlots, relativeTo: heads);

        heads.Add(pointerSlot);
        tail.Add(lengthSlot);
        tail.AddRange(dataSlots);
    }

    // dynamic iterables

    private void EncodeDynamicArray(string abiType, object value, SlotCollection head, SlotCollection tail)
    {
        // e.g. bool[][2] or uint256[][2] or string[]
    }

    private void EncodeDynamicTuple(string abiType, ITuple values, SlotCollection head, SlotCollection tail)
    {
        // e.g. (bool, string) or (bool, (uint256, string))

        var evmParams = EvmParameters.Parse(abiType);

        for (int i = 0; i < evmParams.Count; i++)
        {
            // per the second example tuple above, the first component is a bool

            var parameter = evmParams[i];
            var value = values[i];

            // we can encode single slot values directly into the head, else we need
            // to create a tail and add a pointer to its first slot

            if (AbiTypes.IsTuple(parameter.AbiType))
            {
                throw new NotImplementedException($"Dynamic tuple '{parameter.AbiType}' not yet supported");
            }
            else if (AbiTypes.IsArray(parameter.AbiType))
            {
                throw new NotImplementedException($"Dynamic array '{parameter.AbiType}' not yet supported");
            }
            else
            {

            }

            // this.EncodeValue(parameter.AbiType, value, head, tail);
        }
    }

    // router

    private void EncodeValue(string abiType, object value, SlotCollection destination)
    {
        // determine the type of the value and call the appropriate encoder

        // we need to know if the type is single slot or dynamic, and if it's iterable like an array or tuple

        // heads and tails!
        //
        // heads are slots for either the direct encoded value or a pointer to the first slot
        // of encoded data in the tail
        //
        // the tail (singular) is the collection of slots for encoded data where that data
        // is dynamic and has a variable length

        if (AbiTypes.IsDynamic(abiType))
        {
            // dynamic, encoded into the tail and add the offset pointer to the head

            // e.g. bytes, string, string[], bool[][2] or (string,uint256)[]

            if (AbiTypes.IsArray(abiType))
            {
                // e.g. bool[][2] or uint256[][2] or string[], or even (bool,uint256)[]

                // this.EncodeDynamicArray(abiType, value, head, tail);

                throw new NotImplementedException($"Dynamic array '{abiType}' not yet supported");
            }
            else if (AbiTypes.IsTuple(abiType))
            {
                // e.g. (bool, uint256) or (bool, (uint256, uint256))

                // this.EncodeDynamicTuple(evmParams, value, heads, tail);

                var heads = new SlotCollection(capacity: 8);
                var tails = new List<SlotCollection>();

                var evmParams = EvmParameters.Parse(abiType);
                var values = value as ITuple;

                for (int i = 0; i < evmParams.Count; i++)
                {
                    var componentParam = evmParams[i];
                    var componentType = componentParam.AbiType;
                    var element = values![i];

                    // we need a new tail for each dynamic component

                    if (AbiTypes.IsDynamic(componentType))
                    {
                        // we need a tail for this dynamic value
                        // and a pointer to the first slot of the tail

                        var tail = new SlotCollection(capacity: 8);
                        var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: heads);

                        heads.Add(pointerSlot);

                        this.EncodeValue(componentType, element, tail);

                        tails.Add(tail);
                    }
                    else
                    {
                        // write the value directly into the heads

                        this.EncodeValue(componentType, element, heads);
                    }
                }

                destination.AddRange(heads);
                destination.AddRange(tails.SelectMany(tail => tail)); // pour the all tail slots into the destination
            }
            else
            {
                // e.g. bytes, string

                // the returned heads will have the offset pointer to the tail and the tail will have the data

                var heads = new SlotCollection(capacity: 8);
                var tail = new SlotCollection(capacity: 8);

                this.EncodeDynamicValue(abiType, value, heads, tail);

                // not sure if this is correct; it might be that we need to write the heads into the previous tail
                // which would be the destination, and then hmmm.... what do we do with the tail?

                destination.AddRange(heads);
                destination.AddRange(tail); // ?
            }
        }
        else
        {
            // single slot, directly encoded into the destination

            // e.g. bool or uint256, bool[2] or uint256[2][2] or (bool,uint256), (bool,uint256)[2]

            if (AbiTypes.IsArray(abiType))
            {
                this.EncodeArrayOfSingleSlotValues(abiType, value, destination);
            }
            else if (AbiTypes.IsTuple(abiType))
            {
                // a tuple can only appear within another tuple, and a tuple is paired with a parameter

                if (value is ITuple tuple)
                {
                    this.EncodeTupleOfSingleSlotValues(abiType, tuple, destination);
                }
                else
                {
                    throw new ArgumentException($"The type {abiType} requires a tuple value in its parameter");
                }
            }
            else
            {
                // base case for single slot types like bool or uint256

                this.EncodeSingleSlotStaticValue(abiType, value, destination);
            }
        }
    }

    // (((uint256 eventId, uint8 eventType)[] entries, uint256 count) logs) - logs parameter of type tuple
    // ((uint256 eventId, uint8 eventType)[] entries, uint256 count)        - logs tuple with two components, entries and count
    // (uint256 eventId, uint8 eventType)[] entries                         - entries parameter of type array
    // (uint256 eventId, uint8 eventType)                                   - single entry tuple with two components, eventId and eventType         

    // old

    private SlotCollection EncodeParameterValueOld(EvmParam parameter, object value)
    {
        var abiType = parameter.AbiType;

        var heads = new SlotCollection(capacity: 8);
        var tail = new SlotCollection(capacity: 8);

        if (AbiTypes.IsDynamic(abiType))
        {
            // encode the value into the tail and add the offset pointer to the head

            if (AbiTypes.IsArray(abiType))
            {
                // a dynamic array; encode the length and encode each element's value
                // recursively where each element may be the value itself, or a pointer
                // to a collection of slots containing the element data

                // bool[]       - count of bools, then the bools themselves
                // string[]     - count of strings, then offsets to the slots containing the strings
                // bool[][2]    - 

                if (!AbiTypes.TryRemoveOuterArrayDimension(abiType, out var innerType))
                {
                    throw new ArgumentException(
                        $"Unable to remove outer array dimension from {abiType}");
                }

                var array = value as Array;
                if (array == null)
                {
                    throw new ArgumentException(
                        $"The parameter is of type '{abiType}' but the value is not an array");
                }

                // count the elements in the array

                var count = array.Length;
                var countBytes = UintTypeEncoder.EncodeUint(256, count);
                var countSlot = new Slot(countBytes);
                var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: heads);

                heads.Add(pointerSlot);
                tail.Add(countSlot);

                // encode each element's value into the tail






                if (AbiTypes.IsDynamic(innerType!))
                {
                    // has dynamic inner elements; we'll need pointers to the start of each element's data
                    // to the heads and then encode each element's value into the tail

                    throw new NotImplementedException("Dynamic arrays are not yet supported");
                }
                else
                {
                    // has static inner elements; we can encode each element's value directly into the head

                    for (int i = 0; i < count; i++)
                    {
                        var elementValue = array.GetValue(i);

                        if (!this.TryFindStaticSlotEncoder(innerType!, elementValue, out var encoder))
                        {
                            throw NotImplemented(innerType!, elementValue.GetType().ToString());
                        }

                        heads.Add(encoder!(elementValue));
                    }
                }
            }
            else if (!AbiTypes.IsTuple(abiType))
            {
                if (value is ITuple tuple)
                {
                    // a dynamic tuple; encode each component recursively

                    for (int i = 0; i < tuple.Length; i++)
                    {
                        var headAndTail = this.EncodeParameterValueOld(parameter.Components![i], tuple[i]);

                        var pointerSlot = new Slot(pointsToFirst: headAndTail, relativeTo: heads);

                        heads.Add(pointerSlot);
                        tail.AddRange(headAndTail);
                    }
                }
                else
                {
                    throw NotImplemented(abiType, value.GetType().ToString());
                }
            }
            else
            {
                // a dynamic value which is not an array, nor a tuple, so it's a string, bytes, etc.

                // encode the value into the data and the offset pointer to the head

                if (!this.TryFindDynamicBytesEncoder(abiType, value, out var encoder))
                {
                    throw NotImplemented(abiType, value.GetType().ToString());
                }

                var bytes = encoder!(value);
                var slots = this.BytesToSlots(bytes);

                var pointerSlot = new Slot(pointsToFirst: slots, relativeTo: heads);

                heads.Add(pointerSlot);
                tail.AddRange(slots);
            }
        }
        else
        {
            // type is not dynamic

            if (AbiTypes.IsArray(abiType))
            {
                // encode the elements directly into the heads starting with the count of
                // elements in the array

                // no need to encode the count; it's known from the type

                if (!AbiTypes.TryRemoveOuterArrayDimension(abiType, out var innerType))
                {
                    throw new ArgumentException(
                        $"Unable to remove outer array dimension from {abiType}");
                }

                var array = value as Array;
                if (array == null)
                {
                    throw new ArgumentException(
                        $"The parameter is of type '{abiType}' but the value is not an array");
                }

                for (int i = 0; i < array.Length; i++)
                {
                    var elementValue = array.GetValue(i);

                    if (!this.TryFindStaticSlotEncoder(innerType!, elementValue, out var encoder))
                    {
                        throw NotImplemented(innerType!, elementValue.GetType().ToString());
                    }

                    heads.Add(encoder!(elementValue));
                }
            }
            else if (!AbiTypes.IsTuple(abiType))
            {
                if (value is ITuple tuple)
                {
                    // encode each component recursively

                    // TODO: implement this

                    throw new NotImplementedException("Static tuples are not yet supported");
                }
                else
                {
                    throw NotImplemented(abiType, value.GetType().ToString());
                }
            }
            else
            {
                // encode the value directly into the head, including fixed size arrays

                if (!this.TryFindStaticSlotEncoder(abiType, value, out var encoder))
                {
                    throw NotImplemented(abiType, value.GetType().ToString());
                }

                heads.Add(encoder!(value));
            }
        }

        heads.AddRange(tail);
        return heads;
    }

    //

    private static Exception NotImplemented(string abiType, string clrType) =>
        new NotImplementedException($"Encoding for type {abiType} and value of type {clrType} not implemented");

    private bool TryFindStaticSlotEncoder(string abiType, object value, out Func<object, Slot>? encoder)
    {
        if (value == null)
        {
            encoder = _ => new Slot(new byte[32]); // null value is encoded as a 32-byte zero value
            return true;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {abiType}");
        }

        foreach (var staticEncoder in this.staticTypeEncoders)
        {
            if (staticEncoder.TryEncode(canonicalType, value, out var bytes))
            {
                encoder = _ => new Slot(bytes);
                return true;
            }
        }

        encoder = null;
        return false;
    }

    private bool TryFindDynamicBytesEncoder(string abiType, object value, out Func<object, byte[]>? encoder)
    {
        if (value == null)
        {
            encoder = _ => new byte[32]; // null value is encoded as a 32-byte zero value
            return true;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {abiType}");
        }

        foreach (var dynamicEncoder in this.dynamicTypeEncoders)
        {
            if (dynamicEncoder.TryEncode(canonicalType, value, out var bytes))
            {
                encoder = _ => bytes;
                return true;
            }
        }

        encoder = null;
        return false;
    }

    private SlotCollection BytesToSlots(byte[] bytes)
    {
        bool hasRemainingBytes = bytes.Length % 32 != 0;

        Debug.Assert(!hasRemainingBytes, "Has remaining bytes; bytes expected to be a multiple of 32");

        var slots = new SlotCollection(capacity: bytes.Length / 32 + (hasRemainingBytes ? 1 : 0));

        for (int i = 0; i < bytes.Length; i += 32)
        {
            var chunk = new byte[32];
            var count = Math.Min(32, bytes.Length - i);
            Buffer.BlockCopy(bytes, i, chunk, 0, count);

            slots.Add(new Slot(chunk));
        }

        return slots;
    }
}
