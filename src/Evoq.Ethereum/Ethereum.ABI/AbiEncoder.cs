using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

record class EncodingContext(
    string AbiType,
    string Key,
    object? Value,
    SlotCollection Block,
    bool HasPointer,
    bool IsParameter,
    EncodingContext? Parent = null)
{
    public bool IsRoot => this.Parent == null;
    public bool IsAbiDynamic => AbiTypes.IsDynamic(this.AbiType);
    public bool IsAbiTupleStrict => AbiTypes.IsTuple(this.AbiType, false);
    public bool IsAbiTupleArray => AbiTypes.IsTuple(this.AbiType, true);
    public bool IsAbiArray => AbiTypes.IsArray(this.AbiType);

    public bool IsValueArray(out Array? array)
    {
        if (this.IsAbiArray && this.Value is Array valueArray)
        {
            array = valueArray;
            return true;
        }

        array = null;
        return false;
    }

    public bool IsValueDic(out IDictionary<string, object?>? dictionary)
    {
        if (this.Value is IDictionary<string, object?> valueDictionary)
        {
            dictionary = valueDictionary;
            return true;
        }

        dictionary = null;
        return false;
    }

    [Obsolete]
    public bool IsValueList(out IReadOnlyList<object?>? list)
    {
        if (this.IsAbiTupleStrict && this.Value is IReadOnlyList<object?> valueList)
        {
            list = valueList;
            return true;
        }

        if (this.IsAbiTupleStrict && this.Value is ITuple tuple)
        {
            list = tuple.GetElements().ToList();
            return true;
        }

        list = null;
        return false;
    }

    public EncodingContext? GetParameterContext()
    {
        if (this.IsParameter)
        {
            return this;
        }

        return this.Parent?.GetParameterContext();
    }

    public override string ToString()
    {
        if (Object.ReferenceEquals(this.Parent, this))
        {
            throw new InvalidOperationException("Parent and self are the same object");
        }

        if (this.Parent == null)
        {
            return $"{this.AbiType}";
        }

        var parentString = this.Parent.ToString();
        var prefix = this.Parent != null ? "." : "";

        return $"{parentString}{prefix}{this.AbiType}";
    }
};

