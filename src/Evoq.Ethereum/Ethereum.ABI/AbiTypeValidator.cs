using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Validates compatibility between Solidity types and .NET types/values.
/// </summary>
public class AbiTypeValidator : IAbiValueCompatible
{
    private readonly IReadOnlyList<IAbiEncode> staticTypeEncoders = new AbiStaticTypeEncoders();
    private readonly IReadOnlyList<IAbiEncode> dynamicTypeEncoders = new AbiDynamicTypeEncoders();

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeValidator"/> class.
    /// </summary>
    /// <param name="staticTypeEncoders">The static type encoders.</param>
    /// <param name="dynamicTypeEncoders">The dynamic type encoders.</param>
    public AbiTypeValidator(IReadOnlyList<IAbiEncode> staticTypeEncoders, IReadOnlyList<IAbiEncode> dynamicTypeEncoders)
    {
        this.staticTypeEncoders = staticTypeEncoders;
        this.dynamicTypeEncoders = dynamicTypeEncoders;
    }

    //

    /// <summary>
    /// Checks if a .NET value is compatible with a Solidity type.
    /// </summary>
    /// <param name="abiType">The Solidity type such as "uint256" or "address", or even a tuple like "(uint256,uint256)".</param>
    /// <param name="dotnetValue">The .NET value to check which can be a primitive type or an ITuple.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the value which is more expensive but more robust.</param>
    /// <param name="message">The message if the type is not compatible</param>
    /// <returns>True if compatible, false otherwise.</returns>
    public bool IsCompatible(string abiType, object? dotnetValue, out string message, bool tryEncoding = false)
    {
        if (dotnetValue is null)
        {
            message = "Value is null";
            return false;
        }

        Debug.Assert(!IsOfTypeType(dotnetValue.GetType()), "Value type is Type, this indicates a bug, not a validation error");

        // handle tuple types

        if (abiType.StartsWith("(") && abiType.EndsWith(")"))
        {
            if (dotnetValue is not ITuple tuple)
            {
                message = "Value is not an ITuple";
                return false;
            }

            var componentTypes = ParseTupleComponents(abiType);
            if (componentTypes.Length != tuple.Length)
            {
                message = "Tuple length mismatch";
                return false;
            }

            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (!IsCompatible(componentTypes[i], tuple[i], out message, tryEncoding))
                {
                    return false;
                }
            }

            message = "OK";
            return true;
        }

        // handle array types first

        if (abiType.EndsWith("[]"))
        {
            if (dotnetValue is not Array array)
            {
                message = "Value is not an array";
                return false;
            }

            var elementType = abiType[..^2]; // Remove the [] suffix

            foreach (var item in array)
            {
                if (!IsCompatible(elementType, item, out message, tryEncoding))
                {
                    return false;
                }
            }

            message = "OK";
            return true;
        }

        // handle fixed-size arrays

        if (abiType.Contains('['))
        {
            var openBracket = abiType.IndexOf('[');
            var closeBracket = abiType.IndexOf(']');
            if (!int.TryParse(abiType[(openBracket + 1)..closeBracket], out var size))
            {
                message = "Invalid array size";
                return false;
            }

            if (dotnetValue is not Array array || array.Length != size)
            {
                message = "Array size mismatch";
                return false;
            }

            var elementType = abiType[..openBracket];

            foreach (var item in array)
            {
                if (!IsCompatible(elementType, item, out message, tryEncoding))
                {
                    return false;
                }
            }

            message = "OK";
            return true;
        }

        // normalize the non-array type

        if (!AbiTypes.TryGetCanonicalType(abiType, out var canonicalType) || canonicalType == null)
        {
            message = $"Invalid type '{abiType}'";
            return false;
        }

        //

        // this is the main logic, all the above is just to extract the underlying type which
        // ends up calling back into this method and hitting the code below

        // handle dynamic types string or bytes

        if (AbiTypes.IsDynamicType(canonicalType))
        {
            if (tryEncoding)
            {
                foreach (var encoder in this.dynamicTypeEncoders)
                {
                    if (encoder.TryEncode(canonicalType, dotnetValue, out _))
                    {
                        message = "OK";
                        return true;
                    }
                }

                message = $"No dynamic encoders can encode .NET type '{dotnetValue.GetType()}' to ABI type '{canonicalType}'";
                return false;
            }

            // Debug.Assert(!IsOfTypeType(dotnetValue.GetType()), $"The type is: '{dotnetValue.GetType().FullName}'");

            foreach (var encoder in this.dynamicTypeEncoders)
            {
                if (encoder.IsCompatible(canonicalType, dotnetValue.GetType(), out message))
                {
                    return true;
                }
            }

            message = $"No dynamic encoders support .NET type '{dotnetValue.GetType()}' to ABI type '{canonicalType}'";
            return false;
        }

