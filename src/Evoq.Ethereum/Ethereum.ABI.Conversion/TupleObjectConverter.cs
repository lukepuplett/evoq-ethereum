using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts tuples to strongly-typed objects by mapping tuple elements to properties in order.
/// </summary>
internal class TupleObjectConverter
{
    private readonly AbiClrTypeConverter typeConverter;

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleObjectConverter"/> class.
    /// </summary>
    public TupleObjectConverter()
        : this(new AbiClrTypeConverter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TupleObjectConverter"/> class with a custom type converter.
    /// </summary>
    /// <param name="typeConverter">The type converter to use.</param>
    public TupleObjectConverter(AbiClrTypeConverter typeConverter)
    {
        this.typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    //

    /// <summary>
    /// Converts a tuple of values to a strongly-typed object by mapping tuple elements to properties in order.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="tuple">The tuple containing values.</param>
    /// <returns>An instance of T populated with values from the tuple.</returns>
    public T TupleToObject<T>(ITuple tuple) where T : new()
    {
        if (tuple == null)
            throw new ArgumentNullException(nameof(tuple));

        var result = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        // Map tuple elements to properties in order
        for (int i = 0; i < Math.Min(properties.Length, tuple.Length); i++)
        {
            var property = properties[i];
            var value = tuple[i];

            if (value == null)
            {
                property.SetValue(result, null);
                continue;
            }

            Type propertyType = property.PropertyType;
            Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

            // Try to convert the value to the property type
            if (typeConverter.TryConvert(value, underlyingType, out var convertedValue))
            {
                property.SetValue(result, convertedValue);
            }
            else
            {
                throw new InvalidOperationException(
                    $"Cannot convert value of type {value.GetType()} to property {property.Name} of type {propertyType}");
            }
        }

        return result;
    }
}