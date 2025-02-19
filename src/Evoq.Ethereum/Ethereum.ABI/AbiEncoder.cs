using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Provides methods for encoding values according to the Ethereum ABI specification.
/// </summary>
public class AbiEncoder : IAbiEncoder
{
    private readonly IReadOnlyList<IAbiEncode> staticTypeEncoders;
    private readonly IReadOnlyList<IAbiEncode> dynamicTypeEncoders;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoder"/> class.
    /// </summary>
    public AbiEncoder()
    {
        this.staticTypeEncoders = new AbiStaticTypeEncoders();
        this.dynamicTypeEncoders = new AbiDynamicTypeEncoders();

        this.Validator = new AbiTypeValidator(staticTypeEncoders, dynamicTypeEncoders);
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoder"/> class.
    /// </summary>
    /// <param name="staticTypeEncoders">The static type encoders.</param>
    /// <param name="dynamicTypeEncoders">The dynamic type encoders.</param>
    public AbiEncoder(IReadOnlyList<IAbiEncode> staticTypeEncoders, IReadOnlyList<IAbiEncode> dynamicTypeEncoders)
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
    /// Encodes a single parameter.
    /// </summary>
    /// <typeparam name="T">The type of the value to encode. Must be a value type or string.</typeparam>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public AbiEncodingResult EncodeParameters<T>(EvmParameters parameters, T value) where T : struct, IConvertible
    {
        return this.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a single string parameter.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The string value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, string value)
    {
        return this.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a BigInteger parameter.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The BigInteger value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, BigInteger value)
    {
        return this.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a byte array parameter.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The byte array to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, byte[] value)
    {
        return this.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes the parameters.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="values">The values to encode.</param>
    /// <returns>The encoded parameters.</returns>
    /// <exception cref="ArgumentException">Thrown if the number of values does not match the number of parameters.</exception>
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values)
    {
        var singles = parameters.DeepSingleParams().ToList();
        var staticSlotsRequired = singles.Sum(s => this.ComputeStaticSlotCount(s.AbiType));

        var result = new AbiEncodingResult(staticSlotsRequired);

        if (singles.Count != values.Length)
        {
            throw new ArgumentException($"Expected {singles.Count} values but got {values.Length}");
        }

        for (int i = 0; i < singles.Count; i++)
        {
            var param = singles[i];
            var value = values[i];

            this.EncodeValue(param.AbiType, value, result.StaticData, result.DynamicData);
        }

        return result;
    }

    //

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

            slots.Append(new Slot(chunk));
        }

