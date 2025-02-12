using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

[Serializable]
public class FailedToSubmitTransactionException : EthereumException
{
    public FailedToSubmitTransactionException(string message) : base(message) { }

    public FailedToSubmitTransactionException(string message, Exception innerException) : base(message, innerException) { }

    public FailedToSubmitTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    //

    public bool WasNonceGapCreated { get; init; }
}
