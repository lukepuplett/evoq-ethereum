using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when a request exceeds a defined limit.
/// </summary>
/// <remarks>
/// Common scenarios include:
/// - Exceeding API rate limits
/// - Block gas limits
/// - Maximum call depth
/// - Maximum array size or computation limits
/// </remarks>
[Serializable]
public class LimitExceededException : Exception
{
    /// <summary>
    /// Create a new instance of the LimitExceededException class.
    /// </summary>
    public LimitExceededException() { }

    /// <summary>
    /// Create a new instance of the LimitExceededException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public LimitExceededException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the LimitExceededException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public LimitExceededException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Create a new instance of the LimitExceededException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected LimitExceededException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}