        return slots;
    }

    private void EncodeValue(string abiType, object value, SlotSpace? staticSpace, SlotSpace dynamicSpace)
    {
        if (AbiTypes.IsDynamic(abiType))
        {
            this.EncodeDynamicValue(abiType, value, staticSpace, dynamicSpace);
        }
        else
        {
            this.EncodeStaticValue(abiType, value, staticSpace, dynamicSpace);
        }
    }

    private void EncodeStaticValue(string abiType, object value, SlotSpace? staticSpace, SlotSpace dynamicSpace)
    {
        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            throw new ArgumentException($"Invalid type: {abiType}");
        }

        // handle static arrays

        if (AbiTypes.IsArray(canonicalType) && !AbiTypes.IsDynamicArray(canonicalType))
        {
            EncodeStaticArray(canonicalType, value, staticSpace, dynamicSpace);

            return;
        }

        // handle static types

        if (!this.TryFindStaticSlotEncoder(canonicalType, value, out var encoder))
        {
            throw new NotImplementedException($"Encoding for type {canonicalType} not implemented");
        }

        var slot = encoder!(value);
        if (staticSpace != null)
        {
            staticSpace.Append(slot);
        }
        else
        {
            dynamicSpace.Append(slot);
        }

        return;
    }

    private void EncodeStaticArray(string abiType, object value, SlotSpace? staticSpace, SlotSpace dynamicSpace)
    {
        if (!AbiTypes.TryGetArrayBaseType(abiType, out var baseType) ||
            !AbiTypes.TryGetArrayDimensions(abiType, out var dimensions) ||
            baseType == null ||
            dimensions == null)
        {
            throw new ArgumentException($"Invalid array type: {abiType}");
        }

        var array = (Array)value;
        if (array.Length != dimensions[0])
        {
            throw new ArgumentException($"Array length {array.Length} does not match expected length {dimensions[0]}");
        }

        if (!this.TryFindStaticSlotEncoder(AbiTypeNames.IntegerTypes.Uint256, (uint)array.Length, out var lengthEncoder))
        {
            throw new NotImplementedException($"Cannot encode array length. Encoding for type {AbiTypeNames.IntegerTypes.Uint256} not implemented");
        }

        var slot = lengthEncoder!(array.Length);

        if (staticSpace != null)
        {
            staticSpace.Append(slot);
        }
        else
        {
            dynamicSpace.Append(slot);
        }

        // for static arrays, we encode each element in sequence, simply adding each element to the appropriate space

        foreach (var v in array)
        {
            this.EncodeStaticValue(baseType, v, staticSpace, dynamicSpace);
        }
    }

    private void EncodeDynamicValue(string abiType, object value, SlotSpace? staticSpace, SlotSpace dynamicSpace)
    {
        // staticSpace represents either the root static space or the space for the
        // elements of an array, which may actually also be in the dynamic space if
        // we're encoding a dynamic array and this is a recursive call

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            throw new ArgumentException($"Invalid type: {abiType}");
        }

        // IMPORTANT / post testing, we can remove the v1 array encoding, and also
        // remove the nullable staticSpace and just always encode as per usual into
        // either space depending on whether we're encoding a static or dynamic value

        int ver = 2;

        if (AbiTypes.IsArray(canonicalType))
        {
            // it's an array with a dynamic outer dimension []

            if (!AbiTypes.TryRemoveOuterArrayDimension(canonicalType, out var innerType) ||
                !AbiTypes.TryGetArrayBaseType(canonicalType, out var baseType) ||
                !AbiTypes.TryGetArrayDimensions(canonicalType, out var dimensions) ||
                innerType == null ||
                baseType == null ||
                dimensions == null)
            {
                throw new ArgumentException($"Invalid array type: {canonicalType}");
            }

            var array = (Array)value;

            if (ver == 1)
            {
                // array encoding: pointer to the encoding of the array -> length slot followed
                // by the slots of the elements, each of which will either be the actual value
                // or a pointer to the value, but will always be one slot per element

                var (lengthAndOffsets, elementsSlots) = dynamicSpace.AppendReservedArrayV1(array.Length);
                var pointerSlot = new Slot(pointsToFirst: lengthAndOffsets);

                if (staticSpace != null) // depends on whether we're already encoding a dynamic value
                {
                    staticSpace.Append(pointerSlot); // we're still in the static space
                }
                else
                {
                    dynamicSpace.Append(pointerSlot); // we're in a nested encoding of a dynamic value
                }

                // now encode the inner elements

                for (int i = 0; i < array.Length; i++)
                {
                    var elementValue = array.GetValue(i);       // get the element value
                    var slots = elementsSlots[i];               // get the slots for the element
                    var elementSpace = new SlotSpace(slots);    // wrap the slots

                    this.EncodeValue(innerType, elementValue, null, elementSpace);
                }
            }
            else if (ver == 2)
            {
                var reserved = dynamicSpace.AppendReservedArray(array.Length);

                var pointerSlot = new Slot(pointsToFirst: reserved.ElementsSpace);

                if (staticSpace != null) // depends on whether we're already encoding a dynamic value
                {
                    staticSpace.Append(pointerSlot); // we're still in the static space
                }
                else
                {
                    dynamicSpace.Append(pointerSlot); // we're in a nested encoding of a dynamic value
                }

                // now encode the elements

                for (int i = 0; i < array.Length; i++)
                {
                    var elementValue = array.GetValue(i);

                    this.EncodeValue(innerType, elementValue, reserved.ElementsSpace, reserved.DynamicValuesSpace);
                }
            }
            else if (ver == 3)
            {
                SlotCollection arraySlots = new(capacity: array.Length + 1);

                arraySlots.Add(new Slot(UintTypeEncoder.EncodeUint(256, array.Length)));

                for (int i = 0; i < array.Length; i++)
                {
                    var elementValue = array.GetValue(i);

                    if (AbiTypes.IsDynamic(innerType))
                    {

                    }
                    else
                    {

                    }
                }
            }
            else
            {
                throw new NotImplementedException($"Unsupported array encoding version: {ver}");
            }
        }
        else if (canonicalType == AbiTypeNames.String || canonicalType == AbiTypeNames.Bytes)
        {
            // encoding a string or arbitrary binary is the same shizzle

            if (!this.TryFindDynamicBytesEncoder(canonicalType, value, out var encoder))
            {
                throw new NotImplementedException($"No encoder found for type {canonicalType}");
            }

            var paddedBytes = encoder!(value);

            if (ver == 1)
            {
                // make slots for the length and the actual string data

                var lengthAndSlots = dynamicSpace.AppendReservedBytesV1(paddedBytes.Length);

                // add pointer slot that will point to the offset of the string slots above

                var pointerSlot = new Slot(pointsToFirst: lengthAndSlots);

                if (staticSpace != null)
                {
                    staticSpace.Append(pointerSlot);
                }
                else
                {
                    dynamicSpace.Append(pointerSlot);
                }

                // add the string data

                dynamicSpace.Append(this.BytesToSlots(paddedBytes));
            }
            else if (ver == 2)
            {
                var reserved = dynamicSpace.AppendReservedBytes(paddedBytes.Length);

                // create and add the pointer slot, which means wrapping the length slot
                // in a slots object so we can point to it

                var slots = new SlotCollection(capacity: 1)
                {
                    reserved.LengthSlot
                };

                var pointerSlot = new Slot(pointsToFirst: slots);

                if (staticSpace != null)
                {
                    staticSpace.Append(pointerSlot);
                }
                else
                {
                    dynamicSpace.Append(pointerSlot);
                }

                // add the string data

                reserved.DynamicValueSpace.Append(this.BytesToSlots(paddedBytes));
            }
            else if (ver == 3)
            {
                dynamicSpace.Append(this.BytesToSlots(paddedBytes));
            }
            else
            {
                throw new NotImplementedException($"Unsupported string encoding version: {ver}");
            }
        }
        else
        {
            throw new NotImplementedException($"Unsupported type: {canonicalType}");
        }
    }

    private int ComputeArrayStaticSlotCount(string abiType)
    {
        // computes the slots required to store the array in the static section

        if (!AbiTypes.TryGetArrayBaseType(abiType, out var baseType) ||
            !AbiTypes.TryGetArrayDimensions(abiType, out var dimensions) ||
            baseType == null ||
            dimensions == null)
        {
            throw new ArgumentException($"Invalid array type: {abiType}");
        }

        // start with the size of the base type, which should be 1

        int slotCount = this.ComputeStaticSlotCount(baseType);

        Debug.Assert(slotCount == 1);

        // Process dimensions from right to left
        for (int i = dimensions.Length - 1; i >= 0; i--)
        {
            if (dimensions[i] == -1) // dynamic dimension []
            {
                // dynamic arrays only take one slot for the offset

                slotCount = 1;
            }
            else // static dimension [N]
            {
                // multiply by the dimension size

                slotCount *= dimensions[i];
            }
        }

        return 1 + slotCount; // 1 for the length
    }

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
            encoder = null;
            return false;
        }

        foreach (var staticEncoder in this.staticTypeEncoders)
        {
            if (staticEncoder.TryEncode(canonicalType, value, out var bytes))
            {
                encoder = _ => new Slot(bytes);
                return true;
            }
        }

        throw new NotImplementedException(
            $"Encoding for type {canonicalType} and value of type {value.GetType()} not implemented");
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
            encoder = null;
            return false;
        }

        foreach (var dynamicEncoder in this.dynamicTypeEncoders)
        {
            if (dynamicEncoder.TryEncode(canonicalType, value, out var bytes))
            {
                encoder = _ => bytes;
                return true;
            }
        }

        throw new NotImplementedException(
            $"Encoding for type {canonicalType} and value of type {value.GetType()} not implemented");
    }

    private int ComputeStaticSlotCount(string abiType)
    {
        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            throw new ArgumentException($"Invalid type: {abiType}");
        }

        // dynamic types always take exactly one slot (for the pointer)

        if (AbiTypes.IsDynamic(canonicalType))
        {
            return 1;
        }

        if (AbiTypes.IsArray(canonicalType))
        {
            return ComputeArrayStaticSlotCount(canonicalType);
        }

        // simple types always take one slot

        return 1;
    }
}