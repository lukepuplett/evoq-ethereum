using System;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Provides methods for encoding values according to the Ethereum ABI specification.
/// </summary>
public class AbiEncoder : IAbiEncoder
{
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
    public bool TryResolveEncoder(string abiType, object value, out Func<object, Slot>? encoder)
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

        Exception makeEx()
            => new NotImplementedException($"Encoding for type {canonicalType} and value of type {value.GetType()} not implemented");

        if (canonicalType == AbiTypeNames.Address)
        {
            if (value is EthereumAddress address)
            {
                encoder = _ => new Slot(EncodeAddress(address));
                return true;
            }

            if (value is string addr)
            {
                encoder = _ => new Slot(EncodeAddress(new EthereumAddress(Hex.Parse(addr))));
                return true;
            }

            if (value is Hex hex)
            {
                encoder = _ => new Slot(EncodeAddress(new EthereumAddress(hex)));
                return true;
            }

            if (value is byte[] bytes)
            {
                encoder = _ => new Slot(EncodeAddress(new EthereumAddress(bytes)));
                return true;
            }

            throw makeEx();
        }

        if (canonicalType == AbiTypeNames.IntegerTypes.Uint256)
        {
            if (value is BigInteger bigInt)
            {
                encoder = _ => new Slot(EncodeUint(256, bigInt));
                return true;
            }

            if (value is ulong uLong)
            {
                encoder = _ => new Slot(EncodeUint(256, uLong));
                return true;
            }

            if (value is uint uInt)
            {
                encoder = _ => new Slot(EncodeUint(256, uInt));
                return true;
            }

            if (value is long longInt)
            {
                encoder = _ => new Slot(EncodeUint(256, longInt));
                return true;
            }

            if (value is int intValue)
            {
                encoder = _ => new Slot(EncodeUint(256, intValue));
                return true;
            }

            throw makeEx();
        }

        // TODO / consider how to handle other integer types

        if (canonicalType == AbiTypeNames.Bool)
        {
            if (value is bool boolValue)
            {
                encoder = _ => new Slot(EncodeBool(boolValue));
                return true;
            }

            throw makeEx();
        }

        if (canonicalType == AbiTypeNames.Byte)
        {
            if (value is byte byteValue)
            {
                encoder = _ => new Slot(EncodeUint(8, byteValue));
                return true;
            }

            throw makeEx();
        }

        throw makeEx();
    }

    //

    /// <summary>
    /// Encodes an address as a 32-byte value.
    /// </summary>
    /// <param name="address">The address to encode.</param>
    /// <returns>The encoded address, padded to 32 bytes.</returns>
    public static byte[] EncodeAddress(EthereumAddress address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        var result = new byte[32];
        var addressBytes = address.ToByteArray();
        if (addressBytes.Length != 20)
            throw new ArgumentException("Address must be 20 bytes", nameof(address));
        Buffer.BlockCopy(addressBytes, 0, result, 12, 20);

        Debug.Assert(result.Length == 32);

        return result;
    }

    /// <summary>
    /// Encodes a uint as a 32-byte value.
    /// </summary>
    /// <param name="bits">The number of bits to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeUint(int bits, BigInteger value)
    {
        if (value < 0)
        {
            throw new ArgumentException("Value cannot be negative", nameof(value));
        }

        if (bits < 8 || bits > 256 || bits % 8 != 0)
        {
            throw new ArgumentException("Bits must be between 8 and 256 and a multiple of 8", nameof(bits));
        }

        if (value > BigInteger.Pow(2, bits) - 1)
        {
            throw new ArgumentException($"Value too large for {bits} bits", nameof(value));
        }

        var result = new byte[32];
        var bytes = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        Buffer.BlockCopy(bytes, 0, result, 32 - bytes.Length, bytes.Length); // Right-align

        Debug.Assert(result.Length == 32);

        return result;
    }

    /// <summary>
    /// Encodes a boolean as a 32-byte value.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded value as 32 bytes.</returns>
    public static byte[] EncodeBool(bool value)
    {
        var result = new byte[32];
        if (value)
            result[31] = 1;

        Debug.Assert(result.Length == 32);

        return result;
    }

    //

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

        if (!this.TryResolveEncoder(canonicalType, value, out var encoder))
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

            for (int i = 0; i < utf8Bytes.Length; i += 32)
            {
                var chunk = new byte[32];
                var count = Math.Min(32, utf8Bytes.Length - i);
                Buffer.BlockCopy(utf8Bytes, i, chunk, 0, count);

                stringSlots.Append(new Slot(chunk));
            }
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