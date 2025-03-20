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
    internal AbiConverter(AbiClrTypeConverter typeConverter)
    {
        if (typeConverter == null)
        {
            throw new ArgumentNullException(nameof(typeConverter));
        }

        this.dictionaryConverter = new DictionaryObjectConverter(typeConverter);
        this.tupleConverter = new TupleObjectConverter(typeConverter);
        this.arrayConverter = new ArrayObjectConverter(typeConverter);
    }

    /// <summary>
    /// Converts a dictionary of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <returns>An instance of T populated with values from the dictionary.</returns>
    public T DictionaryToObject<T>(IDictionary<string, object?> dictionary)
    {
        return dictionaryConverter.DictionaryToObject<T>(dictionary);
    }

    /// <summary>
    /// Converts a tuple of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="tuple">The tuple containing values.</param>
    /// <returns>An instance of T populated with values from the tuple.</returns>
    public T TupleToObject<T>(ITuple tuple)
    {
        return tupleConverter.TupleToObject<T>(tuple);
    }

    /// <summary>
    /// Converts an array of values to a strongly-typed object using positional mapping.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="values">The array of values.</param>
    /// <returns>An instance of T populated with values from the array.</returns>
    public T ArrayToObject<T>(object[] values)
    {
        return arrayConverter.ArrayToObject<T>(values);
    }
}
