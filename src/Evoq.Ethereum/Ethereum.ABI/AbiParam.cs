using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using Evoq.Blockchain;
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
public class AbiParam
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param within its parent.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="descriptor">The type descriptor of the param.</param>
    /// <param name="arrayLengths">The lengths of the arrays if the param is an array.</param>
    /// <exception cref="ArgumentException">Thrown when the components contain nested params.</exception>
    public AbiParam(
        int position, string name, string descriptor,
        IReadOnlyList<int>? arrayLengths = null)
    {
        if (string.IsNullOrEmpty(descriptor))
        {
            throw new ArgumentException("Descriptor cannot be null or empty.", nameof(descriptor));
        }

        (this.AbiType, this.Descriptor) = SetCanonicalTypes(descriptor, arrayLengths);

        if (AbiTypes.TryGetArrayDimensions(descriptor, out var dimensions))
        {
            if (arrayLengths != null && arrayLengths.Count > 0)
            {
                throw new ArgumentException(
                    "Type must not have array dimensions when array lengths are specified.",
                     nameof(descriptor));
            }
            else
            {
                arrayLengths = dimensions;
            }
        }

        if (!AbiTypes.IsValidType(this.AbiType))
        {
            throw new ArgumentException($"Invalid type '{this.AbiType}'", nameof(descriptor));
        }

        this.Position = position;
        this.Name = name;
        this.ArrayLengths = arrayLengths;

        // set the default CLR type for the ABI type
        // e.g. uint8[][] -> System.Byte[][] and bytes3[] -> System.Byte[][] (since bytes3 is 3 bytes which needs to be an array, in an array)

        if (AbiTypes.TryGetDefaultClrType(this.AbiType, out var clrType))
        {
            this.ClrType = clrType;
        }
        else
        {
            this.ClrType = typeof(object);
        }

        // set the default CLR type for the base type of the ABI type
        // e.g. uint8[][] -> System.Byte and bytes3[] -> System.Byte[] (since bytes3 is 3 bytes which needs to be an array)

        if (AbiTypes.TryGetArrayBaseType(this.AbiType, out var baseType) &&
            AbiTypes.TryGetDefaultClrType(baseType!, out var baseClrType))
        {
            this.BaseClrType = baseClrType;
        }
        else
        {
            this.BaseClrType = typeof(object);
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param within its parent.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="descriptor">The type descriptor of the param.</param>
    /// <exception cref="ArgumentException">Thrown when the type is a tuple.</exception>
    public AbiParam(int position, string name, string descriptor)
        : this(position, name, descriptor, null)
    {

    }

    //

    /// <summary>
    /// Whether the param is a tuple, but not an array of tuples.
    /// </summary>
    public bool IsTupleStrict => AbiTypes.IsTuple(this.AbiType, false);

    /// <summary>
    /// Whether the param is an array of tuples.
    /// </summary>
    public bool IsTupleArray => AbiTypes.IsTuple(this.AbiType, true);

    /// <summary>
    /// Whether the param is a dynamic type.
    /// </summary>
    public bool IsDynamic => AbiTypes.IsDynamic(this.AbiType);

    /// <summary>
    /// Whether the param is an array.
    /// </summary>
    public bool IsArray => this.ArrayLengths != null && this.ArrayLengths.Count > 0;

    /// <summary>
    /// The ordinal of the param.
    /// </summary>
    public int Position { get; init; }

    /// <summary>
    /// The name of the param.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// The canonical type of the param.
    /// </summary>
    public string AbiType { get; init; } = "";

    /// <summary>
    /// The type descriptor of the param.
    /// </summary>
    public string Descriptor { get; init; } = "";

    /// <summary>   
    /// The lengths of the array.
    /// </summary>
    public IReadOnlyList<int>? ArrayLengths { get; init; }

    // /// <summary>
    // /// The components of the param.
    // /// </summary>
    // public IReadOnlyList<AbiParam>? Components { get; init; }

    //

    internal Type ClrType { get; init; } = typeof(object);
    internal Type BaseClrType { get; init; } = typeof(object);
    internal object? Value { get; set; }

    //

    /// <summary>
    /// Returns the components of the param if it is a tuple or array of tuples.
    /// </summary>
    /// <returns>The components of the param.</returns>
    public bool TryParseComponents(out AbiParameters? parameters)
    {
        if (this.IsTupleArray)
        {
            if (AbiTypes.TryGetArrayBaseType(this.Descriptor, out var baseDescriptor))
            {
                parameters = AbiParameters.Parse(baseDescriptor!);
                return true;
            }

            parameters = AbiParameters.Parse(this.Descriptor);
            return true;
        }
        else
        {
            parameters = null;
            return false;
        }
    }

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

    private string GetCanonicalType(bool includeNames = false, bool includeSpaces = false)
    {
        if (this.TryParseComponents(out var components))
        {
            var componentType = GetCanonicalType(components!, this.ArrayLengths, includeNames, includeSpaces);

            if (includeNames && !string.IsNullOrEmpty(this.Name))
            {
                return $"{componentType} {this.Name}";
            }
            else
            {
                return $"{componentType}";
            }
        }
        else
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
    }

    //

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

    private static (string, string) SetCanonicalTypes(string type, IReadOnlyList<int>? arrayLengths = null)
    {
        if (AbiTypes.IsTuple(type, includeArrays: true))
        {
            var components = AbiParameters.Parse(type);

            var bare = components.GetCanonicalType(false, false);
            var named = components.GetCanonicalType(true, true);

            bare = $"{bare}{FormatArrayLengthsSuffix(arrayLengths)}";
            named = $"{named}{FormatArrayLengthsSuffix(arrayLengths)}";

            return (bare, named);
        }

        // single type

        if (!AbiTypes.TryGetCanonicalType(type, out var singleType))
        {
            throw new ArgumentException($"Invalid ABI type: {type}", nameof(type));
        }

        singleType = $"{singleType}{FormatArrayLengthsSuffix(arrayLengths)}";

        return (singleType, singleType);
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
    /// Converts the parameter to a key-value pair.
    /// </summary>
    /// <returns>The key-value pair.</returns>
    internal KeyValuePair<string, object?> ToKeyValuePair(bool forStringification)
    {
        string name = string.IsNullOrWhiteSpace(this.Name) ? this.Position.ToString() : this.Name;
        object? value = forStringification ? getValue(this.Value) : this.Value;

        return new KeyValuePair<string, object?>(name, value);

        //

        object? getValue(object? value)
        {
            // if (this.IsTupleStrict)
            // {
            //     if (this.Value is IDictionary<string, object?> dict)
            //     {
            //         value = dict;
            //     }
            // }
            // else if (this.IsTupleArray)
            // {
            //     if (this.Value is IEnumerable<IDictionary<string, object?>> list)
            //     {
            //         value = list.ToArray();
            //     }
            // }

            if (value is IDictionary<string, object?> dict)
            {
                return dict.ToDictionary(kvp => kvp.Key, kvp => getValue(kvp.Value));
            }

            if (value is IEnumerable<object?> list)
            {
                return list.Select(getValue).ToArray();
            }

            if (value is Array)
            {
                return ((Array)value).Cast<object?>().Select(getValue).ToArray();
            }

            if (value is BigInteger big)
            {
                value = big.ToString(); // huge number should be stringified
            }

            if (value is byte[] bytes)
            {
                if (AbiTypeNames.String == this.AbiType)
                {
                    value = Encoding.UTF8.GetString(bytes);
                }
                else
                {
                    value = new Hex(bytes).ToString();
                }
            }

            return value;
        }
    }

    /// <summary>
    /// Visits the parameter and its components, recursively.
    /// </summary>
    /// <param name="visitor">The visitor.</param>
    /// <param name="depth">The depth of the parameter to visit where 0 is the current level.</param>
    internal void DeepVisit(Action<AbiParam> visitor, int depth = int.MaxValue)
    {
        visitor(this);

        if (this.TryParseComponents(out var components) && depth > 0)
        {
            foreach (var component in components!.OrderBy(c => c.Position))
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
        if (!this.IsTupleStrict)
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
        if (this.TryParseComponents(out var components))
        {
            if (value is not ITuple tuple)
            {
                vc.SetFailed(this.AbiType, value, currentPath, "expected tuple");
                return false;
            }

            if (tuple.Length != components!.Count)
            {
                vc.SetFailed(this.AbiType, value, currentPath, $"expected tuple of length {components.Count}");
                return false;
            }

            // Check each component recursively
            for (int i = 0; i < components.Count; i++)
            {
                if (!components[i].ValidateValue(validator, tuple[i], vc, currentPath))
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
