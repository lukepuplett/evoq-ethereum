using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when an account has insufficient funds to execute a transaction.
/// </summary>
/// <remarks>
/// This occurs when the account's balance is less than the sum of:
/// - The transaction value being sent
/// - The maximum transaction fee (gas limit * gas price)
/// </remarks>
[Serializable]
public class InsufficientFundsException : Exception
{
    /// <summary>
    /// Create a new instance of the InsufficientFundsException class.
    /// </summary>
    public InsufficientFundsException() { }

    /// <summary>
    /// Create a new instance of the InsufficientFundsException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public InsufficientFundsException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the InsufficientFundsException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>
    public InsufficientFundsException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Create a new instance of the InsufficientFundsException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected InsufficientFundsException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
