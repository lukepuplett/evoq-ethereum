using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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

        var root = new SlotCollection(capacity: slotCount * 8);

        string type;
        type = parameters.GetCanonicalType();
        type = new EvmParam(0, "root", parameters).AbiType;

        this.EncodeValue(type, values, root);       // consider using canonical type

        // we treat the root parameter as a tuple, which means that if it's dynamic,
        // the first slot is a pointer to the first slot of the dynamic data, and
        // that's probably not what we want, so we skip the first slot if that's the case

        var isDynamicTupleRoot = AbiTypes.IsTuple(type) && AbiTypes.IsDynamic(type);

        return new AbiEncodingResult(root, skipFirstSlot: isDynamicTupleRoot);
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

    private void EncodeDynamicValue(string abiType, object value, SlotCollection destination)
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

        byte[] bytes;

        if (value is byte[] bytesValue)
        {
            bytes = bytesValue;
        }
        else if (value is string stringValue)
        {
            bytes = Encoding.UTF8.GetBytes(stringValue);
        }
        else
        {
            throw new ArgumentException($"The value of type {value.GetType()} must be a byte array or string");
        }

        // encode the value into the tail and add the offset pointer to the head

        var lengthSlot = new Slot(UintTypeEncoder.EncodeUint(256, bytes.Length));
        var bytesSlots = this.BytesToSlots(bytes);

        var heads = new SlotCollection(capacity: 1);
        var tail = new SlotCollection(capacity: bytesSlots.Count);

        var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: heads);

        heads.Add(pointerSlot);
        tail.Add(lengthSlot);
        tail.AddRange(bytesSlots);

        // the pour those two into the destination

        destination.AddRange(heads);
        destination.AddRange(tail);
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
                // e.g. uint8[] or bool[][2] or uint256[][2] or string[], or even (bool,uint256)[]

                // this.EncodeDynamicArray(abiType, value, head, tail);

                // similar to the tuple case, except the type is always the same

                var heads = new SlotCollection(capacity: 8); // heads for this array
                var tails = new List<SlotCollection>();      // tails for each array element's value

                if (value is not Array array)
                {
                    throw new ArgumentException($"The type {abiType} requires an array value");
                }

                if (!AbiTypes.TryRemoveOuterArrayDimension(abiType, out var innerType))
                {
                    throw new NotImplementedException($"The type '{abiType}' is not an array");
                }

                if (!AbiTypes.TryGetArrayDimensions(abiType, out var dimensions))
                {
                    throw new ArgumentException($"The type {abiType} is not an array");
                }

                if (dimensions.First() == -1)
                {
                    // T[] dynamic outer needs a single pointer in the heads pointing to its
                    // element data tail, which starts with a count

                    var tail = new SlotCollection(capacity: 8);
                    tails.Add(tail);

                    var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: heads);
                    heads.Add(pointerSlot);

                    var countSlot = new Slot(UintTypeEncoder.EncodeUint(256, array.Length));
                    tail.Add(countSlot);

                    for (int i = 0; i < array.Length; i++)
                    {
                        var element = array.GetValue(i);

                        this.EncodeValue(innerType!, element, tail);

                    }
                }
                else
                {
                    // T[k] fixed outer needs no count and each element has its own tail with
                    // a pointer to it added to the heads

                    for (int i = 0; i < array.Length; i++)
                    {
                        var tail = new SlotCollection(capacity: 8);
                        tails.Add(tail);

                        var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: heads);
                        heads.Add(pointerSlot);

                        var element = array.GetValue(i);

                        this.EncodeValue(innerType!, element, tail);
                    }
                }

                // pour the heads and tails into the destination

                destination.AddRange(heads);
                destination.AddRange(tails.SelectMany(tail => tail));
            }
            else if (AbiTypes.IsTuple(abiType))
            {
                // e.g. (bool, uint256) or (bool, (uint256, uint256))

                // this.EncodeDynamicTuple(evmParams, value, heads, tail);

                var heads = new SlotCollection(capacity: 8); // heads for this dynamic tuple
                var tails = new List<SlotCollection>();      // tails for each dynamic component

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
                        tails.Add(tail);

                        var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: heads);
                        heads.Add(pointerSlot);

                        this.EncodeValue(componentType, element, tail);
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

                this.EncodeDynamicValue(abiType, value, destination);
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
                    throw new ArgumentException(
                        $"The type {abiType} requires a tuple value",
                        nameof(value));
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

    private SlotCollection BytesToSlots(byte[] rawBytes)
    {
        var paddedBytes = BytesTypeEncoder.EncodeBytes(rawBytes);
        bool hasRemainingBytes = paddedBytes.Length % 32 != 0;

        Debug.Assert(!hasRemainingBytes, "Has remaining bytes; bytes expected to be a multiple of 32");

        var slots = new SlotCollection(capacity: paddedBytes.Length / 32 + (hasRemainingBytes ? 1 : 0));

        for (int i = 0; i < paddedBytes.Length; i += 32)
        {
            var chunk = new byte[32];
            var count = Math.Min(32, paddedBytes.Length - i);
            Buffer.BlockCopy(paddedBytes, i, chunk, 0, count);

            slots.Add(new Slot(chunk));
        }

        return slots;
    }
}
