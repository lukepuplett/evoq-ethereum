using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Evoq.Ethereum.ABI.TypeEncoders;

/*

When encoding, the ClrType and Value properties are optional because we can use a
list of values to encode a tuple and each value is of a ClrType and has a Value.

When decoding, they are mandatory because we need to know the type of the value
to decode it correctly.

To populate the ClrType and Value properties for encoding, we could add a new method
to either the AbiValidator or to the AbiParameters class which outputs AbiParam
objects matching the expected parameters and structure.

The new method would take an object which would be expected to be a list of values,
including further lists and tuples. The method would return a list of tuples where
each tuple contains the ClrType and Value of the value in the list at the same
position.

Or, the new method could accept an object which is a POCO with properties that
match the expected parameters, potentially using the attributes to specify the
position and name of the parameter.

The ClrType and Value properties are used to store the value of the parameter in the
CLR type system.

*/

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents a parameter of a function or the components of a parameter.
/// </summary>
public record struct AbiParam()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param within its parent.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="abiType">The type of the param.</param>
    /// <param name="arrayLengths">The lengths of the arrays if the param is an array.</param>
    /// <param name="components">The components of the param.</param>
    /// <exception cref="ArgumentException">Thrown when the components contain nested params.</exception>
    public AbiParam(
        int position, string name, string abiType,
        IReadOnlyList<int>? arrayLengths = null,
        IReadOnlyList<AbiParam>? components = null)
        : this()
    {
        if (abiType != null && abiType.Trim().EndsWith("]"))
        {
            if (arrayLengths != null && arrayLengths.Count > 0)
            {
                throw new ArgumentException(
                    "Type must be a single type when array lengths are specified.",
                     nameof(abiType));
            }
            else
            {
                if (!AbiTypes.TryGetArrayDimensions(abiType, out var dimensions))
                {
                    throw new ArgumentException($"Invalid array type: {abiType}", nameof(abiType));
                }

                abiType = abiType.Substring(0, abiType.IndexOf('['));
                arrayLengths = dimensions;
            }
        }

        if (components == null) // single type
        {
            this.AbiType = GetCanonicalType(abiType, arrayLengths);

            if (!AbiTypes.IsValidType(this.AbiType))
            {
                throw new ArgumentException($"Invalid Solidity type '{this.AbiType}'", nameof(abiType));
            }
        }
        else // tuple
        {
            if (components.Count == 0)
            {
                throw new ArgumentException("Tuple must have at least one component.", nameof(components));
            }

            var canonicalType = GetCanonicalType(components, arrayLengths); // e.g. "(uint256,uint256)[2]"

            if (!string.IsNullOrEmpty(abiType) && canonicalType != abiType)
            {
                throw new ArgumentException(
                    "The components do not match the type. Leave the type empty or null to use the type of the components.",
                    nameof(components));
            }

            this.AbiType = canonicalType;
        }

        this.Position = position;
        this.Name = name;
        this.ArrayLengths = arrayLengths;
        this.Components = components;
        this.IsTuple = components == null || components.Count == 0;
        this.IsArray = arrayLengths != null && arrayLengths.Count > 0;
        this.IsDynamic = AbiTypes.IsDynamic(this.AbiType);

        // pick a default CLR type for the ABI type

        if (AbiTypes.TryGetDefaultClrType(this.AbiType, out var clrType))
        {
            this.ClrType = clrType;
        }
        else
        {
            this.ClrType = typeof(object);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param within its parent.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="abiType">The type of the param.</param>
    /// <exception cref="ArgumentException">Thrown when the type is a tuple.</exception>
    public AbiParam(int position, string name, string abiType)
        : this(position, name, abiType, null)
    {
        if (abiType.Contains("("))
        {
            throw new ArgumentException("Type must be a single type, not a tuple.", nameof(abiType));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param within its parent.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="components">The components of the param.</param>
    public AbiParam(int position, string name, IReadOnlyList<AbiParam> components)
        : this(position, name, GetCanonicalType(components), null, components)
    {
    }

    //

    /// <summary>
    /// Whether the param has components.
    /// </summary>
    public bool IsTuple { get; init; }

    /// <summary>
    /// Whether the param is a dynamic type.
    /// </summary>
    public bool IsDynamic { get; init; }

    /// <summary>
    /// Whether the param is an array.
    /// </summary>
    public bool IsArray { get; init; }

    /// <summary>
    /// The ordinal of the param.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// The name of the param.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The type of the param.
    /// </summary>
    public string AbiType { get; init; } = "";

    /// <summary>   
    /// The lengths of the array.
    /// </summary>
    public IReadOnlyList<int>? ArrayLengths { get; init; }

    /// <summary>
    /// The components of the param.
    /// </summary>
    public IReadOnlyList<AbiParam>? Components { get; init; }

    //

    internal Type ClrType { get; init; }
    internal object? Value { get; set; }

    //

    internal T? GetAs<T>()
    {
        if (this.Value == null)
        {
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
            {
                throw new InvalidCastException($"Cannot cast null to non-nullable value type {typeof(T)}");
            }

            return default; // null for reference types or Nullable<T>
        }

        if (!typeof(T).IsAssignableFrom(this.ClrType))
        {
            throw new InvalidCastException($"Cannot cast {this.ClrType} to {typeof(T)}");
        }

        return (T)this.Value;
    }

    /// <summary>
    /// Returns the canonical type of the parameter, e.g. "uint256" or "(uint256,bool)" or "(uint256 value, bool valid) ticket".
    /// </summary>
    /// <param name="includeNames">Whether to include the names of the components.</param>
    /// <param name="includeSpaces">Whether to include spaces between the components.</param>
    /// <returns>The canonical type of the parameter.</returns>
    public string GetCanonicalType(
        bool includeNames = false, bool includeSpaces = false)
    {
        if (this.IsTuple)
        {
            if (includeNames && !string.IsNullOrEmpty(this.Name))
            {
                return $"{this.AbiType} {this.Name}";
            }
            else
            {
                return this.AbiType;
            }
        }
        else
        {
            var componentType = GetCanonicalType(this.Components!, this.ArrayLengths, includeNames, includeSpaces);

            if (includeNames && !string.IsNullOrEmpty(this.Name))
            {
                return $"{componentType} {this.Name}";
            }
            else
            {
                return $"{componentType}";
            }
        }
    }

    /// <summary>
    /// Returns the canonical type for a type with optional array lengths.
    /// </summary>
    /// <param name="type">The type to get the canonical type of.</param>
    /// <param name="arrayLengths">The lengths of the arrays if the param is an array.</param>
    /// <returns>The canonical type of the type.</returns>
    public static string GetCanonicalType(string? type, IReadOnlyList<int>? arrayLengths = null)
    {
        if (string.IsNullOrEmpty(type))
        {
            return "";
        }

        if (type.Contains("[") || type.Contains("]"))
        {
            throw new ArgumentException("Type must be a single type, not an array.", nameof(type));
        }

        if (!AbiTypes.IsValidBaseType(type))
        {
            throw new ArgumentException($"Invalid Solidity base type: {type}", nameof(type));
        }

        return $"{type}{FormatArrayLengthsSuffix(arrayLengths)}";
    }

    /// <summary>
    /// Returns the canonical type of a list of parameters.
    /// </summary>
    /// <param name="parameters">The parameters to get the canonical type of.</param>
    /// <param name="arrayLengths">The lengths of the arrays if the param is an array.</param>
    /// <param name="includeNames">Whether to include the names of the components.</param>
    /// <param name="includeSpaces">Whether to include spaces between the components.</param>
    /// <returns>The canonical type of the parameters.</returns>
    public static string GetCanonicalType(
        IReadOnlyList<AbiParam> parameters, IReadOnlyList<int>? arrayLengths = null,
        bool includeNames = false, bool includeSpaces = false)
    {
        if (parameters.Count == 0)
        {
            return "()";
        }
        else
        {
            var suffix = FormatArrayLengthsSuffix(arrayLengths);

            if (includeSpaces)
            {
                return $"({string.Join(", ", parameters.Select(p => p.GetCanonicalType(includeNames, includeSpaces)))}){suffix}";
            }
            else
            {
                return $"({string.Join(",", parameters.Select(p => p.GetCanonicalType(includeNames, includeSpaces)))}){suffix}";
            }
        }
    }

    //

    private static string FormatArrayLengthsSuffix(IReadOnlyList<int>? arrayLengths)
    {
        if (arrayLengths != null && arrayLengths.Count > 0)
        {
            return $"{string.Join("", arrayLengths.Select(l => $"[{l}]"))}".Replace("-1", "");
        }

        return "";
    }

    //

    /// <summary>
    /// Returns the string representation of the parameter.
    /// </summary>
    /// <returns>The string representation of the parameter.</returns>
    public override string ToString() => this.GetCanonicalType(includeNames: true, includeSpaces: true);

    /// <summary>
    /// Validates that a value is compatible with this parameter's type.
    /// </summary>
    /// <param name="validator">The validator to use.</param>
    /// <param name="value">The value to validate.</param>
    /// <exception cref="AbiValidationException">Thrown when the value is not compatible with the parameter's type.</exception>
    public int ValidateValue(IAbiValueCompatible validator, object? value)
    {
        Debug.Assert(value != null, "Value cannot be null");
        Debug.Assert(!AbiTypeValidator.IsOfTypeType(value.GetType()), "Value cannot be a Type, this indicates a bug, not a validation error");

        var vc = new ValidationContext();

        if (!this.ValidateValue(validator, value, vc, null))
        {
            throw new AbiValidationException(
                expectedType: vc.ExpectedType ?? this.AbiType,
                valueProvided: vc.ValueProvided,
                validationPath: vc.Path,
                message: vc.Message);
        }

        return vc.ValuesVisitedCount;
    }

    /// <summary>
    /// Visits the parameter and its components, recursively.
    /// </summary>
    /// <param name="visitor">The visitor.</param>
    /// <param name="depth">The depth of the parameter to visit where 0 is the current level.</param>
    internal void DeepVisit(Action<AbiParam> visitor, int depth = int.MaxValue)
    {
        visitor(this);

        if (this.Components != null && depth > 0)
        {
            foreach (var component in this.Components.OrderBy(c => c.Position))
            {
                component.DeepVisit(visitor, depth - 1);
            }
        }
    }

    //

    private class ValidationContext
    {
        public const string Sep = " -> ";

        public string ExpectedType { get; private set; } = "";
        public object? ValueProvided { get; private set; }
        public string Path { get; private set; } = "";
        public string Message { get; private set; } = "";
        public int ValuesVisitedCount { get; private set; } = 0;

        public void IncrementVisitorCounter()
        {
            this.ValuesVisitedCount++;
        }

        public void SetFailed(string expectedType, object? valueProvided, string path, string message)
        {
            this.ExpectedType = expectedType;
            this.ValueProvided = valueProvided;
            this.Path = path[Sep.Length..];
            this.Message = message;
        }
    }

    private bool ValidateValue(IAbiValueCompatible validator, object? value, ValidationContext vc, string? path)
    {
        Debug.Assert(value != null, "Value cannot be null");
        Debug.Assert(!AbiTypeValidator.IsOfTypeType(value.GetType()), "Value cannot be a Type, this indicates a bug, not a validation error");

        var currentPath = $"{path}{ValidationContext.Sep}param-{this.Position} ({this.Name})";

        if (value == null)
        {
            vc.SetFailed(this.AbiType, value, currentPath, "value is null");
            return false;
        }

        // For basic types, use the existing validator
        if (this.IsTuple)
        {
            vc.IncrementVisitorCounter();

            var isValid = validator.IsCompatible(this.AbiType, value, out var message);
            if (!isValid)
            {
                vc.SetFailed(this.AbiType, value, currentPath, $"incompatible type: {message}");
            }
            return isValid;
        }

        // For tuples, validate each component
        if (this.Components != null)
        {
            if (value is not ITuple tuple)
            {
                vc.SetFailed(this.AbiType, value, currentPath, "expected tuple");
                return false;
            }

            if (tuple.Length != this.Components.Count)
            {
                vc.SetFailed(this.AbiType, value, currentPath, $"expected tuple of length {this.Components.Count}");
                return false;
            }

            // Check each component recursively
            for (int i = 0; i < this.Components.Count; i++)
            {
                if (!this.Components[i].ValidateValue(validator, tuple[i], vc, currentPath))
                {
                    return false;
                }
            }

            return true;
        }

        vc.SetFailed(this.AbiType, value, currentPath, "unexpected param state");
        return false;
    }
};
