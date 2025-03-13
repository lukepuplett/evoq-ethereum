using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Evoq.Ethereum.ABI.Conversion;
using Evoq.Ethereum.ABI.TypeEncoders;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Extension methods for the ABI classes.
/// </summary>
public static class AbiExtensions
{
    /// <summary>
    /// Converts an ITuple to a list of object arrays.
    /// </summary>
    /// <param name="tuple">The tuple to convert.</param>
    /// <returns>A list of object arrays.</returns>
    public static IReadOnlyList<object> ToList(this ITuple tuple)
    {
        var list = new List<object>();
        for (int i = 0; i < tuple.Length; i++)
        {
            list.Add(tuple[i]);
        }
        return list;
    }

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
    private static string GetCanonicalType(ContractAbiParameter param)
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
    public static FunctionSignature GetFunctionSignature(this ContractAbiItem item)
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
        string inputsSignature = format(item.Inputs);
        string outputsSignature = format(item.Outputs);

        // Create a new FunctionSignature using the string representations
        return new FunctionSignature(item.Name, inputsSignature, outputsSignature);

        //

        static string format(IEnumerable<ContractAbiParameter>? parameters)
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
    /// Gets the function selector for an ABI item.
    /// </summary>
    /// <param name="item">The ABI item.</param>
    /// <returns>The 4-byte function selector.</returns>
    public static byte[] GetFunctionSelector(this ContractAbiItem item)
    {
        // Use the canonical signature from GetCanonicalSignature
        var signature = item.GetCanonicalSignature();
        return KeccakHash.ComputeHash(Encoding.UTF8.GetBytes(signature)).Take(4).ToArray();
    }

    /// <summary>
    /// Sets the value at the specified index of the array.
    /// </summary>
    /// <param name="array">The array to set the value on.</param>
    /// <param name="index">The index to set the value at.</param>
    /// <param name="value">The value to set.</param>
    /// <exception cref="AbiTypeMismatchException">Thrown when the value is not compatible with the array element type.</exception>
    public static void SetValueEx(this Array array, object? value, int index)
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
    /// Converts an <see cref="AbiParameters"/> object to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type of the object to convert to.</typeparam>
    /// <param name="parameters">The parameters to convert.</param>
    /// <returns>A strongly-typed object.</returns>
    public static T ToObject<T>(this AbiParameters parameters) where T : new()
    {
        var converter = new AbiConverter();

        return converter.DictionaryToObject<T>(parameters.ToDictionary(true));
    }

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