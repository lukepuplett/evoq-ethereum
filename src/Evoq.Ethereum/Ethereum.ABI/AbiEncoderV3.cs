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
        this.Head = new HeadSlotCollection(capacity: 8);
        this.Tail = new TailSlotCollection(capacity: 8);
        this.Parent = parent;
    }

    //

    public string AbiType { get; init; }
    public object Value { get; init; }
    public HeadSlotCollection Head { get; init; }
    public TailSlotCollection Tail { get; init; }
    public EncodingContextV3? Parent { get; init; }

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

    public Slot AddPointer(string name)
    {
        var pointerSlot = new Slot(name, Tail, Head);
        Head.Add(pointerSlot);
        return pointerSlot;
    }

    public SlotCollection GatherAllSlots()
    {
        var all = new SlotCollection(Head.Count + Tail.Count);
        all.AddRange(Head);
        all.AddRange(Tail);
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

        if (Parent == null)
        {
            return $"{AbiType}";
        }

        var parentString = Parent.ToString();
        var prefix = Parent != null ? "." : "";

        return $"{parentString}{prefix}{AbiType}";
    }
}

public class AbiEncoderV3 : IAbiEncoder
{
    public AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values)
    {
        var head = new HeadSlotCollection(parameters.Count * 8);

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var value = values[i];

            var context = new EncodingContextV3(parameter.AbiType, value);

            this.EncodeValue(context);
        }

        return new AbiEncodingResult(head);
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
                return this.EncodeDynamicTuple(context);
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
        throw new NotImplementedException();
    }

    private SlotCollection EncodeStaticTuple(EncodingContextV3 context)
    {
        throw new NotImplementedException();
    }

    private SlotCollection EncodeStaticValue(EncodingContextV3 context)
    {
        throw new NotImplementedException();
    }

    // dynamic encoders

    private SlotCollection EncodeDynamicArray(EncodingContextV3 context)
    {
        // e.g. bool[] or uint256[][2] or bytes[2] or bool[2][]

        // dynamic values need a pointer in the head pointing to the tail where the
        // actual values are encoded

        context.AddPointer(Name(context, "pointer_dyn_array"));
        context.Tail.AddRange(getTailSlots(context));

        return context.GatherAllSlots();

        //

        SlotCollection getTailSlots(EncodingContextV3 c)
        {
            // e.g. bool[] or uint256[][2] or bytes[2] or bool[2][]

            if (c.Value is not Array array)
            {
                // e.g. bool or uint256 or bytes

                // we call back around to the top of the encoder

                var innerContext = c.Spawn(c.AbiType, c.Value);

                return this.EncodeValue(innerContext); // top-level call
            }
            else
            {
                for (int i = 0; i < array.Length; i++)
                {
                    if (!AbiTypes.TryRemoveOuterArrayDimension(c.AbiType, out var innerType))
                    {
                        throw new NotImplementedException($"The type '{c.AbiType}' is not an array");
                    }

                    if (!AbiTypes.TryGetArrayDimensions(c.AbiType, out var dimensions))
                    {
                        throw new ArgumentException($"The type {c.AbiType} is not an array");
                    }

                    bool isVariableLengthOuter = dimensions.First() == -1;

                    if (isVariableLengthOuter)
                    {
                        // e.g. bool[] or uint256[][] needs a count slot

                        var countSlot = new Slot(Name(c, "count"), UintTypeEncoder.EncodeUint(256, array.Length));

                        c.Tail.Add(countSlot);
                    }

                    // e.g. string[][2] or bool[]

                    var elementValue = array.GetValue(i);
                    var innerContext = c.Spawn(innerType!, elementValue);

                    c.Tail.AddRange(getTailSlots(innerContext));
                }

                return c.GatherAllSlots();
            }
        }
    }

    private SlotCollection EncodeDynamicTuple(EncodingContextV3 context)
    {
        throw new NotImplementedException();
    }

    private SlotCollection EncodeDynamicValue(EncodingContextV3 context)
    {
        throw new NotImplementedException();
    }

    //

    private static string Name(EncodingContextV3 context, string name)
    {
        return $"{context}.{name}";
    }
}