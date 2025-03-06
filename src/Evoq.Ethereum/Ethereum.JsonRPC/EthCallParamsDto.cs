using System.Text.Json.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents the transaction call object within eth_call parameters.
/// </summary>
public class EthCallParamObjectDto
{
    /// <summary>
    /// (Optional) The address the transaction is sent from (20 bytes, hex-encoded).
    /// </summary>
    [JsonPropertyName("from")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? From { get; set; }

    /// <summary>
    /// The address the transaction is directed to (20 bytes, hex-encoded).
    /// </summary>
    [JsonPropertyName("to")]
    public string To { get; set; } = EthereumAddress.Zero.ToString();

    /// <summary>
    /// (Optional) Integer of the gas provided for the transaction execution (hex-encoded).
    /// </summary>
    [JsonPropertyName("gas")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Gas { get; set; }

    /// <summary>
    /// (Optional) Integer of the gas price used for each paid gas (hex-encoded).
    /// </summary>
    [JsonPropertyName("gasPrice")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? GasPrice { get; set; }

    /// <summary>
    /// (Optional) Integer of the value sent with this transaction (hex-encoded).
    /// </summary>
    [JsonPropertyName("value")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Value { get; set; }

    /// <summary>
    /// (Optional) Hash of the method signature and encoded parameters (hex-encoded).
    /// </summary>
    [JsonPropertyName("input")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Input { get; set; }
}
