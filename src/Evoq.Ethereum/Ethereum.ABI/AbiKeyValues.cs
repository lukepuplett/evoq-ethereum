using System.Collections.Generic;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// A read-only dictionary of key-value pairs.      
/// </summary>
public class AbiKeyValues : System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiKeyValues"/> class.
    /// </summary>
    /// <param name="dictionary">The dictionary to wrap.</param>
    public AbiKeyValues(IDictionary<string, object?> dictionary) : base(dictionary)
    {

    }

    //

    /// <summary>
    /// Creates a new instance of the <see cref="AbiKeyValues"/> class.
    /// </summary>
    /// <param name="name">The name of the key.</param>
    /// <param name="value">The value of the key.</param>
    /// <returns>A new instance of the <see cref="AbiKeyValues"/> class.</returns>
    public static AbiKeyValues Create(string name, object? value)
    {
        return new AbiKeyValues(new Dictionary<string, object?> { { name, value } });
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AbiKeyValues"/> class.
    /// </summary>
    /// <param name="name1">The name of the first key.</param>
    /// <param name="value1">The value of the first key.</param>
    /// <param name="name2">The name of the second key.</param>
    /// <param name="value2">The value of the second key.</param>
    /// <returns>A new instance of the <see cref="AbiKeyValues"/> class.</returns>
    public static AbiKeyValues Create(string name1, object? value1, string name2, object? value2)
    {
        return new AbiKeyValues(new Dictionary<string, object?> { { name1, value1 }, { name2, value2 } });
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AbiKeyValues"/> class.
    /// </summary>
    /// <param name="name1">The name of the first key.</param>
    /// <param name="value1">The value of the first key.</param>
    /// <param name="name2">The name of the second key.</param>
    /// <param name="value2">The value of the second key.</param>
    /// <param name="name3">The name of the third key.</param>
    /// <param name="value3">The value of the third key.</param>
    /// <returns>A new instance of the <see cref="AbiKeyValues"/> class.</returns>
    public static AbiKeyValues Create(string name1, object? value1, string name2, object? value2, string name3, object? value3)
    {
        return new AbiKeyValues(new Dictionary<string, object?> { { name1, value1 }, { name2, value2 }, { name3, value3 } });
    }

    /// <summary>
    /// Creates a new instance of the <see cref="AbiKeyValues"/> class.
    /// </summary>
    /// <param name="keyValues">The key-value pairs to add to the dictionary.</param>
    /// <returns>A new instance of the <see cref="AbiKeyValues"/> class.</returns>
    public static AbiKeyValues Create(params (string Name, object? Value)[] keyValues)
    {
        return new AbiKeyValues(keyValues.ToDictionary(kv => kv.Name, kv => kv.Value));
    }
}
