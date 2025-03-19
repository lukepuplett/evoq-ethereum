using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// An exception that is thrown when a transaction is missing an event log.
/// </summary>
[Serializable]
public class MissingEventLogException : EthereumException
{
    /// <summary>
    /// Create a new instance of the MissingEventLogException class.
    /// </summary>
    /// <param name="message">The message of the exception.</param> 
    public MissingEventLogException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the MissingEventLogException class.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    /// <param name="inner">The inner exception.</param>
    public MissingEventLogException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Create a new instance of the MissingEventLogException class.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    public MissingEventLogException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
