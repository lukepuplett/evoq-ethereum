using System.Text.Json.Serialization;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Provides factory methods for creating common Ethereum JSON-RPC requests.
/// </summary>
public static class JsonRpcRequestFactory
{
    /// <summary>
    /// Creates a request for the eth_estimateGas method.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_estimateGas method.</returns>
    public static JsonRpcRequestDto<TransactionParamsDto[]> CreateEstimateGasRequest(TransactionParamsDto transactionParams, int id = 1)
    {
        return new JsonRpcRequestDto<TransactionParamsDto[]>("eth_estimateGas", new[] { transactionParams }, id);
    }

    /// <summary>
    /// Creates a request for the eth_sendTransaction method.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_sendTransaction method.</returns>
    public static JsonRpcRequestDto<TransactionParamsDto[]> CreateSendTransactionRequest(TransactionParamsDto transactionParams, int id = 1)
    {
        return new JsonRpcRequestDto<TransactionParamsDto[]>("eth_sendTransaction", new[] { transactionParams }, id);
    }

    /// <summary>
    /// Creates a request for the eth_sendRawTransaction method.
    /// </summary>
    /// <param name="signedTransactionHex">The RLP encoded signed transaction as a hex string (with 0x prefix).</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_sendRawTransaction method.</returns>
    public static JsonRpcRequestDto<string[]> CreateSendRawTransactionRequest(string signedTransactionHex, int id = 1)
    {
        // Ensure the hex string has 0x prefix
        if (!signedTransactionHex.StartsWith("0x"))
        {
            signedTransactionHex = "0x" + signedTransactionHex;
        }

        return new JsonRpcRequestDto<string[]>("eth_sendRawTransaction", new[] { signedTransactionHex }, id);
    }

    /// <summary>
    /// Creates a request for the eth_call method.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_call method.</returns>
    public static JsonRpcRequestDto<object[]> CreateCallRequest(TransactionParamsDto transactionParams, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto<object[]>("eth_call", new object[] { transactionParams, blockParameter }, id);
    }

    /// <summary>
    /// Creates a request for the eth_getTransactionCount method.
    /// </summary>
    /// <param name="address">The address to get the transaction count for.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getTransactionCount method.</returns>
    public static JsonRpcRequestDto<object[]> CreateGetTransactionCountRequest(string address, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto<object[]>("eth_getTransactionCount", new object[] { address, blockParameter }, id);
    }

    /// <summary>
    /// Creates a request for the eth_getBalance method.
    /// </summary>
    /// <param name="address">The address to get the balance for.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getBalance method.</returns>
    public static JsonRpcRequestDto<object[]> CreateGetBalanceRequest(string address, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto<object[]>("eth_getBalance", new object[] { address, blockParameter }, id);
    }
}