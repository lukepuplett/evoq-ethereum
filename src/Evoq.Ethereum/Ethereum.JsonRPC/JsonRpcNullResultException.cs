using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a JSON-RPC response that has a null result.
/// </summary>
[Serializable]
internal class JsonRpcNullResultException : Exception
{
    public JsonRpcNullResultException()
    {
    }

    public JsonRpcNullResultException(string message) : base(message)
    {
    }

    public JsonRpcNullResultException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected JsonRpcNullResultException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}