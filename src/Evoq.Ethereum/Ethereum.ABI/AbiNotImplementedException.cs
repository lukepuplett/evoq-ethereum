using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Exception thrown when an ABI encoding or decoding operation is not implemented.
/// </summary>
[Serializable]
public class AbiNotImplementedException : AbiException
{
    /// <summary>
    /// Gets the CLR type that was involved in the operation.
    /// </summary>
    public Type? ClrType { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotImplementedException"/> class.
    /// </summary>
    public AbiNotImplementedException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotImplementedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AbiNotImplementedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotImplementedException"/> class with a specified error message,
    /// the ABI type, and the CLR type that was involved in the operation.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="abiType">The ABI type that caused the exception.</param>
    /// <param name="clrType">The CLR type that was involved in the operation.</param>
    public AbiNotImplementedException(string message, string abiType, Type? clrType) : base(message, abiType)
    {
        ClrType = clrType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotImplementedException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AbiNotImplementedException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotImplementedException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected AbiNotImplementedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ClrType = (Type?)info.GetValue(nameof(ClrType), typeof(Type));
    }

    /// <summary>
    /// Sets the SerializationInfo with information about the exception.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ClrType), ClrType);
    }
}
