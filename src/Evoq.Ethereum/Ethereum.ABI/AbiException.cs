using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Base exception for all ABI encoding and decoding errors.
/// </summary>
[Serializable]
public abstract class AbiException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiException"/> class.
    /// </summary>
    protected AbiException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    protected AbiException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    protected AbiException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiException"/> class with a specified error message
    /// and the ABI type that caused the exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="abiType">The ABI type that caused the exception.</param>
    protected AbiException(string message, string? abiType) : base(message)
    {
        this.AbiType = abiType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected AbiException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        this.AbiType = info.GetString(nameof(AbiType));
    }

    //

    /// <summary>
    /// Gets the ABI type that caused the exception, if applicable.
    /// </summary>
    public string? AbiType { get; }

    //

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(AbiType), AbiType);
    }
}
