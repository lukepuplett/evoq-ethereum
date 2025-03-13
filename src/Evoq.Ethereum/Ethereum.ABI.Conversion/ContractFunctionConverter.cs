using System;
using System.Collections.Generic;
using System.Linq;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts contract function outputs to strongly-typed objects.
/// </summary>
internal class ContractFunctionConverter
{
    private readonly DictionaryObjectConverter dictionaryConverter;

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="ContractFunctionConverter"/> class with a custom dictionary converter.
    /// </summary>
    /// <param name="dictionaryConverter">The dictionary converter to use.</param>
    public ContractFunctionConverter(DictionaryObjectConverter dictionaryConverter)
    {
        this.dictionaryConverter = dictionaryConverter ?? throw new ArgumentNullException(nameof(dictionaryConverter));
    }

    //

    /// <summary>
    /// Converts contract function output values to a strongly-typed object using the contract ABI.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="contractAbi">The contract ABI containing function definitions.</param>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="outputValues">The dictionary of output values.</param>
    /// <returns>An instance of T populated with values from the function output.</returns>
    public T ContractFunctionOutputToObject<T>(
        ContractAbi contractAbi, string functionName,
        IDictionary<string, object?> outputValues) where T : new()
    {
        if (contractAbi == null)
            throw new ArgumentNullException(nameof(contractAbi));

        if (string.IsNullOrEmpty(functionName))
            throw new ArgumentNullException(nameof(functionName));

        if (outputValues == null)
            throw new ArgumentNullException(nameof(outputValues));

        // Find the function in the ABI
        if (!contractAbi.TryGetFunction(functionName, out var function))
        {
            throw new ArgumentException($"Function '{functionName}' not found in the contract ABI", nameof(functionName));
        }

        // Map the output values to a dictionary with proper names and ABI types
        var mappedValues = new Dictionary<string, object?>();

        if (function.Outputs != null)
        {
            foreach (var output in function.Outputs)
            {
                string name = string.IsNullOrEmpty(output.Name) ? output.Type : output.Name;

                if (outputValues.TryGetValue(name, out var value))
                {
                    mappedValues[name] = value;
                }
                else if (int.TryParse(name, out int index) && index < function.Outputs.Count)
                {
                    // Try positional mapping
                    mappedValues[index.ToString()] = value;
                }
            }
        }

        // Convert the mapped values to the target type
        return dictionaryConverter.DictionaryToObject<T>(mappedValues);
    }

    /// <summary>
    /// Converts function output values to a strongly-typed object using the function signature.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="signature">The function signature.</param>
    /// <param name="outputValues">The array of output values.</param>
    /// <returns>An instance of T populated with values from the function output.</returns>
    public T FunctionOutputToObject<T>(
        FunctionSignature signature, object[] outputValues) where T : new()
    {
        if (signature == null)
            throw new ArgumentNullException(nameof(signature));

        if (outputValues == null)
            throw new ArgumentNullException(nameof(outputValues));

        // Convert the array of values to a dictionary
        var dictionary = new Dictionary<string, object?>();

        var outputTypes = signature.GetOutputParameterTypes();
        for (int i = 0; i < Math.Min(outputTypes.Length, outputValues.Length); i++)
        {
            dictionary[i.ToString()] = outputValues[i];
        }

        // Convert the dictionary to the target type
        return dictionaryConverter.DictionaryToObject<T>(dictionary);
    }
}