using System.Text.Json.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a generic JSON-RPC response from an Ethereum API.
/// </summary>
/// <typeparam name="TResult">The type of the result returned by the API.</typeparam>
public class JsonRpcResponseDto<TResult>
{
    /// <summary>
    /// The JSON-RPC version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// The result of the request. This will be null if there was an error.
    /// </summary>
    [JsonPropertyName("result")]
    public TResult? Result { get; set; }

    /// <summary>
    /// The error information if the request failed. This will be null if the request succeeded.
    /// </summary>
    [JsonPropertyName("error")]
    public JsonRpcError? Error { get; set; }

    /// <summary>
    /// The request identifier that this response corresponds to.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Gets a value indicating whether the request was successful.
    /// </summary>
    [JsonIgnore]
    public bool IsSuccess => Error == null;
}

/// <summary>
/// Represents an error in a JSON-RPC response.
/// </summary>
public class JsonRpcError
{
    /// <summary>
    /// The error code.
    /// </summary>
    [JsonPropertyName("code")]
    public int Code { get; set; }

    /// <summary>
    /// The error message.
    /// </summary>
    [JsonPropertyName("message")]
    public string Message { get; set; } = null!;

    /// <summary>
    /// Additional data about the error.
    /// </summary>
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}