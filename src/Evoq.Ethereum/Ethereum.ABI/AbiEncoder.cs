using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
    /// Computes the number of 32-byte slots needed for the static portion of a value.
    /// </summary>
    /// <param name="abiType">The ABI type to compute the size for.</param>
    /// <returns>The number of 32-byte slots needed.</returns>
    public int ComputeStaticSlotCount(string abiType)
    {
        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            throw new ArgumentException($"Invalid type: {abiType}");
        }

        // dynamic types always take exactly one slot (for the pointer)

        if (AbiTypes.IsDynamicType(canonicalType))
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

    /// <summary>
    /// Encodes a single parameter.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, object value)
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
            throw new ArgumentException($"Expected {singles.Count} values but got {values.Length}");

        for (int i = 0; i < singles.Count; i++)
        {
            var param = singles[i];
            var value = values[i];

            this.EncodeValue(param.AbiType, value, result.StaticData, result.DynamicData);
        }

        return result;
    }

    /// <summary>
    /// Resolves the encoder for a given type.
    /// </summary>
    /// <param name="abiType">The type to resolve the encoder for.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoder">The encoder for the given type.</param>
    /// <returns>True if the encoder was resolved, false otherwise.</returns>
    public bool TryFindStaticSlotEncoder(string abiType, object value, out Func<object, Slot>? encoder)
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

    /// <summary>
    /// Resolves the encoder for a given type.
    /// </summary>
    /// <param name="abiType">The type to resolve the encoder for.</param>
    /// <param name="value">The value to encode.</param>
    /// <param name="encoder">The encoder for the given type.</param>
    /// <returns>True if the encoder was resolved, false otherwise.</returns>
    public bool TryFindDynamicBytesEncoder(string abiType, object value, out Func<object, byte[]>? encoder)
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

    //

    private Slots BytesToSlots(byte[] bytes)
    {
        bool hasRemainingBytes = bytes.Length % 32 != 0;

        Debug.Assert(!hasRemainingBytes, "Has remaining bytes; bytes expected to be a multiple of 32");

        var slots = new Slots(capacity: bytes.Length / 32 + (hasRemainingBytes ? 1 : 0));

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
        if (AbiTypes.IsDynamicType(abiType))
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

        // for static arrays, we encode each element in sequence, simply adding each element to the appropriate space

        foreach (var element in array)
        {
            this.EncodeStaticValue(baseType, element, staticSpace, dynamicSpace);
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

        bool useV1ArrayEncoding = false;

        if (AbiTypes.IsArray(canonicalType))
        {
            // it's an array with a dynamic outer dimension

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

            if (useV1ArrayEncoding)
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
            else
            {
                // much simpler than the v1 encoding, which probably does not work

                var reserved = dynamicSpace.AppendReservedArray(array.Length);

                // now encode the elements

                for (int i = 0; i < array.Length; i++)
                {
                    var elementValue = array.GetValue(i);

                    this.EncodeValue(innerType, elementValue, reserved.ElementsSpace, reserved.DynamicValuesSpace);
                }
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

            if (useV1ArrayEncoding)
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
            else
            {
                var reserved = dynamicSpace.AppendReservedBytes(paddedBytes.Length);

                // create and add the pointer slot, which means wrapping the length slot
                // in a slots object so we can point to it

                var slots = new Slots(capacity: 1)
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
        }
        else
        {
            throw new NotImplementedException($"Encoding for type {canonicalType} not implemented");
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

        // Start with the size of the base type, which should be 1
        int slotCount = this.ComputeStaticSlotCount(baseType);

        Debug.Assert(slotCount == 1);

        // Process dimensions from right to left
        for (int i = dimensions.Length - 1; i >= 0; i--)
        {
            if (dimensions[i] == -1) // Dynamic dimension []
            {
                // Dynamic arrays only take one slot for the offset
                slotCount = 1;
            }
            else // Static dimension [N]
            {
                // Multiply by the dimension size
                slotCount *= dimensions[i];
            }
        }

        return slotCount;
    }
}