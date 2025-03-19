using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// An exception that is thrown when a transaction is reverted.
/// </summary>
[Serializable]
public class RevertedTransactionException : EthereumException
{
    /// <summary>
    /// Create a new instance of the RevertedTransactionException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public RevertedTransactionException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the RevertedTransactionException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public RevertedTransactionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Create a new instance of the RevertedTransactionException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    public RevertedTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    //

    /// <summary>
    /// Gets a value indicating whether a nonce gap was created.
    /// </summary>
    public bool WasNonceGapCreated { get; init; }
}
