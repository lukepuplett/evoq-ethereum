using System;
using System.Numerics;
using Evoq.Blockchain;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
internal class AbiClrTypeConverter
{
    private readonly ILogger<AbiClrTypeConverter> logger;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiClrTypeConverter"/> class.
    /// </summary>
    public AbiClrTypeConverter(ILoggerFactory? loggerFactory = null)
    {
        this.logger = loggerFactory?.CreateLogger<AbiClrTypeConverter>() ?? NullLogger<AbiClrTypeConverter>.Instance;
    }

    //

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
        this.logger.LogDebug(
            "Converting value of type {SourceType} to {TargetType} (ABI type hint: {AbiType})",
            value?.GetType().Name ?? "null",
            targetType.Name,
            abiType ?? "none");

        if (value == null)
        {
            result = null;
            bool canBeNull = targetType.IsClass || Nullable.GetUnderlyingType(targetType) != null;

            this.logger.LogDebug(
                "Handling null value for type {TargetType} (can be null: {CanBeNull})",
                targetType.Name,
                canBeNull);

            return canBeNull;
        }

        if (!string.IsNullOrEmpty(abiType) && AbiTypes.TryGetDefaultClrType(abiType, out var defaultClrType))
        {
            if (targetType.IsAssignableFrom(defaultClrType))
            {
                this.logger.LogDebug(
                    "Using ABI type hint to adjust target type from {OriginalType} to {NewType}",
                    targetType.Name,
                    defaultClrType.Name);

                targetType = defaultClrType;
            }
        }

        bool success = false;
        if (targetType == typeof(BigInteger))
        {
            success = this.TryConvertToBigInteger(value, out result);
        }
        else if (targetType == typeof(byte[]))
        {
            success = this.TryConvertToByteArray(value, out result);
        }
        else if (targetType == typeof(Hex))
        {
            success = this.TryConvertToHex(value, out result);
        }
        else if (targetType == typeof(EthereumAddress))
        {
            success = this.TryConvertToEthereumAddress(value, out result);
        }
        else if (targetType == typeof(DateTime))
        {
            success = this.TryConvertToDateTime(value, out result);
        }
        else if (targetType == typeof(DateTimeOffset))
        {
            success = this.TryConvertToDateTimeOffset(value, out result);
        }
        else if (targetType.IsEnum)
        {
            success = this.TryConvertToEnum(value, targetType, out result);
        }
        else
        {
            success = this.TryStandardConversion(value, targetType, out result);
        }

        this.logger.LogDebug(
            "Conversion {Status}: {SourceType} -> {TargetType} = {Result}",
            success ? "succeeded" : "failed",
            value.GetType().Name,
            targetType.Name,
            result?.ToString() ?? "null");

        return success;
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
        this.logger.LogDebug("Converting {Type} to BigInteger", value.GetType().Name);
        result = null;

