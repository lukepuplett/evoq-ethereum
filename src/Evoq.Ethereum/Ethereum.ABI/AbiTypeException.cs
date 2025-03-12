using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Exception thrown when an ABI type is not supported or cannot be encoded/decoded.
/// </summary>
[Serializable]
public class AbiTypeException : AbiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeException"/> class.
    /// </summary>
    public AbiTypeException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AbiTypeException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeException"/> class with a specified error message
    /// and the ABI type that caused the exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="abiType">The ABI type that caused the exception.</param>
    public AbiTypeException(string message, string abiType) : base(message, abiType) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AbiTypeException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected AbiTypeException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
