using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents an Ethereum function signature and provides methods to generate function selectors.
/// </summary>
public class FunctionSignature
{
    private readonly string _name;

    //

    /// <summary>
    /// Creates a new function signature from a name and parameter descriptor.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="descriptor">The function descriptor in parenthesis, e.g. "((string,uint256,address),bool)" or "(address,uint256)" or "(address[],uint256[])" or "(uint256[2][3])".</param>
    public FunctionSignature(string name, string descriptor)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Function name cannot be empty", nameof(name));

        descriptor = descriptor.Trim();

        if (string.IsNullOrWhiteSpace(descriptor))
            throw new ArgumentException("Function descriptor cannot be empty", nameof(descriptor));

        if (descriptor.StartsWith("(") && descriptor.EndsWith(")"))
        {
            _name = name;
            Parameters = EvmParameters.Parse(descriptor);
        }
        else
        {
            _name = name;
            Parameters = EvmParameters.Parse($"({descriptor})");
        }
    }

    private FunctionSignature(string name, IEnumerable<EvmParam> parameters)
    {
        _name = name;
        Parameters = new EvmParameters(parameters.ToList());
    }

    //

    /// <summary>
    /// Gets the parameters of the function signature.
    /// </summary>
    public EvmParameters Parameters { get; }

    //

    /// <summary>
    /// Gets the canonical signature string, e.g. "transfer(address,uint256)" or "setPerson((string,uint256,address),bool)".
    /// </summary>
    /// <returns>The canonical signature.</returns>
    public string GetCanonicalSignature()
    {
        return $"{_name}{Parameters.GetCanonicalType(includeNames: false, includeSpaces: false)}";
    }

    /// <summary>
    /// Gets the 4-byte function selector.
    /// </summary>
    /// <returns>The function selector.</returns>
    public byte[] GetSelector()
    {
        var signature = GetCanonicalSignature();
        return Crypto.KeccakHash.ComputeHash(Encoding.UTF8.GetBytes(signature)).Take(4).ToArray();
    }

    /// <summary>
    /// Gets the parameter types from the function signature.
    /// </summary>
    /// <returns>An array of parameter type strings.</returns>
    public string[] GetParameterTypes()
    {
        return Parameters.Select(p => p.AbiType).ToArray();
    }

    /// <summary>
    /// Validates the parameters of the function signature.
    /// </summary>
    /// <param name="validator">The validator to use.</param>
    /// <param name="values">The values to validate.</param>
    /// <param name="m">The message if the parameters are invalid.</param>
    /// <param name="tryEncoding">If true, the validator will try to encode the values to validate them.</param>
    /// <returns>True if the parameters are valid, false otherwise.</returns>
    public bool ValidateParameters(AbiTypeValidator validator, ITuple values, out string m, bool tryEncoding = false)
    {
        return validator.ValidateParameters(this, values, out m, tryEncoding);
    }

    //

    /// <summary>
    /// Creates a function signature from a full signature string.
    /// </summary>
    /// <param name="fullSignature">The full function signature in one of these formats:
    /// <list type="bullet">
    /// <item><description>Simple types: "transfer(address,uint256)"</description></item>
    /// <item><description>Array types: "batch(address[],uint256[])"</description></item>
    /// <item><description>Fixed arrays: "matrix(uint256[2][3])"</description></item>
    /// <item><description>Single tuple: "setPerson((string,uint256,address))"</description></item>
    /// <item><description>Mixed tuple: "setPersonActive((string,uint256,address),bool)"</description></item>
    /// <item><description>Nested tuples: "complex((uint256,(address,bool)[]))"</description></item>
    /// </list>
    /// </param>
    /// <returns>A new FunctionSignature instance.</returns>
    /// <exception cref="ArgumentException">If the signature format is invalid.</exception>
    public static FunctionSignature Parse(string fullSignature)
    {
        // Normalize the input first
        var input = fullSignature.Trim();

        // Find the start of parameters
        var startIndex = input.IndexOf('(');
        if (startIndex == -1)
            throw new ArgumentException("Invalid function signature format - missing opening parenthesis", nameof(fullSignature));

        // Extract the function name
        var name = input[..startIndex];
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Invalid function signature format - missing function name", nameof(fullSignature));

        var parameters = EvmParameters.Parse(input[startIndex..]);

        return new FunctionSignature(name, parameters);
    }

    private static string NormalizeParameterType(string paramType)
    {
        // Remove parameter names if present
        var parts = paramType.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var type = parts[0];

        // Handle tuples
        if (type.StartsWith("(") && type.EndsWith(")"))
        {
            // For tuples, we need to normalize each component
            var inner = type[1..^1];
            var components = AbiTypeValidator.ParseTupleComponents(inner)
                .Select(NormalizeParameterType);
            return $"({string.Join(",", components)})";
        }

        // Handle arrays
        if (type.Contains('['))
        {
            var baseType = type[..type.IndexOf('[')];
            var arrayPart = type[type.IndexOf('[')..];
            return NormalizeBaseType(baseType) + arrayPart;
        }

        return NormalizeBaseType(type);
    }

    private static string NormalizeBaseType(string type)
    {
        // Handle common aliases and normalize types
        return type.ToLowerInvariant() switch
        {
            "int" => "int256",
            "uint" => "uint256",
            "byte" => "bytes1",
            "fixed" => "fixed128x18",
            "ufixed" => "ufixed128x18",
            _ => type.ToLowerInvariant()
        };
    }
}