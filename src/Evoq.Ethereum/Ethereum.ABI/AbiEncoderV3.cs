using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

class EncodingContextV3
{
    public EncodingContextV3(
        string abiType,
        object value,
        EncodingContextV3? parent = null)
    {
        this.AbiType = abiType;
        this.Value = value;
        this.Parent = parent;
    }

    //

    public string AbiType { get; init; }
    public object Value { get; init; }
    public EncodingContextV3? Parent { get; init; }
    public HeadSlotCollection Heads { get; } = new(capacity: 8);
    public TailSlotCollection Tails { get; } = new(capacity: 8);

    public bool IsRoot => Parent == null;
    public bool IsDynamic => AbiTypes.IsDynamic(AbiType);
    public bool IsTuple => AbiTypes.IsTuple(AbiType);
    public bool IsArray => AbiTypes.IsArray(AbiType);

    //

    public bool IsValueArray(out Array? array)
    {
        if (IsArray && Value is Array valueArray)
        {
            array = valueArray;
            return true;
        }

        array = null;
        return false;
    }

    public bool IsValueTuple(out ITuple? tuple)
    {
        if (IsTuple && Value is ITuple valueTuple)
        {
            tuple = valueTuple;
            return true;
        }

        tuple = null;
        return false;
    }

    public void AddSlots(IEnumerable<Slot> slots)
    {
        if (this.IsDynamic)
        {
            this.Tails.AddRange(slots);
        }
        else
        {
            this.Heads.AddRange(slots);
        }
    }

    public void AddToHeads(SlotCollection slots)
    {
        this.Heads.AddRange(slots);
    }

    public void AddToTails(SlotCollection slots)
    {
        if (!this.IsDynamic)
        {
            throw new InvalidOperationException("Cannot add tails to a static type");
        }

        this.Tails.AddRange(slots);
    }

    public void AddPointerHead(string name, SlotCollection to, SlotCollection from)
    {
        var pointerSlot = new Slot($"{this.Parent}.{name}", to, from);
        this.Heads.Add(pointerSlot);
    }

    public void AddLengthTail(int length)
    {
        var lengthSlot = new Slot($"{this.Parent}.length", UintTypeEncoder.EncodeUint(256, length));
        this.Tails.Add(lengthSlot);
    }

    public void AddCountTail(int count)
    {
        var countSlot = new Slot($"{this.Parent}.count", UintTypeEncoder.EncodeUint(256, count));
        this.Tails.Add(countSlot);
    }

    public void AddEmptyHead()
    {
        this.Heads.Add(new Slot($"{this.Parent}.empty", new byte[32]));
    }

    public void AddBytesHead(byte[] rawBytes)
    {
        this.Heads.Add(new Slot($"{this.Parent}.value", rawBytes));
    }

    public void AddBytesTails(byte[] rawBytes)
    {
        var paddedBytes = BytesTypeEncoder.EncodeBytes(rawBytes);
        bool hasRemainingBytes = paddedBytes.Length % 32 != 0;

        Debug.Assert(!hasRemainingBytes, "Has remaining bytes; bytes expected to be a multiple of 32");

        for (int i = 0; i < paddedBytes.Length; i += 32)
        {
            var chunk = new byte[32];
            var count = Math.Min(32, paddedBytes.Length - i);
            Buffer.BlockCopy(paddedBytes, i, chunk, 0, count);

            this.Tails.Add(new Slot($"{this.Parent}.chunk_{i}", chunk));
        }
    }

    public SlotCollection GatherAllSlots()
    {
        var all = new SlotCollection(Heads.Count + Tails.Count);
        all.AddRange(Heads);
        all.AddRange(Tails);
        return all;
    }

    public EncodingContextV3 Spawn(string abiType, object value)
    {
        return new EncodingContextV3(abiType, value, this);
    }

    public override string ToString()
    {
        if (Object.ReferenceEquals(Parent, this))
        {
            throw new InvalidOperationException("Parent and self are the same object");
        }

        if (this.Parent == null)
        {
            return $"{this.AbiType}";
        }

        var parentString = this.Parent.ToString();

        return $"{parentString}{"."}{this.AbiType}";
    }
}

