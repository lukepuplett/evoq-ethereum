using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum;

[Serializable]
public class EthereumException : Exception
{
    public EthereumException(string message) : base(message) { }

    public EthereumException(string message, Exception inner) : base(message, inner) { }

    public EthereumException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    //

    public string? TransactionHash { get; init; }
}
