using System;

namespace Evoq.Ethereum.ABI.TypeEncoders;

/// <summary>
/// Extension methods for <see cref="IAbiDecode"/>
/// </summary>
public static class AbiExtensions
{
    /// <summary>
    /// Attempts to decode a value from its ABI binary representation
    /// </summary>
    /// <typeparam name="T">The type to decode to</typeparam>
    /// <param name="decoder">The decoder to use</param>
    /// <param name="abiType">The ABI type string (e.g. "uint256", "address")</param>
    /// <param name="data">The data to decode</param>
    /// <param name="decoded">The decoded value if successful</param>
    /// <returns></returns>
    public static bool TryDecode<T>(this IAbiDecode decoder, string abiType, byte[] data, out T decoded)
    {
        if (decoder.TryDecode(abiType, data, typeof(T), out var decodedObject) && decodedObject is T t)
        {
            decoded = t;
            return true;
        }

        decoded = default!;
        return false;
    }

    /// <summary>
    /// Gets the base element type of an array or list type.
    /// </summary>
    /// <param name="type">The type to get the base element type of.</param>
    /// <returns>The base element type of the array or list type.</returns>
    public static Type GetBaseElementType(this Type type)
    {
        var elementType = type.GetElementType();

        if (elementType == null)
        {
            return type;
        }

        if (elementType.IsArray)
        {
            return GetBaseElementType(elementType);
        }

        return elementType;
    }
}