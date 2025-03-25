using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts .NET tuples to POCOs by mapping tuple elements to properties in order.
/// </summary>
internal class TupleObjectConverter
{
    private readonly AbiClrTypeConverter typeConverter;
    private readonly InstanceFactory instanceFactory = new InstanceFactory();

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
    public T TupleToObject<T>(ITuple tuple)
    {
        return (T)TupleToObject(typeof(T), tuple);
    }

    /// <summary>
    /// Converts a tuple of values to a strongly-typed object by mapping tuple elements to properties in order.
    /// </summary>
    /// <param name="type">The type to convert to.</param>
    /// <param name="tuple">The tuple containing values.</param>
    /// <returns>An instance of T populated with values from the tuple.</returns>
    public object TupleToObject(Type type, ITuple tuple)
    {
        if (tuple == null)
            throw new ArgumentNullException(nameof(tuple));

        var result = instanceFactory.CreateInstance(type);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        // Map tuple elements to properties in order
        for (int i = 0; i < Math.Min(properties.Length, tuple.Length); i++)
        {
            var property = properties[i];
            var value = tuple[i];

            try
            {
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
                    throw new ConversionException(
                        $"Cannot set property '{property.Name}' on type '{type.Name}'.\n" +
                        $"Value type: {(value?.GetType().Name ?? "null")}\n" +
                        $"Target type: {property.PropertyType.Name}\n" +
                        $"Value: {FormatValueForDisplay(value)}");
                }
            }
            catch (Exception ex) when (ex is not ConversionException)
            {
                // Create a more detailed exception with context about the conversion
                var message = $"Error setting property '{property.Name}' (tuple item {i}) on type '{type.Name}'.\n" +
                              $"Value type: {(value?.GetType().Name ?? "null")}\n" +
                              $"Target type: {property.PropertyType.Name}\n" +
                              $"Value: {FormatValueForDisplay(value)}";

                throw new ArgumentException(message, ex);
            }
        }

        return result;
    }

    // Helper method to format values for display in error messages
    private string FormatValueForDisplay(object? value)
    {
        if (value == null)
            return "null";

        if (value is byte[] bytes)
            return $"byte[{bytes.Length}]: 0x{BitConverter.ToString(bytes).Replace("-", "")}";

        if (value is Array array)
            return $"{value.GetType().Name}[{array.Length}]";

        if (value is string str && str.Length > 100)
            return $"\"{str.Substring(0, 97)}...\" (length: {str.Length})";

        return value.ToString() ?? "null";
    }
}