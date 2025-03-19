using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.Transactions;

/// <summary>
/// An exception that is thrown when a fee calculation fails.
/// </summary>
[Serializable]
public class FeeCalculationException : EthereumException
{
    /// <summary>
    /// Create a new instance of the FeeCalculationException class.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    public FeeCalculationException(string message) : base(message) { }

    /// <summary>
    /// Create a new instance of the FeeCalculationException class.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    /// <param name="innerException">The inner exception.</param>
    public FeeCalculationException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Create a new instance of the FeeCalculationException class.
    /// </summary>
    /// <param name="info">The serialization info.</param>
    /// <param name="context">The streaming context.</param>
    public FeeCalculationException(SerializationInfo info, StreamingContext context) : base(info, context) { }
}