        if (value is string strValue)
        {
            if (BigInteger.TryParse(strValue, out var bigInt))
            {
                result = bigInt;
                this.logger.LogDebug("Converted string '{Value}' to BigInteger {Result}", strValue, bigInt);

                return true;
            }
        }
        else if (value is BigInteger bigIntValue)
        {
            result = bigIntValue;
            this.logger.LogDebug("Value already BigInteger: {Value}", bigIntValue);

            return true;
        }
        else if (value is IConvertible)
        {
            try
            {
                result = new BigInteger(Convert.ToInt64(value));
                this.logger.LogDebug("Converted numeric value {Value} to BigInteger {Result}", value, result);

                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(
                    ex,
                    "Failed to convert {Value} to BigInteger",
                    value);
            }
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
        this.logger.LogDebug("Converting {Type} to byte[]", value.GetType().Name);
        result = null;

        if (value is string hexString)
        {
            try
            {
                var options = HexParseOptions.AllowOddLength | HexParseOptions.AllowEmptyString;
                result = Hex.Parse(hexString[2..], options).ToByteArray();
                this.logger.LogDebug("Converted hex string of length {Length} to byte array", hexString.Length);

                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to parse hex string '{Value}'", hexString);
            }
        }
        else if (value is byte[] byteArray)
        {
            result = byteArray;
            this.logger.LogDebug("Value already byte array of length {Length}", byteArray.Length);

            return true;
        }
        else if (value is Hex hex)
        {
            result = hex.ToByteArray();
            this.logger.LogDebug("Converted Hex of length {Length} to byte array", hex.Length);

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
        this.logger.LogDebug("Converting {Type} to Hex", value.GetType().Name);
        result = null;

        if (value is string hexString)
        {
            try
            {
                var options = HexParseOptions.AllowOddLength | HexParseOptions.AllowEmptyString;
                result = Hex.Parse(hexString, options);
                this.logger.LogDebug("Parsed string '{Value}' to Hex", hexString);

                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Failed to parse hex string '{Value}'", hexString);
            }
        }
        else if (value is byte[] byteArray)
        {
            result = new Hex(byteArray);
            this.logger.LogDebug("Converted byte array of length {Length} to Hex", byteArray.Length);

            return true;
        }
        else if (value is Hex hex)
        {
            result = hex;
            this.logger.LogDebug("Value already Hex of length {Length}", hex.Length);

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
        this.logger.LogDebug("Converting {Type} to EthereumAddress", value.GetType().Name);
        result = null;

        if (value is string addressStr)
        {
            try
            {
                result = new EthereumAddress(addressStr);
                this.logger.LogDebug("Parsed string '{Value}' to EthereumAddress", addressStr);

                return true;
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(
                    ex,
                    "Failed to parse address string '{Value}'",
                    addressStr);
            }
        }
        else if (value is byte[] byteArray)
        {
            if (byteArray.Length == 20)
            {
                try
                {
                    result = new EthereumAddress(byteArray);
                    this.logger.LogDebug("Converted 20-byte array to EthereumAddress");

                    return true;
                }
                catch (Exception ex)
                {
                    this.logger.LogWarning(
                        ex,
                        "Failed to create address from byte array");
                }
            }
            else
            {
                this.logger.LogWarning(
                    "Invalid byte array length {Length} for EthereumAddress (expected 20)",
                    byteArray.Length);
            }
        }
        else if (value is Hex hex)
        {
            result = new EthereumAddress(hex.ToByteArray());
            this.logger.LogDebug("Converted Hex of length {Length} to EthereumAddress", hex.Length);

            return true;
        }
        else if (value is EthereumAddress address)
        {
            result = address;
            this.logger.LogDebug("Value already EthereumAddress: {Address}", address);

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
    /// Attempts to convert a value to a DateTime.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Converts Unix timestamps to DateTime with Kind.Unspecified.
    /// </remarks>
    private bool TryConvertToDateTime(object value, out object? result)
    {
        result = null;

        if (TryConvertToBigInteger(value, out var bigIntResult) && bigIntResult is BigInteger timestamp)
        {
            try
            {
                result = DateTimeOffset.FromUnixTimeSeconds((long)timestamp).DateTime;
                return true;
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to convert a value to a DateTimeOffset.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <param name="result">The converted value if successful.</param>
    /// <returns>True if the conversion was successful, false otherwise.</returns>
    /// <remarks>
    /// Converts Unix timestamps to DateTimeOffset with zero offset (UTC).
    /// </remarks>
    private bool TryConvertToDateTimeOffset(object value, out object? result)
    {
        result = null;

        if (TryConvertToBigInteger(value, out var bigIntResult) && bigIntResult is BigInteger timestamp)
        {
            try
            {
                result = DateTimeOffset.FromUnixTimeSeconds((long)timestamp);
                return true;
            }
            catch
            {
                return false;
            }
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
        this.logger.LogDebug(
            "Attempting standard conversion from {SourceType} to {TargetType}",
            value.GetType().Name,
            targetType.Name);

        result = null;

        Type? underlyingType = Nullable.GetUnderlyingType(targetType);
        if (underlyingType != null)
        {
            this.logger.LogDebug(
                "Handling nullable type conversion to {UnderlyingType}",
                underlyingType.Name);

            if (TryStandardConversion(value, underlyingType, out var innerResult))
            {
                result = innerResult;
                return true;
            }
            return false;
        }

        if (targetType.IsAssignableFrom(value.GetType()))
        {
            this.logger.LogDebug("Direct assignment possible, no conversion needed");
            result = value;

            return true;
        }

        try
        {
            if (value is IConvertible convertible)
            {
                result = Convert.ChangeType(convertible, targetType);
                this.logger.LogDebug(
                    "Standard conversion succeeded: {Value} -> {Result}",
                    value,
                    result);

                return true;
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(
                ex,
                "Standard conversion failed from {SourceType} to {TargetType}",
                value.GetType().Name,
                targetType.Name);
        }

        return false;
    }
}