using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Evoq.Ethereum.ABI.TypeEncoders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

/// <summary>
/// A new and improved ABI decoder that uses a more efficient decoding scheme.
/// </summary>
public class AbiDecoder : IAbiDecoder
{
    private readonly ILogger<AbiDecoder> logger;
    private readonly IReadOnlyList<IAbiDecode> staticTypeDecoders;
    private readonly IReadOnlyList<IAbiDecode> dynamicTypeDecoders;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDecoder"/> class.
    /// </summary>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public AbiDecoder(ILoggerFactory? loggerFactory = null)
    {
        this.logger = loggerFactory?.CreateLogger<AbiDecoder>() ?? NullLoggerFactory.Instance.CreateLogger<AbiDecoder>();
        this.staticTypeDecoders = new AbiStaticTypeDecoders();
        this.dynamicTypeDecoders = new AbiDynamicTypeDecoders();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiDecoder"/> class.
    /// </summary>
    /// <param name="staticTypeDecoders">The static type decoders.</param>
    /// <param name="dynamicTypeDecoders">The dynamic type decoders.</param>
    /// <param name="loggerFactory">Optional logger factory.</param>
    public AbiDecoder(
        IReadOnlyList<IAbiDecode> staticTypeDecoders,
        IReadOnlyList<IAbiDecode> dynamicTypeDecoders,
        ILoggerFactory? loggerFactory = null)
    {
        this.logger = loggerFactory?.CreateLogger<AbiDecoder>() ?? NullLoggerFactory.Instance.CreateLogger<AbiDecoder>();
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

        DecodeComponents(parameters, allSlots.First(), allSlots);

        return new AbiDecodingResult(parameters);
    }

    // routers

    private int DecodeComponents(IReadOnlyList<AbiParam> parameters, Slot firstValueOrPointer, SlotCollection allSlots)
    {
        SlotCollection subSlots;

        if (firstValueOrPointer.IsPointer)
        {
            subSlots = allSlots.SkipToPoint(firstValueOrPointer);
        }
        else
        {
            subSlots = allSlots.SkipTo(firstValueOrPointer);
        }

        int consumedSlots = 0;

        foreach (var p in parameters)
        {
            this.logger.LogDebug(
                "Decoding parameter '{Name}' (type: {AbiType}, isDynamic: {IsDynamic})",
                p.SafeName,
                p.AbiType,
                p.IsDynamic);

            var slot = subSlots[consumedSlots];

            if (p.IsDynamic)
            {
                // expect a pointer to the next slot, though we cannot check IsPointer on
                // the slot because the slot doesn't yet know, i.e. it has no PointsTo
                // information

                // the first pointer is relative to the root slots, and points to a slot that
                // contains the data for the parameter

                slot.DecodePointer(relativeTo: subSlots);

                // then what?

                this.DecodeDynamicComponent(p, slot, allSlots);
                consumedSlots += 1; // advance +1 past the pointer
            }
            else
            {
                int c = this.DecodeStaticComponent(p, slot, allSlots);
                consumedSlots += c;
            }
        }

        return consumedSlots;
    }

    private int DecodeStaticComponent(AbiParam parameter, Slot valueSlot, SlotCollection allSlots)
    {
        // check for array first, because it may be an array of tuples

        if (parameter.IsArray)
        {
            // array of static, fixed-length values in sequential slots

            return DecodeStaticArray(parameter, valueSlot, allSlots);
        }
        else if (parameter.IsTupleStrict)
        {
            // tuple of static, fixed-length values in sequential slots

            return DecodeStaticTuple(parameter, valueSlot, allSlots);
        }
        else
        {
            // static value

            return DecodeSingleSlotStaticValue(parameter, valueSlot);
        }
    }

    private int DecodeDynamicComponent(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        // check for array first, because it may be an array of tuples

        if (parameter.IsArray)
        {
            return DecodeDynamicArray(parameter, pointer, allSlots);
        }
        else if (parameter.IsTupleStrict)
        {
            return DecodeTupleComponents(parameter, pointer, allSlots);
        }
        else
        {
            return DecodeDynamicValue(parameter, pointer, allSlots);
        }
    }

    //

    private int DecodeTupleComponents(AbiParam tuple, Slot firstValueOrPointer, SlotCollection allSlots)
    {
        if (!tuple.TryParseComponents(out var components))
        {
            throw new AbiTypeException(
                $"Unable to decode components: '{tuple.AbiType}' is not a valid tuple type. " +
                $"Please ensure the ABI type is correctly formatted.",
                tuple.AbiType);
        }

        var dic = Activator.CreateInstance(tuple.ClrType) as IDictionary<string, object?>;

        if (dic == null)
        {
            throw new AbiTypeMismatchException(
                $"Unsupported tuple type: Expected IDictionary<string, object?> but got {tuple.ClrType.Name}. " +
                $"Tuples must be decoded into IDictionary<string, object?> containers.",
                tuple.AbiType,
                tuple.ClrType);
        }

        int c = this.DecodeComponents(components!, firstValueOrPointer, allSlots);

        foreach (var p in components!)
        {
            dic.Add(p.SafeName, p.Value);
        }

        tuple.Value = dic;

        return c;
    }

    private int DecodeSingleSlotStaticValue(AbiParam parameter, Slot valueSlot)
    {
        // just decode the value

        parameter.Value = this.DecodeStaticSlotValue(parameter.AbiType, parameter.ClrType, valueSlot);

        return 1;
    }

    private int DecodeStaticArray(AbiParam parameter, Slot firstValueSlot, SlotCollection allSlots)
    {
        if (!AbiTypes.TryGetArrayDimensions(parameter.AbiType, out var dimensions))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine dimensions for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        if (!AbiTypes.TryGetArrayBaseType(parameter.AbiType, out var baseType))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine base type for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        if (!AbiTypes.TryGetArrayMultiLength(parameter.AbiType, out var multiLength))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine total length for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        if (!parameter.ClrType.IsArray)
        {
            throw new AbiTypeMismatchException(
                $"Type mismatch: ABI type '{parameter.AbiType}' requires an array CLR type, but got {parameter.ClrType.Name}. " +
                $"Please ensure the parameter is configured with the correct CLR type.",
                parameter.AbiType,
                parameter.ClrType);
        }

        // e.g. uint8[2], bytes32[4][2], (bool, bool)[2]

        // for static arrays, due to their fixed size and values, all their values are in sequential
        // slots, so we can just decode them into the array, or array of arrays, etc.

        // the first slot should be the first element and all the values are contiguous

        if (parameter.TryParseComponents(out var components))
        {
            // array of static tuples, these are also laid out in contiguous slots, but
            // since each element has many values, we need to multiply the multiple!

            multiLength *= components!.Count;
        }

        var subSlots = allSlots.SkipTo(firstValueSlot).Take(multiLength).ToList();
        var decodedSubValues = subSlots
            .Select(s => this.DecodeStaticSlotValue(baseType!, parameter.BaseClrType, s))
            .ToList();

        var (array, skip) = getArray(parameter.AbiType, parameter.ClrType, 0);

        parameter.Value = array;

        return skip;

        //

        // define recursive function to decode the array (see note, top)

        (Array Array, int Skip) getArray(string abiType, Type clrType, int skip)
        {
            if (!AbiTypes.TryGetArrayOuterLength(abiType, out var outerArrayLength))
            {
                throw new AbiTypeException(
                    $"Invalid array type: Failed to determine outer array length for '{abiType}'. " +
                    $"Please ensure the array type is correctly formatted.",
                    abiType);
            }

            if (AbiTypes.TryGetArrayInnerType(abiType, out var innerType) && AbiTypes.IsArray(innerType!))
            {
                // inner type is itself an array, so we create an array to hold arrays of that type

                var arrayOfArrays = Array.CreateInstance(clrType.GetElementType(), outerArrayLength);

                for (int i = 0; i < outerArrayLength; i++)
                {
                    var inner = getArray(innerType!, clrType.GetElementType(), skip); // recurse

                    skip = inner.Skip;

                    arrayOfArrays.SetValueEx(inner.Array, i);
                }

                return (arrayOfArrays, skip);
            }
            else if (parameter.TryParseComponents(out var components))
            {
                // all arrays have their values stored in the Value property of the parameter

                var array = Array.CreateInstance(clrType.GetElementType(), outerArrayLength);

                int consumedSlots = 0;
                for (int i = 0; i < outerArrayLength; i++)
                {
                    // decode into a clone of the components, one clone per element of the array
                    var clonedComponents = CloneComponents(components!);

                    var slot = subSlots.Skip(skip).First(); // skip used here
                    int c = DecodeComponents(clonedComponents, slot, allSlots);
                    consumedSlots += c; // advance

                    // Convert components to dictionary for storage
                    var dic = new Dictionary<string, object?>();
                    foreach (var p in clonedComponents)
                    {
                        dic.Add(p.SafeName, p.Value);
                    }

                    array.SetValueEx(dic, i);
                }

                return (array, skip + consumedSlots);
            }
            else
            {
                // inner type is a non-array type, i.e. a single value, so we create an array
                // of that type and fill it with the values

                var array = Array.CreateInstance(clrType.GetElementType(), outerArrayLength);

                // fill the array

                int c = 0;
                foreach (var slot in subSlots.Skip(skip).Take(outerArrayLength)) // skip used here
                {
                    var value = this.DecodeStaticSlotValue(innerType!, clrType.GetElementType(), slot);

                    array.SetValueEx(value, c++);
                }

                // return the filled array and a skip value that will be used by the recursive caller
                // above to skip over the array values that have already been decoded

                return (array, skip + outerArrayLength);
            }
        }
    }

    private int DecodeStaticTuple(AbiParam parameter, Slot firstValueSlot, SlotCollection allSlots)
    {
        if (!parameter.TryParseComponents(out var components))
        {
            throw new AbiTypeException(
                $"Invalid tuple type: '{parameter.AbiType}' is not a valid tuple. " +
                $"Please ensure the ABI type is correctly formatted.",
                parameter.AbiType);
        }

        var dic = Activator.CreateInstance(parameter.ClrType) as IDictionary<string, object?>;

        if (dic == null)
        {
            throw new AbiTypeMismatchException(
                $"Unsupported tuple type: Expected IDictionary<string, object?> but got {parameter.ClrType.Name}. " +
                $"Tuples must be decoded into IDictionary<string, object?> containers.",
                parameter.AbiType,
                parameter.ClrType);
        }

        int consumedSlots = 0;
        var subSlots = allSlots.SkipTo(firstValueSlot);

        foreach (var p in components!)
        {
            var slot = subSlots.Skip(consumedSlots).FirstOrDefault();

            int c = this.DecodeStaticComponent(p, slot, allSlots);
            consumedSlots += c;

            dic.Add(p.SafeName, p.Value);
        }

        parameter.Value = dic;

        return consumedSlots;
    }

    // dyn

    private int DecodeDynamicValue(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        // a dynamic value has its pointer and then the length and then the data slots, e.g. string, bytes, etc.

        parameter.Value = this.DecodeDynamicSlotValue(parameter.AbiType, parameter.ClrType, pointer, allSlots);

        return 1;
    }

    private int DecodeDynamicArray(AbiParam parameter, Slot pointer, SlotCollection allSlots)
    {
        // for variable length arrays the length is in the first slot, else it's in the ABI type

        if (!AbiTypes.TryGetArrayOuterLength(parameter.AbiType, out var length))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine outer array length for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        if (!AbiTypes.TryGetArrayDimensions(parameter.AbiType, out var dimensions))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine dimensions for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        if (!AbiTypes.TryGetArrayBaseType(parameter.AbiType, out var baseType))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine base type for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        if (!AbiTypes.TryGetArrayInnerType(parameter.AbiType, out var innerType))
        {
            throw new AbiTypeException(
                $"Invalid array type: Failed to determine inner type for '{parameter.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.",
                parameter.AbiType);
        }

        var hasDynamicLength = length == -1;
        var hasDynamicBase = AbiTypes.IsDynamic(baseType!);
        var hasDynamicInner = AbiTypes.IsDynamic(innerType!);
        var hasInnerArray = AbiTypes.TryGetArrayDimensions(innerType!, out var innerDimensions);
        var baseClrType = parameter.BaseClrType;

        var subSlots = allSlots.SkipToPoint(pointer);
        var dataSlots = subSlots;
        int consumedSlots = 0;

        if (hasDynamicLength)
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

        if (hasInnerArray)
        {
            // e.g. string[2][4], uint8[2][][4], uint8[][3], (bool, bool)[][2]

            // we're handling an array of arrays, so create an array for this outer one and fill
            // it with decoded inner (array) elements

            var array = Array.CreateInstance(parameter.ClrType.GetElementType(), length);

            for (int i = 0; i < length; i++)
            {
                var slot = dataSlots[consumedSlots];
                var stuntParam = new AbiParam(0, $"{parameter.Name}.stunt", innerType!);

                if (hasDynamicInner) // if (isDynamicBase || isDynamicLength)
                {
                    // dynamic inner array, so the slot for this element will be a pointer

                    slot.DecodePointer(dataSlots);

                    this.DecodeDynamicArray(stuntParam, slot, allSlots); // recurse
                    consumedSlots += 1; // advanced +1 past the pointer

                    array.SetValueEx(stuntParam.Value, i); // stick the value from our stunt parameter into the array
                }
                else
                {
                    // static inner array, so the slot for this element will be the first value of
                    // a static layout of values, which means we'll need to advance i past those
                    // slots

                    int c = this.DecodeStaticArray(stuntParam, slot, allSlots);
                    consumedSlots += c; // advance past all static slots

                    array.SetValueEx(stuntParam.Value, i); // stick the value from our stunt parameter into the array
                }
            }

            parameter.Value = array;
        }
        else
        {
            // e.g. uint8[], bytes[], (bool, bool)[]

            // innermost array, so create the array and fill it with its decoded elements

            var array = Array.CreateInstance(baseClrType, length);

            for (int i = 0; i < length; i++)
            {
                // decode the elements into the array

                var slot = dataSlots[consumedSlots];

                if (hasDynamicBase)
                {
                    // e.g. string, bytes, (string, bool) demand a pointer

                    slot.DecodePointer(dataSlots);

                    if (parameter.TryParseComponents(out var components))
                    {
                        // decode into a clone of the components, one clone per element of the array
                        var clonedComponents = CloneComponents(components!);

                        DecodeComponents(clonedComponents, slot, allSlots);
                        consumedSlots += 1; // advance +1 past the pointer

                        checkTypeAndSet(array, parameter.AbiType, baseClrType, clonedComponents, i);
                    }
                    else
                    {
                        var obj = this.DecodeDynamicSlotValue(baseType!, baseClrType, slot, allSlots);
                        consumedSlots += 1; // advance +1 past the pointer to data

                        checkTypeAndSet(array, parameter.AbiType, baseClrType, obj, i);
                    }
                }
                else
                {
                    // e.g. uint8, bool, (bool, bool) are laid out sequentially

                    if (parameter.TryParseComponents(out var components))
                    {
                        // decode into a clone of the components, one clone per element of the array
                        var clonedComponents = CloneComponents(components!);

                        DecodeComponents(clonedComponents, slot, allSlots);
                        consumedSlots += 1;

                        checkTypeAndSet(array, parameter.AbiType, baseClrType, clonedComponents, i);
                    }
                    else
                    {
                        var obj = this.DecodeStaticSlotValue(baseType!, baseClrType, slot);
                        consumedSlots += 1; // advance +1 past the static value

                        checkTypeAndSet(array, parameter.AbiType, baseClrType, obj, i);
                    }
                }
            }

            parameter.Value = array;
        }

        return consumedSlots;

        //

        static void checkTypeAndSet(Array array, string abiType, Type supportedClrType, object? value, int index)
        {
            if (value is AbiParameters components)
            {
                if (typeof(IDictionary<string, object?>).IsAssignableFrom(supportedClrType))
                {
                    // Create dictionary from components, ensuring SafeName is never empty and keys are unique
                    var dict = new Dictionary<string, object?>();
                    foreach (var p in components)
                    {
                        // Ensure SafeName is never empty - fall back to position if it is
                        var key = string.IsNullOrWhiteSpace(p.SafeName) ? p.Position.ToString() : p.SafeName;
                        
                        // Ensure key is unique - append position if duplicate
                        var originalKey = key;
                        int suffix = 0;
                        while (dict.ContainsKey(key))
                        {
                            suffix++;
                            key = $"{originalKey}_{suffix}";
                        }
                        
                        dict.Add(key, p.Value);
                    }
                    value = dict;
                }
                else
                {
                    throw new AbiNotSupportedException(
                        $"Tuple components cannot be converted to collections other than IDictionary<string, object?>. " +
                        $"See {nameof(AbiTypes)}.{nameof(AbiTypes.TryGetDefaultClrType)} for supported " +
                        $"collection types for tuples.");
                }
            }

            array.SetValueEx(value, index);
        }
    }

    //

    private object? DecodeString(string abiType, Type clrType, IList<Slot> slots, int length)
    {
        this.logger.LogDebug(
            "Decoding string (type: {AbiType}, length: {Length})",
            abiType,
            length);

        if (length == 0)
        {
            this.logger.LogDebug("Empty string detected, returning empty string");
            return string.Empty;
        }

        var bytes = SlotsToBytes(slots, length);

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            this.logger.LogError(
                "Failed to resolve canonical type for '{AbiType}'",
                abiType);

            throw new AbiInternalException(
                $"Internal error: Failed to resolve canonical type for '{abiType}'. " +
                $"This is likely a bug in the ABI decoder implementation.",
                abiType);
        }

        foreach (var decoder in this.dynamicTypeDecoders)
        {
            this.logger.LogTrace(
                "Trying {DecoderType} for string decoding",
                decoder.GetType().Name);

            if (decoder.TryDecode(canonicalType, bytes, clrType, out var value))
            {
                this.logger.LogDebug(
                    "Successfully decoded string using {DecoderType}: {Value}",
                    decoder.GetType().Name,
                    value?.ToString() ?? "null");

                return value;
            }
        }

        this.logger.LogError(
            "No decoder found for string (type: {AbiType}, CLR type: {ClrType})",
            abiType,
            clrType.Name);

        throw new AbiNotSupportedException(
            $"No decoder found: ABI type '{abiType}' with CLR type '{clrType.Name}' is not supported. " +
            $"Please use a supported type combination or implement a custom decoder.",
            abiType,
            clrType);
    }

