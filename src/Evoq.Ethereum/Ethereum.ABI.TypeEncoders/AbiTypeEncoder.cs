using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Base class for all ABI type checkers.
/// </summary>
public abstract class AbiTypeChecker
{
    private readonly HashSet<string> _supportedAbiTypes;
    private readonly HashSet<Type> _supportedDotNetTypes;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeChecker"/> class.
    /// </summary>
    /// <param name="supportedAbiTypes">The ABI types supported by the encoder.</param>
    /// <param name="supportedDotNetTypes">The .NET types supported by the encoder.</param>
    public AbiTypeChecker(HashSet<string> supportedAbiTypes, HashSet<Type> supportedDotNetTypes)
    {
        _supportedAbiTypes = supportedAbiTypes;
        _supportedDotNetTypes = supportedDotNetTypes;
    }

    /// <summary>
    /// Determines if the given type is compatible with the ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="valueType">The type to check</param>
    /// <returns>True if the type is compatible, false otherwise</returns>
    public bool IsCompatible(string abiType, Type valueType)
    {
        if (!_supportedAbiTypes.Contains(abiType))
        {
            return false;
        }

        if (valueType == null || !_supportedDotNetTypes.Contains(valueType))
        {
            return false;
        }

        return true;
    }
}
