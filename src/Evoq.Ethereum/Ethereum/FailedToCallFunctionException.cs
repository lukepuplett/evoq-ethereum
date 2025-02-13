using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

/// <summary>
/// Thrown when a function call fails.
/// </summary>
[Serializable]
public class FailedToCallFunctionException : EthereumException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToCallFunctionException"/> class.
    /// </summary>
    /// <param name="message"></param>
    public FailedToCallFunctionException(string message) : base(message) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToCallFunctionException"/> class.
    /// </summary>
    /// <param name="message"></param>
    /// <param name="innerException"></param>
    public FailedToCallFunctionException(string message, Exception innerException) : base(message, innerException) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="FailedToCallFunctionException"/> class.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="context"></param>
    protected FailedToCallFunctionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

}
