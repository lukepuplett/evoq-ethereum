using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Evoq.Blockchain;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts ABI parameters to strongly-typed objects.
/// </summary>
public class AbiConverter
{
    private readonly DictionaryObjectConverter dictionaryConverter;
    private readonly ContractFunctionConverter contractFunctionConverter;
    private readonly TupleObjectConverter tupleConverter;
    private readonly ArrayObjectConverter arrayConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiConverter"/> class.
    /// </summary>
    public AbiConverter()
        : this(new AbiClrTypeConverter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiConverter"/> class with a custom type converter.
    /// </summary>
    /// <param name="typeConverter">The type converter to use.</param>
    public AbiConverter(AbiClrTypeConverter typeConverter)
    {
        if (typeConverter == null)
        {
            throw new ArgumentNullException(nameof(typeConverter));
        }

        this.dictionaryConverter = new DictionaryObjectConverter(typeConverter);
        this.contractFunctionConverter = new ContractFunctionConverter(this.dictionaryConverter);
        this.tupleConverter = new TupleObjectConverter(typeConverter);
        this.arrayConverter = new ArrayObjectConverter(typeConverter);
    }

    /// <summary>
    /// Converts a dictionary of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <returns>An instance of T populated with values from the dictionary.</returns>
    public T DictionaryToObject<T>(IDictionary<string, object?> dictionary) where T : new()
    {
        return dictionaryConverter.DictionaryToObject<T>(dictionary);
    }

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
        return contractFunctionConverter.ContractFunctionOutputToObject<T>(contractAbi, functionName, outputValues);
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
        return contractFunctionConverter.FunctionOutputToObject<T>(signature, outputValues);
    }

    /// <summary>
    /// Converts a tuple of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="tuple">The tuple containing values.</param>
    /// <returns>An instance of T populated with values from the tuple.</returns>
    public T TupleToObject<T>(ITuple tuple) where T : new()
    {
        return tupleConverter.TupleToObject<T>(tuple);
    }

    /// <summary>
    /// Converts an array of values to a strongly-typed object using positional mapping.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="values">The array of values.</param>
    /// <returns>An instance of T populated with values from the array.</returns>
    public T ArrayToObject<T>(object[] values) where T : new()
    {
        return arrayConverter.ArrayToObject<T>(values);
    }
}
