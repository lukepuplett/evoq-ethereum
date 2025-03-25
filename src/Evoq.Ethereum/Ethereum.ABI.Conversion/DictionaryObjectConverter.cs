using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Org.BouncyCastle.Bcpg;

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
        object obj, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        // Get properties that have the AbiParameterAttribute
        var attributeMappedProperties = properties
            .Where(p => !defaultValueChecker.HasNonDefaultValue(obj, p))
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

            // Try to map by attribute name
            if (!string.IsNullOrEmpty(attribute.Name) && dictionary.TryGetValue(attribute.Name, out var valueByName))
            {
                MapValueToProperty(obj, property, valueByName, attribute.AbiType);
                continue;
            }

            // Try to map by attribute position
            if (attribute.Position >= 0 && dictionary.Values.ElementAt(attribute.Position) is var valueByPosition)
            {
                MapValueToProperty(obj, property, valueByPosition, attribute.AbiType);
            }
        }
    }

    private void MapPropertiesByName(
        object obj, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
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
                MapValueToProperty(obj, property, value);
            }
        }
    }

    private void MapPropertiesByPosition(
        object obj, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
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

            MapValueToProperty(obj, property, value);
        }
    }

    private void MapValueToProperty(object obj, PropertyInfo property, object? value, string? abiType = null)
    {
        var attribute = property.GetCustomAttribute<AbiParameterAttribute>();

        if (attribute != null && attribute.Ignore)
        {
            return;
        }

        if (value == null)
        {
            property.SetValue(obj, null);
            return;
        }

        // If we have an ABI type hint, try to convert using it
        if (!string.IsNullOrEmpty(abiType) &&
            this.typeConverter.TryConvert(value, property.PropertyType, out var convertedValue, abiType))
        {
            property.SetValue(obj, convertedValue);
            return;
        }

        if (IsDickie(value, out var dickie) && IsComplexType(property.PropertyType))
        {
            var nestedObj = DictionaryToObject(dickie!, property.PropertyType); // recursive call
            property.SetValue(obj, nestedObj);
        }
        else if (CollectionTypeDetector.IsCollectionValue(value) &&
                 CollectionTypeDetector.IsCollectionType(property.PropertyType))
        {
            // Handle collection values
            Type elementType = CollectionTypeDetector.GetElementType(property.PropertyType);

            if (value is IEnumerable<object> enumerableValue)
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
            else if (value is Array sourceArray)
            {
                var bucket = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                foreach (var item in sourceArray)
                {
                    if (this.typeConverter.TryConvert(item, elementType, out var convertedItem))
                    {
                        bucket.Add(convertedItem);
                    }
                }

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
                // For other IEnumerable types
                var enumerable = (IEnumerable)value;
                var bucket = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

                foreach (var item in enumerable)
                {
                    if (this.typeConverter.TryConvert(item, elementType, out var convertedItem))
                    {
                        bucket.Add(convertedItem);
                    }
                }

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
        }
        else
        {
            SetPropertyValue(obj, property, value);
        }
    }

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

    private void PopulateListFromEnumerable(IList destination, IEnumerable<object> source, Type elementType)
    {
        foreach (var item in source)
        {
            if (IsDickie(item, out var dickie))
            {
                var nestedObj = DictionaryToObject(dickie!, elementType);

                destination.Add(nestedObj);
            }
            else if (this.typeConverter.TryConvert(item, elementType, out var convertedItem))
            {
                destination.Add(convertedItem);
            }
        }
    }

    private Array CreateArrayFromList(IList list, Type elementType)
    {
        var array = Array.CreateInstance(elementType, list.Count);
        list.CopyTo(array, 0);
        return array;
    }

    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && type != typeof(string) && !type.IsValueType;
    }

    private void SetPropertyValue(object obj, PropertyInfo property, object? value)
    {
        try
        {
            if (this.typeConverter.TryConvert(value, property.PropertyType, out var convertedValue))
            {
                property.SetValue(obj, convertedValue);
            }
            else
            {
                // Don't try to set the property if conversion failed
                throw new ConversionException(
                    $"Cannot set property '{property.Name}' on type '{obj.GetType().Name}'.\n" +
                    $"Value type: {(value?.GetType().Name ?? "<null>")}\n" +
                    $"Target type: {property.PropertyType.Name}\n" +
                    $"Value: {FormatValueForDisplay(value)}");
            }
        }
        catch (Exception ex) when (ex is not ConversionException)
        {
            // Create a more detailed exception with context about the conversion
            var message = $"Error setting property '{property.Name}' on type '{obj.GetType().Name}'.\n" +
                          $"Value type: {(value?.GetType().Name ?? "<null>")}\n" +
                          $"Target type: {property.PropertyType.Name}\n" +
                          $"Value: {FormatValueForDisplay(value)}";

            throw new ConversionException(message, ex);
        }
    }

    // Helper method to format values for display in error messages
    private string FormatValueForDisplay(object? value)
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
            return $"byte[{bytes.Length}]: 0x{BitConverter.ToString(bytes).Replace("-", "")}";

        if (value is Array array)
            return $"{value.GetType().Name}[{array.Length}]";

        return value.ToString() ?? "<toString returned null>";
    }

    //

    private static bool IsDickie(object obj, out IReadOnlyDictionary<string, object?>? dickie)
    {
        if (obj is Dictionary<string, object> stringObjDic)
        {
            dickie = stringObjDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is Dictionary<string, object?> stringObjMaybeDic)
        {
            dickie = stringObjMaybeDic;
            return true;
        }
        else if (obj is Dictionary<object, object> objObjDic)
        {
            dickie = objObjDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is Dictionary<object, object?> objObjMaybeDic)
        {
            dickie = objObjMaybeDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        // Read-only dictionaries
        else if (obj is IReadOnlyDictionary<string, object> stringObjReadDic)
        {
            dickie = stringObjReadDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IReadOnlyDictionary<string, object?> stringObjMaybeReadDic)
        {
            dickie = stringObjMaybeReadDic;
            return true;
        }
        else if (obj is IReadOnlyDictionary<object, object> objObjReadDic)
        {
            dickie = objObjReadDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IReadOnlyDictionary<object, object?> objObjMaybeReadDic)
        {
            dickie = objObjMaybeReadDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        // IDictionaries
        else if (obj is IDictionary<string, object> stringObjDickish)
        {
            dickie = stringObjDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IDictionary<string, object?> stringObjMaybeDickish)
        {
            dickie = stringObjMaybeDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IDictionary<object, object> objObjDickish)
        {
            dickie = objObjDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IDictionary<object, object?> objObjMaybeDickish)
        {
            dickie = objObjMaybeDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }

        dickie = null;
        return false;
    }

    private static Dictionary<string, object?> ConvertObjectDictionaryToStringDictionary(IReadOnlyDictionary<object, object> objDict)
    {
        var stringDict = new Dictionary<string, object?>();
        foreach (var kvp in objDict)
        {
            stringDict[kvp.Key.ToString()] = kvp.Value;
        }
        return stringDict;
    }
}