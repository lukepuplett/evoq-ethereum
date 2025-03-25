using System;
using System.Runtime.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a failure to make a JSON-RPC request or receive a valid response.
/// </summary>
/// <remarks>
/// This exception is thrown when a JSON-RPC request fails to be sent or when the response
/// is malformed or otherwise invalid. Successfully received errors are represented by
/// <see cref="JsonRpcProviderErrorException"/>.
/// </remarks>
[Serializable]
public class JsonRpcRequestFailedException : JsonRpcException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcRequestFailedException"/> class.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public JsonRpcRequestFailedException(string message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcRequestFailedException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>  
    public JsonRpcRequestFailedException(string message, Exception innerException) : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonRpcRequestFailedException"/> class with serialized data.
    /// </summary>
    /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
    /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
    protected JsonRpcRequestFailedException(SerializationInfo info, StreamingContext context) : base(info, context)
    {
    }
}