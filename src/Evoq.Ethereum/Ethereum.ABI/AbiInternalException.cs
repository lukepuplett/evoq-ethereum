using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Exception thrown when there is an internal error in the ABI encoder or decoder.
/// </summary>
[Serializable]
public class AbiInternalException : AbiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiInternalException"/> class.
    /// </summary>
    public AbiInternalException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiInternalException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AbiInternalException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiInternalException"/> class with a specified error message
    /// and the ABI type that caused the exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="abiType">The ABI type that caused the exception.</param>
    public AbiInternalException(string message, string abiType) : base(message, abiType) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiInternalException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AbiInternalException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiInternalException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected AbiInternalException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
