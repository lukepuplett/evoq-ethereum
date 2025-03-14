using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Factory for creating instances of types, with special handling for record structs.
/// </summary>
internal class InstanceFactory
{
    /// <summary>
    /// Creates an instance of the specified type.
    /// </summary>
    /// <param name="type">The type to create an instance of.</param>
    /// <returns>An instance of the specified type.</returns>
    public object CreateInstance(Type type)
    {
        try
        {
            // Try to create an instance using Activator.CreateInstance
            return Activator.CreateInstance(type);
        }
        catch (MissingMethodException)
        {
            // If that fails, it might be a record type without a parameterless constructor
            if (type.IsValueType)
            {
                // For structs, we can use RuntimeHelpers.GetUninitializedObject
                // This creates a zeroed-out instance of the struct without calling a constructor
                return RuntimeHelpers.GetUninitializedObject(type);
            }

            // For reference types, we can also use RuntimeHelpers.GetUninitializedObject
            // This works for record classes too, which might not have accessible parameterless constructors
            return RuntimeHelpers.GetUninitializedObject(type);
        }
    }

    /// <summary>
    /// Creates an instance of the specified type.
    /// </summary>
    /// <typeparam name="T">The type to create an instance of.</typeparam>
    /// <returns>An instance of the specified type.</returns>
    public T CreateInstance<T>()
    {
        return (T)CreateInstance(typeof(T));
    }
}