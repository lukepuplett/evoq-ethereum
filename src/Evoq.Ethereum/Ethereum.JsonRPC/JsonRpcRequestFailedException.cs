using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a failure to make a JSON-RPC request or receive a valid response.
/// </summary>
/// <remarks>
/// This exception is thrown when a JSON-RPC request fails to be sent or when the response
/// is malformed or otherwise invalid. Successfully received errors are represented by
/// <see cref="JsonRpcProvidedErrorException"/>.
/// </remarks>
[Serializable]
internal class JsonRpcRequestFailedException : Exception
{
    public JsonRpcRequestFailedException()
    {
    }

    public JsonRpcRequestFailedException(string message) : base(message)
    {
    }

    public JsonRpcRequestFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    protected JsonRpcRequestFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}