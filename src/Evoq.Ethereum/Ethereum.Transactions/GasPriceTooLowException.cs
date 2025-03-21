using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// Exception thrown when a transaction's gas price is below the network's minimum acceptable price.
/// </summary>
/// <remarks>
/// This typically occurs when the network is congested and minimum gas prices have increased,
/// or when using outdated gas price estimates.
/// </remarks>
[Serializable]
public class GasPriceTooLowException : Exception
{

    /// <summary>
    /// Create a new instance of the GasPriceTooLowException class.
    /// </summary>
    public GasPriceTooLowException() { }

    /// <summary>
    /// Create a new instance of the GasPriceTooLowException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public GasPriceTooLowException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the GasPriceTooLowException class.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="inner">The exception that is the cause of the current exception.</param>   
    public GasPriceTooLowException(string message, Exception inner) : base(message, inner) { }

    /// <summary>
    /// Create a new instance of the GasPriceTooLowException class.
    /// </summary>
    /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
    protected GasPriceTooLowException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
