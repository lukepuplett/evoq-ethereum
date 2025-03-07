using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// A contract ABI.
/// </summary>
public class ContractAbi
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ContractAbi"/> class with the specified items.
    /// </summary>
    /// <param name="items">The ABI items to initialize with.</param>
    public ContractAbi(IEnumerable<ContractAbiItem> items)
    {
        this.Items = new List<ContractAbiItem>(items);
    }

    //

    /// <summary>
    /// The items in the ABI.
    /// </summary>
    public IReadOnlyList<ContractAbiItem> Items { get; set; } = new List<ContractAbiItem>();

    //

    /// <summary>
    /// Gets all functions in the ABI.
    /// </summary>
    /// <returns>An enumerable of function items.</returns>
    public IReadOnlyList<ContractAbiItem> GetFunctions()
    {
        return Items.Where(item => item.Type == "function").ToList();
    }

    /// <summary>
    /// Gets a function by its name.
    /// </summary>
    /// <param name="name">The function name to find.</param>
    /// <param name="function">The function if found, null otherwise.</param>
    /// <returns>True if the function was found, false otherwise.</returns>
    public bool TryGetFunction(string name, [NotNullWhen(true)] out ContractAbiItem? function)
    {
        var first = Items.FirstOrDefault(item =>
            item.Type == "function" &&
            string.Equals(item.Name, name, StringComparison.Ordinal));

        if (first == null)
        {
            function = null;
            return false;
        }

        function = first;
        return true;
    }

    /// <summary>
    /// Gets all functions matching the given name.
    /// </summary>
    /// <param name="name">The function name to find.</param>
    /// <returns>An enumerable of matching functions (for overloaded functions).</returns>
    public IReadOnlyList<ContractAbiItem> GetFunctions(string name)
    {
        return Items.Where(item =>
            item.Type == "function" &&
            string.Equals(item.Name, name, StringComparison.Ordinal)).ToList();
    }
}

/// <summary>
/// An item in a contract ABI.
/// </summary>
public class ContractAbiItem
{
    /// <summary>
    /// The type of the item.
    /// </summary>
    public string @Type { get; set; } = string.Empty;  // "function", "event", "error", "constructor"
    /// <summary>
    /// The name of the item.
    /// </summary>
    public string? Name { get; set; }
    /// <summary>
    /// The inputs of the item.
    /// </summary>
    public List<ContractAbiParameter> Inputs { get; set; } = new();
    /// <summary>
    /// The outputs of the item.
    /// </summary>
    public List<ContractAbiParameter>? Outputs { get; set; }
    /// <summary>
    /// The state mutability of the item.
    /// </summary>
    public string? StateMutability { get; set; }  // "pure", "view", "nonpayable", "payable"
    /// <summary>
    /// Whether the item is anonymous.
    /// </summary>
    public bool? Anonymous { get; set; }  // for events
}

/// <summary>
/// A parameter in a contract ABI.
/// </summary>
public class ContractAbiParameter
{
    /// <summary>
    /// The name of the parameter.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The type of the parameter.
    /// </summary>
    public string Type { get; set; } = string.Empty;  // e.g., "address", "uint256", "bytes32"
    /// <summary>
    /// The internal type of the parameter.
    /// </summary>
    public string? InternalType { get; set; }  // e.g., "contract ISchemaRegistry"
    /// <summary>
    /// Whether the parameter is indexed.
    /// </summary>
    public bool Indexed { get; set; }  // for event parameters
    /// <summary>
    /// The components of the parameter.
    /// </summary>
    public List<ContractAbiParameter>? Components { get; set; }  // for tuple types

    /// <summary>
    /// Whether the parameter is a tuple.
    /// </summary>
    public bool IsTuple => this.Components != null && this.Components.Any();
}
