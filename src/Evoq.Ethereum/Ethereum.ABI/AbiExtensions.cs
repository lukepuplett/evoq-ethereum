using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
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
        string inputsSignature = FormatParameterSignature(item.Inputs);
        string outputsSignature = FormatParameterSignature(item.Outputs);

        // Create a new FunctionSignature using the string representations
        return new FunctionSignature(item.Name, inputsSignature, outputsSignature);
    }

    /// <summary>
    /// Formats a list of parameters into a signature string.
    /// </summary>
    /// <param name="parameters">The parameters to format.</param>
    /// <returns>A formatted signature string.</returns>
    private static string FormatParameterSignature(IEnumerable<ContractAbiParameter>? parameters)
    {
        if (parameters == null || !parameters.Any())
        {
            return string.Empty;
        }

        // var formattedParams = string.Join(",", parameters.Select(AbiParameterFormatter.FormatParameter));

        var formattedParams = AbiParameterFormatter.FormatParameters(parameters);

        return formattedParams;
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
}