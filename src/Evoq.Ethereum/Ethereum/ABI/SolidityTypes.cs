using System;
using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Contains definitions and validation methods for Solidity types.
/// </summary>
public static class SolidityTypes
{
    /// <summary>
    /// Basic types that don't require size specification.
    /// </summary>
    public static readonly HashSet<string> BasicTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        SolidityTypeNames.Address,
        SolidityTypeNames.Bool,
        SolidityTypeNames.String,
        SolidityTypeNames.Bytes,
        SolidityTypeNames.Byte
    };

    /// <summary>
    /// Integer types with their allowed sizes.
    /// </summary>
    public static readonly Dictionary<string, int[]> IntegerTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [SolidityTypeNames.IntegerTypes.Uint] = Enumerable.Range(1, 32).Select(x => x * 8).ToArray(),
        [SolidityTypeNames.IntegerTypes.Int] = Enumerable.Range(1, 32).Select(x => x * 8).ToArray()
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
        [SolidityTypeNames.FixedTypes.Fixed] = (Enumerable.Range(1, 32).Select(x => x * 8).ToArray(), Enumerable.Range(1, 80).ToArray()),
        [SolidityTypeNames.FixedTypes.Ufixed] = (Enumerable.Range(1, 32).Select(x => x * 8).ToArray(), Enumerable.Range(1, 80).ToArray())
    };

    // Define valid size ranges
    private static readonly Range IntegerSizeRange = new(8, 256);
    private static readonly Range BytesSizeRange = new(1, 32);
    private const int IntegerSizeStep = 8;

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
    /// <returns>The canonical type string.</returns>
    public static string GetCanonicalType(string type)
    {
        if (!IsValidType(type))
            throw new ArgumentException($"Invalid Solidity type '{type}'", nameof(type));

        // Handle array types
        if (type.Contains('['))
        {
            var baseType = type[..type.IndexOf('[')];
            var arrayPart = type[type.IndexOf('[')..];
            return GetCanonicalBaseType(baseType) + arrayPart;
        }

        return GetCanonicalBaseType(type);
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
        return dimensions.All(IsValidArraySpecifier);
    }

    private static string[]? SplitArrayDimensions(string arrayPart)
    {
        var dimensions = new List<string>();
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
                    dimensions.Add(arrayPart[start..(i + 1)]);
                else if (depth < 0)
                    return null; // Mismatched brackets
            }
        }

        return depth == 0 ? dimensions.ToArray() : null;
    }

    private static bool IsValidArraySpecifier(string arrayPart)
    {
        if (!arrayPart.StartsWith("[") || !arrayPart.EndsWith("]"))
            return false;

        // Handle dynamic arrays
        if (arrayPart == "[]")
            return true;

        // Handle fixed-size arrays
        var sizeStr = arrayPart[1..^1];
        if (!int.TryParse(sizeStr, out var size))
            return false;

        return size > 0; // Size must be positive
    }

    private static string GetCanonicalBaseType(string type)
    {
        // Handle special case of 'byte' first
        if (type.Equals(SolidityTypeNames.Byte, StringComparison.OrdinalIgnoreCase))
            return SolidityTypeNames.ByteArrays.Bytes1;

        if (BasicTypes.Contains(type))
            return type.ToLowerInvariant();

        return type.ToLowerInvariant() switch
        {
            SolidityTypeNames.IntegerTypes.Int => SolidityTypeNames.IntegerTypes.Int256,
            SolidityTypeNames.IntegerTypes.Uint => SolidityTypeNames.IntegerTypes.Uint256,
            SolidityTypeNames.FixedTypes.Fixed => SolidityTypeNames.FixedTypes.Fixed128x18,
            SolidityTypeNames.FixedTypes.Ufixed => SolidityTypeNames.FixedTypes.Ufixed128x18,
            _ => type.ToLowerInvariant()
        };
    }

    private static bool TryParseFixedPointType(string spec, out int bits, out int decimals)
    {
        bits = 0;
        decimals = 0;

        var parts = spec.Split('x');
        if (parts.Length != 2)
            return false;

        return int.TryParse(parts[0], out bits) && int.TryParse(parts[1], out decimals);
    }
}