/// <summary>
/// A new and improved ABI encoder that uses a more efficient encoding scheme.
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
    /// Encodes the parameters.
    /// </summary>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="keyValues">The values to encode.</param>
    /// <returns>The encoded parameters.</returns>
    /// <exception cref="ArgumentException">Thrown if the number of values does not match the number of parameters.</exception>
    public AbiEncodingResult EncodeParameters(AbiParameters parameters, IDictionary<string, object?> keyValues)
    {
        var slotCount = parameters.Count;

        if (slotCount != keyValues.Count)
        {
            string type = parameters.GetCanonicalType();
            string valuesStr = string.Join(", ", keyValues.Select(kv => $"{kv.Key}: {kv.Value?.ToString() ?? "null"}"));

            throw new AbiEncodingException(
                $"Unable to encode parameters for signature {type}: expected {slotCount} values but got {keyValues.Count}: '{valuesStr}'. " +
                "Note that nested tuples can be a source of confusion.",
                type);
        }

        var root = new SlotCollection(capacity: slotCount * 8);
        var heads = new SlotCollection(capacity: slotCount * 8);
        var tails = new List<SlotCollection>(capacity: slotCount);

        for (int i = 0; i < slotCount; i++)
        {
            var parameter = parameters[i];
            var keyValue = keyValues.ElementAt(i);
            var context = new EncodingContext(parameter.AbiType, keyValue.Key, keyValue.Value, root, true, true);

            if (AbiTypes.IsDynamic(parameter.AbiType))
            {
                // dynamic, so we need a pointer in the heads and a tail for the dynamic value

                var tail = new SlotCollection(capacity: 8);
                tails.Add(tail);

                var pointerSlot = new Slot(
                    Name(context, "pointer_dyn_item"),
                    pointsToFirst: tail,
                    relativeTo: heads);
                heads.Add(pointerSlot);

                this.EncodeValue(new EncodingContext(parameter.AbiType, keyValue.Key, keyValue.Value, tail, true, false, context));
            }
            else
            {
                // static, so we can encode directly into the heads

                this.EncodeValue(new EncodingContext(parameter.AbiType, keyValue.Key, keyValue.Value, heads, false, false, context));
            }
        }

        // pour the heads and tails into the root

        root.AddRange(heads);
        root.AddRange(tails.SelectMany(tail => tail));

        return new AbiEncodingResult(root);
    }

    // privates

    // single-slot fixed-size values

    private void EncodeSingleSlotStaticValue(EncodingContext context)
    {
        // e.g. bool or uint256, but not tuples like (bool,uint256)

        if (context.IsAbiTupleStrict)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is a tuple, not a single slot static value",
                context.AbiType);
        }

        if (context.Value == null)
        {
            context.Block.Add(new Slot());
            return;
        }

        if (!this.TryFindStaticSlotEncoder(context, out var encoder))
        {
            throw NotImplemented(context.AbiType, context.Value.GetType());
        }

        context.Block.Add(encoder!(context.Value));
    }

    // single-slot fixed-size iterables

    private void EncodeSingleSlotArray(EncodingContext context)
    {
        // e.g. bool[2][2] or uint256[2], but not tuples like (bool,uint256)[4]

        // fixed size array of single slot values, including pointers
        // all elements are directly encoded into the block with no count
        // of elements since this can be determined from the ABI type

        if (context.IsAbiTupleStrict)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is a tuple, not an array",
                context.AbiType);
        }

        if (!AbiTypes.TryGetArrayInnerType(context.AbiType, out var innerType))
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is not an array",
                context.AbiType);
        }

        if (!context.IsValueArray(out var array))
        {
            throw new AbiTypeMismatchException(
                $"The value is not an array",
                context.AbiType,
                context.Value?.GetType());
        }

        // encode each element directly into the block

        for (int i = 0; i < array!.Length; i++)
        {
            var element = array.GetValue(i);

            this.EncodeValue(new EncodingContext(innerType!, context.Key, element, context.Block, false, false, context));
        }
    }

    private void EncodeSingleSlotTuple(EncodingContext context)
    {
        // e.g. (bool, uint256) or (bool, (uint256, uint256))

        // simply iterate over the components and directly encode them into the block,
        // one after the other

        if (context.IsAbiDynamic)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is dynamic, not a fixed size tuple",
                context.AbiType);
        }

        if (context.IsAbiArray)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is an array, not a tuple",
                context.AbiType);
        }

        if (!context.IsAbiTupleStrict)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is not a tuple",
                context.AbiType);
        }

        if (!context.IsValueList(out var list))
        {
            throw new AbiTypeMismatchException(
                $"The value is not a list of values",
                context.AbiType,
                context.Value?.GetType());
        }

        var abiParams = AbiParameters.Parse(context.AbiType);

        for (int i = 0; i < abiParams.Count; i++)
        {
            var parameter = abiParams[i];
            var componentValue = list![i];

            this.EncodeValue(new EncodingContext(
                parameter.AbiType, parameter.Name, componentValue, context.Block, false, false, context)); // send back into the router
        }
    }

    // dynamic variable-size value

    private void EncodeDynamicValue(EncodingContext context)
    {
        // e.g. string or bytes, but not tuples like (string,bytes) or arrays like string[] or bytes[]

        if (context.IsAbiTupleStrict)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is a tuple, not a single slot dynamic value",
                context.AbiType);
        }

        if (context.IsAbiArray)
        {
            throw new AbiTypeException(
                $"The type {context.AbiType} is an array, not a single slot dynamic value",
                context.AbiType);
        }

        if (context.Value == null)
        {
            context.Block.Add(new Slot());
            return;
        }

        if (!this.TryFindDynamicBytesEncoder(context, out var encoder))
        {
            throw NotImplemented(context.AbiType, context.Value.GetType());
        }

        byte[] paddedBytes = encoder!(context.Value);
        int length;

        if (context.Value is byte[] bytesValue)
        {
            length = bytesValue.Length;
        }
        else if (context.Value is string stringValue)
        {
            length = Encoding.UTF8.GetBytes(stringValue).Length;
        }
        else
        {
            throw new ArgumentException($"The value of type {context.Value.GetType()} must be a byte array or string");
        }

        // encode the value into the tail and add the offset pointer to the head

        var lengthSlot = new Slot(Name(context, "length"), UintTypeEncoder.EncodeUint(256, length));
        var bytesSlots = BytesToSlots(context, paddedBytes);

        var heads = new SlotCollection(capacity: 1);
        var tail = new SlotCollection(capacity: bytesSlots.Count);

        if (!context.HasPointer)
        {
            // assume the outer loop has already added a pointer to the first slot of the tail

            var pointerSlot = new Slot(
                Name(context, "pointer_dyn_value"),
                pointsToFirst: tail,
                relativeTo: heads);
            heads.Add(pointerSlot);
        }

        tail.Add(lengthSlot);
        tail.AddRange(bytesSlots);

        // now pour those two into the block

        context.Block.AddRange(heads);
        context.Block.AddRange(tail);
    }

    // dynamic variable-size iterables

    private void EncodeDynamicArray(EncodingContext context)
    {
        // e.g. uint8[] or bool[][2] or uint256[][2] or string[], or even (bool,uint256)[]

        // similar to the tuple case, except the type is always the same

        Array array;
        if (context.Value is Array valueArray)
        {
            array = valueArray;
        }
        else if (context.Value == null)
        {
            throw new AbiTypeMismatchException(
                $"Type mismatch: ABI type '{context.AbiType}' requires an array value, but received null. " +
                $"Please provide a compatible array type for encoding.",
                context.AbiType,
                null);
        }
        else if (!TryConvertToArray(context.Value, out array))
        {
            throw new AbiTypeMismatchException(
                $"Type mismatch: ABI type '{context.AbiType}' requires an array value, but received {context.Value.GetType().Name}. " +
                $"The value could not be converted to an array. Please provide a compatible array or collection type.",
                context.AbiType,
                context.Value.GetType());
        }

        if (!AbiTypes.TryGetArrayInnerType(context.AbiType, out var innerType))
        {
            throw new AbiTypeException(
                $"Invalid array type: '{context.AbiType}' could not be parsed as an array. " +
                $"This may indicate a malformed ABI type string.",
                context.AbiType);
        }

        if (!AbiTypes.TryGetArrayDimensions(context.AbiType, out var dimensions))
        {
            throw new ArgumentException(
                $"Invalid array dimensions: Could not determine dimensions for ABI type '{context.AbiType}'. " +
                $"Please ensure the array type is correctly formatted.");
        }
        bool isVariableLength = dimensions.First() == -1;

        if (context.HasPointer && false) // FORCE FALSE FOR NOW
        {
            // already within an iterable, e.g. the outermost parameters, or an array or tuple

            // what difference does it make if we're already within an array?

            throw new NotImplementedException(
                $"Nested dynamic arrays not yet supported: Found dynamic array within another array. " +
                $"Support for encoding nested dynamic arrays is planned for a future release.");
        }
        else
        {
            // caller is responsible for adding the single pointer for this new dynamic array:
            //
            // we start with a count followed by the elements of type T (the inner type)
            // which will either be pointers to their own tails or the actual encoded data
            // if the inner type is not dynamic
            //
            // the count is only added if the array is variable length, otherwise the count
            // is implied by the type and the number of elements in the array
            //
            // NOTE / the count does not form part of the "official" encoding of the array
            // so it is not regarded as the slot from which pointers are relative to, so we
            // make a set of heads that pointers can be relative to

            if (isVariableLength)
            {
                var countSlot = new Slot(Name(context, "count"), UintTypeEncoder.EncodeUint(256, array.Length));
                context.Block.Add(countSlot);
            }

            if (AbiTypes.IsDynamic(innerType!))
            {
                // dynamic inner type, so we need new pointers and tails for each element

                var heads = new SlotCollection(capacity: 8);
                var elementTails = new List<SlotCollection>(capacity: array.Length + 1);

                for (int i = 0; i < array.Length; i++)
                {
                    var elementTail = new SlotCollection(capacity: 8);
                    elementTails.Add(elementTail);

                    var elementPointerSlot = new Slot(
                        Name(context, $"pointer_dyn_elem_{i}"),
                        pointsToFirst: elementTail,
                        relativeTo: heads); // not relative to block, because the count slot is not regarded as part of the encoding!

                    heads.Add(elementPointerSlot);

                    var elementValue = array.GetValue(i);

                    // recursive call to encode will drop back into this function
                    // with the tail as the block, if the inner type is dynamic

                    this.EncodeValue(new EncodingContext(
                        innerType!, context.Key, elementValue, elementTail, true, false, context));
                }

                // heads then tails

                context.Block.AddRange(heads);
                context.Block.AddRange(elementTails.SelectMany(tail => tail));
            }
            else
            {
                // fixed size inner type, so we can encode directly into the block

                for (int i = 0; i < array.Length; i++)
                {
                    var elementValue = array.GetValue(i);

                    this.EncodeValue(new EncodingContext(
                        innerType!, context.Key, elementValue, context.Block, false, false, context));
                }
            }
        }
    }

    private void EncodeDynamicTuple(EncodingContext context)
    {
        // e.g. (bool, uint256) or (bool, (uint256, uint256))

        // a tuple is like a fixed-size dynamic array; the size is determined by the
        // number of components in the tuple's ABI type (bool, uint256) has two

        if (!context.IsValueList(out var tuple))
        {
            throw new ArgumentException($"The type {context.AbiType} is not a tuple");
        }

        var heads = new SlotCollection(capacity: 8); // heads for this dynamic tuple
        var tails = new List<SlotCollection>();      // tails for each dynamic component

        var evmParams = AbiParameters.Parse(context.AbiType);

        for (int i = 0; i < evmParams.Count; i++)
        {
            var componentType = evmParams[i].AbiType;
            var componentKey = evmParams[i].Name;
            var componentValue = tuple![i];

            // we need a new tail for each dynamic component

            if (AbiTypes.IsDynamic(componentType))
            {
                // we need a tail for this dynamic value
                // and a pointer to the first slot of the tail

                var tail = new SlotCollection(capacity: 8);
                tails.Add(tail);

                var pointerSlot = new Slot(
                    Name(context, $"pointer_dyn_comp_{i}"),
                    pointsToFirst: tail,
                    relativeTo: heads);
                heads.Add(pointerSlot);

                this.EncodeValue(new EncodingContext(
                    componentType, componentKey, componentValue, tail, true, false, context));
            }
            else
            {
                // write the value directly into the heads

                this.EncodeValue(new EncodingContext(
                    componentType, componentKey, componentValue, heads, false, false, context));
            }
        }

        context.Block.AddRange(heads);
        context.Block.AddRange(tails.SelectMany(tail => tail)); // pour the all tail slots into the block
    }

    // router

    private void EncodeValue(EncodingContext context)
    {
        bool isArray = context.IsAbiArray;
        bool isTuple = context.IsAbiTupleStrict;
        bool isDynamic = context.IsAbiDynamic;
        bool hasPointer = context.HasPointer;

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
                if (hasPointer)
                {
                    // already has a pointer, so we can just encode the dynamic array

                    this.EncodeDynamicArray(context);
                }
                else
                {
                    // fresh encounter with this dynamic value, so we need a pointer

                    var tail = new SlotCollection(capacity: 8);
                    var pointerSlot = new Slot(
                        Name(context, "pointer_dyn_array"),
                        pointsToFirst: tail,
                        relativeTo: context.Block);

                    context.Block.Add(pointerSlot);

                    this.EncodeDynamicArray(new EncodingContext(
                        context.AbiType, context.Key, context.Value, tail, true, true, context.Parent));

                    context.Block.AddRange(tail);
                }
            }
            else if (isTuple)
            {
                // e.g. (bool, bytes) or (bool, (uint256[][2], uint256))

                if (hasPointer)
                {
                    // TODO
                }

                // TODO / I reckon this will need a pointer as per the array above

                this.EncodeDynamicTuple(context);
            }
            else
            {
                // e.g. bytes, string

                this.EncodeDynamicValue(context);
            }
        }
        else // not dynamic, i.e. the ABI type conveys the size of the value
        {
            // single slot, directly encoded into the block

            // e.g. bool or uint256, bool[2] or uint256[2][2] or (bool,uint256), (bool,uint256)[2]

            if (isArray)
            {
                this.EncodeSingleSlotArray(context);
            }
            else if (isTuple)
            {
                // a tuple can only appear within another tuple, and a tuple is paired with a parameter

                if (context.IsValueList(out var list))
                {
                    this.EncodeSingleSlotTuple(context);
                }
                else
                {
                    throw new ArgumentException(
                        $"The type {context.AbiType} requires a tuple value",
                        nameof(context.Value));
                }
            }
            else
            {
                // base case for single slot types like bool or uint256

                this.EncodeSingleSlotStaticValue(context);
            }
        }
    }

    //

    private static Exception NotImplemented(string abiType, Type clrType) =>
        new NotImplementedException(
            $"Encoding not implemented: ABI type '{abiType}' with .NET type '{clrType}' is not supported. " +
            $"Please use a supported type combination or implement a custom encoder.");

    private bool TryFindStaticSlotEncoder(EncodingContext context, out Func<object, Slot>? encoder)
    {
        if (context.Value == null)
        {
            encoder = _ => new Slot(Name(context, "null"), new byte[32]); // null value is encoded as a 32-byte zero value
            return true;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(context.AbiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException(
                $"Internal error: Failed to resolve canonical type for '{context.AbiType}'. " +
                $"This is likely a bug in the ABI encoder implementation.");
        }

        foreach (var staticEncoder in this.staticTypeEncoders)
        {
            if (staticEncoder.TryEncode(canonicalType, context.Value, out var bytes))
            {
                encoder = _ => new Slot(Name(context, "value"), bytes);
                return true;
            }
        }

        encoder = null;
        return false;
    }

    private bool TryFindDynamicBytesEncoder(EncodingContext context, out Func<object, byte[]>? encoder)
    {
        if (context.Value == null)
        {
            encoder = _ => new byte[32]; // null value is encoded as a 32-byte zero value
            return true;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(context.AbiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {context.AbiType}");
        }

        foreach (var dynamicEncoder in this.dynamicTypeEncoders)
        {
            if (dynamicEncoder.TryEncode(canonicalType, context.Value, out var bytes))
            {
                encoder = _ => bytes;
                return true;
            }
        }

        encoder = null;
        return false;
    }

    //

    private static SlotCollection BytesToSlots(EncodingContext context, byte[] paddedBytes)
    {
        return SlotCollection.FromBytes(Name(context, "chunk"), paddedBytes);
    }

    private static string Name(EncodingContext context, string name)
    {
        return $"{context}.{name}";
    }

    /// <summary>
    /// Tries to convert any object to an Array.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="array">When this method returns, contains the Array if conversion was successful, or null if conversion failed.</param>
    /// <returns>true if the conversion was successful; otherwise, false.</returns>
    private static bool TryConvertToArray(object? value, out Array array)
    {
        if (value is Array valueArray)
        {
            array = valueArray;
            return true;
        }

        if (value is not System.Collections.IEnumerable items)
        {
            array = Array.Empty<object>();
            return false;
        }

        try
        {
            // First, collect all items into a generic list
            var list = new List<object?>();
            foreach (var item in items)
            {
                list.Add(item);
            }

            // Determine the element type
            Type elementType = typeof(object);

            // Try to infer a more specific element type if all elements are of the same type
            if (list.Count > 0)
            {
                var firstNonNull = list.FirstOrDefault(x => x != null);
                if (firstNonNull != null)
                {
                    elementType = firstNonNull.GetType();

                    // Check if all elements are of the same type or can be converted to it
                    bool allSameType = list.All(item => item == null || item.GetType() == elementType);

                    if (!allSameType)
                    {
                        // Fall back to object if types are mixed
                        elementType = typeof(object);
                    }
                }
            }

            // Create and populate the array
            Array result = Array.CreateInstance(elementType, list.Count);
            for (int i = 0; i < list.Count; i++)
            {
                result.SetValue(list[i], i);
            }

            array = result;
            return true;
        }
        catch
        {
            // If any exception occurs during conversion, return false
            array = Array.Empty<object>();
            return false;
        }
    }
}
