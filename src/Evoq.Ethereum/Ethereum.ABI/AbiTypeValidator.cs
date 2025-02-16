using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Validates compatibility between Solidity types and .NET types/values.
/// </summary>
public static class AbiTypeValidator
{
    /// <summary>
    /// Checks if a .NET value is compatible with a Solidity type.
    /// </summary>
    /// <param name="solidityType">The Solidity type such as "uint256" or "address", or even a tuple like "(uint256,uint256)".</param>
    /// <param name="dotnetValue">The .NET value to check which can be a primitive type or an ITuple.</param>
    /// <returns>True if compatible, false otherwise.</returns>
    public static bool IsCompatible(string solidityType, object? dotnetValue)
    {
        if (dotnetValue is null)
            return false;

        // Handle tuple types
        if (solidityType.StartsWith("(") && solidityType.EndsWith(")"))
        {
            if (dotnetValue is not ITuple tuple)
                return false;

            var componentTypes = ParseTupleComponents(solidityType);
            if (componentTypes.Length != tuple.Length)
                return false;

            for (int i = 0; i < componentTypes.Length; i++)
            {
                if (!IsCompatible(componentTypes[i], tuple[i]))
                    return false;
            }
            return true;
        }

        // Handle array types first
        if (solidityType.EndsWith("[]"))
        {
            if (dotnetValue is not Array array)
                return false;

            var elementType = solidityType[..^2]; // Remove the [] suffix
            return array.Cast<object?>().All(item => IsCompatible(elementType, item));
        }

        // Handle fixed-size arrays
        if (solidityType.Contains('['))
        {
            var openBracket = solidityType.IndexOf('[');
            var closeBracket = solidityType.IndexOf(']');
            if (!int.TryParse(solidityType[(openBracket + 1)..closeBracket], out var size))
                return false;

            if (dotnetValue is not Array array || array.Length != size)
                return false;

            var elementType = solidityType[..openBracket];
            return array.Cast<object?>().All(item => IsCompatible(elementType, item));
        }

        // Normalize the non-array type
        if (!AbiTypes.TryGetCanonicalType(solidityType, out var canonicalType) || canonicalType == null)
            return false;

        return canonicalType switch
        {
            // Basic types
            AbiTypeNames.Bool => dotnetValue is bool,
            AbiTypeNames.Address => dotnetValue is EthereumAddress,
            AbiTypeNames.String => dotnetValue is string,

            // Explicitly reject certain .NET types that might seem convertible
            _ when dotnetValue is DateTime or DateTimeOffset => false,

            // Integer types
            _ when canonicalType.StartsWith("uint") => IsCompatibleUnsignedInteger(canonicalType, dotnetValue),
            _ when canonicalType.StartsWith("int") => IsCompatibleSignedInteger(canonicalType, dotnetValue),

            // Bytes
            AbiTypeNames.Bytes => dotnetValue is byte[],
            _ when canonicalType.StartsWith("bytes") => IsCompatibleFixedBytes(canonicalType, dotnetValue),

            _ => false // Unknown type combinations
        };
    }

    /// <summary>
    /// Validates if the provided values are compatible with a function's parameters.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="values">The parameter values to validate.</param>
    /// <returns>True if all values are compatible, false otherwise.</returns>
    public static bool ValidateParameters(FunctionSignature signature, ITuple values)
    {
        var parameterTypes = signature.GetParameterTypes();

        if (parameterTypes.Length != values.Length)
            return false;

        return parameterTypes.Zip(values.GetElements(), IsCompatible).All(x => x);
    }

    /// <summary>
    /// Validates if the provided values are compatible with an ABI function's parameters.
    /// </summary>
    /// <param name="function">The ABI function item.</param>
    /// <param name="values">The parameter values to validate.</param>
    /// <returns>True if all values are compatible, false otherwise.</returns>
    /// <exception cref="ArgumentException">If the ABI item is not a function.</exception>
    public static bool ValidateParameters(AbiItem function, ITuple values)
    {
        if (function.Type != "function")
            throw new ArgumentException("ABI item must be a function", nameof(function));

        if (function.Inputs.Count != values.Length)
            return false;

        return function.Inputs.Zip(values.GetElements(), (param, value) => IsCompatible(param.Type, value)).All(x => x);
    }

    /// <summary>
    /// Validates if a single value is compatible with a function's single parameter.
    /// </summary>
    /// <param name="function">The ABI function item.</param>
    /// <param name="value">The parameter value to validate.</param>
    /// <returns>True if the value is compatible, false otherwise.</returns>
    /// <exception cref="ArgumentException">If the ABI item is not a function or has more than one parameter.</exception>
    public static bool ValidateParameters(AbiItem function, object? value)
    {
        if (function.Type != "function")
            throw new ArgumentException("ABI item must be a function", nameof(function));

        if (function.Inputs.Count != 1)
            throw new ArgumentException("Function must have exactly one parameter", nameof(function));

        return IsCompatible(function.Inputs[0].Type, value);
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
            inner = tupleType[1..^1];

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

    private static bool IsCompatibleUnsignedInteger(string canonicalType, object dotnetValue)
    {
        // Extract bit size (e.g., "uint256" -> 256)
        var bitSize = int.Parse(canonicalType[4..]);

        return dotnetValue switch
        {
            byte => true, // Always fits in uint
            ushort => true, // Always fits in uint
            uint => bitSize >= 32,
            ulong => bitSize >= 64,
            BigInteger bi => bi >= BigInteger.Zero && bi <= GetMaxValueForBits(bitSize),
            _ => false
        };
    }

    private static bool IsCompatibleSignedInteger(string canonicalType, object dotnetValue)
    {
        var bitSize = int.Parse(canonicalType[3..]);

        return dotnetValue switch
        {
            sbyte => true, // Always fits in int
            short => true, // Always fits in int
            int => bitSize >= 32,
            long => bitSize >= 64,
            BigInteger bi => bi >= GetMinValueForBits(bitSize) && bi <= GetMaxValueForBits(bitSize - 1),
            _ => false
        };
    }

    private static bool IsCompatibleFixedBytes(string canonicalType, object dotnetValue)
    {
        if (dotnetValue is not byte[] bytes)
            return false;

        // Extract N from bytesN
        var size = int.Parse(canonicalType[5..]);
        return bytes.Length == size;
    }

    private static BigInteger GetMaxValueForBits(int bits)
    {
        return (BigInteger.One << bits) - 1;
    }

    private static BigInteger GetMinValueForBits(int bits)
    {
        return -(BigInteger.One << (bits - 1));
    }
}