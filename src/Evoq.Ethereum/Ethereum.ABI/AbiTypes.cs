using System;
using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Contains definitions and validation methods for Solidity types.
/// </summary>
public static class AbiTypes
{
    // Define valid size ranges
    private static readonly Range IntegerSizeRange = new(8, 256);
    private static readonly Range BytesSizeRange = new(1, 32);
    private const int IntegerSizeStep = 8;

    //

    /// <summary>
    /// Basic types that don't require size specification.
    /// </summary>
    public static readonly HashSet<string> BasicTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        AbiTypeNames.Address,
        AbiTypeNames.Bool,
        AbiTypeNames.String,
        AbiTypeNames.Bytes,
        AbiTypeNames.Byte
    };

    /// <summary>
    /// Integer types with their allowed sizes.
    /// </summary>
    public static readonly Dictionary<string, int[]> IntegerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [AbiTypeNames.IntegerTypes.Uint] = Enumerable.Range(1, 32).Select(x => x * 8).ToArray(),
        [AbiTypeNames.IntegerTypes.Int] = Enumerable.Range(1, 32).Select(x => x * 8).ToArray()
    };

    /// <summary>
    /// Fixed-size byte array sizes (bytes1 to bytes32).
    /// </summary>
    public static readonly int[] BytesNSizes = Enumerable.Range(1, 32).ToArray();

    /// <summary>
    /// Fixed point number types (not commonly used).
    /// </summary>
    public static readonly Dictionary<string, (int[] Bits, int[] Decimals)> FixedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [AbiTypeNames.FixedTypes.Fixed] = (Enumerable.Range(1, 32).Select(x => x * 8).ToArray(), Enumerable.Range(1, 80).ToArray()),
        [AbiTypeNames.FixedTypes.Ufixed] = (Enumerable.Range(1, 32).Select(x => x * 8).ToArray(), Enumerable.Range(1, 80).ToArray())
    };

    /// <summary>
    /// Validates if a type string represents a valid Solidity type.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the type is valid.</returns>
    public static bool IsValidType(string type)
    {
        if (string.IsNullOrWhiteSpace(type))
            return false;

        // Handle array types first
        if (type.Contains('['))
            return IsValidArrayType(type);

        return IsValidBaseType(type);
    }

    /// <summary>
    /// Gets the canonical representation of a type.
    /// </summary>
    /// <param name="type">The type to normalize.</param>
    /// <param name="canonicalType">The canonical type string if successful.</param>
    /// <returns>True if the type was successfully normalized, false otherwise.</returns>
    public static bool TryGetCanonicalType(string type, out string? canonicalType)
    {
        canonicalType = null;
        if (!IsValidType(type))
            return false;

        // Handle array types
        if (IsArray(type))
        {
            if (!TryGetArrayBaseType(type, out var baseType) || baseType == null)
                return false;

            var arrayPart = type[type.IndexOf('[')..];

            if (!TryGetCanonicalBaseType(baseType, out var canonicalBase) || canonicalBase == null)
                return false;

            if (!TryGetArrayDimensions(type, out var dimensions) || dimensions == null)
                return false;

            canonicalType = canonicalBase + arrayPart;

            return true;
        }

        return TryGetCanonicalBaseType(type, out canonicalType);
    }

    /// <summary>
    /// Validates if a type string represents a valid base Solidity type (without array suffixes).
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the base type is valid.</returns>
    public static bool IsValidBaseType(string type)
    {
        if (BasicTypes.Contains(type))
            return true;

        // Handle uint/int sizes
        if (type.StartsWith("uint", StringComparison.OrdinalIgnoreCase) ||
            type.StartsWith("int", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidIntegerType(type);
        }

        // Handle bytesN
        if (type.StartsWith("bytes", StringComparison.OrdinalIgnoreCase) && type.Length > 5)
        {
            return IsValidBytesType(type);
        }

        return false;
    }

    /// <summary>
    /// Checks if the type is an array type (fixed or dynamic).
    /// </summary>
    public static bool IsArray(string type)
    {
        return type.Contains('[');
    }

    /// <summary>
    /// Checks if the type is a dynamic array (ends with "[]").
    /// </summary>
    public static bool IsDynamicArray(string type)
    {
        return TryGetArrayDimensions(type, out var dimensions) &&
               dimensions != null &&
               dimensions[0] == -1; // -1 represents dynamic size []
    }

    /// <summary>
    /// Determines if a type is dynamic string, bytes or array.
    /// </summary>
    public static bool IsDynamicType(string type)
    {
        if (type == AbiTypeNames.String || type == AbiTypeNames.Bytes)
            return true;

        return IsDynamicArray(type);
    }

    /// <summary>
    /// Gets the array dimensions from a type (e.g., [-1] or [2,3] from "uint256[][3]").
    /// </summary>
    /// <param name="type">The type to get dimensions from.</param>
    /// <param name="dimensions">The array dimensions if successful. -1 represents dynamic size [].</param>
    /// <returns>True if dimensions were successfully parsed, false otherwise.</returns>
    public static bool TryGetArrayDimensions(string type, out int[]? dimensions)
    {
        dimensions = null;
        var bracketIndex = type.IndexOf('[');
        if (bracketIndex <= 0)
            return false;

        dimensions = SplitArrayDimensions(type[bracketIndex..]);
        return dimensions != null;
    }

    /// <summary>
    /// Gets the base type of an array type (e.g., "uint256" from "uint256[]" or "uint256[2]").
    /// </summary>
    /// <param name="type">The type to get the base from.</param>
    /// <param name="baseType">The base type if successful.</param>
    /// <returns>True if base type was successfully parsed, false otherwise.</returns>
    public static bool TryGetArrayBaseType(string type, out string? baseType)
    {
        baseType = null;
        var bracketIndex = type.IndexOf('[');
        if (bracketIndex <= 0)
            return false;

        baseType = type[..bracketIndex];
        return true;
    }

    /// <summary>
    /// Removes the rightmost array dimension from a type.
    /// </summary>
    /// <param name="type">The type to remove the array dimension from.</param>
    /// <param name="innerType">The type with one less array dimension.</param>
    /// <returns>True if an array dimension was successfully removed, false otherwise.</returns>
    /// <remarks>
    /// For example:
    /// - "string[]" -> "string"
    /// - "string[][]" -> "string[]"
    /// - "bool[2][4][12]" -> "bool[2][4]"
    /// - "uint256" -> null (not an array type)
    /// </remarks>
    public static bool TryRemoveOuterArrayDimension(string type, out string? innerType)
    {
        innerType = null;

        if (!IsArray(type))
            return false;

        // Find the last opening bracket
        var lastOpenBracket = type.LastIndexOf('[');
        if (lastOpenBracket <= 0)
            return false;

        // Find the matching closing bracket
        var depth = 1;
        for (int i = lastOpenBracket + 1; i < type.Length; i++)
        {
            if (type[i] == '[')
                depth++;
            else if (type[i] == ']')
            {
                depth--;
                if (depth == 0 && i == type.Length - 1)
                {
                    innerType = type[..lastOpenBracket];
                    return true;
                }
            }
        }

        return false;
    }

    //

    private static bool IsValidIntegerType(string type)
    {
        // Handle basic uint/int (defaults to 256)
        if (type.Equals("uint", StringComparison.OrdinalIgnoreCase) ||
            type.Equals("int", StringComparison.OrdinalIgnoreCase))
            return true;

        // Extract size part (after "uint" or "int")
        var prefix = type.StartsWith("uint", StringComparison.OrdinalIgnoreCase) ? "uint" : "int";
        if (!int.TryParse(type[prefix.Length..], out var size))
            return false;

        return size >= IntegerSizeRange.Start.Value &&
               size <= IntegerSizeRange.End.Value &&
               size % IntegerSizeStep == 0;
    }

    private static bool IsValidBytesType(string type)
    {
        // Handle dynamic bytes
        if (type.Equals("bytes", StringComparison.OrdinalIgnoreCase))
            return true;

        // Extract N from bytesN
        if (!int.TryParse(type[5..], out var size))
            return false;

        return size >= BytesSizeRange.Start.Value &&
               size <= BytesSizeRange.End.Value;
    }

    private static bool IsValidArrayType(string type)
    {
        var bracketIndex = type.IndexOf('[');
        if (bracketIndex <= 0)
            return false;

        var baseType = type[..bracketIndex];
        var arrayPart = type[bracketIndex..];

        // Validate base type first
        if (!IsValidBaseType(baseType))
            return false;

        // Split array part into individual dimensions
        var dimensions = SplitArrayDimensions(arrayPart);
        if (dimensions == null)
            return false;

        // Validate each dimension
        return dimensions.All(dim => dim > 0 || dim == -1); // Allow positive sizes and dynamic (-1)
    }

    private static int[]? SplitArrayDimensions(string arrayPart)
    {
        var dimensions = new List<int>();
        var depth = 0;
        var start = 0;

        for (var i = 0; i < arrayPart.Length; i++)
        {
            if (arrayPart[i] == '[')
            {
                if (depth == 0)
                    start = i;
                depth++;
            }
            else if (arrayPart[i] == ']')
            {
                depth--;
                if (depth == 0)
                {
                    var dim = arrayPart[(start + 1)..i];
                    if (string.IsNullOrEmpty(dim))
                        dimensions.Add(-1); // Dynamic array []
                    else if (int.TryParse(dim, out var size) && size > 0)
                        dimensions.Add(size);
                    else
                        return null; // Invalid dimension
                }
                else if (depth < 0)
                    return null; // Mismatched brackets
            }
        }

        return depth == 0 ? dimensions.ToArray() : null;
    }

    private static bool TryGetCanonicalBaseType(string type, out string? canonicalType)
    {
        // this function tries to convert a type into its canonical form
        // e.g. uint256[3] -> uint256
        // e.g. uint256 -> uint256
        // e.g. bytes32 -> bytes32
        // e.g. bool -> bool
        // e.g. address -> address

        canonicalType = null;

        // Handle special case of 'byte' first
        if (type.Equals(AbiTypeNames.Byte, StringComparison.OrdinalIgnoreCase))
        {
            canonicalType = AbiTypeNames.FixedByteArrays.Bytes1;
            return true;
        }

        if (BasicTypes.Contains(type))
        {
            canonicalType = type.ToLowerInvariant();
            return true;
        }

        canonicalType = type.ToLowerInvariant() switch
        {
            AbiTypeNames.IntegerTypes.Int => AbiTypeNames.IntegerTypes.Int256,
            AbiTypeNames.IntegerTypes.Uint => AbiTypeNames.IntegerTypes.Uint256,
            AbiTypeNames.FixedTypes.Fixed => AbiTypeNames.FixedTypes.Fixed128x18,
            AbiTypeNames.FixedTypes.Ufixed => AbiTypeNames.FixedTypes.Ufixed128x18,
            _ => type.ToLowerInvariant()
        };
        return true;
    }
}