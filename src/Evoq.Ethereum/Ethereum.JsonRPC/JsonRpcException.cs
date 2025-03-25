using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.JsonRPC;

[Serializable]
internal abstract class JsonRpcException : EthereumException
{
    public JsonRpcException(string message) : base(message)
    {
    }

    public JsonRpcException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected JsonRpcException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}