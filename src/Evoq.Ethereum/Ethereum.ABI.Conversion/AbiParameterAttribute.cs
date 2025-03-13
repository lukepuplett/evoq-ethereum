using System;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Attribute for specifying the name of an ABI parameter.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class AbiParameterAttribute : Attribute
{
    /// <summary>
    /// Gets the name of the ABI parameter.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets the position of the ABI parameter.
    /// </summary>
    public int Position { get; set; } = -1;

    /// <summary>
    /// Gets or sets the ABI type of the parameter.
    /// </summary>
    public string? AbiType { get; set; }

    /// <summary>
    /// Gets or sets whether the parameter is indexed (for events).
    /// </summary>
    public bool IsIndexed { get; set; }

    /// <summary>
    /// Gets or sets whether the parameter should be ignored during conversion.
    /// </summary>
    public bool Ignore { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParameterAttribute"/> class.
    /// </summary>
    /// <param name="name">The name of the ABI parameter.</param>
    public AbiParameterAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParameterAttribute"/> class with a position.
    /// </summary>
    /// <param name="position">The position of the ABI parameter.</param>
    public AbiParameterAttribute(int position)
    {
        Name = position.ToString();
        Position = position;
    }
}
