using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// A default compatibility checker that checks if the ABI type is supported and if the .NET type is supported.
/// </summary>
public class AbiCompatChecker : IAbiTypeCompatible, IAbiValueCompatible
{
    private readonly HashSet<string> _supportedAbiTypes;
    private readonly HashSet<Type> _supportedDotNetTypes;
    private readonly IAbiEncode? _encoder;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiCompatChecker"/> class.
    /// </summary>
    /// <param name="supportedAbiTypes">The ABI types supported by the encoder.</param>
    /// <param name="supportedDotNetTypes">The .NET types supported by the encoder.</param>
    /// <param name="encoder">The encoder to use.</param>
    public AbiCompatChecker(
        HashSet<string> supportedAbiTypes,
        HashSet<Type> supportedDotNetTypes,
        IAbiEncode? encoder = null)
    {
        _supportedAbiTypes = supportedAbiTypes;
        _supportedDotNetTypes = supportedDotNetTypes;
        _encoder = encoder;
    }

    /// <summary>
    /// Determines if the given type is compatible with the ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="valueType">The type to check</param>
    /// <param name="message">The message if the type is not compatible</param>
    /// <returns>True if the type is compatible, false otherwise</returns>
    public virtual bool IsCompatible(string abiType, Type valueType, out string message)
    {
        if (abiType == null)
        {
            message = "ABI type is null";
            return false;
        }

        if (valueType == null)
        {
            message = "Value type is null";
            return false;
        }

        Debug.Assert(!AbiTypeValidator.IsOfTypeType(valueType), "Value type is Type, this indicates a bug, not a validation error");

        // ABI type must be in its supported types, and .NET type must be in its supported types

        if (!_supportedAbiTypes.Contains(abiType))
        {
            message = $"Unsupported ABI type: {abiType}";
            return false;
        }

        if (!_supportedDotNetTypes.Contains(valueType))
        {
            message = $"Unsupported .NET type: {valueType.FullName}";
            return false;
        }

        message = "OK";
        return true;
    }

    /// <summary>
    /// Determines if the given value is compatible with the ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="value">The value to check</param>
    /// <param name="message">The message if the value is not compatible</param>
    /// <param name="tryEncoding">If true, the method will try to encode the value which is more expensive but more robust.</param>
    /// <returns>True if the value is compatible, false otherwise</returns>
    public virtual bool IsCompatible(string abiType, object value, out string message, bool tryEncoding = false)
    {
        if (tryEncoding && _encoder == null)
        {
            throw new InvalidOperationException("Cannot check compatibility by encoding because no encoder is set");
        }

        if (!this.IsCompatible(abiType, value.GetType(), out message))
        {
            return false;
        }

        if (tryEncoding && _encoder != null)
        {
            return _encoder.TryEncode(abiType, value, out var _);
        }

        message = "OK";
        return true;
    }
}