    private object? DecodeStaticSlotValue(string abiType, Type clrType, Slot slot)
    {
        this.logger.LogDebug(
            "Decoding static slot (type: {AbiType}, CLR type: {ClrType})",
            abiType,
            clrType.Name);

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            this.logger.LogError(
                "Failed to resolve canonical type for '{AbiType}'",
                abiType);

            throw new AbiInternalException(
                $"Internal error: Failed to resolve canonical type for '{abiType}'. " +
                $"This is likely a bug in the ABI decoder implementation.",
                abiType);
        }

        foreach (var staticDecoder in this.staticTypeDecoders)
        {
            this.logger.LogTrace(
                "Trying {DecoderType} for static value",
                staticDecoder.GetType().Name);

            if (staticDecoder.TryDecode(canonicalType, slot.Data, clrType, out var value))
            {
                this.logger.LogDebug(
                    "Successfully decoded static value using {DecoderType}: {AbiType} -> {Value}",
                    staticDecoder.GetType().Name,
                    abiType,
                    value?.ToString() ?? "null");

                return value;
            }
        }

        if (slot.IsNull)
        {
            this.logger.LogDebug("Null slot detected, returning null");
            return null;
        }

        this.logger.LogError(
            "No decoder found for static value (type: {AbiType}, CLR type: {ClrType})",
            abiType,
            clrType.Name);

