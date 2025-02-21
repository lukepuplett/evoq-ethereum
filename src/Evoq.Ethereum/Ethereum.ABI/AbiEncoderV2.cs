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

        this.EncodeValue(type, values, root, parentType: "");       // consider using canonical type

        // we treat the root parameter as a tuple, which means that if it's dynamic,
        // the first slot is a pointer to the first slot of the dynamic data, and
        // that's probably not what we want, so we skip the first slot if that's the case

        var isDynamicTupleRoot = AbiTypes.IsTuple(type) && AbiTypes.IsDynamic(type);

        return new AbiEncodingResult(root, skipFirstSlot: isDynamicTupleRoot);
    }

    // privates

    // single slot value

    private void EncodeSingleSlotStaticValue(string abiType, object value, SlotCollection block)
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

        block.Add(encoder!(value));
    }

    // single slot static iterables

    private void EncodeArrayOfSingleSlotValues(string abiType, object value, SlotCollection block)
    {
        // e.g. bool[2][2] or uint256[2], but not tuples like (bool,uint256)[4]

        // fixed size array of single slot values, including pointers
        // all elements are directly encoded into the block with no count
        // of elements since this can be determined from the ABI type

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

        // encode each element directly into the block

        for (int i = 0; i < array.Length; i++)
        {
            var element = array.GetValue(i);

            this.EncodeValue(innerType!, element, block, parentType: abiType); // the router should handle fixed size, single slot arrays appropriately
        }
    }

    private void EncodeTupleOfSingleSlotValues(string abiType, ITuple values, SlotCollection block)
    {
        // e.g. (bool, uint256) or (bool, (uint256, uint256))

        // simply iterate over the components and directly encode them into the block,
        // one after the other

        if (AbiTypes.IsDynamic(abiType))
        {
            throw new ArgumentException($"The type {abiType} is dynamic, not a fixed size tuple");
        }

        if (AbiTypes.IsArray(abiType))
        {
            throw new ArgumentException($"The type {abiType} is an array, not a tuple");
        }

        if (!AbiTypes.IsTuple(abiType))
        {
            throw new ArgumentException($"The type {abiType} is not a tuple");
        }

        var evmParams = EvmParameters.Parse(abiType);

        for (int i = 0; i < evmParams.Count; i++)
        {
            var parameter = evmParams[i];
            var value = values[i];

            this.EncodeValue(parameter.AbiType, value, block, parentType: abiType); // send back into the router
        }
    }

    // dynamic value

    private void EncodeDynamicValue(string abiType, object value, SlotCollection block)
    {
        // e.g. string or bytes, but not tuples like (string,bytes) or arrays like string[] or bytes[]

        if (AbiTypes.IsTuple(abiType))
        {
            throw new ArgumentException($"The type {abiType} is a tuple, not a single slot dynamic value");
        }

        if (AbiTypes.IsArray(abiType))
        {
            throw new ArgumentException($"The type {abiType} is an array, not a single slot dynamic value");
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

        // now pour those two into the block

        block.AddRange(heads);
        block.AddRange(tail);
    }

    // dynamic iterables

    private void EncodeDynamicArray(string abiType, object value, SlotCollection block, string parentType)
    {
        // e.g. uint8[] or bool[][2] or uint256[][2] or string[], or even (bool,uint256)[]

        // experimental: I think when encoding an array, we enter a kind of special mode
        // where recursion is within this special mode until the array is fully encoded
        //
        // this is because in the example I have for bool[][], the outer-most slot is a
        // single pointer to its data tail, which starts with a count and then a pointer
        // for each element of the array, pointing to the tails for those elements
        //
        // if we use the standard recursion where we add a single pointer to its data tail
        // with its length, then "break off" the inner type bool[] and recursively encode
        // that using the standard "back in the top" recursion, we get a result that is
        // incorrect because the inner type bool[] is encoded as a *single* pointer to its
        // own data tail, which is not what we want
        //
        // so we need to recurse this function to control it all
        //
        // the caller is responsible for adding the pointer and passing in the tail as the
        // block

        // similar to the tuple case, except the type is always the same

        if (value is not Array array)
        {
            throw new ArgumentException($"The type {abiType} requires an array value");
        }

        var elementTails = new List<SlotCollection>(capacity: array.Length + 1);      // tails for each element's value

        if (!AbiTypes.TryRemoveOuterArrayDimension(abiType, out var innerType))
        {
            throw new NotImplementedException($"The type '{abiType}' is not an array");
        }

        if (!AbiTypes.TryGetArrayDimensions(abiType, out var dimensions))
        {
            throw new ArgumentException($"The type {abiType} is not an array");
        }
        bool isVariableLength = dimensions.First() == -1;

        if (AbiTypes.IsArray(parentType) && false) // FORCE FALSE FOR NOW
        {
            // already within an array

            // what difference does it make if we're already within an array?

            throw new NotImplementedException($"Waiting to support dynamic arrays within arrays");
        }
        else
        {
            // caller is responsible for adding the single pointer for this new
            // dynamic array:
            //
            // the block starts with a count followed by the elements of type T (the
            // inner type) which will either be pointers to their own tails or the
            // actual encoded data if the inner type is not dynamic, I think

            if (isVariableLength)
            {
                var countSlot = new Slot(UintTypeEncoder.EncodeUint(256, array.Length));
                block.Add(countSlot);
            }

            if (AbiTypes.IsDynamic(innerType!))
            {
                // dynamic, so we need new pointers and tails for each element

                for (int i = 0; i < array.Length; i++)
                {
                    var elementTail = new SlotCollection(capacity: 8);
                    elementTails.Add(elementTail);

                    var elementPointerSlot = new Slot(pointsToFirst: elementTail, relativeTo: block);
                    block.Add(elementPointerSlot);

                    var element = array.GetValue(i);

                    // recursive call to encode will drop back into this function
                    // with the tail as the block, if the inner type is dynamic

                    this.EncodeValue(innerType!, element, elementTail, parentType: abiType);
                }
            }
            else
            {
                // fixed size, so we can encode directly into the block

                for (int i = 0; i < array.Length; i++)
                {
                    var element = array.GetValue(i);

                    this.EncodeValue(innerType!, element, block, parentType: abiType);
                }
            }
        }

        // pour it all into the block now that the tails are encoded

        block.AddRange(elementTails.SelectMany(tail => tail));
    }

    // router

    private void EncodeValue(string abiType, object value, SlotCollection block, string parentType)
    {
        bool isArray = AbiTypes.IsArray(abiType);
        bool isTuple = AbiTypes.IsTuple(abiType);
        bool isDynamic = AbiTypes.IsDynamic(abiType);
        bool isNestedWithinArray = AbiTypes.IsArray(parentType);

        // determine the type of the value and call the appropriate encoder

        // we need to know if the type is dynamic or can be encoded directly into the block,
        // and if it's iterable like an array or tuple

        // heads and tails!
        //
        // heads are slots for either the direct-encoded value or a pointer to the first slot
        // of encoded data in the tail
        //
        // the tail (singular) is the collection of slots for encoded data where that data
        // is dynamic and has a variable length

        if (isDynamic)
        {
            // dynamic, encoded into the tail and add the offset pointer to the head

            // e.g. bytes, string, string[], bool[][2] or (string,uint256)[]

            if (isArray)
            {
                if (isNestedWithinArray)
                {
                    // no need to add a pointer

                    this.EncodeDynamicArray(abiType, value, block, parentType: abiType);
                }
                else
                {
                    // fresh encounter with this dynamic value, so we need a pointer

                    var tail = new SlotCollection(capacity: 8);
                    var pointerSlot = new Slot(pointsToFirst: tail, relativeTo: block);

                    block.Add(pointerSlot);

                    this.EncodeDynamicArray(abiType, value, tail, parentType: abiType);

                    block.AddRange(tail);
                }
            }
            else if (isTuple)
            {
                // e.g. (bool, uint256) or (bool, (uint256, uint256))

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

                        this.EncodeValue(componentType, element, tail, parentType: abiType);
                    }
                    else
                    {
                        // write the value directly into the heads

                        this.EncodeValue(componentType, element, heads, parentType: abiType);
                    }
                }

                block.AddRange(heads);
                block.AddRange(tails.SelectMany(tail => tail)); // pour the all tail slots into the block
            }
            else
            {
                // e.g. bytes, string

                this.EncodeDynamicValue(abiType, value, block);
            }
        }
        else // not dynamic, i.e. the ABI type conveys the size of the value
        {
            // single slot, directly encoded into the block

            // e.g. bool or uint256, bool[2] or uint256[2][2] or (bool,uint256), (bool,uint256)[2]

            if (isArray)
            {
                this.EncodeArrayOfSingleSlotValues(abiType, value, block);
            }
            else if (isTuple)
            {
                // a tuple can only appear within another tuple, and a tuple is paired with a parameter

                if (value is ITuple tuple)
                {
                    this.EncodeTupleOfSingleSlotValues(abiType, tuple, block);
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

                this.EncodeSingleSlotStaticValue(abiType, value, block);
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

    // private bool TryFindDynamicBytesEncoder(string abiType, object value, out Func<object, byte[]>? encoder)
    // {
    //     if (value == null)
    //     {
    //         encoder = _ => new byte[32]; // null value is encoded as a 32-byte zero value
    //         return true;
    //     }

    //     // get the canonical type

    //     if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
    //     {
    //         // canonical type not found; this should never happen

    //         throw new InvalidOperationException($"Canonical type not found for {abiType}");
    //     }

    //     foreach (var dynamicEncoder in this.dynamicTypeEncoders)
    //     {
    //         if (dynamicEncoder.TryEncode(canonicalType, value, out var bytes))
    //         {
    //             encoder = _ => bytes;
    //             return true;
    //         }
    //     }

    //     encoder = null;
    //     return false;
    // }

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
