using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

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
    public T DictionaryToObject<T>(IReadOnlyDictionary<string, object?> dictionary)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        var mutableDict = new Dictionary<string, object?>(dictionary);

        return dictionaryConverter.DictionaryToObject<T>(mutableDict);
    }

    // not used
    private T TupleToObject<T>(ITuple tuple)
    {
        if (tuple == null)
        {
            throw new ArgumentNullException(nameof(tuple));
        }

        return tupleConverter.TupleToObject<T>(tuple);
    }

    // not used
    private T ArrayToObject<T>(object[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        return arrayConverter.ArrayToObject<T>(values);
    }
}
