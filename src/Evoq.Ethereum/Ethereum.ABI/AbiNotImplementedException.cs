using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Exception thrown when an ABI encoding or decoding operation is not supported.
/// </summary>
[Serializable]
public class AbiNotSupportedException : AbiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotSupportedException"/> class.
    /// </summary>
    public AbiNotSupportedException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotSupportedException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AbiNotSupportedException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotSupportedException"/> class with a specified error message,
    /// the ABI type, and the CLR type that was involved in the operation.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="abiType">The ABI type that caused the exception.</param>
    /// <param name="clrType">The CLR type that was involved in the operation.</param>
    public AbiNotSupportedException(string message, string abiType, Type? clrType) : base(message, abiType)
    {
        ClrType = clrType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotSupportedException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AbiNotSupportedException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiNotSupportedException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected AbiNotSupportedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ClrType = (Type?)info.GetValue(nameof(ClrType), typeof(Type));
    }

    //

    /// <summary>
    /// Gets the CLR type that was involved in the operation.
    /// </summary>
    public Type? ClrType { get; }

    //

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
