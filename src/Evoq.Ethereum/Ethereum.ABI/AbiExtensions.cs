using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI.Conversion;
using Evoq.Ethereum.ABI.TypeEncoders;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Extension methods for the ABI classes.
/// </summary>
public static class AbiExtensions
{
    /// <summary>
    /// Get the canonical signature for an item.
    /// </summary>
    /// <param name="item">The item to get the canonical signature for.</param>
    /// <returns>The canonical signature.</returns>
    public static string GetCanonicalSignature(this ContractAbiItem item)
    {
        if (item.Type != "function" && item.Type != "event" && item.Type != "error")
            throw new ArgumentException("Item must be a function, event, or error", nameof(item));

        // Use AbiParameterFormatter to format the parameters
        var parametersString = AbiParameterFormatter.FormatParameters(item.Inputs);

        return $"{item.Name}{parametersString}";
    }

    /// <summary>
    /// Get the canonical type for a parameter.
    /// </summary>
    /// <param name="param">The parameter to get the canonical type for.</param>
    /// <returns>The canonical type.</returns>
    public static string GetCanonicalType(this ContractAbiParameter param)
    {
        if (param.Components != null && param.Components.Any())
        {
            // Handle tuple type
            var components = string.Join(",", param.Components.Select(GetCanonicalType));
            return $"({components})";
        }

        return param.Type;
    }

