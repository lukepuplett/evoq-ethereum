using System;
using System.Numerics;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Provides conversion between various .NET types with support for Ethereum-specific types and ABI type hints.
/// </summary>
/// <remarks>
/// This class handles conversion between different .NET representations of values, with special handling for
/// Ethereum-specific types like BigInteger, Hex, EthereumAddress, and byte arrays. It can optionally use
/// ABI type information as a hint to guide the conversion process.
/// 
/// Key features:
/// - Converts between standard .NET types (string, int, bool, etc.)
/// - Handles Ethereum-specific types (BigInteger, Hex, EthereumAddress)
/// - Supports nullable types
/// - Uses ABI type information as conversion hints when available
/// - Provides robust null handling
/// </remarks>
public class AbiClrTypeConverter
{
    /// <summary>
    /// Attempts to convert a value to the specified target type, optionally using ABI type information to guide the conversion.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <param name="abiType">Optional ABI type string (e.g., "uint256", "address") to assist with conversion.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// This method serves as the main entry point for type conversion. It handles:
    /// - Null values (returns true if target type can accept nulls)
    /// - ABI type hints (uses AbiTypes.TryGetDefaultClrType to determine appropriate CLR type)
    /// - Special type conversions for Ethereum-specific types
    /// - Standard type conversions for common .NET types
    /// </remarks>
    public bool TryConvert(object? value, Type targetType, out object? result, string? abiType = null)
    {
        // Handle null values - return true only if target type can accept nulls
        if (value == null)
        {
            result = null;
            return targetType.IsClass || Nullable.GetUnderlyingType(targetType) != null;
        }

        // If we have an ABI type, try to use it for conversion
        // This helps with cases where the ABI type provides more specific type information
        if (!string.IsNullOrEmpty(abiType) && AbiTypes.TryGetDefaultClrType(abiType, out var defaultClrType))
        {
            // If the target type is compatible with the default CLR type, use it
            if (targetType.IsAssignableFrom(defaultClrType))
            {
                targetType = defaultClrType;
            }
        }

        // Handle specific type conversions based on target type

        // Handle BigInteger conversion (for uint/int types in ABI)
        if (targetType == typeof(BigInteger))
        {
            return TryConvertToBigInteger(value, out result);
        }

        // Handle byte array conversion (for bytes types in ABI)
        if (targetType == typeof(byte[]))
        {
            return TryConvertToByteArray(value, out result);
        }

        // Handle Hex conversion (for bytes/hash types in ABI)
        if (targetType == typeof(Hex))
        {
            return TryConvertToHex(value, out result);
        }

        // Handle EthereumAddress conversion (for address type in ABI)
        if (targetType == typeof(EthereumAddress))
        {
            return TryConvertToEthereumAddress(value, out result);
        }

        // Handle enum conversion
        if (targetType.IsEnum)
        {
            return TryConvertToEnum(value, targetType, out result);
        }

        // Handle standard conversions for other types
        return TryStandardConversion(value, targetType, out result);
    }