        throw new AbiNotSupportedException(
            $"No decoder found: ABI type '{abiType}' with CLR type '{clrType.Name}' is not supported. " +
            $"Please use a supported type combination or implement a custom decoder.",
            abiType,
            clrType);
    }

    private object? DecodeDynamicSlotValue(string abiType, Type clrType, Slot pointer, SlotCollection allSlots)
    {
        this.logger.LogDebug(
            "Decoding dynamic slot (type: {AbiType}, CLR type: {ClrType})",
            abiType,
            clrType.Name);

        var subSlots = allSlots.SkipToPoint(pointer);
        var lengthSlot = subSlots[0];
        int length = this.DecodeLength(lengthSlot);

        this.logger.LogDebug(
            "Dynamic value length: {Length} slots",
            length);

        var dataSlots = subSlots.Skip(1).Take(length).ToList();

        if (abiType == AbiTypeNames.Bytes)
        {
            this.logger.LogDebug(
                "Decoding as bytes array of length {Length} from {SlotCount} slots",
                length,
                dataSlots.Count);

            var result = SlotsToBytes(dataSlots, length);

            this.logger.LogDebug(
                "Decoded bytes array: 0x{Bytes}",
                BitConverter.ToString(result).Replace("-", ""));

            return result;
        }
        else if (abiType == AbiTypeNames.String)
        {
            this.logger.LogDebug("Decoding as string");

            return this.DecodeString(abiType, clrType, dataSlots, length);
        }
        else
        {
            this.logger.LogError(
                "Unsupported dynamic type: {AbiType}",
                abiType);

            throw new AbiTypeException(
                $"Unsupported dynamic type: '{abiType}' is not a recognized dynamic ABI type. " +
                $"Only 'bytes' and 'string' are supported as basic dynamic types.",
                abiType);
        }
    }

    private int DecodeLength(Slot lengthSlot)
    {
        var bigLength = (BigInteger)this.DecodeStaticSlotValue(AbiTypeNames.IntegerTypes.Uint, typeof(BigInteger), lengthSlot)!;
        var length = (int)bigLength;
        this.logger.LogDebug(
            "Decoded length value: {Length} (from BigInteger {BigLength})",
            length,
            bigLength);
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

    /// <summary>
    /// Clones a collection of AbiParam components, creating new instances with the same type information
    /// but without values. This is needed when decoding arrays of tuples to avoid reusing the same
    /// component objects for multiple array elements.
    /// </summary>
    private static AbiParameters CloneComponents(AbiParameters components)
    {
        var cloned = new List<AbiParam>();
        foreach (var component in components)
        {
            cloned.Add(new AbiParam(
                component.Position,
                component.Name,
                component.Descriptor,
                component.ArrayLengths)
            {
                IsIndexed = component.IsIndexed
            });
        }
        return new AbiParameters(cloned);
    }

}