    /// <summary>
    /// Gets the function signature for an ABI item.
    /// </summary>
    /// <param name="item">The ABI item.</param>
    /// <returns>The function signature.</returns>
    /// <exception cref="ArgumentException">If the item is not a function.</exception>
    public static AbiSignature GetFunctionSignature(this ContractAbiItem item)
    {
        if (item.Type != "function")
        {
            throw new ArgumentException("ABI item must be a function", nameof(item));
        }

        if (string.IsNullOrEmpty(item.Name))
        {
            throw new ArgumentException("Function must have a name", nameof(item));
        }

        // Format the inputs and outputs using FormatParameterSignature
        string inputsSignature = formatSigStr(item.Inputs);
        string outputsSignature = formatSigStr(item.Outputs);

        // Create a new FunctionSignature using the string representations
        return new AbiSignature(AbiItemType.Function, item.Name, inputsSignature, outputsSignature);

        //

        static string formatSigStr(IEnumerable<ContractAbiParameter>? parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return string.Empty;
            }

            var formattedParams = AbiParameterFormatter.FormatParameters(parameters, includeNames: true);

            return formattedParams;
        }
    }

    /// <summary>
    /// Gets the event signature for an ABI item.
    /// </summary>
    /// <param name="item">The ABI item.</param>
    /// <returns>The event signature.</returns>
    /// <exception cref="ArgumentException">If the item is not an event.</exception>
    public static AbiSignature GetEventSignature(this ContractAbiItem item)
    {
        if (item.Type != "event")
        {
            throw new ArgumentException("ABI item must be an event", nameof(item));
        }

        if (string.IsNullOrEmpty(item.Name))
        {
            throw new ArgumentException("Event must have a name", nameof(item));
        }

        string inputsSignature = formatSigStr(item.Inputs);

        return new AbiSignature(AbiItemType.Event, item.Name, inputsSignature)
        {
            IsAnonymous = item.Anonymous ?? false
        };

        //

        static string formatSigStr(IEnumerable<ContractAbiParameter>? parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                return string.Empty;
            }

            var formattedParams = AbiParameterFormatter.FormatParameters(
                parameters,
                includeNames: true,
                includeIndexed: true);

            return formattedParams;
        }
    }

    // encode

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

    // decode

    /// <summary>
    /// Decodes a single parameter.
    /// </summary>
    /// <param name="decoder">The decoder.</param>
    /// <param name="parameter">The parameter to decode.</param>
    /// <param name="data">The data to decode.</param>
    /// <returns>The decoded parameter.</returns>
    public static object? DecodeParameter(this IAbiDecoder decoder, AbiParam parameter, byte[] data)
    {
        var parameters = new AbiParameters(new[] { parameter });
        var r = decoder.DecodeParameters(parameters, data);

        return r.Parameters.First().Value;
    }

    /// <summary>
    /// Decodes a set of parameters from a hex string.
    /// </summary>
    /// <param name="decoder">The decoder to use.</param>
    /// <param name="parameters">The parameters to decode.</param>
    /// <param name="data">The hex string to decode.</param>
    /// <returns>The decoded parameters.</returns>
    public static AbiDecodingResult DecodeParameters(
        this IAbiDecoder decoder, AbiParameters parameters, Hex data) =>
            decoder.DecodeParameters(parameters, data.ToByteArray());

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

    //

    /// <summary>
    /// Converts an <see cref="AbiParameters"/> object to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert to.</typeparam>
    /// <param name="parameters">The parameters to convert.</param>
    /// <param name="loggerFactory">The logger factory to use.</param>
    /// <returns>A strongly-typed object.</returns>
    internal static T ToObject<T>(this AbiParameters parameters, ILoggerFactory? loggerFactory = null) where T : new()
    {
        // if a function returns a tuple, it will be the first and only unnamed parameter
        // in the dictionary

        // the consumer doesn't expect this unusual situation and expects to just
        // convert the tuple directly, else they'd have to create a root POCO with a
        // single 'actual' POCO property for the tuple

        // we can detect this situation 'edit out' the extra dictionary layer
        // and just convert the tuple directly

        Dictionary<string, object?> keyValues = parameters.ToDictionary(true);

        if (keyValues.Count == 1 &&
            keyValues.First().Value is Dictionary<string, object?> innerActual &&
            parameters.Count == 1 &&
            string.IsNullOrEmpty(parameters.First().Name) &&
            parameters.First().IsTupleStrict)
        {
            keyValues = innerActual;
        }

        return new AbiConverter(loggerFactory).DictionaryToObject<T>(keyValues);
    }

    /// <summary>
    /// Converts an ITuple of two values to a KeyValuePair.
    /// </summary>
    /// <param name="tuple">The tuple to convert.</param>
    /// <returns>A KeyValuePair.</returns>
    /// <exception cref="ArgumentException">Thrown when the tuple has more than two elements.</exception>
    internal static KeyValuePair<string, object?> ToKeyValue(this ITuple tuple)
    {
        if (tuple.Length != 2)
        {
            throw new ArgumentException("Tuple must have exactly two elements", nameof(tuple));
        }

        var elements = tuple.GetElements().ToList();

        return new KeyValuePair<string, object?>(elements[0]!.ToString()!, elements[1]);
    }

    /// <summary>
    /// Converts an ITuple to a list of object arrays.
    /// </summary>
    /// <param name="tuple">The tuple to convert.</param>
    /// <returns>A list of object arrays.</returns>
    internal static IReadOnlyList<object> ToList(this ITuple tuple)
    {
        var list = new List<object>();
        for (int i = 0; i < tuple.Length; i++)
        {
            list.Add(tuple[i]);
        }
        return list;
    }

    /// <summary>
    /// Sets the value at the specified index of the array.
    /// </summary>
    /// <param name="array">The array to set the value on.</param>
    /// <param name="index">The index to set the value at.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="AbiTypeMismatchException">Thrown when the value is not compatible with the array element type.</exception>
    internal static void SetValueEx(this Array array, object? value, int index)
    {
        try
        {
            array.SetValue(value, index);
        }
        catch (InvalidCastException invalidCast)
        {
            throw new AbiTypeMismatchException(
                $"Could not set value at index {index} of array '{array.GetType().Name}'. " +
                $"Value of type '{value?.GetType().Name}' is not compatible with the array element type '{array.GetType().GetElementType()?.Name}'.",
                invalidCast);
        }
    }

    /// <summary>
    /// Gets the base element type of an array or list type.
    /// </summary>
    /// <param name="type">The type to get the base element type of.</param>
    /// <returns>The base element type of the array or list type.</returns>
    internal static Type GetBaseElementType(this Type type)
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