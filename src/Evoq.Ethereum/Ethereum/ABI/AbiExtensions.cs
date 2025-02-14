using System;
using System.Linq;
using System.Text;
using Evoq.Ethereum.Crypto;

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
    public static string GetCanonicalSignature(this AbiItem item)
    {
        if (item.Type != "function" && item.Type != "event" && item.Type != "error")
            throw new ArgumentException("Item must be a function, event, or error", nameof(item));

        var parameters = string.Join(",", item.Inputs.Select(p => GetCanonicalType(p)));
        return $"{item.Name}({parameters})";
    }

    /// <summary>
    /// Get the canonical type for a parameter.
    /// </summary>
    /// <param name="param">The parameter to get the canonical type for.</param>
    /// <returns>The canonical type.</returns>
    private static string GetCanonicalType(Parameter param)
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
    public static FunctionSignature GetFunctionSignature(this AbiItem item)
    {
        if (item.Type != "function")
            throw new ArgumentException("ABI item must be a function", nameof(item));

        if (string.IsNullOrEmpty(item.Name))
            throw new ArgumentException("Function must have a name", nameof(item));

        // Convert parameters to canonical parameter string
        var parameters = string.Join(",", item.Inputs.Select(p => p.Type));

        return new FunctionSignature(item.Name, parameters);
    }

    /// <summary>
    /// Gets the function selector for an ABI item.
    /// </summary>
    /// <param name="item">The ABI item.</param>
    /// <returns>The 4-byte function selector.</returns>
    public static byte[] GetFunctionSelector(this AbiItem item)
    {
        return item.GetFunctionSignature().GetSelector();
    }
}