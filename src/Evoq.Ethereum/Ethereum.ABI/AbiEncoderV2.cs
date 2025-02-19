using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;
using Nethereum.Contracts.Standards.ENS.ETHRegistrarController.ContractDefinition;

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

        var gather = new SlotCollection(capacity: slotCount * 8);

        for (int i = 0; i < slotCount; i++)
        {
            var parameter = parameters[i];
            var value = values[i];

            var headAndTail = this.EncodeParameterValue(parameter, value);

            gather.AddRange(headAndTail);
        }

        //

        var result = new AbiEncodingResult(gather);

        return result;
    }

    //

    private SlotCollection EncodeParameterValue(EvmParam parameter, object value)
    {
        var head = new SlotCollection(capacity: 8);
        var tail = new SlotCollection(capacity: 8);

        if (parameter.IsDynamic)
        {
            // encode the value into the data and add the offset pointer to the head

            if (AbiTypes.IsArray(parameter.AbiType))
            {
                // a dynamic array; encode the length and encode each element's value
                // recursively

                if (!AbiTypes.TryRemoveOuterArrayDimension(parameter.AbiType, out var innerType))
                {
                    throw new ArgumentException(
                        $"Unable to remove outer array dimension from {parameter.AbiType}");
                }

                var array = value as Array;

                if (array == null)
                {
                    throw new ArgumentException(
                        $"The parameter is of type '{parameter.AbiType}' but the value is not an array");
                }

                // encode the length of the array

                var count = array.Length;
                var countBytes = UintTypeEncoder.EncodeUint(256, count);
                var countSlot = new Slot(countBytes);

                head.Add(countSlot);

                if (AbiTypes.IsDynamic(innerType!))
                {
                    // has dynamic inner elements; we'll need pointers to the start of each element's data
                    // to the head and then encode each element's value into the tail

                    for (int i = 0; i < count; i++)
                    {
                        var elementValue = array.GetValue(i);

                        // TODO: implement this

                        throw new NotImplementedException("Dynamic arrays are not yet supported");

                        // var headAndTail = this.EncodeParameterValue(parameter.Components![i], elementValue);

                        // var pointerSlot = new Slot(headAndTail);

                        // head.Add(pointerSlot);
                        // tail.AddRange(headAndTail);
                    }
                }
                else
                {
                    // has static inner elements; we can encode each element's value directly into the head

                    for (int i = 0; i < count; i++)
                    {
                        var headAndTail = this.EncodeParameterValue(parameter.Components![i], array.GetValue(i));

                        head.AddRange(headAndTail);
                    }
                }
            }
            else if (parameter.IsSingle)
            {
                // a dynamic value which is not an array, nor a tuple, so it's a string, bytes, etc.

                // encode the value into the data and the offset pointer to the head

                if (!this.TryFindDynamicBytesEncoder(parameter.AbiType, value, out var encoder))
                {
                    throw NotImplemented(parameter.AbiType, value.GetType().ToString());
                }

                var bytes = encoder!(value);
                var slots = this.BytesToSlots(bytes);

                var pointerSlot = new Slot(slots);

                head.Add(pointerSlot);
                tail.AddRange(slots);
            }
            else
            {
                if (value is ITuple tuple)
                {
                    // a dynamic tuple; encode each component recursively

                    for (int i = 0; i < tuple.Length; i++)
                    {
                        var headAndTail = this.EncodeParameterValue(parameter.Components![i], tuple[i]);

                        var pointerSlot = new Slot(headAndTail);

                        head.Add(pointerSlot);
                        tail.AddRange(headAndTail);
                    }
                }
                else
                {
                    throw NotImplemented(parameter.AbiType, value.GetType().ToString());
                }
            }
        }
        else
        {
            // type is not dynamic

            if (AbiTypes.IsArray(parameter.AbiType))
            {
                // encode the value directly into the head starting with the count of
                // elements in the array

                throw new NotImplementedException("Static arrays are not yet supported");
            }
            else if (parameter.IsSingle)
            {
                // encode the value directly into the head, including fixed size arrays

                if (!this.TryFindStaticSlotEncoder(parameter.AbiType, value, out var encoder))
                {
                    throw NotImplemented(parameter.AbiType, value.GetType().ToString());
                }

                head.Add(encoder!(value));
            }
            else
            {
                if (value is ITuple tuple)
                {
                    // encode each component recursively

                    // TODO: implement this

                    throw new NotImplementedException("Static tuples are not yet supported");
                }
                else
                {
                    throw NotImplemented(parameter.AbiType, value.GetType().ToString());
                }
            }
        }

        head.AddRange(tail);
        return head;

        //

        Exception NotImplemented(string abiType, string clrType) =>
            new NotImplementedException($"Encoding for type {abiType} and value of type {clrType} not implemented");
    }

    //

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
