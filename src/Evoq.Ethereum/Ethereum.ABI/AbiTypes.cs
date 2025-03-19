using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
        {
            return false;
        }

        if (IsArray(type))
        {
            return IsValidArrayType(type);
        }

        if (IsTuple(type, false)) // because we check for arrays first
        {
            return IsValidTuple(type);
        }

        return IsValidBaseType(type);
    }

    /// <summary>
    /// Gets the canonical representation of a type, e.g. uint[3] -> uint256[3] or uint -> uint256.
    /// </summary>
    /// <param name="type">The type to normalize.</param>
    /// <param name="canonicalType">The canonical type string if successful.</param>
    /// <returns>True if the type was successfully normalized, false otherwise.</returns>
    public static bool TryGetCanonicalType(string type, out string? canonicalType)
    {
        canonicalType = null;

        if (!IsValidType(type))
        {
            return false;
        }

        if (IsArray(type))
        {
            if (!TryGetArrayBaseType(type, out var baseType) || baseType == null)
            {
                return false;
            }

            if (!TryGetCanonicalType(baseType, out var canonicalBase) || canonicalBase == null)
            {
                return false;
            }

            string arrayPart = type.Substring(baseType.Length);

            canonicalType = canonicalBase + arrayPart;
            return true;
        }
        else if (IsTuple(type, includeArrays: false)) // because we check for arrays first
        {
            canonicalType = AbiParameters.Parse(type).GetCanonicalType(includeNames: false, includeSpaces: false);
            return true;
        }

        return TryGetCanonicalBaseType(type, out canonicalType);
    }

    /// <summary>
    /// Gets the default CLR type for an ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type.</param>
    /// <param name="clrType">The CLR type if successful.</param>
    /// <returns>True if a CLR type was successfully matched, false otherwise.</returns>
    public static bool TryGetDefaultClrType(string abiType, out Type clrType)
    {
        clrType = typeof(object);

        if (!IsValidType(abiType))
        {
            return false;
        }

        if (IsArray(abiType))
        {
            if (!TryGetArrayBaseType(abiType, out var baseType) || baseType == null)
            {
                return false;
            }

            if (!TryGetDefaultClrType(baseType, out var baseClrType))
            {
                return false;
            }

            if (!TryGetArrayDimensions(abiType, out var dimensions) || dimensions == null)
            {
                return false;
            }

            clrType = baseClrType;
            foreach (var _ in dimensions)
            {
                clrType = clrType.MakeArrayType(); // this is the only way to get a jagged array
            }

            return true;
        }
        else if (IsTuple(abiType, includeArrays: false)) // because we check for arrays first
        {
            clrType = typeof(Dictionary<string, object?>);

            return true;
        }
        else
        {
            // Handle base types using the type encoders

            // Try uint types
            if (abiType.StartsWith(AbiTypeNames.IntegerTypes.Uint, StringComparison.OrdinalIgnoreCase))
            {
                return TypeEncoders.UintTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Try int types
            if (abiType.StartsWith(AbiTypeNames.IntegerTypes.Int, StringComparison.OrdinalIgnoreCase))
            {
                return TypeEncoders.IntTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Try fixed bytes types (bytes1 to bytes32)
            if (abiType.StartsWith(AbiTypeNames.Bytes, StringComparison.OrdinalIgnoreCase) &&
                abiType.Length > AbiTypeNames.Bytes.Length)
            {
                return TypeEncoders.FixedBytesTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Try dynamic bytes type
            if (abiType == AbiTypeNames.Bytes)
            {
                return TypeEncoders.BytesTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Try string type
            if (abiType == AbiTypeNames.String)
            {
                return TypeEncoders.StringTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Try address type
            if (abiType == AbiTypeNames.Address)
            {
                return TypeEncoders.AddressTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Try bool type
            if (abiType == AbiTypeNames.Bool)
            {
                return TypeEncoders.BoolTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // Handle byte alias (which is bytes1)
            if (abiType == AbiTypeNames.Byte)
            {
                return TypeEncoders.FixedBytesTypeEncoder.TryGetDefaultClrType(abiType, out clrType);
            }

            // If we get here, we don't have a handler for this type
            return false;
        }
    }

    /// <summary>
    /// Checks if the type has some form of length suffix, e.g. uint8 -> 8, uint256[] -> -1, uint256[][3] -> 3.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="length">The length in bytes for a fixed-length type or the outer array length for an array.</param>
    /// <returns>True if the type has a length suffix, false otherwise.</returns>
    public static bool HasLengthSuffix(string type, out int length)
    {
        if (IsArray(type))
        {
            if (!TryGetArrayDimensions(type, out var dimensions) || dimensions == null)
            {
                throw new InvalidOperationException($"Invalid array type: {type}");
            }

            if (dimensions.First() == -1)
            {
                // dynamic array has no length suffix

                length = 0;
                return false;
            }

            length = dimensions.First();
            return true;
        }

        if (type.StartsWith(AbiTypeNames.IntegerTypes.Uint, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(type[AbiTypeNames.IntegerTypes.Uint.Length..], out var bits))
            {
                length = bits / 8;
                return true;
            }
        }

        if (type.StartsWith(AbiTypeNames.IntegerTypes.Int, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(type[AbiTypeNames.IntegerTypes.Int.Length..], out var bits))
            {
                length = bits / 8;
                return true;
            }
        }

        if (type.StartsWith(AbiTypeNames.Bytes, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(type[AbiTypeNames.Bytes.Length..], out var bytesSize))
            {
                length = bytesSize;
                return true;
            }
        }

        length = 0;
        return false;
    }

    /// <summary>
    /// Validates if a type string represents a valid base Solidity type (without array suffixes).
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the base type is valid.</returns>
    public static bool IsValidBaseType(string type)
    {
        if (BasicTypes.Contains(type))
        {
            return true;
        }

        if (type.StartsWith("uint", StringComparison.OrdinalIgnoreCase) ||
            type.StartsWith("int", StringComparison.OrdinalIgnoreCase))
        {
            return IsValidIntegerType(type);
        }

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
        return type.Trim().EndsWith(']');
    }

    /// <summary>
    /// Checks if the type is a tuple type.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <param name="includeArrays">Whether to arrays of tuples count as tuples.</param>
    public static bool IsTuple(string type, bool includeArrays)
    {
        if (includeArrays && TryGetArrayBaseType(type, out var baseType) && baseType != null)
        {
            return IsTuple(baseType, includeArrays);
        }

        return type.Trim().StartsWith('(') && type.Trim().EndsWith(')');
    }

    /// <summary>
    /// Checks if the type is either an array containing a variable-length type or itself has a variable-length.
    /// </summary>
    /// <remarks>
    /// A dynamic array is an array with a dynamic size, e.g. uint256[] or string[]
    /// or bool[][2] because the inner array is dynamic, so it is considered a fixed
    /// size array of two dynamic types (arrays).
    /// 
    /// Contract this with a fixed-length array of a fixed-length type, e.g. uint256[3]
    /// which can be known ahead of time from the ABI alone.
    /// </remarks>
    public static bool IsDynamicArray(string type)
    {
        if (IsArray(type))
        {
            if (!TryGetArrayBaseType(type, out var baseType) || baseType == null)
            {
                throw new InvalidOperationException($"Invalid array type: {type}");
            }

            if (!TryGetArrayDimensions(type, out var dimensions) || dimensions == null)
            {
                throw new InvalidOperationException($"Invalid array type: {type}");
            }

            return IsDynamic(baseType) || dimensions.Any(d => d == -1);
        }

        return false;
    }

    /// <summary>
    /// Determines if a type has a variable length such as a string, bytes or a dynamic array.
    /// </summary>
    public static bool IsDynamic(string type)
    {
        if (type == AbiTypeNames.String || type == AbiTypeNames.Bytes)
        {
            return true;
        }

        if (IsDynamicArray(type))
        {
            return true;
        }

        if (IsTuple(type, includeArrays: true)) // although we check for arrays first, that check is not deep
        {
            if (TryGetArrayBaseType(type, out var baseDescriptor))
            {
                return AbiParameters.Parse(baseDescriptor!).Any(p => IsDynamic(p.AbiType));
            }

            return AbiParameters.Parse(type).Any(p => IsDynamic(p.AbiType));
        }

        return false;
    }

    /// <summary>
    /// Gets the number of bits for a type.
    /// </summary>
    /// <param name="type">The type to get the bits for.</param>
    /// <param name="bits">The number of bits if successful.</param>
    /// <returns>True if the bits were successfully parsed, false otherwise.</returns>
    public static bool TryGetBitsSize(string type, out int bits)
    {
        bits = 0;
        if (!IsValidType(type))
        {
            return false;
        }

        if (type.StartsWith(AbiTypeNames.IntegerTypes.Uint, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(type[AbiTypeNames.IntegerTypes.Uint.Length..], out var size))
            {
                bits = size;
                return true;
            }

            bits = 256;
            return true;
        }

        if (type.StartsWith(AbiTypeNames.IntegerTypes.Int, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(type[AbiTypeNames.IntegerTypes.Int.Length..], out var size))
            {
                bits = size;
                return true;
            }

            bits = 256;
            return true;
        }

        if (type.StartsWith(AbiTypeNames.Bytes, StringComparison.OrdinalIgnoreCase))
        {
            if (int.TryParse(type[AbiTypeNames.Bytes.Length..], out var bytesSize))
            {
                bits = bytesSize * 8;
                return true;
            }
        }

        //

        if (type == AbiTypeNames.Address)
        {
            // address is 20 bytes

            bits = 20 * 8;
            return true;
        }

        if (type == AbiTypeNames.Bool)
        {
            // bool is 1 bit

            bits = 1;
            return true;
        }

        //        

        const int maxDivisibleBy8 = int.MaxValue - (int.MaxValue % 8);

        if (type == AbiTypeNames.String)
        {
            bits = maxDivisibleBy8;
            return true;
        }

        if (type == AbiTypeNames.Bytes)
        {
            bits = maxDivisibleBy8;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the maximum number of bytes a type can hold.
    /// </summary>
    /// <param name="type">The type to get the bytes for.</param>
    /// <param name="bytes">The number of bytes if successful.</param>
    /// <returns>True if the bytes were successfully parsed, false otherwise.</returns>
    public static bool TryGetBytesSize(string type, out int bytes)
    {
        if (TryGetBitsSize(type, out var bits) && bits % 8 == 0)
        {
            bytes = bits / 8;
            return true;
        }

        bytes = 0;
        return false;
    }

    /// <summary>
    /// Gets the count of the components in a tuple.
    /// </summary>
    /// <param name="type">The type to get the tuple length for.</param>
    /// <param name="includeArrays">Whether to include arrays of tuples in the count.</param>
    /// <param name="length">The tuple length if successful.</param>
    /// <returns>True if the tuple length was successfully parsed, false otherwise.</returns>
    public static bool TryGetTupleLength(string type, bool includeArrays, out int length)
    {
        length = 0;
        if (!IsTuple(type, includeArrays))
        {
            return false;
        }

        length = AbiParameters.Parse(type).Count;
        return true;
    }

    /// <summary>
    /// Gets the array dimensions from a type from outer to inner e.g. uint256[][3] -> [3, -1]
    /// </summary>
    /// <param name="type">The type to get dimensions from.</param>
    /// <param name="dimensions">The array dimensions if successful. -1 represents dynamic size [].</param>
    /// <returns>True if dimensions were successfully parsed, false otherwise.</returns>
    public static bool TryGetArrayDimensions(string type, out IReadOnlyList<int>? dimensions)
    {
        dimensions = null;
        if (string.IsNullOrEmpty(type) || !IsArray(type))
        {
            return false;
        }

        if (!TryGetArrayBaseType(type, out var baseType) || baseType == null)
        {
            return false;
        }

        string arrayPart = type.Substring(baseType.Length);

        dimensions = SplitArrayDimensions(arrayPart);

        return dimensions != null;
    }

    /// <summary>
    /// Gets the length of the outer array dimension.
    /// </summary>
    /// <param name="type">The type to get the length for.</param>
    /// <param name="length">The length if successful.</param>
    /// <returns>True if the length was successfully parsed, false otherwise.</returns>
    public static bool TryGetArrayOuterLength(string type, out int length)
    {
        if (!TryGetArrayDimensions(type, out var dimensions) || dimensions == null)
        {
            length = 0;
            return false;
        }

        length = dimensions.First();
        return true;
    }

    /// <summary>
    /// Gets the full multiplicative length of an array including all dimensions.
    /// </summary>
    /// <remarks>
    /// For example:
    /// - uint256[2] -> 2, one for each offset
    /// - uint256[2][2] -> 4, two for the outer array and two for the inner
    /// - uint256[2][3] -> 6, three for the inner array and two for the outer
    /// - uint256[3][3][2] -> 18, two for the outer array and nine for the inner jagged array
    /// - uint256[][3] -> -1, dynamic array
    /// </remarks>
    /// <param name="type">The type to get the length for.</param>
    /// <param name="length">The length if successful.</param>
    /// <returns>True if the length was successfully parsed, false otherwise.</returns>
    public static bool TryGetArrayMultiLength(string type, out int length)
    {
        if (!TryGetArrayDimensions(type, out var dimensions) || dimensions == null)
        {
            length = 0;
            return false;
        }

        if (dimensions.Any(d => d == -1))
        {
            length = -1;
            return true;
        }

        length = dimensions.Aggregate(1, (acc, dim) => acc * dim);
        return true;
    }

    /// <summary>
    /// Gets the base type by stripping all outer array notations, preserving tuple structures
    /// (e.g., "(uint8[],uint16,uint32)" from "(uint8[],uint16,uint32)[][]").
    /// </summary>
    /// <param name="type">The type to get the base from.</param>
    /// <param name="baseType">The base type if successful.</param>
    /// <returns>True if base type was successfully parsed, false otherwise.</returns>
    public static bool TryGetArrayBaseType(string type, out string? baseType)
    {
        baseType = null;

        if (string.IsNullOrEmpty(type))
        {
            return false;
        }

        string currentType = type.Trim();

        // First check if this is actually an array type
        if (!IsArray(currentType))
        {
            return false;
        }

        // Find the outermost array notations and strip them
        int lastBracketIndex = currentType.LastIndexOf('[');
        if (lastBracketIndex < 0 || currentType.LastIndexOf(']') < lastBracketIndex)
        {
            return false; // Invalid array notation
        }

        // Strip everything from the last '[' to the end if it's an array notation
        while (lastBracketIndex >= 0 && currentType.EndsWith("]"))
        {
            currentType = currentType[..lastBracketIndex].TrimEnd();
            lastBracketIndex = currentType.LastIndexOf('[');
            if (lastBracketIndex >= 0 && currentType.LastIndexOf(']') < lastBracketIndex)
            {
                break; // No more valid array notations
            }
        }

        if (string.IsNullOrEmpty(currentType))
        {
            return false;
        }

        baseType = currentType;
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
    public static bool TryGetArrayInnerType(string type, out string? innerType)
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

    /// <summary>
    /// Validates if a type string represents a valid tuple.
    /// </summary>
    /// <param name="type">The type to validate.</param>
    /// <returns>True if the type is a valid tuple, false otherwise.</returns>
    public static bool IsValidTuple(string type)
    {
        return AbiParameters.Parse(type).All(p => IsValidType(p.AbiType));
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
        if (!TryGetArrayBaseType(type, out var baseType) || baseType == null)
        {
            return false;
        }

        if (IsTuple(baseType, false)) // check for tuple in the base type
        {
            if (!IsValidTuple(baseType))
                return false;
        }
        else
        {
            if (!IsValidBaseType(baseType))
                return false;
        }

        // Use TryGetArrayDimensions to validate the array dimensions
        if (!TryGetArrayDimensions(type, out var dimensions) || dimensions == null)
        {
            return false;
        }

        return dimensions.All(dim => dim > 0 || dim == -1); // Allow positive sizes and dynamic (-1)
    }

    private static IReadOnlyList<int>? SplitArrayDimensions(string arrayPart)
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

        dimensions.Reverse();

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

    /// <summary>
    /// Determines if a type can be directly packed in the ABI's packed encoding mode (abi.encodePacked).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type can be directly packed, false otherwise.</returns>
    /// <remarks>
    /// According to Solidity documentation for abi.encodePacked:
    /// 
    /// 1. Basic types (uint/int of any size, address, bool, bytes1-32) are concatenated directly
    /// 2. Dynamic types (string, bytes) are encoded in-place without length field
    /// 3. Arrays have their elements padded and are not directly packable
    /// 4. Structs and nested arrays are not supported
    /// 
    /// This method returns true only for types that can be directly packed without special handling.
    /// </remarks>
    public static bool CanBePacked(string type)
    {
        if (!IsValidType(type))
        {
            return false;
        }

        // Arrays (both fixed and dynamic) are not directly packable
        if (IsArray(type))
        {
            return false;
        }

        // Structs/tuples are not supported in packed mode
        if (IsTuple(type, includeArrays: false))
        {
            return false;
        }

        // All basic types (uint/int of any size, address, bool, bytes1-32)
        // and dynamic types (string, bytes) can be packed
        return true;
    }

    /// <summary>
    /// Determines if a type is supported in ABI's packed encoding mode (abi.encodePacked).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is supported in packed encoding, false otherwise.</returns>
    /// <remarks>
    /// According to Solidity documentation, the following are not supported in packed encoding:
    /// - Structs (tuples)
    /// - Nested arrays
    /// 
    /// Basic types, dynamic types, and simple (non-nested) arrays are supported.
    /// </remarks>
    public static bool IsPackingSupported(string type)
    {
        if (!IsValidType(type))
        {
            return false;
        }

        // Structs/tuples are not supported in packed mode
        if (IsTuple(type, includeArrays: true))
        {
            return false;
        }

        // Check for arrays
        if (TryGetArrayDimensions(type, out var dimensions))
        {
            if (dimensions!.Count > 1)
            {
                return false;
            }

            if (!TryGetArrayBaseType(type, out var baseType))
            {
                throw new ArgumentException($"Invalid array type '{type}'", nameof(type));
            }

            return IsPackingSupported(baseType!);
        }

        // All basic types and dynamic types are supported
        return true;
    }

    /// <summary>
    /// Determines if an indexed parameter's value is hashed in the event topic.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the indexed parameter is hashed in the topic, false if stored directly.</returns>
    /// <remarks>
    /// According to Solidity documentation:
    /// - Value types (uint, address, bool) are stored directly in topics
    /// - Everything else (arrays, strings, bytes, structs) is stored as its keccak256 hash
    /// </remarks>
    public static bool IsHashedInTopic(string type)
    {
        if (!IsValidType(type))
        {
            throw new ArgumentException($"Invalid type: {type}", nameof(type));
        }

        // Arrays (fixed or dynamic) are always hashed
        if (IsArray(type))
        {
            return true;
        }

        // Tuples are always hashed
        if (IsTuple(type, includeArrays: false))
        {
            return true;
        }

        // Dynamic types (string, bytes) are always hashed
        if (type == AbiTypeNames.String || type == AbiTypeNames.Bytes)
        {
            return true;
        }

        // Value types are stored directly
        return false;
    }
}