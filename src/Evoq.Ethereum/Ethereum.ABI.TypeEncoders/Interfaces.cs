using System;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Interface for checking if a type is compatible with an ABI type.
/// </summary>
public interface IAbiTypeCompatible
{
    /// <summary>
    /// Determines if the given type is compatible with the ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="valueType">The type to check</param>
    /// <param name="message">The message if the type is not compatible</param>
    bool IsCompatible(string abiType, Type valueType, out string message);
}

/// <summary>
/// Interface for checking if a value is compatible with an ABI type.
/// </summary>
public interface IAbiValueCompatible
{
    /// <summary>
    /// Determines if the given type is compatible with the ABI type.
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="value">The value to check</param>
    /// <param name="message">The message if the value is not compatible</param>
    /// <param name="tryEncoding">If true, the method will try to encode the value which is more expensive but more robust.</param>
    bool IsCompatible(string abiType, object value, out string message, bool tryEncoding = false);
}

/// <summary>
/// Interface for encoding specific ABI types to their binary representation
/// </summary>
public interface IAbiEncode : IAbiTypeCompatible
{
    /// <summary>
    /// Attempts to encode a value to its ABI binary representation
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="value">The value to encode</param>
    /// <param name="bytes">The encoded bytes if successful</param>
    /// <param name="length">The length of the bytes to encode.</param>
    /// <returns>True if encoding was successful, false otherwise</returns>
    bool TryEncode(string abiType, object value, out byte[] bytes, int length = 32);
}

/// <summary>
/// Interface for decoding specific ABI types from their binary representation
/// </summary>
public interface IAbiDecode
{
    /// <summary>
    /// Attempts to decode a value from its ABI binary representation
    /// </summary>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="data">The data to decode</param>
    /// <param name="clrType">The CLR type to decode to</param>
    /// <param name="decoded">The decoded value if successful</param>
    /// <returns>True if decoding was successful, false otherwise</returns>
    bool TryDecode(string abiType, byte[] data, Type clrType, out object? decoded);
}
