using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.CompilerServices;
using Evoq.Blockchain;
using Evoq.Ethereum.ABI;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Converts ABI parameters to strongly-typed objects.
/// </summary>
public class AbiConverter
{
    private readonly AbiClrTypeConverter typeConverter;

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
        this.typeConverter = typeConverter ?? throw new ArgumentNullException(nameof(typeConverter));
    }

    /// <summary>
    /// Converts a dictionary of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="dictionary">The dictionary containing values.</param>
    /// <returns>An instance of T populated with values from the dictionary.</returns>
    public T DictionaryToObject<T>(IReadOnlyDictionary<string, object?> dictionary) where T : new()
    {
        if (dictionary == null)
            throw new ArgumentNullException(nameof(dictionary));

        var result = new T();
        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite)
            .ToList();

        // First try to map by attribute
        MapPropertiesByAttribute(result, properties, dictionary);

        // Then try to map by name
        MapPropertiesByName(result, properties, dictionary);

        // Finally try to map by position for any unmapped properties
        MapPropertiesByPosition(result, properties, dictionary);

        return result;
    }

    private void MapPropertiesByAttribute<T>(T target, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<AbiParameterAttribute>();
            if (attribute == null) continue;
            if (attribute.Ignore) continue;

            string paramName = attribute.Name;
            if (dictionary.TryGetValue(paramName, out var value))
            {
                SetPropertyValue(target, property, value, attribute.AbiType);
            }
            else if (attribute.Position >= 0 && dictionary.TryGetValue(attribute.Position.ToString(), out value))
            {
                SetPropertyValue(target, property, value, attribute.AbiType);
            }
        }
    }

    private void MapPropertiesByName<T>(T target, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        foreach (var property in properties)
        {
            // Skip properties that already have non-default values (likely set by attribute mapping)
            if (HasNonDefaultValue(target, property)) continue;

            // Try exact name match
            if (dictionary.TryGetValue(property.Name, out var value))
            {
                SetPropertyValue(target, property, value);
                continue;
            }

            // Try case-insensitive match
            var caseInsensitiveMatch = dictionary.Keys
                .FirstOrDefault(k => string.Equals(k, property.Name, StringComparison.OrdinalIgnoreCase));

            if (caseInsensitiveMatch != null)
            {
                SetPropertyValue(target, property, dictionary[caseInsensitiveMatch]);
            }
        }
    }

    private void MapPropertiesByPosition<T>(T target, List<PropertyInfo> properties, IReadOnlyDictionary<string, object?> dictionary)
    {
        var unmappedProperties = properties
            .Where(p => !HasNonDefaultValue(target, p))
            .ToList();

        if (!unmappedProperties.Any()) return;

        var positionMappedValues = dictionary
            .Where(kvp => int.TryParse(kvp.Key, out _))
            .OrderBy(kvp => int.Parse(kvp.Key))
            .Select(kvp => kvp.Value)
            .ToList();

        for (int i = 0; i < Math.Min(unmappedProperties.Count, positionMappedValues.Count); i++)
        {
            SetPropertyValue(target, unmappedProperties[i], positionMappedValues[i]);
        }
    }

    private bool HasNonDefaultValue<T>(T target, PropertyInfo property)
    {
        var value = property.GetValue(target);
        if (value == null) return false;

        Type propertyType = property.PropertyType;
        if (propertyType.IsValueType)
        {
            var defaultValue = Activator.CreateInstance(propertyType);
            return !value.Equals(defaultValue);
        }

        return true;
    }

    private void SetPropertyValue<T>(T target, PropertyInfo property, object? value, string? abiType = null)
    {
        if (value == null)
        {
            property.SetValue(target, null);
            return;
        }

        // Get ABI type from attribute if not provided
        if (string.IsNullOrEmpty(abiType))
        {
            var attribute = property.GetCustomAttribute<AbiParameterAttribute>();
            if (attribute != null && !string.IsNullOrEmpty(attribute.AbiType))
            {
                abiType = attribute.AbiType;
            }
        }

        Type propertyType = property.PropertyType;
        Type valueType = value.GetType();

        // Handle nullable types
        Type underlyingType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        // Handle simple type conversion
        if (typeConverter.TryConvert(value, underlyingType, out var convertedValue, abiType))
        {
            property.SetValue(target, convertedValue);
            return;
        }

        // Handle dictionary to object conversion (for nested objects)
        if (value is IDictionary<string, object?> nestedDict &&
            !underlyingType.IsPrimitive &&
            underlyingType != typeof(string))
        {
            var nestedObj = Activator.CreateInstance(underlyingType);
            var nestedProperties = underlyingType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var nestedProperty in nestedProperties)
            {
                if (nestedDict.TryGetValue(nestedProperty.Name, out var nestedValue))
                {
                    SetPropertyValue(nestedObj, nestedProperty, nestedValue);
                }
            }

            property.SetValue(target, nestedObj);
            return;
        }

        // Handle array/list conversion
        if ((value is IEnumerable enumerable && !(value is string)) &&
            (underlyingType.IsArray || (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))))
        {
            Type elementType;
            if (underlyingType.IsArray)
            {
                elementType = underlyingType.GetElementType();
            }
            else
            {
                elementType = underlyingType.GetGenericArguments()[0];
            }

            var list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));

            foreach (var item in enumerable)
            {
                if (typeConverter.TryConvert(item, elementType, out var convertedItem))
                {
                    list.Add(convertedItem);
                }
                else if (item is IDictionary<string, object?> itemDict)
                {
                    // Handle nested objects in arrays
                    var nestedObj = DictionaryToObjectInternal(elementType, itemDict);
                    list.Add(nestedObj);
                }
                // Add this case to handle arrays of dictionaries that need to be converted to arrays of objects
                else if (item is Dictionary<object, object> objDict)
                {
                    // Convert to Dictionary<string, object?>
                    var stringDict = new Dictionary<string, object?>();
                    foreach (var kvp in objDict)
                    {
                        stringDict[kvp.Key.ToString()] = kvp.Value;
                    }

                    var nestedObj = DictionaryToObjectInternal(elementType, stringDict);
                    list.Add(nestedObj);
                }
            }

            if (underlyingType.IsArray)
            {
                var array = Array.CreateInstance(elementType, list.Count);
                list.CopyTo(array, 0);
                property.SetValue(target, array);
            }
            else
            {
                property.SetValue(target, list);
            }
            return;
        }

        // Handle ValueTuple conversion
        if (underlyingType.IsValueType && underlyingType.IsGenericType &&
            underlyingType.FullName.StartsWith("System.ValueTuple`"))
        {
            if (value is IDictionary<string, object?> tupleDict)
            {
                var tupleTypes = underlyingType.GenericTypeArguments;
                var tupleValues = new object?[tupleTypes.Length];

                // Try to map by position first
                for (int i = 0; i < tupleTypes.Length; i++)
                {
                    if (tupleDict.TryGetValue(i.ToString(), out var tupleValue))
                    {
                        if (typeConverter.TryConvert(tupleValue!, tupleTypes[i], out var convertedTupleValue))
                        {
                            tupleValues[i] = convertedTupleValue;
                        }
                    }
                }

                // Create the tuple
                var tuple = Activator.CreateInstance(underlyingType, tupleValues);
                property.SetValue(target, tuple);
                return;
            }
        }

        // If we get here, try direct assignment
        try
        {
            property.SetValue(target, value);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"Cannot convert value of type {valueType} to property {property.Name} of type {propertyType}", ex);
        }
    }

    private object DictionaryToObjectInternal(Type type, IDictionary<string, object?> dictionary)
    {
        var obj = Activator.CreateInstance(type);
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanWrite);

        foreach (var property in properties)
        {
            if (dictionary.TryGetValue(property.Name, out var value))
            {
                SetPropertyValue(obj, property, value);
            }
        }

        return obj;
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
        IReadOnlyDictionary<string, object?> outputValues) where T : new()
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
        return DictionaryToObject<T>(mappedValues);
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
        return DictionaryToObject<T>(dictionary);
    }

    /// <summary>
    /// Converts a tuple of values to a strongly-typed object.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="tuple">The tuple containing values.</param>
    /// <returns>An instance of T populated with values from the tuple.</returns>
    public T TupleToObject<T>(ITuple tuple) where T : new()
    {
        if (tuple == null)
            throw new ArgumentNullException(nameof(tuple));

        // Convert the tuple to a dictionary
        var dictionary = new Dictionary<string, object?>();

        for (int i = 0; i < tuple.Length; i++)
        {
            dictionary[i.ToString()] = tuple[i];
        }

        // Convert the dictionary to the target type
        return DictionaryToObject<T>(dictionary);
    }

    /// <summary>
    /// Converts an array of values to a strongly-typed object using positional mapping.
    /// </summary>
    /// <typeparam name="T">The type to convert to.</typeparam>
    /// <param name="values">The array of values.</param>
    /// <returns>An instance of T populated with values from the array.</returns>
    public T ArrayToObject<T>(object[] values) where T : new()
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        // Convert the array to a dictionary
        var dictionary = new Dictionary<string, object?>();

        for (int i = 0; i < values.Length; i++)
        {
            dictionary[i.ToString()] = values[i];
        }

        // Convert the dictionary to the target type
        return DictionaryToObject<T>(dictionary);
    }
}
