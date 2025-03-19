using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Provides methods for formatting ABI parameters into canonical string representations.
/// </summary>
internal static class AbiParameterFormatter
{
    /// <summary>
    /// Formats a list of parameters into a canonical signature string.
    /// </summary>
    /// <param name="parameters">The parameters to format.</param>
    /// <param name="includeNames">Whether to include parameter names in the output.</param>
    /// <param name="includeIndexed">Whether to include indexed in the output.</param>
    /// <returns>A formatted signature string.</returns>
    public static string FormatParameters(
        IEnumerable<ContractAbiParameter>? parameters, bool includeNames = false, bool includeIndexed = false)
    {
        if (parameters == null || !parameters.Any())
        {
            return "()";
        }

        var formattedParams = string.Join(",", parameters.Select(p =>
        {
            // Use FormatParameter for all parameters to ensure consistency
            return FormatParameter(p, includeNames, includeIndexed);
        }));

        return $"({formattedParams})";
    }

    /// <summary>
    /// Formats a single parameter into its canonical type representation.
    /// </summary>
    /// <param name="parameter">The parameter to format.</param>
    /// <param name="includeNames">Whether to include parameter names in the output.</param>
    /// <param name="includeIndexed">Whether to include indexed in the output.</param>
    /// <returns>The formatted parameter type.</returns>
    public static string FormatParameter(
        ContractAbiParameter parameter, bool includeNames = false, bool includeIndexed = false)
    {
        string typeStr;

        // If it's a tuple, format it specially
        if (parameter.IsTuple)
        {
            // Format the tuple components
            var componentsString = FormatTupleComponents(parameter.Components, includeNames, includeIndexed);

            // Check if this is an array type
            if (parameter.Type.Contains("["))
            {
                // Extract the array part (e.g., "[]", "[5]")
                var arrayStartIndex = parameter.Type.IndexOf('[');
                var arrayPart = parameter.Type.Substring(arrayStartIndex);

                // For tuple arrays, format as (components)[] without extra parentheses
                typeStr = $"({componentsString}){arrayPart}";
            }
            else
            {
                // For regular tuples
                typeStr = $"({componentsString})";
            }
        }
        else
        {
            // For non-tuple types
            typeStr = parameter.Type;
        }

        return $"{typeStr}{indexed()}{name()}";

        //

        string name()
        {
            if (includeNames)
            {
                return $" {parameter.Name}";
            }

            return "";
        }

        string indexed()
        {
            if (includeIndexed && parameter.Indexed)
            {
                return " indexed";
            }

            return "";
        }
    }

    /// <summary>
    /// Formats the components of a tuple parameter.
    /// </summary>
    /// <param name="components">The tuple components.</param>
    /// <param name="includeNames">Whether to include parameter names in the output.</param>
    /// <param name="includeIndexed">Whether to include indexed in the output.</param>
    /// <returns>A formatted string of the tuple components.</returns>
    internal static string FormatTupleComponents(
        IEnumerable<ContractAbiParameter>? components, bool includeNames = false, bool includeIndexed = false)
    {
        if (components == null || !components.Any())
        {
            return "";
        }

        return string.Join(",", components.Select(c => FormatParameter(c, includeNames, includeIndexed)));
    }
}