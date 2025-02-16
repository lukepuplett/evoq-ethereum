using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Validates compatibility between Solidity types and .NET types/values.
/// </summary>
public static class AbiTypeValidator
{
    private static readonly IReadOnlyList<IAbiTypeEncoder> staticTypeEncoders = new AbiStaticTypeEncoders();
    private static readonly IReadOnlyList<IAbiTypeEncoder> dynamicTypeEncoders = new AbiDynamicTypeEncoders();

    //

    /// <summary>
    /// Checks if a .NET value is compatible with a Solidity type.
    /// </summary>
    /// <param name="abiType">The Solidity type such as "uint256" or "address", or even a tuple like "(uint256,uint256)".</param>
    /// <param name="dotnetValue">The .NET value to check which can be a primitive type or an ITuple.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the value which is more expensive but more robust.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    public static bool IsCompatible(string abiType, object? dotnetValue, bool tryEncoding = false)
    {
        if (dotnetValue is null)
        {
            return false;
        }

        // handle tuple types

        if (abiType.StartsWith("(") && abiType.EndsWith(")"))
        {
            if (dotnetValue is not ITuple tuple)
            {
                return false;
            }

            var componentTypes = ParseTupleComponents(abiType);
            if (componentTypes.Length != tuple.Length)
            {
                return false;
            }

            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (!IsCompatible(componentTypes[i], tuple[i], tryEncoding))
                {
                    return false;
                }
            }

            return true;
        }

        // handle array types first

        if (abiType.EndsWith("[]"))
        {
            if (dotnetValue is not Array array)
            {
                return false;
            }

            var elementType = abiType[..^2]; // Remove the [] suffix

            return array
                .Cast<object?>()
                .All(item => IsCompatible(elementType, item, tryEncoding));
        }

        // handle fixed-size arrays

        if (abiType.Contains('['))
        {
            var openBracket = abiType.IndexOf('[');
            var closeBracket = abiType.IndexOf(']');
            if (!int.TryParse(abiType[(openBracket + 1)..closeBracket], out var size))
            {
                return false;
            }

            if (dotnetValue is not Array array || array.Length != size)
            {
                return false;
            }

            var elementType = abiType[..openBracket];

            return array
                .Cast<object?>()
                .All(item => IsCompatible(elementType, item, tryEncoding));
        }

        // normalize the non-array type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            return false;
        }

        //

        // this is the main logic, all the above is just to extract the underlying type which
        // ends up calling back into this method and hitting the code below

        // handle dynamic types

        if (AbiTypes.IsDynamicType(canonicalType))
        {
            if (tryEncoding)
            {
                return dynamicTypeEncoders.Any(e => e.TryEncode(canonicalType, dotnetValue, out _));
            }

            return dynamicTypeEncoders.Any(e => e.IsCompatible(canonicalType, dotnetValue.GetType()));
        }

        // handle static types

        if (tryEncoding)
        {
            return staticTypeEncoders.Any(e => e.TryEncode(canonicalType, dotnetValue, out _));
        }

        return staticTypeEncoders.Any(e => e.IsCompatible(canonicalType, dotnetValue.GetType()));
    }

    /// <summary>
    /// Validates if the provided values are compatible with a function's parameters.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="values">The parameter values to validate.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the values which is more expensive but more robust.</param>
    /// <returns>True if all values are compatible, false otherwise.</returns>
    public static bool ValidateParameters(FunctionSignature signature, ITuple values, bool tryEncoding = false)
    {
        var parameterTypes = signature.GetParameterTypes();

        if (parameterTypes.Length != values.Length)
            return false;

        return parameterTypes.Zip(values.GetElements(), (type, value) => IsCompatible(type, value, tryEncoding)).All(x => x);
    }

    /// <summary>
    /// Validates if the provided values are compatible with an ABI function's parameters.
    /// </summary>
    /// <param name="function">The ABI function item.</param>
    /// <param name="values">The parameter values to validate.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the values which is more expensive but more robust.</param>
    /// <returns>True if all values are compatible, false otherwise.</returns>
    /// <exception cref="ArgumentException">If the ABI item is not a function.</exception>
    public static bool ValidateParameters(AbiItem function, ITuple values, bool tryEncoding = false)
    {
        if (function.Type != "function")
            throw new ArgumentException("ABI item must be a function", nameof(function));

        if (function.Inputs.Count != values.Length)
            return false;

        return function.Inputs.Zip(values.GetElements(), (param, value) => IsCompatible(param.Type, value, tryEncoding)).All(x => x);
    }

    /// <summary>
    /// Validates if a single value is compatible with a function's single parameter.
    /// </summary>
    /// <param name="function">The ABI function item.</param>
    /// <param name="value">The parameter value to validate.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the value which is more expensive but more robust.</param>
    /// <returns>True if the value is compatible, false otherwise.</returns>
    /// <exception cref="ArgumentException">If the ABI item is not a function or has more than one parameter.</exception>
    public static bool ValidateParameters(AbiItem function, object? value, bool tryEncoding = false)
    {
        if (function.Type != "function")
            throw new ArgumentException("ABI item must be a function", nameof(function));

        if (function.Inputs.Count != 1)
            throw new ArgumentException("Function must have exactly one parameter", nameof(function));

        return IsCompatible(function.Inputs[0].Type, value, tryEncoding);
    }

    /// <summary>
    /// Parses the components of a tuple type string into an array of component type strings.
    /// </summary>
    /// <param name="tupleType">The tuple type string (with or without outer parentheses), e.g. "(uint256,uint256)" or "uint256,uint256".</param>
    /// <returns>An array of component type strings.</returns>
    public static string[] ParseTupleComponents(string tupleType)
    {
        // If the string includes parentheses, remove them
        var inner = tupleType;
        if (tupleType.StartsWith("(") && tupleType.EndsWith(")"))
        {
            inner = tupleType[1..^1];
        }

        var components = new List<string>();
        var depth = 0;
        var start = 0;

        for (int i = 0; i < inner.Length; i++)
        {
            switch (inner[i])
            {
                case '(':
                    depth++;
                    break;
                case ')':
                    depth--;
                    break;
                case ',' when depth == 0:
                    components.Add(inner[start..i].Trim());
                    start = i + 1;
                    break;
            }
        }

        if (start < inner.Length)
            components.Add(inner[start..].Trim());

        return components.ToArray();
    }
}