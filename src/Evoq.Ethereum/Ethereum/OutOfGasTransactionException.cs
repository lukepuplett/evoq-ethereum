using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

[Serializable]
public class OutOfGasTransactionException : EthereumException
{
    public OutOfGasTransactionException(string message) : base(message) { }

    public OutOfGasTransactionException(string message, Exception innerException) : base(message, innerException) { }

    public OutOfGasTransactionException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    //

    public bool WasNonceGapCreated { get; init; }
}
