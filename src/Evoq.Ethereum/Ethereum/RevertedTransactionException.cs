using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

[Serializable]
public class RevertedTransactionException : EthereumException
{
    public RevertedTransactionException(string message) : base(message) { }

    public RevertedTransactionException(string message, Exception innerException) : base(message, innerException) { }

    public RevertedTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    //

    public bool WasNonceGapCreated { get; init; }
}
