using System;
using System.Collections.Generic;
using System.Linq;
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
    AbiEncodingResult EncodeParameters(AbiParameters parameters, IDictionary<string, object?> values);

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
        this IAbiEncoder encoder, AbiParameters parameters, T value)
        where T : struct, IConvertible
    {
        if (parameters.Count != 1)
        {
            throw new InvalidOperationException("Expected a single parameter");
        }

        var firstKey = parameters.First().Name;
        var dictionary = new Dictionary<string, object?> { { firstKey, value } };

        return encoder.EncodeParameters(parameters, dictionary);
    }

    /// <summary>
    /// Encodes a single string parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The string value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, AbiParameters parameters, string value)
    {
        if (parameters.Count != 1)
        {
            throw new InvalidOperationException("Expected a single parameter");
        }

        var firstKey = parameters.First().Name;
        var dictionary = new Dictionary<string, object?> { { firstKey, value } };

        return encoder.EncodeParameters(parameters, dictionary);
    }

    /// <summary>
    /// Encodes a BigInteger parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The BigInteger value to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, AbiParameters parameters, BigInteger value)
    {
        if (parameters.Count != 1)
        {
            throw new InvalidOperationException("Expected a single parameter");
        }

        var firstKey = parameters.First().Name;
        var dictionary = new Dictionary<string, object?> { { firstKey, value } };

        return encoder.EncodeParameters(parameters, dictionary);
    }

    /// <summary>
    /// Encodes a byte array parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="value">The byte array to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, AbiParameters parameters, byte[] value)
    {
        if (parameters.Count != 1)
        {
            throw new InvalidOperationException("Expected a single parameter");
        }

        var firstKey = parameters.First().Name;
        var dictionary = new Dictionary<string, object?> { { firstKey, value } };

        return encoder.EncodeParameters(parameters, dictionary);
    }

    /// <summary>
    /// Encodes a byte array parameter.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="parameters">The parameters to encode.</param>
    /// <param name="tuple">The tuple to encode.</param>
    /// <returns>The encoded parameters.</returns>
    public static AbiEncodingResult EncodeParameters(
        this IAbiEncoder encoder, AbiParameters parameters, ITuple tuple)
    {
        // zip the tuple with the parameters and place into a dictionary

        if (parameters.Count != tuple.Length)
        {
            throw new InvalidOperationException("Expected a tuple with the same number of parameters");
        }

        var dictionary = new Dictionary<string, object?>();

        for (int i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var value = tuple[i];
            dictionary.Add(parameter.Name, value);
        }

        return encoder.EncodeParameters(parameters, dictionary);
    }
}