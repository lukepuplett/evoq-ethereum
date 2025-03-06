using System.Collections.Generic;
using System.Text.Json.Serialization;
using Nethereum.RPC.Eth.Transactions;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Provides methods for creating pre-filled common Ethereum JSON-RPC requests.
/// </summary>
public static class JsonRpcRequestDtoFactory
{
    /// <summary>
    /// Creates a request for the eth_estimateGas method.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_estimateGas method.</returns>
    public static JsonRpcRequestDto CreateEstimateGasRequest(TransactionParamsDto transactionParams, int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_estimateGas",
            new List<object> { transactionParams },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_sendTransaction method.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_sendTransaction method.</returns>
    public static JsonRpcRequestDto CreateSendTransactionRequest(TransactionParamsDto transactionParams, int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_sendTransaction",
            new List<object> { transactionParams },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_sendRawTransaction method.
    /// </summary>
    /// <param name="signedTransactionHex">The RLP encoded signed transaction as a hex string (with 0x prefix).</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_sendRawTransaction method.</returns>
    public static JsonRpcRequestDto CreateSendRawTransactionRequest(string signedTransactionHex, int id = 1)
    {
        // Ensure the hex string has 0x prefix
        if (!signedTransactionHex.StartsWith("0x"))
        {
            signedTransactionHex = "0x" + signedTransactionHex;
        }

        return new JsonRpcRequestDto(
            "eth_sendRawTransaction",
            new List<object> { signedTransactionHex },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_call method.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_call method.</returns>
    public static JsonRpcRequestDto CreateCallRequest(TransactionParamsDto transactionParams, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_call",
            new List<object>
            {
                new EthCallParamDto {
                    Call = new EthCallParamObjectDto {
                        From = transactionParams.From,
                        To = transactionParams.To!,
                        Value = transactionParams.Value,
                        Input = transactionParams.Data },
                    BlockParameter = blockParameter }
            },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_getTransactionCount method.
    /// </summary>
    /// <param name="address">The address to get the transaction count for.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getTransactionCount method.</returns>
    public static JsonRpcRequestDto CreateGetTransactionCountRequest(string address, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_getTransactionCount",
            new List<object> { address, blockParameter },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_getBalance method.
    /// </summary>
    /// <param name="address">The address to get the balance for.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getBalance method.</returns>
    public static JsonRpcRequestDto CreateGetBalanceRequest(string address, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_getBalance",
            new List<object> { address, blockParameter },
            id);
    }
}