using System;
using System.Linq;
using System.Numerics;
using System.Text.Json.Serialization;
using Evoq.Ethereum.RLP;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Represents the transaction parameters used in Ethereum JSON-RPC methods like eth_call, eth_estimateGas, and eth_sendTransaction.
/// </summary>
/// <remarks>
/// This class represents the JSON format of transaction parameters used in JSON-RPC requests.
/// It is NOT used for eth_sendRawTransaction, which takes a single hex string parameter of an RLP-encoded signed transaction.
/// 
/// Common usage:
/// - eth_estimateGas: Estimate gas for a transaction without sending it
/// - eth_call: Execute a read-only call to a contract
/// - eth_sendTransaction: Send a transaction (node signs it with an unlocked account)
/// </remarks>
public class TransactionParamsDto
{
    /// <summary>
    /// The address the transaction is sent from.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// The address the transaction is directed to.
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }

    /// <summary>
    /// The compiled code of a contract OR the hash of the invoked method signature and encoded parameters.
    /// </summary>
    [JsonPropertyName("data")]
    public string? Data { get; set; }

    /// <summary>
    /// Integer of the gas provided for the transaction execution (hex format).
    /// </summary>
    [JsonPropertyName("gas")]
    public string? Gas { get; set; }

    /// <summary>
    /// Integer of the gasPrice used for each paid gas (hex format).
    /// For EIP-1559 transactions, use maxFeePerGas and maxPriorityFeePerGas instead.
    /// </summary>
    [JsonPropertyName("gasPrice")]
    public string? GasPrice { get; set; }

    /// <summary>
    /// Integer of the value sent with this transaction (hex format).
    /// </summary>
    [JsonPropertyName("value")]
    public string? Value { get; set; }

    /// <summary>
    /// Integer of the transaction's nonce (hex format).
    /// </summary>
    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    /// <summary>
    /// The maximum fee per gas (EIP-1559 transactions only).
    /// </summary>
    [JsonPropertyName("maxFeePerGas")]
    public string? MaxFeePerGas { get; set; }

    /// <summary>
    /// The maximum priority fee per gas (tip for miners/validators, EIP-1559 transactions only).
    /// </summary>
    [JsonPropertyName("maxPriorityFeePerGas")]
    public string? MaxPriorityFeePerGas { get; set; }

    /// <summary>
    /// The chain ID of the network (hex format).
    /// </summary>
    [JsonPropertyName("chainId")]
    public string? ChainId { get; set; }

    //

    /// <summary>
    /// Creates a TransactionParams object for a legacy transaction.
    /// </summary>
    /// <param name="from">The sender address (hex format with 0x prefix).</param>
    /// <param name="to">The recipient address (hex format with 0x prefix).</param>
    /// <param name="data">The transaction data (hex format with 0x prefix).</param>
    /// <param name="gas">The gas limit (hex format with 0x prefix).</param>
    /// <param name="gasPrice">The gas price (hex format with 0x prefix).</param>
    /// <param name="value">The transaction value (hex format with 0x prefix).</param>
    /// <param name="nonce">The transaction nonce (hex format with 0x prefix).</param>
    /// <returns>A TransactionParams object for a legacy transaction.</returns>
    public static TransactionParamsDto CreateLegacy(
        string from,
        string? to = null,
        string? data = null,
        string? gas = null,
        string? gasPrice = null,
        string? value = null,
        string? nonce = null)
    {
        return new TransactionParamsDto
        {
            From = from,
            To = to,
            Data = data,
            Gas = gas,
            GasPrice = gasPrice,
            Value = value,
            Nonce = nonce
        };
    }

    /// <summary>
    /// Creates a TransactionParams object for an EIP-1559 transaction.
    /// </summary>
    /// <param name="from">The sender address (hex format with 0x prefix).</param>
    /// <param name="to">The recipient address (hex format with 0x prefix).</param>
    /// <param name="data">The transaction data (hex format with 0x prefix).</param>
    /// <param name="gas">The gas limit (hex format with 0x prefix).</param>
    /// <param name="maxFeePerGas">The maximum fee per gas (hex format with 0x prefix).</param>
    /// <param name="maxPriorityFeePerGas">The maximum priority fee per gas (hex format with 0x prefix).</param>
    /// <param name="value">The transaction value (hex format with 0x prefix).</param>
    /// <param name="nonce">The transaction nonce (hex format with 0x prefix).</param>
    /// <param name="chainId">The chain ID (hex format with 0x prefix).</param>
    /// <returns>A TransactionParams object for an EIP-1559 transaction.</returns>
    public static TransactionParamsDto CreateEIP1559(
        string from,
        string? to = null,
        string? data = null,
        string? gas = null,
        string? maxFeePerGas = null,
        string? maxPriorityFeePerGas = null,
        string? value = null,
        string? nonce = null,
        string? chainId = null)
    {
        return new TransactionParamsDto
        {
            From = from,
            To = to,
            Data = data,
            Gas = gas,
            MaxFeePerGas = maxFeePerGas,
            MaxPriorityFeePerGas = maxPriorityFeePerGas,
            Value = value,
            Nonce = nonce,
            ChainId = chainId
        };
    }

    /// <summary>
    /// Creates a TransactionParams object for estimating gas.
    /// </summary>
    /// <param name="from">The sender address (hex format with 0x prefix).</param>
    /// <param name="to">The recipient address (hex format with 0x prefix).</param>
    /// <param name="data">The transaction data (hex format with 0x prefix).</param>
    /// <param name="value">The transaction value (hex format with 0x prefix).</param>
    /// <returns>A TransactionParams object for estimating gas.</returns>
    public static TransactionParamsDto CreateForGasEstimation(
        string from,
        string? to = null,
        string? data = null,
        string? value = null)
    {
        return new TransactionParamsDto
        {
            From = from,
            To = to,
            Data = data,
            Value = value
        };
    }
}