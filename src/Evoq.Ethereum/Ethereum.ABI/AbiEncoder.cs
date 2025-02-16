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
    private readonly IReadOnlyList<IAbiTypeEncoder> staticTypeEncoders;
    private readonly IReadOnlyList<IAbiTypeEncoder> dynamicTypeEncoders;
    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoder"/> class.
    /// </summary>
    public AbiEncoder()
    {
        this.staticTypeEncoders = new AbiStaticTypeEncoders();
        this.dynamicTypeEncoders = new AbiDynamicTypeEncoders();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoder"/> class.
    /// </summary>
    /// <param name="staticTypeEncoders">The static type encoders.</param>
    /// <param name="dynamicTypeEncoders">The dynamic type encoders.</param>
    public AbiEncoder(IReadOnlyList<IAbiTypeEncoder> staticTypeEncoders, IReadOnlyList<IAbiTypeEncoder> dynamicTypeEncoders)
    {
        this.staticTypeEncoders = staticTypeEncoders;
        this.dynamicTypeEncoders = dynamicTypeEncoders;
    }

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

        // dynamic types always take exactly one slot (for the offset)

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
    public bool TryFindDynamicSlotEncoder(string abiType, object value, out Func<object, Slots>? encoder)
    {
        if (value == null)
        {
            encoder = _ => new Slots(new Slot(new byte[32])); // null value is encoded as a 32-byte zero value
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
                encoder = _ => this.BytesToSlots(bytes);
                return true;
            }
        }

        throw new NotImplementedException(
            $"Encoding for type {canonicalType} and value of type {value.GetType()} not implemented");
    }

    //

    private Slots BytesToSlots(byte[] bytes)
    {
        var slots = new Slots(capacity: bytes.Length / 32 + 1);

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
            throw new ArgumentException($"Array length {array.Length} does not match expected length {dimensions[0]}");

        // for static arrays, we encode each element in sequence, simply adding each element to the appropriate space

        foreach (var element in array)
        {
            this.EncodeStaticValue(baseType, element, staticSpace, dynamicSpace);
        }
    }

    private void EncodeDynamicValue(string abiType, object value, SlotSpace? staticSpace, SlotSpace dynamicSpace)
    {
        // staticSpace is null if we're already encoding a dynamic value and this is being called
        // recursively, otherwise we're encoding a static length value in the static space and we
        // need to add an offset slot to the static space

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            throw new ArgumentException($"Invalid type: {abiType}");
        }

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

            // it's a dynamic array; so we add an offset slot to the static section
            // and then add the array length to the dynamic section along with slots
            // for each element in the array (the slot will either be the actual value
            // or an offset to the value, but will always be one slot per element)

            // adding the offset slot to the static section is done in the AppendStatic
            // method so we just need to build the dynamic data

            var (lengthAndOffsets, elementsSlots) = dynamicSpace.AppendReservedArray(array.Length);
            var pointerSlot = new Slot(pointsToFirst: lengthAndOffsets);

            if (staticSpace != null) // see note above
            {
                staticSpace.Append(pointerSlot);
            }
            else
            {
                dynamicSpace.Append(pointerSlot);
            }

            // now encode the inner elements

            for (int i = 0; i < array.Length; i++)
            {
                var element = array.GetValue(i);
                var slots = elementsSlots[i];
                var elementSpace = new SlotSpace(slots);

                this.EncodeValue(innerType, element, null, elementSpace);
            }
        }
        else if (canonicalType == AbiTypeNames.String)
        {
            var utf8Bytes = System.Text.Encoding.UTF8.GetBytes((string)value);
            var length = utf8Bytes.Length;

            // make slots for the length and the actual string data

            var stringSlots = dynamicSpace.AppendReservedString(length);

            // add pointer slot that will point to the offset of the string slots above

            var pointerSlot = new Slot(pointsToFirst: stringSlots);

            if (staticSpace != null)
            {
                staticSpace.Append(pointerSlot);
            }
            else
            {
                dynamicSpace.Append(pointerSlot);
            }

            // add the string data

            dynamicSpace.Append(this.BytesToSlots(utf8Bytes));
        }
        else
        {
            throw new NotImplementedException($"Encoding for type {canonicalType} not implemented");
        }

        // Q: what other dynamic types are there in ABI?
        // A: dynamic types are:
        // - string
        // - bytes
        // - array

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