public class AbiEncoderV3 : IAbiEncoder
{
    private readonly IReadOnlyList<IAbiEncode> staticTypeEncoders;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiEncoderV3"/> class.
    /// </summary>
    public AbiEncoderV3()
    {
        this.staticTypeEncoders = new AbiStaticTypeEncoders();
    }

    //

    public AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values)
    {
        var context = new EncodingContextV3(parameters.ToString(), values);

        var slots = this.EncodeSeries(context);

        return new AbiEncodingResult(slots);
    }

    //

    private SlotCollection EncodeValue(EncodingContextV3 context)
    {
        if (context.IsDynamic)
        {
            if (context.IsArray)
            {
                return this.EncodeDynamicArray(context);
            }
            else if (context.IsTuple)
            {
                return this.EncodeSeries(context);
            }
            else
            {
                return this.EncodeDynamicValue(context);
            }
        }
        else
        {
            if (context.IsArray)
            {
                return this.EncodeStaticArray(context);
            }
            else if (context.IsTuple)
            {
                return this.EncodeStaticTuple(context);
            }
            else
            {
                return this.EncodeStaticValue(context);
            }
        }
    }

    // static encoders

    private SlotCollection EncodeStaticArray(EncodingContextV3 context)
    {
        // e.g. uint8[2] or bool[2][2] or uint256[2], but not tuples like (bool,uint256)[4]

        if (context.IsTuple)
        {
            throw new ArgumentException($"The type {context.AbiType} is a tuple, not an array");
        }

        if (context.IsDynamic)
        {
            throw new ArgumentException($"The type {context.AbiType} is dynamic, not a static array");
        }

        addSlotsToContext(context);

        return context.GatherAllSlots();

        //

        void addSlotsToContext(EncodingContextV3 c)
        {
            // e.g. bool[2] or uint256[2][2] etc.

            if (c.Value is not Array array)
            {
                throw new ArgumentException($"The value is not an array");
            }
            else
            {
                if (!AbiTypes.TryRemoveOuterArrayDimension(c.AbiType, out var innerType))
                {
                    throw new NotImplementedException($"The type '{c.AbiType}' is not an array");
                }

                if (!AbiTypes.TryGetArrayDimensions(c.AbiType, out var dimensions))
                {
                    throw new ArgumentException($"The type {c.AbiType} is not an array");
                }

                for (int i = 0; i < array.Length; i++)
                {
                    // e.g. uint256[2] or bool[2][2] etc.

                    var elementValue = array.GetValue(i);
                    var elementContext = c.Spawn(innerType!, elementValue);

                    if (dimensions!.Count == 1)
                    {
                        // we have reached the innermost array

                        c.AddSlots(this.EncodeValue(elementContext));
                    }
                    else
                    {
                        // we still have yet to reach the innermost array

                        addSlotsToContext(elementContext);

                        c.AddSlots(elementContext.GatherAllSlots());
                    }
                }
            }
        }
    }

    private SlotCollection EncodeStaticTuple(EncodingContextV3 context)
    {
        // e.g. (bool, uint256) or (bool, (uint256, uint256))

        // simply iterate over the components and directly encode them into the head,
        // one after the other

        if (context.IsDynamic)
        {
            throw new ArgumentException($"The type {context.AbiType} is dynamic, not a fixed size tuple");
        }

        if (context.IsArray)
        {
            throw new ArgumentException($"The type {context.AbiType} is an array, not a tuple");
        }

        if (!context.IsTuple)
        {
            throw new ArgumentException($"The type {context.AbiType} is not a tuple");
        }

        if (!context.IsValueTuple(out var tuple))
        {
            throw new ArgumentException($"The value is not an ITuple");
        }

        var evmParams = EvmParameters.Parse(context.AbiType);

        for (int i = 0; i < evmParams.Count; i++)
        {
            var parameter = evmParams[i];
            var componentValue = tuple![i];

            var innerContext = context.Spawn(parameter.AbiType, componentValue);

            context.AddSlots(this.EncodeValue(innerContext));
        }

        return context.GatherAllSlots();
    }

    private SlotCollection EncodeStaticValue(EncodingContextV3 context)
    {
        // e.g. bool or uint256, but not tuples like (bool,uint256)

        if (context.IsTuple)
        {
            throw new ArgumentException($"The type {context.AbiType} is a tuple, not a single slot static value");
        }

        if (context.IsArray)
        {
            throw new ArgumentException($"The type {context.AbiType} is an array, not a single slot static value");
        }

        if (context.IsDynamic)
        {
            throw new ArgumentException($"The type {context.AbiType} is dynamic, not a single slot static value");
        }

        if (!this.TryAddStaticSlot(context))
        {
            throw NotImplemented(context.AbiType, context.Value.GetType().ToString());
        }

        return context.GatherAllSlots();
    }

    // dynamic encoders

    private SlotCollection EncodeDynamicArray(EncodingContextV3 context)
    {
        // e.g. bool[] or uint256[][2] or bytes[2] or bool[2][]

        // dynamic values need a pointer in the head pointing to the tail where the
        // actual values are encoded

        if (context.Value is not Array array)
        {
            throw new ArgumentException($"The value is not an array");
        }

        if (!AbiTypes.TryRemoveOuterArrayDimension(context.AbiType, out var innerType))
        {
            throw new NotImplementedException($"The type '{context.AbiType}' is not an array");
        }

        if (!AbiTypes.TryGetArrayDimensions(context.AbiType, out var dimensions))
        {
            throw new ArgumentException($"The type {context.AbiType} is not an array");
        }

        // context.AddPointerHead("pointer_dyn_array");

        bool isVariableLengthOuter = dimensions.First() == -1;
        if (isVariableLengthOuter)
        {
            // e.g. bool[] or uint256[][] needs a count slot

            context.AddCountTail(array.Length);
        }

        // each item in the array adds a head of either the value or its pointer

        List<SlotCollection> tailses = new(array.Length);

        for (int i = 0; i < array.Length; i++)
        {
            var value = array.GetValue(i);
            var elemContext = context.Spawn(innerType!, value); // if elem is dynamic, will add a pointer to current heads, relative to current context

            this.EncodeValue(elemContext);  // add heads and tails to the child context as appropriate

            context.AddSlots(elemContext.Heads);
            tailses.Add(elemContext.Tails);
        }

        // add the tails to the root context

        context.AddSlots(tailses.SelectMany(c => c)); // if current value is dynamic, this will add to tails of the current context

        return context.GatherAllSlots();



        // context.AddPointerHead("pointer_dyn_array");
        // addSlotsToContext(context);

        // return context.GatherAllSlots();

        // //

        // void addSlotsToContext(EncodingContextV3 c)
        // {
        //     // e.g. bool[] or uint256[][2] or bytes[2] or bool[2][]

        //     if (c.Value is not Array array)
        //     {
        //         throw new ArgumentException($"The value is not an array");
        //     }
        //     else
        //     {
        //         if (!AbiTypes.TryRemoveOuterArrayDimension(c.AbiType, out var innerType))
        //         {
        //             throw new NotImplementedException($"The type '{c.AbiType}' is not an array");
        //         }

        //         if (!AbiTypes.TryGetArrayDimensions(c.AbiType, out var dimensions))
        //         {
        //             throw new ArgumentException($"The type {c.AbiType} is not an array");
        //         }

        //         bool isVariableLengthOuter = dimensions.First() == -1;
        //         if (isVariableLengthOuter)
        //         {
        //             // e.g. bool[] or uint256[][] needs a count slot

        //             c.AddCountTail(array.Length);
        //         }

        //         for (int i = 0; i < array.Length; i++)
        //         {
        //             // e.g. string[][2] or bool[]

        //             var elementValue = array.GetValue(i);
        //             var innerContext = c.Spawn(innerType!, elementValue);

        //             if (dimensions!.Count == 1)
        //             {
        //                 // we have reached the innermost array

        //                 c.AddSlots(this.EncodeValue(innerContext));
        //             }
        //             else
        //             {
        //                 // we still have yet to reach the innermost array

        //                 var innerSlots = this.EncodeDynamicArray(innerContext);

        //                 c.AddSlots(innerSlots);
        //             }
        //         }
        //     }
        // }
    }

    private SlotCollection EncodeSeries(EncodingContextV3 context)
    {
        // e.g. (bool, string) or (bool, (bytes, uint256))

        if (!context.IsTuple)
        {
            throw new ArgumentException($"The type {context.AbiType} is not a tuple");
        }

        List<object> values = new();

        if (context.IsValueTuple(out var tuple))
        {
            for (int i = 0; i < tuple!.Length; i++)
            {
                values.Add(tuple[i]);
            }
        }
        else if (context.IsValueArray(out var array))
        {
            for (int i = 0; i < array!.Length; i++)
            {
                values.Add(array.GetValue(i));
            }
        }
        else
        {
            throw new ArgumentException($"The value is neither an array nor an ITuple");
        }

        if (context.IsDynamic && !context.IsRoot)
        {
            // dynamic tuples need a pointer in the head pointing to the tail where the
            // actual values are encoded

            // context.AddPointerHead("pointer_dyn_tuple");
        }

        var parameters = EvmParameters.Parse(context.AbiType);

        // each parameter adds a head of either the value or its pointer

        List<SlotCollection> tailses = new(parameters.Count);

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var value = values[i];

            var paramContext = context.Spawn(parameter.AbiType, value);

            if (paramContext.IsDynamic)
            {
                context.AddPointerHead($"pointer_dyn_{parameter.AbiType}", paramContext.Heads, context.Heads);
            }

            this.EncodeValue(paramContext);

            context.AddSlots(paramContext.Heads);
            tailses.Add(paramContext.Tails);
        }

        // add the heads and tails to the root context

        context.AddSlots(tailses.SelectMany(c => c));

        return context.GatherAllSlots();
    }

    private SlotCollection EncodeDynamicValue(EncodingContextV3 context)
    {
        // e.g. string or bytes, but not tuples like (string,bytes) or arrays like string[] or bytes[]

        if (context.IsTuple)
        {
            throw new ArgumentException($"The type {context.AbiType} is a tuple, not a single slot dynamic value");
        }

        if (context.IsArray)
        {
            throw new ArgumentException($"The type {context.AbiType} is an array, not a single slot dynamic value");
        }

        byte[] bytes;

        if (context.Value is byte[] bytesValue)
        {
            bytes = bytesValue;
        }
        else if (context.Value is string stringValue)
        {
            bytes = Encoding.UTF8.GetBytes(stringValue);
        }
        else
        {
            throw new ArgumentException($"The value of type {context.Value.GetType()} must be a byte array or string");
        }

        // dynamic values need a pointer in the head pointing to the tail where the
        // actual value(s) are encoded

        // context.AddPointerHead("pointer_dyn_value", context.Heads);
        context.AddLengthTail(bytes.Length);
        context.AddBytesTails(bytes);

        return context.GatherAllSlots();
    }

    //

    private static Exception NotImplemented(string abiType, string clrType) =>
        new NotImplementedException($"Encoding for type {abiType} and value of type {clrType} not implemented");

    private bool TryAddStaticSlot(EncodingContextV3 context)
    {
        if (context.Value == null)
        {
            context.AddEmptyHead();
            return true;
        }

        // get the canonical type

        if (!AbiTypes.TryGetCanonicalType(context.AbiType, out var canonicalType) || canonicalType == null)
        {
            // canonical type not found; this should never happen

            throw new InvalidOperationException($"Canonical type not found for {context.AbiType}");
        }

        foreach (var staticEncoder in this.staticTypeEncoders)
        {
            if (staticEncoder.TryEncode(canonicalType, context.Value, out var bytes))
            {
                context.AddBytesHead(bytes);
                return true;
            }
        }

        return false;
    }
}