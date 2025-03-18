
using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when a nonce is invalid.
/// </summary>
[Serializable]
public class InvalidNonceException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidNonceException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param> 
    public InvalidNonceException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidNonceException"/> class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>   
    public InvalidNonceException(string message, Exception inner) : base(message, inner)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidNonceException"/> class.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected InvalidNonceException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}