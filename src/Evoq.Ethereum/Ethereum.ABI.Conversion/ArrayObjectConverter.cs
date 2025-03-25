using System;
using System.Linq;
using System.Reflection;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts .NET arrays to POCOs by mapping array elements to properties in order.
/// </summary>
internal class ArrayObjectConverter
{
    private readonly AbiClrTypeConverter typeConverter;
    private readonly InstanceFactory instanceFactory = new InstanceFactory();

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayObjectConverter"/> class.
    /// </summary>
    public ArrayObjectConverter()
        : this(new AbiClrTypeConverter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrayObjectConverter"/> class with a custom type converter.
    /// </summary>
    /// <param name="typeConverter">The type converter to use.</param>
    public ArrayObjectConverter(AbiClrTypeConverter typeConverter)
    {
        this.typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    //
    /// <summary>
    /// Converts an array of values to a POCO by mapping array elements to properties in order.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="values">The array of values.</param>
    /// <returns>An instance of T populated with values from the array.</returns>
    public T ArrayToObject<T>(object[] values)
    {
        return (T)ArrayToObject(typeof(T), values);
    }

    /// <summary>
    /// Converts an array of values to a POCO by mapping array elements to properties in order.
    /// </summary>
    /// <param name="type">The type to convert to.</param>
    /// <param name="values">The array of values.</param>
    /// <returns>An instance of T populated with values from the array.</returns>
    public object ArrayToObject(Type type, object[] values)
    {
        if (values == null)
        {
            throw new ArgumentNullException(nameof(values));
        }

        var result = instanceFactory.CreateInstance(type);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToArray();

        // Map values to properties in order
        for (int i = 0; i < Math.Min(properties.Length, values.Length); i++)
        {
            var property = properties[i];
            var value = values[i];

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
                var message = $"Error setting property '{property.Name}' (index {i}) on type '{type.Name}'.\n" +
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