    /// <summary>
    /// Attempts to convert a value to a BigInteger.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Handles conversion from various numeric types and strings to BigInteger.
    /// This is particularly useful for Ethereum uint/int types which can be larger than standard .NET numeric types.
    /// </remarks>
    private bool TryConvertToBigInteger(object value, out object? result)
    {
        result = null;

        // Try to convert from string representation
        if (value is string strValue && BigInteger.TryParse(strValue, out var bigInt))
        {
            result = bigInt;
            return true;
        }
        // Handle various numeric types
        else if (value is int intValue)
        {
            result = new BigInteger(intValue);
            return true;
        }
        else if (value is uint uintValue)
        {
            result = new BigInteger(uintValue);
            return true;
        }
        else if (value is long longValue)
        {
            result = new BigInteger(longValue);
            return true;
        }
        else if (value is ulong ulongValue)
        {
            result = new BigInteger(ulongValue);
            return true;
        }
        else if (value is byte byteValue)
        {
            result = new BigInteger(byteValue);
            return true;
        }
        else if (value is sbyte sbyteValue)
        {
            result = new BigInteger(sbyteValue);
            return true;
        }
        else if (value is short shortValue)
        {
            result = new BigInteger(shortValue);
            return true;
        }
        else if (value is ushort ushortValue)
        {
            result = new BigInteger(ushortValue);
            return true;
        }
        // If already a BigInteger, just return it
        else if (value is BigInteger bigIntValue)
        {
            result = bigIntValue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to convert a value to a byte array.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Handles conversion from hex strings and Hex objects to byte arrays.
    /// This is particularly useful for Ethereum bytes types.
    /// </remarks>
    private bool TryConvertToByteArray(object value, out object? result)
    {
        result = null;

        // Try to convert from hex string
        if (value is string hexString)
        {
            try
            {
                // Assume the string starts with "0x" and parse the rest
                result = Hex.Parse(hexString[2..]).ToByteArray();
                return true;
            }
            catch
            {
                return false;
            }
        }
        // If already a byte array, just return it
        else if (value is byte[] byteArray)
        {
            result = byteArray;
            return true;
        }
        // Convert from Hex object
        else if (value is Hex hex)
        {
            result = hex.ToByteArray();
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to convert a value to a Hex.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Handles conversion from strings and byte arrays to Hex objects.
    /// This is useful for representing binary data in hexadecimal format.
    /// </remarks>
    private bool TryConvertToHex(object value, out object? result)
    {
        result = null;

        // Try to convert from string
        if (value is string hexString)
        {
            try
            {
                result = Hex.Parse(hexString);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Convert from byte array
        else if (value is byte[] byteArray)
        {
            result = new Hex(byteArray);
            return true;
        }
        // If already a Hex, just return it
        else if (value is Hex hex)
        {
            result = hex;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to convert a value to an EthereumAddress.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Handles conversion from strings and byte arrays to EthereumAddress objects.
    /// This is particularly useful for Ethereum address type.
    /// </remarks>
    private bool TryConvertToEthereumAddress(object value, out object? result)
    {
        result = null;

        // Try to convert from string
        if (value is string addressStr)
        {
            try
            {
                result = new EthereumAddress(addressStr);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Convert from byte array (must be 20 bytes for Ethereum address)
        else if (value is byte[] byteArray && byteArray.Length == 20)
        {
            try
            {
                result = new EthereumAddress(byteArray);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // If already an EthereumAddress, just return it
        else if (value is EthereumAddress address)
        {
            result = address;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to convert a value to an enum.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="enumType">The enum type to convert to.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Handles conversion from strings and integers to enum values.
    /// This is useful for mapping numeric or string values to enum constants.
    /// </remarks>
    private bool TryConvertToEnum(object value, Type enumType, out object? result)
    {
        result = null;

        // Try to convert from string (enum name)
        if (value is string enumStr)
        {
            try
            {
                result = Enum.Parse(enumType, enumStr, true);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // Try to convert from int (enum value)
        else if (value is int enumInt)
        {
            try
            {
                result = Enum.ToObject(enumType, enumInt);
                return true;
            }
            catch
            {
                return false;
            }
        }
        // If already the correct enum type, just return it
        else if (value.GetType().IsEnum && value.GetType() == enumType)
        {
            result = value;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Attempts to perform a standard conversion using Convert.ChangeType.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="targetType">The target type to convert to.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Handles standard type conversions for common .NET types.
    /// This includes handling nullable types and direct type assignments.
    /// </remarks>
    private bool TryStandardConversion(object value, Type targetType, out object? result)
    {
        result = null;

        // Handle nullable types by unwrapping them and converting to the underlying type
        Type underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            if (TryStandardConversion(value, underlyingType, out var innerResult))
            {
                result = innerResult;
                return true;
            }
            return false;
        }

        // Direct assignment if types are compatible (no conversion needed)
        if (targetType.IsAssignableFrom(value.GetType()))
        {
            result = value;
            return true;
        }

        // Try standard conversion using Convert.ChangeType for IConvertible types
        try
        {
            if (value is IConvertible convertible)
            {
                result = Convert.ChangeType(convertible, targetType);
                return true;
            }
        }
        catch
        {
            return false;
        }

        return false;
    }
}