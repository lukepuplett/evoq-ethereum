using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Represents a param of parameters for a function or the components of a param.
/// </summary>
public record struct EvmParam()
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EvmParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="abiType">The type of the param.</param>
    /// <param name="arrayLengths">The lengths of the arrays if the param is an array.</param>
    /// <param name="components">The components of the param.</param>
    /// <exception cref="ArgumentException">Thrown when the components contain nested params.</exception>
    public EvmParam(
        int position, string name, string abiType,
        IReadOnlyList<int>? arrayLengths = null,
        IReadOnlyList<EvmParam>? components = null)
        : this()
    {
        if (abiType != null && (abiType.Contains("[") || abiType.Contains("]")))
        {
            throw new ArgumentException("Type must be a single type. Its array lengths must be specified as a separate parameter.", nameof(abiType));
        }

        if (components != null)
        {
            var canonicalType = GetCanonicalType(components, arrayLengths);

            if (!string.IsNullOrEmpty(abiType) && canonicalType != abiType)
            {
                throw new ArgumentException(
                    "The components do not match the type. Leave the type empty or null to use the type of the components.",
                    nameof(components));
            }

            this.AbiType = canonicalType;
        }
        else
        {
            this.AbiType = GetCanonicalType(abiType, arrayLengths);

            if (!SolidityTypes.IsValidType(this.AbiType))
            {
                throw new ArgumentException($"Invalid Solidity type '{this.AbiType}'", nameof(abiType));
            }
        }

        this.Position = position;
        this.Name = name;
        this.ArrayLengths = arrayLengths;
        this.Components = components;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvmParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="abiType">The type of the param.</param>
    /// <exception cref="ArgumentException">Thrown when the type is a tuple.</exception>
    public EvmParam(int position, string name, string abiType)
        : this(position, name, abiType, null)
    {
        if (abiType.Contains("("))
        {
            throw new ArgumentException("Type must be a single type, not a tuple.", nameof(abiType));
        }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="EvmParam"/> struct.
    /// </summary>
    /// <param name="position">The ordinal of the param.</param>
    /// <param name="name">The name of the param.</param>
    /// <param name="components">The components of the param.</param>
    public EvmParam(int position, string name, IReadOnlyList<EvmParam> components)
        : this(position, name, GetCanonicalType(components), null, components)
    {
    }

    //

    /// <summary>
    /// Whether the param is a single value.
    /// </summary>
    public bool IsSingle => Components == null || Components.Count == 0;

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
    public IReadOnlyList<EvmParam>? Components { get; init; }

    //

    /// <summary>
    /// Returns the canonical type of the parameter, e.g. "uint256" or "(uint256,bool)" or "(uint256 value, bool valid) ticket".
    /// </summary>
    /// <param name="includeNames">Whether to include the names of the components.</param>
    /// <param name="includeSpaces">Whether to include spaces between the components.</param>
    /// <returns>The canonical type of the parameter.</returns>
    public string GetCanonicalType(
        bool includeNames = false, bool includeSpaces = false)
    {
        if (this.IsSingle)
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

        if (!SolidityTypes.IsValidBaseType(type))
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
        IReadOnlyList<EvmParam> parameters, IReadOnlyList<int>? arrayLengths = null, bool includeNames = false, bool includeSpaces = false)
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
    /// <param name="value">The value to validate.</param>
    /// <exception cref="AbiValidationException">Thrown when the value is not compatible with the parameter's type.</exception>
    public int ValidateValue(object? value)
    {
        var vc = new ValidationContext();

        if (!ValidateValue(value, vc))
        {
            throw new AbiValidationException(
                expectedType: vc.ExpectedType ?? this.AbiType,
                valueProvided: vc.ValueProvided,
                validationPath: vc.Path,
                message: vc.Message);
        }

        return vc.VisitCount;
    }

    /// <summary>
    /// Encodes a value according to this parameter's type.
    /// </summary>
    /// <param name="value">The value to encode.</param>
    /// <returns>The ABI encoded value.</returns>
    public byte[] Encode(object? value)
    {
        if (value == null)
            throw new ArgumentNullException(nameof(value));

        this.ValidateValue(value);

        // Handle basic types
        if (this.IsSingle)
        {
            return this.AbiType switch
            {
                "address" => AbiEncoder.EncodeAddress((EthereumAddress)value),
                "uint256" => AbiEncoder.EncodeUint256((BigInteger)value),
                "bool" => AbiEncoder.EncodeBool((bool)value),
                _ => throw new NotImplementedException($"Encoding for type {AbiType} not implemented")
            };
        }

        // Handle arrays and tuples
        if (this.Components != null)
        {
            if (value is not ITuple tuple)
                throw new ArgumentException("Value must be a tuple", nameof(value));

            if (tuple.Length != this.Components.Count)
                throw new ArgumentException("Tuple length does not match component count", nameof(value));

            // For tuples, encode each component and concatenate

            var byteArrays = new List<byte[]>();

            for (int i = 0; i < this.Components.Count; i++)
            {
                byteArrays.Add(this.Components[i].Encode(tuple[i]));
            }

            // Concatenate the byte arrays

            return byteArrays.SelectMany(x => x).ToArray();
        }

        throw new NotImplementedException($"Encoding for type {AbiType} not implemented");
    }

    /// <summary>
    /// Visits the parameter and its components, recursively.
    /// </summary>
    /// <param name="visitor">The visitor.</param>
    /// <param name="depth">The depth of the parameter to visit where 0 is the current level.</param>
    internal void DeepVisit(Action<EvmParam> visitor, int depth = int.MaxValue)
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

        public int VisitCount { get; private set; } = 0;

        public void IncrementVisitorCounter()
        {
            this.VisitCount++;
        }

        public void SetFailed(string expectedType, object? valueProvided, string path, string message)
        {
            this.ExpectedType = expectedType;
            this.ValueProvided = valueProvided;
            this.Path = path[Sep.Length..];
            this.Message = message;
        }
    }

    private bool ValidateValue(object? value, ValidationContext vc, string? path = null)
    {
        var currentPath = $"{path}{ValidationContext.Sep}param-{this.Position} ({this.Name})";

        if (value == null)
            return false;

        // For basic types, use the existing validator
        if (this.IsSingle)
        {
            vc.IncrementVisitorCounter();

            var isValid = SolidityTypeValidator.IsCompatible(this.AbiType, value);
            if (!isValid)
            {
                vc.SetFailed(this.AbiType, value, currentPath, "incompatible type");
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
                if (!this.Components[i].ValidateValue(tuple[i], vc, currentPath))
                    return false;
            }
            return true;
        }

        return false;
    }
};
