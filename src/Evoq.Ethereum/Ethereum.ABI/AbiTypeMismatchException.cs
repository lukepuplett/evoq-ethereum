using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.ABI;

/// <summary>
/// Exception thrown when there is a mismatch between an ABI type and a CLR type.
/// </summary>
[Serializable]
public class AbiTypeMismatchException : AbiException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeMismatchException"/> class.
    /// </summary>
    public AbiTypeMismatchException() : base() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeMismatchException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public AbiTypeMismatchException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeMismatchException"/> class with a specified error message,
    /// the ABI type, and the CLR type that caused the mismatch.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="abiType">The ABI type that caused the exception.</param>
    /// <param name="clrType">The CLR type that caused the mismatch.</param>
    public AbiTypeMismatchException(string message, string abiType, Type? clrType) : base(message, abiType)
    {
        ClrType = clrType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeMismatchException"/> class with a specified error message
    /// and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public AbiTypeMismatchException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbiTypeMismatchException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected AbiTypeMismatchException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
        ClrType = (Type?)info.GetValue(nameof(ClrType), typeof(Type));
    }

    //

    /// <summary>
    /// Gets the CLR type that caused the mismatch.
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
