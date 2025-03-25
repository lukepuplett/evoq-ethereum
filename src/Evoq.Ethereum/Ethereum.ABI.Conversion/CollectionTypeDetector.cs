using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Provides methods to detect collection types.
/// </summary>
internal static class CollectionTypeDetector
{
    /// <summary>
    /// Determines whether the specified object is a dictionary.
    /// </summary>
    /// <param name="obj">The object to check.</param>
    /// <param name="dic">The dictionary if the object is a dictionary; otherwise, null.</param>
    /// <returns>true if the object is a dictionary; otherwise, false.</returns>
    public static bool IsDictionaryValue(object obj, out IReadOnlyDictionary<string, object?>? dic)
    {
        if (obj is Dictionary<string, object> stringObjDic)
        {
            dic = stringObjDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is Dictionary<string, object?> stringObjMaybeDic)
        {
            dic = stringObjMaybeDic;
            return true;
        }
        else if (obj is Dictionary<object, object> objObjDic)
        {
            dic = objObjDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is Dictionary<object, object?> objObjMaybeDic)
        {
            dic = objObjMaybeDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        // Read-only dictionaries
        else if (obj is IReadOnlyDictionary<string, object> stringObjReadDic)
        {
            dic = stringObjReadDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IReadOnlyDictionary<string, object?> stringObjMaybeReadDic)
        {
            dic = stringObjMaybeReadDic;
            return true;
        }
        else if (obj is IReadOnlyDictionary<object, object> objObjReadDic)
        {
            dic = objObjReadDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IReadOnlyDictionary<object, object?> objObjMaybeReadDic)
        {
            dic = objObjMaybeReadDic.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        // IDictionaries
        else if (obj is IDictionary<string, object> stringObjDickish)
        {
            dic = stringObjDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IDictionary<string, object?> stringObjMaybeDickish)
        {
            dic = stringObjMaybeDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IDictionary<object, object> objObjDickish)
        {
            dic = objObjDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }
        else if (obj is IDictionary<object, object?> objObjMaybeDickish)
        {
            dic = objObjMaybeDickish.ToDictionary(kvp => kvp.Key.ToString(), kvp => (object?)kvp.Value);
            return true;
        }

        dic = null;
        return false;
    }

    /// <summary>
    /// Determines whether the specified type is a collection type (excluding string).
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>true if the type is a collection type; otherwise, false.</returns>
    public static bool IsCollectionType(Type type)
    {
        if (type == null)
        {
            return false;
        }

        // Explicitly exclude string type
        if (type == typeof(string))
        {
            return false;
        }

        // Check for array
        if (type.IsArray)
        {
            return true;
        }

        // Check for common generic collection types
        if (type.IsGenericType)
        {
            Type genericTypeDef = type.GetGenericTypeDefinition();

            // Check for common generic collections
            if (genericTypeDef == typeof(List<>) ||
                genericTypeDef == typeof(IList<>) ||
                genericTypeDef == typeof(ICollection<>) ||
                genericTypeDef == typeof(IEnumerable<>) ||
                genericTypeDef == typeof(HashSet<>) ||
                genericTypeDef == typeof(ISet<>))
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

    /// <summary>
    /// Gets the element type of a collection type.
    /// </summary>
    /// <param name="collectionType">The collection type.</param>
    /// <returns>The element type of the collection.</returns>
    public static Type GetElementType(Type collectionType)
    {
        if (collectionType == null)
        {
            throw new ArgumentNullException(nameof(collectionType));
        }

        // Handle array types
        if (collectionType.IsArray)
        {
            return collectionType.GetElementType();
        }

        // Handle generic collections
        if (collectionType.IsGenericType)
        {
            Type[] genericArgs = collectionType.GetGenericArguments();
            if (genericArgs.Length > 0)
            {
                return genericArgs[0];
            }
        }

        // Handle non-generic collections by looking at interfaces
        foreach (Type interfaceType in collectionType.GetInterfaces())
        {
            if (interfaceType.IsGenericType)
            {
                Type genericDef = interfaceType.GetGenericTypeDefinition();
                if (genericDef == typeof(IEnumerable<>) ||
                    genericDef == typeof(ICollection<>) ||
                    genericDef == typeof(IList<>))
                {
                    return interfaceType.GetGenericArguments()[0];
                }
            }
        }

        // Default to object if we can't determine the element type
        return typeof(object);
    }

    /// <summary>
    /// Determines whether the specified value is a collection (excluding string).
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>true if the value is a collection; otherwise, false.</returns>
    public static bool IsCollectionValue(object value)
    {
        if (value == null)
        {
            return false;
        }

        // Explicitly exclude string
        if (value is string)
        {
            return false;
        }

        // Check if it's an array
        if (value.GetType().IsArray)
        {
            return true;
        }

        // Check if it implements IEnumerable<T> (but not string)
        if (value is IEnumerable<object>)
        {
            return true;
        }

        // Check if it implements IEnumerable (but not string)
        if (value is IEnumerable)
        {
            return true;
        }

        return false;
    }
}