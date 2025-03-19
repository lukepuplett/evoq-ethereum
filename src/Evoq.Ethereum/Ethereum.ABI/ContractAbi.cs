using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// The type of an item in a contract ABI.
/// </summary>  
public enum AbiItemType
{
    /// <summary>
    /// A function.
    /// </summary>
    Function,
    /// <summary>
    /// An event.
    /// </summary>
    Event,
    /// <summary>
    /// An error.
    /// </summary>
    Error,
    /// <summary>
    /// A constructor.
    /// </summary>
    Constructor
}

/// <summary>
/// A contract ABI.
/// </summary>
public class ContractAbi
{
    private readonly static string FunctionType = AbiItemType.Function.ToString().ToLowerInvariant();
    private readonly static string EventType = AbiItemType.Event.ToString().ToLowerInvariant();
    private readonly static string ErrorType = AbiItemType.Error.ToString().ToLowerInvariant();
    private readonly static string ConstructorType = AbiItemType.Constructor.ToString().ToLowerInvariant();

    //

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

    private bool TryGetItem(string type, string name, [NotNullWhen(true)] out ContractAbiItem? item)
    {
        var first = Items.FirstOrDefault(item =>
            item.Type == type &&
            string.Equals(item.Name, name, StringComparison.Ordinal));

        if (first == null)
        {
            item = null;
            return false;
        }

        item = first;
        return true;
    }

    private IReadOnlyList<ContractAbiItem> GetItems(string type, string? name = null)
    {
        var query = Items.Where(item => item.Type == type);

        if (name != null)
        {
            query = query.Where(item =>
                string.Equals(item.Name, name, StringComparison.Ordinal));
        }

        return query.ToList();
    }

    /// <summary>
    /// Gets all functions in the ABI.
    /// </summary>
    /// <returns>An enumerable of function items.</returns>
    public IReadOnlyList<ContractAbiItem> GetFunctions()
    {
        return GetItems(FunctionType);
    }

    /// <summary>
    /// Tries to get a function by its name.
    /// </summary>
    /// <param name="name">The function name to find.</param>
    /// <param name="function">The function if found, null otherwise.</param>
    /// <returns>True if the function was found, false otherwise.</returns>
    public bool TryGetFunction(string name, [NotNullWhen(true)] out ContractAbiItem? function)
    {
        return TryGetItem(FunctionType, name, out function);
    }

    /// <summary>
    /// Gets all functions matching the given name.
    /// </summary>
    /// <param name="name">The function name to find.</param>
    /// <returns>An enumerable of matching functions (for overloaded functions).</returns>
    public IReadOnlyList<ContractAbiItem> GetFunctions(string name)
    {
        return GetItems(FunctionType, name);
    }

    /// <summary>
    /// Gets all events in the ABI.
    /// </summary>
    /// <returns>An enumerable of event items.</returns>
    public IReadOnlyList<ContractAbiItem> GetEvents()
    {
        return GetItems(EventType);
    }

    /// <summary>
    /// Tries to get an event by its name.
    /// </summary>
    /// <param name="name">The event name to find.</param>
    /// <param name="event">The event if found, null otherwise.</param>
    /// <returns>True if the event was found, false otherwise.</returns>
    public bool TryGetEvent(string name, [NotNullWhen(true)] out ContractAbiItem? @event)
    {
        return TryGetItem(EventType, name, out @event);
    }

    /// <summary>
    /// Gets all events matching the given name.
    /// </summary>
    /// <param name="name">The event name to find.</param>
    /// <returns>An enumerable of matching events (for overloaded events).</returns>
    public IReadOnlyList<ContractAbiItem> GetEvents(string name)
    {
        return GetItems(EventType, name);
    }
}

/// <summary>
/// An item in a contract ABI.
/// </summary>
/// <remarks>
/// Different item types have different characteristics:
/// 
/// Functions:
/// - Type = "function"
/// - Name is required
/// - Inputs represent the function parameters
/// - Outputs represent the return values
/// - StateMutability will be "pure", "view", "nonpayable", or "payable"
/// 
/// Events:
/// - Type = "event"
/// - Name is required
/// - Inputs represent the event parameters
/// - Outputs is null/empty
/// - Anonymous determines if the event is anonymous (no event signature topic)
/// - Input parameters may have Indexed = true (up to 3 per event)
/// 
/// Errors:
/// - Type = "error"
/// - Name is required
/// - Inputs represent the error parameters
/// - Outputs is null/empty
/// 
/// Constructor:
/// - Type = "constructor"
/// - Name is null/empty
/// - Inputs represent the constructor parameters
/// - Outputs is null/empty
/// - StateMutability will be "payable" or "nonpayable"
/// </remarks>
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
