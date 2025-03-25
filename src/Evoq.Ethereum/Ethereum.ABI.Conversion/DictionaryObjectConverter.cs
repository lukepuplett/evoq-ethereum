using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts .NET dictionaries to POCOs by mapping dictionary keys to properties.
/// </summary>
internal class DictionaryObjectConverter
{
    private readonly AbiClrTypeConverter typeConverter;
    private readonly DefaultValueChecker defaultValueChecker = new DefaultValueChecker();
    private readonly InstanceFactory instanceFactory = new InstanceFactory();

    //

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryObjectConverter"/> class.
    /// </summary>
    public DictionaryObjectConverter()
        : this(new AbiClrTypeConverter())
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DictionaryObjectConverter"/> class with a custom type converter.
    /// </summary>
    /// <param name="typeConverter">The type converter to use.</param>
    public DictionaryObjectConverter(AbiClrTypeConverter typeConverter)
    {
        this.typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    //

    /// <summary>
    /// Converts a dictionary of values to a POCO.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <returns>An instance of T populated with values from the dictionary.</returns>
    public T DictionaryToObject<T>(IReadOnlyDictionary<string, object?> dictionary)
    {
        return (T)DictionaryToObject(dictionary, typeof(T));
    }

    /// <summary>
    /// Converts a dictionary of values to a POCO.
    /// </summary>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <param name="type">The type to convert to.</param>
    /// <returns>An instance of the specified type populated with values from the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dictionary is null.</exception>
    public object DictionaryToObject(IReadOnlyDictionary<string, object?> dictionary, Type type)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        // Create an instance of the type using our InstanceFactory
        var obj = instanceFactory.CreateInstance(type);

        // Get properties that can be written to
        var objectProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        // Map properties by attribute
        MapPropertiesByAttribute(obj, objectProperties, dictionary);

        // Map properties by name
        MapPropertiesByName(obj, objectProperties, dictionary);

        // Map properties by position
        MapPropertiesByPosition(obj, objectProperties, dictionary);

        return obj;
    }

    //

    private void MapPropertiesByAttribute(
        object poco, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        // Get properties that have the AbiParameterAttribute
        var attributeMappedProperties = properties
            .Where(p => !defaultValueChecker.HasNonDefaultValue(poco, p))
            .Select(p => new
            {
                Property = p,
                Attribute = p.GetCustomAttribute<AbiParameterAttribute>()
            })
            .Where(x => x.Attribute != null && !x.Attribute.Ignore)
            .ToList();

        if (!attributeMappedProperties.Any())
        {
            return;
        }

        foreach (var propInfo in attributeMappedProperties)
        {
            var property = propInfo.Property;
            var attribute = propInfo.Attribute;

            if (!string.IsNullOrEmpty(attribute.Name) && dictionary.TryGetValue(attribute.Name, out var v))
            {
                MapValueToProperty(poco, property, v, attribute.AbiType);
            }
        }
    }

    private void MapPropertiesByName(
        object poco, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        // Get properties that don't have a value yet
        var unmappedProperties = properties
            .Where(p => !defaultValueChecker.HasNonDefaultValue(poco, p))
            .ToList();

        if (!unmappedProperties.Any())
        {
            return;
        }

        foreach (var property in unmappedProperties)
        {
            // Try to get value by property name (case-sensitive first, then case-insensitive)
            if (TryGetValue(dictionary, property.Name, out var value))
            {
                MapValueToProperty(poco, property, value);
            }
        }
    }

    private void MapPropertiesByPosition(
        object poco, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        // Use the key as a positional index only if all keys look like positional indices
        List<object?> positionMappedValues;
        if (dictionary.All(kvp => int.TryParse(kvp.Key, out _)))
        {
            positionMappedValues = dictionary
                .OrderBy(kvp => int.Parse(kvp.Key))
                .Select(kvp => kvp.Value)
                .ToList();
        }
        else
        {
            positionMappedValues = dictionary
                .Select(kvp => kvp.Value)
                .ToList();
        }

        var propCount = Math.Min(properties.Count, positionMappedValues.Count);

        for (int i = 0; i < propCount; i++)
        {
            if (defaultValueChecker.HasNonDefaultValue(poco, properties[i]))
            {
                continue;
            }

            var property = properties[i];
            var value = positionMappedValues[i];

            MapValueToProperty(poco, property, value);
        }
    }

    //

    private void MapValueToProperty(object poco, PropertyInfo property, object? value, string? abiType = null)
    {
        if (value?.GetType() == typeof(object[]))
        {
            throw new NotImplementedException($"{value.GetType().Name} is not supported yet. AbiType: {abiType}");
        }

        var attribute = property.GetCustomAttribute<AbiParameterAttribute>();

        if (attribute != null && attribute.Ignore)
        {
            return;
        }

        if (value == null)
        {
            property.SetValue(poco, null);
            return;
        }

        // If we have an ABI type hint, try to convert using it
        if (!string.IsNullOrEmpty(abiType) &&
            this.typeConverter.TryConvert(value, property.PropertyType, out var convertedValue, abiType))
        {
            property.SetValue(poco, convertedValue);
            return;
        }

        if (CollectionTypeDetector.IsDictionaryValue(value, out var dic) &&
            IsComplexType(property.PropertyType))
        {
            var nestedObj = DictionaryToObject(dic!, property.PropertyType); // recursive call
            property.SetValue(poco, nestedObj);
        }
        else if (CollectionTypeDetector.IsCollectionValue(value) &&
                 CollectionTypeDetector.IsCollectionType(property.PropertyType))
        {
            MapCollectionValueToProperty(poco, property, value);
        }
        else
        {
            SetSinglePropertyValue(poco, property, value);
        }
    }

