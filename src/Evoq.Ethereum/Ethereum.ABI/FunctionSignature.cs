using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Evoq.Ethereum.Crypto;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents an Ethereum function signature and provides methods to generate function selectors.
/// </summary>
public class FunctionSignature
{
    /// <summary>
    /// Creates a new function signature from a name and parameter descriptor.
    /// </summary>
    /// <param name="name">The function name.</param>
    /// <param name="inputsSignature">The function descriptor in parenthesis, e.g. "((string,uint256,address),bool)" or "(address,uint256)" or "(address[],uint256[])" or "(uint256[2][3])".</param>
    /// <param name="outputsSignature">The function descriptor in parenthesis, e.g. "((string,uint256,address),bool)" or "(address,uint256)" or "(address[],uint256[])" or "(uint256[2][3])".</param>
    public FunctionSignature(string name, string inputsSignature, string outputsSignature = "")
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Function name cannot be empty", nameof(name));
        }

        inputsSignature = inputsSignature.Trim();
        outputsSignature = outputsSignature.Trim();

        if (string.IsNullOrWhiteSpace(inputsSignature))
        {
            throw new ArgumentException("Function descriptor cannot be empty", nameof(inputsSignature));
        }

        if (inputsSignature.StartsWith("(") && inputsSignature.EndsWith(")"))
        {
            this.Name = name;
            this.Inputs = AbiParameters.Parse(inputsSignature);
        }
        else
        {
            this.Name = name;
            this.Inputs = AbiParameters.Parse($"({inputsSignature})");
        }

        if (outputsSignature.StartsWith("(") && outputsSignature.EndsWith(")"))
        {
            this.Outputs = AbiParameters.Parse(outputsSignature);
        }
        else if (!string.IsNullOrEmpty(outputsSignature))
        {
            this.Outputs = AbiParameters.Parse($"({outputsSignature})");
        }
    }

    private FunctionSignature(string name, IEnumerable<AbiParam> inputs, IEnumerable<AbiParam> outputs)
    {
        this.Name = name;
        this.Inputs = new AbiParameters(inputs.ToList());
        this.Outputs = new AbiParameters(outputs.ToList());
    }

    //

    /// <summary>
    /// Gets the name of the function.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets the parameters of the function signature.
    /// </summary>
    public AbiParameters Inputs { get; }

    /// <summary>
    /// Gets the parameters of the function signature.
    /// </summary>
    public AbiParameters? Outputs { get; }

    //

    /// <summary>
    /// Encodes the full function signature, including the name and parameter values.
    /// </summary>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="values">The values to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public byte[] AbiEncodeCallValues(IAbiEncoder encoder, IReadOnlyList<object?> values)
    {
        var result = encoder.EncodeParameters(this.Inputs, values);

        var selectorBytes = this.GetSelector();
        var resultBytes = result.GetBytes();

        var fullBytes = new byte[selectorBytes.Length + resultBytes.Length];
        selectorBytes.CopyTo(fullBytes, 0);
        resultBytes.CopyTo(fullBytes, selectorBytes.Length);

        return fullBytes;
    }

    /// <summary>
    /// Gets the canonical signature string, e.g. "transfer(address,uint256)" or "setPerson((string,uint256,address),bool)".
    /// </summary>
    /// <returns>The canonical signature.</returns>
    public string GetCanonicalInputsSignature()
    {
        return $"{this.Name}{this.Inputs.GetCanonicalType(includeNames: false, includeSpaces: false)}";
    }

    /// <summary>
    /// Gets the canonical outputs signature string, e.g. "((string,uint256,address),bool)" or "(address,uint256)" or "(address[],uint256[])" or "(uint256[2][3])".
    /// </summary>
    /// <returns>The canonical outputs signature.</returns>
    public string GetCanonicalOutputsSignature()
    {
        if (this.Outputs == null || this.Outputs.Count == 0)
        {
            return "()";
        }

        return this.Outputs.GetCanonicalType(includeNames: false, includeSpaces: false);
    }

    /// <summary>
    /// Gets the 4-byte function selector.
    /// </summary>
    /// <returns>The function selector.</returns>
    public byte[] GetSelector()
    {
        var signature = this.GetCanonicalInputsSignature();

        return KeccakHash.ComputeHash(Encoding.UTF8.GetBytes(signature)).Take(4).ToArray();
    }

    /// <summary>
    /// Gets the parameter types from the function signature.
    /// </summary>
    /// <returns>An array of parameter type strings.</returns>
    public string[] GetInputParameterTypes()
    {
        return this.Inputs.Select(p => p.AbiType).ToArray();
    }

    /// <summary>
    /// Gets the output parameter types from the function signature.
    /// </summary>
    /// <returns>An array of parameter type strings.</returns>
    public string[] GetOutputParameterTypes()
    {
        return this.Outputs.Select(p => p.AbiType).ToArray();
    }

    /// <summary>
    /// Validates the parameters of the function signature.
    /// </summary>
    /// <param name="validator">The validator to use.</param>
    /// <param name="values">The values to validate.</param>
    /// <param name="m">The message if the parameters are invalid.</param>
    /// <param name="tryEncoding">If true, the validator will try to encode the values to validate them.</param>
    /// <returns>True if the parameters are valid, false otherwise.</returns>
    public bool ValidateParameters(AbiTypeValidator validator, IReadOnlyList<object?> values, out string m, bool tryEncoding = false)
    {
        return validator.ValidateParameters(this, values, out m, tryEncoding);
    }

    //

    /// <summary>
    /// Returns the canonical signature string.
    /// </summary>
    /// <returns>The canonical signature.</returns>
    public override string ToString()
    {
        return this.GetCanonicalInputsSignature();
    }

    //

    /// <summary>
    /// Creates a function signature from a full signature string.
    /// </summary>
    /// <param name="fullSignature">The full function signature in one of these formats:
    /// <list type="bullet">
    /// <item><description>Simple types: "transfer(address,uint256) returns (bool)"</description></item>
    /// <item><description>Array types: "batch(address[],uint256[])"</description></item>
    /// <item><description>Fixed arrays: "matrix(uint256[2][3])"</description></item>
    /// <item><description>Single tuple: "setPerson((string,uint256,address))"</description></item>
    /// <item><description>Mixed tuple: "setPersonActive((string,uint256,address),bool)"</description></item>
    /// <item><description>Nested tuples: "complex((uint256,(address,bool)[]))"</description></item>
    /// <item><description>No parameters: "getEverything()"</description></item>
    /// <item><description>No parameters with returns: "getEverything() returns (uint256,(bool,string)[])"</description></item>
    /// </list>
    /// </param>
    /// <returns>A new FunctionSignature instance.</returns>
    /// <exception cref="ArgumentException">If the signature format is invalid.</exception>
    public static FunctionSignature Parse(string fullSignature)
    {
        // Split the signature into inputs and outputs
        var parts = fullSignature.Split(" returns ", StringSplitOptions.None);

        var nameAndInputs = parts[0].Trim();
        var outputs = parts.Length > 1 ? parts[1].Trim() : string.Empty;

        // Find the start of parameters
        var startIndex = nameAndInputs.IndexOf('(');
        if (startIndex == -1)
        {
            throw new ArgumentException(
                "Invalid function signature format. Missing opening parenthesis", nameof(fullSignature));
        }

        // Extract the function name
        var name = nameAndInputs[..startIndex];
        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException(
                "Invalid function signature format. Missing function name", nameof(fullSignature));
        }

        var ins = AbiParameters.Parse(nameAndInputs[startIndex..]);
        var outs = AbiParameters.Parse(outputs);

        return new FunctionSignature(name, ins, outs);
    }

    //

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

