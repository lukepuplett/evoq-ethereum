using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a failure reported by a JSON-RPC provider with an error code.
/// </summary>
/// <remarks>
/// The JSON-RPC invocation was successful, but the provider returned an error.
/// </remarks>
[Serializable]
internal class JsonRpcProviderErrorException : JsonRpcException
{
    public JsonRpcProviderErrorException(JsonRpcError error)
    : base($"JSON-RPC provider error: {error.Code} - {error.Message}")
    {
        this.JsonRpcErrorCode = error.Code;
        this.JsonRpcErrorData = error.Data;

        if (this.Data != null)
        {
            this.Data["JsonRpcErrorCode"] = error.Code;
            this.Data["JsonRpcErrorData"] = error.Data;
        }
    }

    protected JsonRpcProviderErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    //

    public int JsonRpcErrorCode { get; }

    public object? JsonRpcErrorData { get; }
}