        // handle static types

        if (tryEncoding)
        {
            foreach (var encoder in this.staticTypeEncoders)
            {
                if (encoder.TryEncode(canonicalType, dotnetValue, out _))
                {
                    message = "OK";
                    return true;
                }
            }

            message = $"No static encoders can encode .NET type '{dotnetValue.GetType()}' to ABI type '{canonicalType}'";
            return false;
        }

        foreach (var encoder in this.staticTypeEncoders)
        {
            if (encoder.IsCompatible(canonicalType, dotnetValue.GetType(), out message))
            {
                return true;
            }
        }

        message = $"No static encoders support .NET type '{dotnetValue.GetType()}' to ABI type '{canonicalType}'";
        return false;
    }

    /// <summary>
    /// Validates if the provided values are compatible with a function's parameters.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="values">The parameter values to validate.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the values which is more expensive but more robust.</param>
    /// <param name="message">The message if the values are not compatible</param>
    /// <returns>True if all values are compatible, false otherwise.</returns>
    public bool ValidateParameters(FunctionSignature signature, ITuple values, out string message, bool tryEncoding = false)
    {
        var parameterTypes = signature.GetParameterTypes();

        if (parameterTypes.Length != values.Length)
        {
            message = "Parameter length mismatch";
            return false;
        }

        for (int i = 0; i < parameterTypes.Length; i++)
        {
            if (!IsCompatible(parameterTypes[i], values[i], out message, tryEncoding))
            {
                return false;
            }
        }

        message = "OK";
        return true;
    }

    /// <summary>
    /// Validates if the provided values are compatible with an ABI function's parameters.
    /// </summary>
    /// <param name="function">The ABI function item.</param>
    /// <param name="values">The parameter values to validate.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the values which is more expensive but more robust.</param>
    /// <param name="message">The message if the values are not compatible</param>
    /// <returns>True if all values are compatible, false otherwise.</returns>
    /// <exception cref="ArgumentException">If the ABI item is not a function.</exception>
    public bool ValidateParameters(AbiItem function, ITuple values, out string message, bool tryEncoding = false)
    {
        if (function.Inputs == null || function.Inputs.Count == 0)
        {
            throw new ArgumentException("ABI item must have inputs");
        }

        if (function.Inputs.Count != values.Length)
        {
            message = "Parameter length mismatch";
            return false;
        }

        for (int i = 0; i < function.Inputs.Count; i++)
        {
            if (!IsCompatible(function.Inputs[i].Type, values[i], out message, tryEncoding))
            {
                return false;
            }
        }

        message = "OK";
        return true;
    }

    /// <summary>
    /// Validates if a single value is compatible with a function's single parameter.
    /// </summary>
    /// <param name="function">The ABI function item.</param>
    /// <param name="value">The parameter value to validate.</param>
    /// <param name="tryEncoding">If true, the method will try to encode the value which is more expensive but more robust.</param>
    /// <param name="message">The message if the value is not compatible</param>
    /// <returns>True if the value is compatible, false otherwise.</returns>
    /// <exception cref="ArgumentException">If the ABI item is not a function or has more than one parameter.</exception>
    public bool ValidateParameters(AbiItem function, object? value, out string message, bool tryEncoding = false)
    {
        if (function.Inputs == null || function.Inputs.Count == 0)
        {
            throw new ArgumentException("ABI item must have inputs");
        }

        if (function.Inputs.Count != 1)
        {
            message = "ABI item must have exactly one parameter";
            return false;
        }

        return IsCompatible(function.Inputs[0].Type, value, out message, tryEncoding);
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

    /// <summary>
    /// Checks if the value is .NET type information.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is a Type, false otherwise.</returns>
    public static bool IsOfTypeType(object value)
    {
        return value is Type; // this should catch both Type and RuntimeType
    }

    /// <summary>
    /// Checks if the type is .NET type information.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is a Type, false otherwise.</returns>
    public static bool IsOfTypeType(Type type)
    {
        return type.FullName == "System.Type" || type.FullName == "System.RuntimeType";
    }
}