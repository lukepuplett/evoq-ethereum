using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Converts dictionaries to strongly-typed objects.
/// </summary>
public class DictionaryObjectConverter
{
    private readonly AbiClrTypeConverter typeConverter;
    private readonly DefaultValueChecker defaultValueChecker = new DefaultValueChecker();

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

    /// <summary>
    /// Converts a dictionary of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <returns>An instance of T populated with values from the dictionary.</returns>
    public T DictionaryToObject<T>(IDictionary<string, object?> dictionary) where T : new()
    {
        return (T)DictionaryToObject(dictionary, typeof(T));
    }

    /// <summary>
    /// Converts a dictionary of values to an object of the specified type.
    /// </summary>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <param name="type">The type to convert to.</param>
    /// <returns>An instance of the specified type populated with values from the dictionary.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the dictionary is null.</exception>
    public object DictionaryToObject(IDictionary<string, object?> dictionary, Type type)
    {
        if (dictionary == null)
        {
            throw new ArgumentNullException(nameof(dictionary));
        }

        var obj = Activator.CreateInstance(type);
        var objectProperties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        // First try to map by attribute
        MapPropertiesByAttribute(obj, objectProperties, dictionary);

        // Then try to map by name
        MapPropertiesByName(obj, objectProperties, dictionary);

        // Finally try to map by position for any unmapped properties
        MapPropertiesByPosition(obj, objectProperties, dictionary);

        return obj;
    }

    //

    private void MapPropertiesByAttribute(object obj, List<PropertyInfo> properties, IDictionary<string, object?> dictionary)
    {
    }

    private void MapPropertiesByName(object obj, List<PropertyInfo> properties, IDictionary<string, object?> dictionary)
    {
        // Get properties that don't have a value yet
        var unmappedProperties = properties
            .Where(p => !defaultValueChecker.HasNonDefaultValue(obj, p))
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
                Map(obj, property, value);
            }
        }
    }

    private void MapPropertiesByPosition(object obj, List<PropertyInfo> properties, IDictionary<string, object?> dictionary)
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
            if (defaultValueChecker.HasNonDefaultValue(obj, properties[i]))
            {
                continue;
            }

            var property = properties[i];
            var value = positionMappedValues[i];

            Map(obj, property, value);
        }
    }

    private void Map(object obj, PropertyInfo property, object? value)
    {
        if (value is IDictionary<string, object?> nestedDic && IsComplexType(property.PropertyType))
        {
            var nestedObj = DictionaryToObject(nestedDic, property.PropertyType); // recursive call

            property.SetValue(obj, nestedObj);
        }
        else if (value is IDictionary<object, object> objDict && IsComplexType(property.PropertyType))
        {
            var stringDict = ConvertObjectDictionaryToStringDictionary(objDict);
            var nestedObj = DictionaryToObject(stringDict, property.PropertyType); // recursive call

            property.SetValue(obj, nestedObj);
        }
        else if (value is IEnumerable<object> enumerableValue && IsCollectionType(property.PropertyType))
        {
            Type elementType = GetElementType(property.PropertyType);

            if (IsComplexType(elementType))
            {
                var bucket = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                PopulateListFromEnumerable(bucket, enumerableValue, elementType);

                if (property.PropertyType.IsArray)
                {
                    var array = CreateArrayFromList(bucket, elementType);
                    property.SetValue(obj, array);
                }
                else
                {
                    property.SetValue(obj, bucket);
                }
            }
            else
            {
                SetPropertyValue(obj, property, value);
            }
        }
        else
        {
            SetPropertyValue(obj, property, value);
        }
    }

    private bool TryGetValue(IDictionary<string, object?> dictionary, string key, out object? value)
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

    /// <summary>
    /// Converts a dictionary with object keys to a dictionary with string keys.
    /// </summary>
    private Dictionary<string, object?> ConvertObjectDictionaryToStringDictionary(IDictionary<object, object> objDict)
    {
        var stringDict = new Dictionary<string, object?>();
        foreach (var kvp in objDict)
        {
            stringDict[kvp.Key.ToString()] = kvp.Value;
        }
        return stringDict;
    }

    /// <summary>
    /// Populates a list with converted items from an enumerable.
    /// </summary>
    private void PopulateListFromEnumerable(IList destination, IEnumerable<object> source, Type elementType)
    {
        foreach (var item in source)
        {
            if (item is IDictionary<string, object?> itemDict)
            {
                var nestedObj = DictionaryToObject(itemDict, elementType);

                destination.Add(nestedObj);
            }
            else if (item is IDictionary<object, object> itemObjDict)
            {
                var stringDict = ConvertObjectDictionaryToStringDictionary(itemObjDict);
                var nestedObj = DictionaryToObject(stringDict, elementType);

                destination.Add(nestedObj);
            }
            else if (typeConverter.TryConvert(item, elementType, out var convertedItem))
            {
                destination.Add(convertedItem);
            }
        }
    }

    /// <summary>
    /// Creates an array from a list.
    /// </summary>
    private Array CreateArrayFromList(IList list, Type elementType)
    {
        var array = Array.CreateInstance(elementType, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    /// <summary>
    /// Determines if a type is a complex type (not primitive, string, or value type).
    /// </summary>
    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && type != typeof(string) && !type.IsValueType;
    }

    /// <summary>
    /// Determines if a type is a collection type (array or generic list).
    /// </summary>
    private bool IsCollectionType(Type type)
    {
        return type.IsArray ||
               (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
    }

    /// <summary>
    /// Gets the element type of a collection type.
    /// </summary>
    private Type GetElementType(Type collectionType)
    {
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }
        else
        {
            return collectionType.GetGenericArguments()[0];
        }
    }

    private void SetPropertyValue(object obj, PropertyInfo property, object? value)
    {
        if (typeConverter.TryConvert(value, property.PropertyType, out var convertedValue))
        {
            property.SetValue(obj, convertedValue);
        }
        else
        {
            property.SetValue(obj, value);
        }
    }
}