using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// An encoder for Ethereum ABI parameters.
/// </summary>
public interface IAbiEncoder
{
    /// <summary>
    /// Encodes the parameters.
    /// </summary>
    /// <remarks>
    /// If a signature has a single tuple parameter, the values must be forced into a
    /// <see cref="ValueTuple"/> rather than using the () brackets for the root tuple.
    /// 
    /// For example, this signature:
    /// <code>
    /// function foo((bool isActive, uint256 seenUnix) prof)
    /// </code>
    /// 
    /// 
    /// 
    /// </remarks>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="values">The values to encode.</param>
    /// <returns>The encoded parameters.</returns>
    AbiEncodingResult EncodeParameters(EvmParameters parameters, ITuple values);

    // /// <summary>
    // /// Resolves the encoder for a given type.
    // /// </summary>
    // /// <param name="abiType">The type to resolve the encoder for.</param>
    // /// <param name="value">The value to encode.</param>
    // /// <param name="encoder">The encoder for the given type.</param>
    // /// <returns>True if the encoder was resolved, false otherwise.</returns>
    // bool TryFindStaticSlotEncoder(string abiType, object value, out Func<object, Slot>? encoder);
}

/// <summary>
/// Extension methods for <see cref="IAbiEncoder"/>.
/// </summary>
public static class AbiEncoderExtensions
{
    /// <summary>
    /// Encodes a single parameter.
    /// </summary>
    /// <typeparam name="T">The type of the value to encode. Must be a value type or string.</typeparam>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters<T>(
        this IAbiEncoder encoder, EvmParameters parameters, T value)
        where T : struct, IConvertible
    {
        return encoder.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a single string parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The string value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, EvmParameters parameters, string value)
    {
        return encoder.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a BigInteger parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The BigInteger value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, EvmParameters parameters, BigInteger value)
    {
        return encoder.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a byte array parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The byte array to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, EvmParameters parameters, byte[] value)
    {
        return encoder.EncodeParameters(parameters, ValueTuple.Create(value));
    }

    /// <summary>
    /// Encodes a byte array parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The array to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, EvmParameters parameters, Array value)
    {
        return encoder.EncodeParameters(parameters, ValueTuple.Create(value));
    }
}