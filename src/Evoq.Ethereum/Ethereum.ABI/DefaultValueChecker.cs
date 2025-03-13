using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Provides methods to check if values are default or non-default for their types.
/// </summary>
internal class DefaultValueChecker
{
    /// <summary>
    /// Determines if a property of an object has a non-default value.
    /// </summary>
    /// <param name="obj">The object containing the property.</param>
    /// <param name="property">The property to check.</param>
    /// <returns>True if the property has a non-default value; otherwise, false.</returns>
    public bool HasNonDefaultValue(object obj, PropertyInfo property)
    {
        var value = property.GetValue(obj);

        return HasNonDefaultValue(value, property.PropertyType);
    }

    /// <summary>
    /// Determines if a value is non-default for its type.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <param name="type">The type of the value.</param>
    /// <returns>True if the value is non-default; otherwise, false.</returns>
    public bool HasNonDefaultValue(object? value, Type type)
    {
        if (value == null)
        {
            return false; // Null is considered the default for reference types
        }

        // Handle nullable value types
        Type underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            // For nullable types, any non-null value is considered non-default
            return true;
        }

        if (type.IsValueType)
        {
            // For value types, compare with the default value of that type
            object defaultValue = Activator.CreateInstance(type);

            return !value.Equals(defaultValue);
        }
        else
        {
            // For reference types like string, arrays, or custom classes

            if (type == typeof(string))
            {
                // For strings, empty string might be considered default
                return !string.IsNullOrEmpty((string)value);
            }

            if (IsCollectionType(type))
            {
                // For collections, we need to check if they have any non-default elements
                var enumerable = (IEnumerable)value;

                // First check if the collection is empty
                if (!enumerable.Cast<object>().Any())
                {
                    return false; // Empty collection is default
                }

                // If it has elements, check if any of them are non-default
                foreach (var item in enumerable)
                {
                    if (item == null)
                    {
                        continue; // Null items are default
                    }

                    Type itemType = item.GetType();
                    if (HasNonDefaultValue(item, itemType))
                    {
                        return true; // Found a non-default item
                    }
                }

                // All items are default
                return false;
            }

            // For complex types, recursively check all properties
            if (IsComplexType(type))
            {
                return HasNonDefaultComplexValue(value);
            }

            // For other reference types, the mere existence of an instance means it's non-default
            return true;
        }
    }

    /// <summary>
    /// Recursively checks if a complex object has any non-default property values.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <returns>True if any property has a non-default value; otherwise, false.</returns>
    public bool HasNonDefaultComplexValue(object obj)
    {
        Type type = obj.GetType();
        var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead);

        foreach (var property in properties)
        {
            var value = property.GetValue(obj);
            if (HasNonDefaultValue(value, property.PropertyType))
            {
                return true;
            }
        }

        // If we get here, all properties have default values
        return false;
    }

    /// <summary>
    /// Determines if a type is a complex type (not primitive, string, or value type).
    /// </summary>
    private bool IsComplexType(Type type)
    {
        return !type.IsPrimitive && type != typeof(string) && !type.IsValueType;
    }

    /// <summary>
    /// Determines if a type is a collection type.
    /// </summary>
    private bool IsCollectionType(Type type)
    {
        if (type == null)
            return false;

        // Check for array
        if (type.IsArray)
            return true;

        // Check for common collection types
        if (type.IsGenericType)
        {
            Type genericTypeDef = type.GetGenericTypeDefinition();

            // Check for common generic collections
            if (genericTypeDef == typeof(List<>) ||
                genericTypeDef == typeof(HashSet<>) ||
                genericTypeDef == typeof(Dictionary<,>) ||
                genericTypeDef == typeof(Queue<>) ||
                genericTypeDef == typeof(Stack<>))
            {
                return true;
            }
        }

        // Check for collection interfaces
        foreach (Type interfaceType in type.GetInterfaces())
        {
            if (interfaceType == typeof(ICollection) ||
                interfaceType == typeof(IEnumerable) ||
                (interfaceType.IsGenericType &&
                 (interfaceType.GetGenericTypeDefinition() == typeof(ICollection<>) ||
                  interfaceType.GetGenericTypeDefinition() == typeof(IEnumerable<>) ||
                  interfaceType.GetGenericTypeDefinition() == typeof(IList<>))))
            {
                return true;
            }
        }

        return false;
    }
}