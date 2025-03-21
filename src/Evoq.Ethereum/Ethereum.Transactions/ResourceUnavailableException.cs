using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when a requested resource is temporarily unavailable.
/// </summary>
/// <remarks>
/// This can occur due to:
/// - Node synchronization issues
/// - Network congestion
/// - Rate limiting
/// - Temporary service unavailability
/// </remarks>
[Serializable]
public class ResourceUnavailableException : Exception
{
    /// <summary>
    /// Create a new instance of the ResourceUnavailableException class.
    /// </summary>
    public ResourceUnavailableException() { }

    /// <summary>
    /// Create a new instance of the ResourceUnavailableException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public ResourceUnavailableException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the ResourceUnavailableException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public ResourceUnavailableException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Create a new instance of the ResourceUnavailableException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected ResourceUnavailableException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
