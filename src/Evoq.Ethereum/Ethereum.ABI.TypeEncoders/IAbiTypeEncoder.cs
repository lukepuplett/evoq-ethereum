using System;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Interface for encoding specific ABI types to their binary representation
/// </summary>
public interface IAbiTypeEncoder
{
    /// <summary>
    /// Determines if the given type is compatible with the ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="valueType">The type to check</param>
    /// <returns>True if the type is compatible, false otherwise</returns>
    bool IsCompatible(string abiType, Type valueType);

    /// <summary>
    /// Attempts to encode a value to its ABI binary representation
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="value">The value to encode</param>
    /// <param name="bytes">The encoded bytes if successful</param>
    /// <returns>True if encoding was successful, false otherwise</returns>
    bool TryEncode(string abiType, object value, out byte[] bytes);
}