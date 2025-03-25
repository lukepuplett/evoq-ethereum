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
public class JsonRpcProviderErrorException : JsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcProviderErrorException"/> class.
    /// </summary>
    /// <param name="error">The JSON-RPC error.</param>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcProviderErrorException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected JsonRpcProviderErrorException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }

    //

    /// <summary>
    /// Gets the JSON-RPC error code.
    /// </summary>
    public int JsonRpcErrorCode { get; }

    /// <summary>
    /// Gets the JSON-RPC error data.
    /// </summary>
    public object? JsonRpcErrorData { get; }
}
