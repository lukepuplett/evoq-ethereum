using System;
using System.Runtime.Serialization;
using Evoq.Blockchain;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// An exception that is thrown when a transaction times out.
/// </summary>
[Serializable]
public class TransactionTimeoutException : Exception
{
    private Hex id;

    /// <summary>
    /// Create a new instance of the TransactionTimeoutException class.
    /// </summary>
    public TransactionTimeoutException()
    {
    }

    /// <summary>
    /// Create a new instance of the TransactionTimeoutException class.
    /// </summary>
    /// <param name="id">The ID of the transaction that timed out.</param>
    public TransactionTimeoutException(Hex id)
    {
        this.id = id;
    }

    /// <summary>
    /// Create a new instance of the TransactionTimeoutException class.
    /// </summary>
    /// <param name="message">The message of the exception.</param> 
    public TransactionTimeoutException(string message) : base(message)
    {
    }

    /// <summary>
    /// Create a new instance of the TransactionTimeoutException class.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    /// <param name="innerException">The inner exception.</param>
    public TransactionTimeoutException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Create a new instance of the TransactionTimeoutException class.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    protected TransactionTimeoutException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}