    //

    private void MapCollectionValueToProperty(object poco, PropertyInfo property, object? value)
    {
        // Handle collection values
        Type elementType = CollectionTypeDetector.GetElementType(property.PropertyType);

        if (value is IEnumerable<object> enumerableObjects)
        {
            IList convertedItems = GetConvertedItemsList(enumerableObjects, elementType);

            SetCollectionPropertyValue(poco, property, elementType, convertedItems);
        }
        else if (value is Array arrayOfObjects)
        {
            IList convertedItems = GetConvertedItemsList(arrayOfObjects, elementType);

            SetCollectionPropertyValue(poco, property, elementType, convertedItems);
        }
        else if (value is IEnumerable enumerable)
        {
            IList convertedItems = GetConvertedItemsList(enumerable, elementType);

            SetCollectionPropertyValue(poco, property, elementType, convertedItems);
        }
        else
        {
            throw new ConversionException(
                $"Cannot set property '{property.Name}' on type '{poco.GetType().Name}'.\n" +
                $"Value type: {(value?.GetType().Name ?? "<null>")}\n" +
                $"Target type: {property.PropertyType.Name}\n" +
                $"Value: {FormatValueForDisplay(value)}");
        }
    }

    private IList GetConvertedItemsList(IEnumerable items, Type elementType)
    {
        IList destination = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

        foreach (var item in items)
        {
            if (CollectionTypeDetector.IsDictionaryValue(item, out var dic))
            {
                var nestedObj = this.DictionaryToObject(dic!, elementType);

                destination.Add(nestedObj);
            }
            else if (this.typeConverter.TryConvert(item, elementType, out var convertedItem))
            {
                destination.Add(convertedItem);
            }
            else
            {
                throw new ConversionException(
                    $"Cannot convert item to type '{elementType.Name}'.\n" +
                    $"Value type: {(item?.GetType().Name ?? "<null>")}\n" +
                    $"Value: {FormatValueForDisplay(item)}");
            }
        }
        return destination;
    }

    private void SetCollectionPropertyValue(object poco, PropertyInfo property, Type elementType, IList convertedItems)
    {
        if (property.PropertyType.IsArray)
        {
            var array = CreateArrayFromList(convertedItems, elementType);
            property.SetValue(poco, array);
        }
        else
        {
            property.SetValue(poco, convertedItems);
        }
    }

    private Array CreateArrayFromList(IList list, Type elementType)
    {
        var array = Array.CreateInstance(elementType, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    //

    private bool TryGetValue(IReadOnlyDictionary<string, object?> dictionary, string key, out object? value)
    {
        // Try exact match first
        if (dictionary.TryGetValue(key, out value))
        {
            return true;
        }

        // Try case-insensitive match
        var caseInsensitiveMatch = dictionary.Keys
            .FirstOrDefault(k => string.Equals(k, key, StringComparison.OrdinalIgnoreCase));

        if (caseInsensitiveMatch != null)
        {
            value = dictionary[caseInsensitiveMatch];
            return true;
        }

        value = null;
        return false;
    }

    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && type != typeof(string) && !type.IsValueType;
    }

    private void SetSinglePropertyValue(object poco, PropertyInfo property, object? value)
    {
        try
        {
            if (this.typeConverter.TryConvert(value, property.PropertyType, out var convertedValue))
            {
                property.SetValue(poco, convertedValue);
            }
            else
            {
                // Don't try to set the property if conversion failed
                throw new ConversionException(
                    $"Cannot set property '{property.Name}' on type '{poco.GetType().Name}'.\n" +
                    $"Value type: {(value?.GetType().Name ?? "<null>")}\n" +
                    $"Target type: {property.PropertyType.Name}\n" +
                    $"Value: {FormatValueForDisplay(value)}");
            }
        }
        catch (Exception ex) when (ex is not ConversionException)
        {
            // Create a more detailed exception with context about the conversion
            var message = $"Error setting property '{property.Name}' on type '{poco.GetType().Name}'.\n" +
                          $"Value type: {(value?.GetType().Name ?? "<null>")}\n" +
                          $"Target type: {property.PropertyType.Name}\n" +
                          $"Value: {FormatValueForDisplay(value)}";

            throw new ConversionException(message, ex);
        }
    }

    // Helper method to format values for display in error messages
    internal string FormatValueForDisplay(object? value)
    {
        if (value == null)
            return "<null>";

        if (value is string strValue)
        {
            if (string.IsNullOrEmpty(strValue))
                return "<empty string>";

            if (string.IsNullOrWhiteSpace(strValue))
                return $"<whitespace string: '{strValue}'>";

            if (strValue.Length > 100)
                return $"\"{strValue.Substring(0, 97)}...\" (length: {strValue.Length})";

            return $"\"{strValue}\"";
        }

        if (value is byte[] bytes)
        {
            return $"byte[{bytes.Length}]: 0x{BitConverter.ToString(bytes).Replace("-", "")}";
        }

        if (value is Array array)
        {
            return $"{value.GetType().Name.TrimEnd('[', ']')}[{array.Length}]";
        }

        if (CollectionTypeDetector.IsDictionaryValue(value, out var dic))
        {
            return $"{{{string.Join(", ", dic!.Select(kvp => $"{kvp.Key}: {FormatValueForDisplay(kvp.Value)}"))}}}";
        }

        return value.ToString() ?? "<toString returned null>";
    }
}