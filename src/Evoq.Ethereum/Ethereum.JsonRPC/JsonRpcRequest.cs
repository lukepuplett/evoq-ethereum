using System.Text.Json.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a generic JSON-RPC request that can be used for any Ethereum API method.
/// </summary>
/// <typeparam name="TParams">The type of the parameters for the request.</typeparam>
public class JsonRpcRequest<TParams>
{
    /// <summary>
    /// The JSON-RPC version.
    /// </summary>
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";

    /// <summary>
    /// The method name.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = null!;

    /// <summary>
    /// The parameters for the request.
    /// </summary>
    [JsonPropertyName("params")]
    public TParams Params { get; set; } = default!;

    /// <summary>
    /// The request identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="JsonRpcRequest{TParams}"/> class.
    /// </summary>
    public JsonRpcRequest()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="JsonRpcRequest{TParams}"/> class with the specified parameters.
    /// </summary>
    /// <param name="method">The JSON-RPC method name.</param>
    /// <param name="params">The parameters for the request.</param>
    /// <param name="id">The request identifier.</param>
    public JsonRpcRequest(string method, TParams @params, int id = 1)
    {
        Method = method;
        Params = @params;
        Id = id;
    }
}