/// <summary>
/// Extension methods for <see cref="FunctionSignature"/>.
/// </summary>
public static class FunctionSignatureExtensions
{
    /// <summary>
    /// Encodes a single parameter for a function signature.
    /// </summary>
    /// <typeparam name="T">The type of the value to encode. Must be a value type or string.</typeparam>
    /// <param name="signature">The function signature.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="value">The value to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public static byte[] EncodeFullSignature<T>(
        this FunctionSignature signature, IAbiEncoder encoder, T value)
        where T : struct, IConvertible
    {
        return signature.AbiEncodeCallValues(encoder, new object[] { value });
    }

    /// <summary>
    /// Encodes a single string parameter for a function signature.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="value">The string value to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public static byte[] EncodeFullSignature(
        this FunctionSignature signature, IAbiEncoder encoder, string value)
    {
        return signature.AbiEncodeCallValues(encoder, new object[] { value });
    }

    /// <summary>
    /// Encodes a BigInteger parameter for a function signature.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="value">The BigInteger value to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public static byte[] EncodeFullSignature(
        this FunctionSignature signature, IAbiEncoder encoder, BigInteger value)
    {
        return signature.AbiEncodeCallValues(encoder, new object[] { value });
    }

    /// <summary>
    /// Encodes a byte array parameter for a function signature.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="value">The byte array to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public static byte[] EncodeFullSignature(
        this FunctionSignature signature, IAbiEncoder encoder, byte[] value)
    {
        return signature.AbiEncodeCallValues(encoder, new object[] { value });
    }

    /// <summary>
    /// Encodes an array parameter for a function signature.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="value">The array to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public static byte[] EncodeFullSignature(
        this FunctionSignature signature, IAbiEncoder encoder, Array value)
    {
        return signature.AbiEncodeCallValues(encoder, new object[] { value });
    }

    /// <summary>
    /// Encodes a tuple parameter for a function signature.
    /// </summary>
    /// <param name="signature">The function signature.</param>
    /// <param name="encoder">The encoder to use.</param>
    /// <param name="values">The tuple to encode.</param>
    /// <returns>The encoded full function signature.</returns>
    public static byte[] EncodeFullSignature(
        this FunctionSignature signature, IAbiEncoder encoder, ITuple values)
    {
        return signature.AbiEncodeCallValues(encoder, values.GetElements().ToList());
    }
}