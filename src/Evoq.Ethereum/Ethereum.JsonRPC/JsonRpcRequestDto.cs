using System.Collections.Generic;
using System.Text.Json.Serialization;
using Evoq.Blockchain;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents a generic JSON-RPC request that can be used for any Ethereum API method.
/// </summary>
/// <remarks>
/// The JSON for a request is as follows:
/// <code>
/// {
///     "jsonrpc": "2.0",
///     "method": "eth_getBlockByHash",
///     "params": ["0x123", true],
///     "id": 1
/// }
/// </code>
/// The <c>id</c> is optional and defaults to 1. It is used to match the response to the request. The
/// specification for JSON-RPC for Ethereum can be found here:
/// <see href="https://ethereum.org/en/developers/docs/apis/json-rpc/"/>
/// </remarks>
public class JsonRpcRequestDto
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
    public List<object> Params { get; set; } = new List<object>();

    /// <summary>
    /// The request identifier.
    /// </summary>
    [JsonPropertyName("id")]
    public int Id { get; set; }

    /// <summary>
    /// Creates a new instance of the <see cref="JsonRpcRequestDto"/> class.
    /// </summary>
    public JsonRpcRequestDto()
    {
    }

    /// <summary>
    /// Creates a new instance of the <see cref="JsonRpcRequestDto"/> class with the specified parameters.
    /// </summary>
    /// <param name="method">The JSON-RPC method name.</param>
    /// <param name="params">The parameters for the request.</param>
    /// <param name="id">The request identifier.</param>
    public JsonRpcRequestDto(string method, List<object> @params, int id = 1)
    {
        Method = method;
        Params = @params;
        Id = id;
    }
}
