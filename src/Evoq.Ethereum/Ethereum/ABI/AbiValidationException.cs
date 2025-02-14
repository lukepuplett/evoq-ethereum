using System;
using System.Collections.Generic;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Exception thrown when ABI parameter validation fails.
/// </summary>
public class AbiValidationException : Exception
{
    /// <summary>
    /// The type that was expected.
    /// </summary>
    public string ExpectedType { get; }

    /// <summary>
    /// The actual value that failed validation.
    /// </summary>
    public object? ValueProvided { get; }

    /// <summary>
    /// The type of the actual value.
    /// </summary>
    public Type? TypeProvided { get; }

    /// <summary>
    /// The path to the parameter that failed validation.
    /// </summary>
    public string ValidationPath { get; }

    /// <summary>
    /// Creates a new ABI validation exception.
    /// </summary>
    public AbiValidationException(
        string expectedType,
        object? valueProvided,
        string validationPath,
        string? message = null)
        : base(FormatMessage(expectedType, valueProvided, message))
    {
        ExpectedType = expectedType;
        ValueProvided = valueProvided;
        TypeProvided = valueProvided?.GetType();
        ValidationPath = validationPath;
    }

    private static string FormatMessage(
        string expectedType,
        object? actualValue,
        string? message)
    {
        var actualType = actualValue?.GetType();

        var baseMessage = $"Value of type {actualType} is not compatible with parameter type {expectedType}";

        return message == null ? baseMessage : $"{baseMessage}: {message}";
    }
}