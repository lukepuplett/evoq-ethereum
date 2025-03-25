using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Represents an exception that occurs when a request to a chain fails.
/// </summary>
[Serializable]
public class ChainRequestException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ChainRequestException"/> class.
    /// </summary>
    public ChainRequestException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainRequestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ChainRequestException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainRequestException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ChainRequestException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ChainRequestException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected ChainRequestException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}