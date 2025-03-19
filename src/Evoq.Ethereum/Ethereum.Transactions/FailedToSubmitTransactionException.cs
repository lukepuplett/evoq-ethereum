using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Thrown when a transaction is not submitted to the blockchain.
/// </summary>
[Serializable]
public class FailedToSubmitTransactionException : EthereumException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToSubmitTransactionException"/> class.
    /// </summary>
    /// <param name="message"></param>
    public FailedToSubmitTransactionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToSubmitTransactionException"/> class.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public FailedToSubmitTransactionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToSubmitTransactionException"/> class.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected FailedToSubmitTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    //

    /// <summary>
    /// Gets a value indicating whether a nonce gap was created.
    /// </summary>
    public bool WasNonceGapCreated { get; init; }
}
