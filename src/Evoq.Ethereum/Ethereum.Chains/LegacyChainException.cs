using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Chains;

/// <summary>
/// Exception thrown when a chain is detected to be a legacy chain.
/// </summary>
[Serializable]
public class LegacyChainException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyChainException"/> class.
    /// </summary>
    public LegacyChainException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyChainException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public LegacyChainException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyChainException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public LegacyChainException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="LegacyChainException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected LegacyChainException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}