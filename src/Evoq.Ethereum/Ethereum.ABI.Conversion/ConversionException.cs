using System;

namespace Evoq.Ethereum.ABI.Conversion;

/// <summary>
/// Exception thrown when a conversion error occurs during object mapping.
/// </summary>
public class ConversionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class.
    /// </summary>
    public ConversionException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ConversionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConversionException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public ConversionException(string message, Exception innerException) : base(message, innerException) { }
}