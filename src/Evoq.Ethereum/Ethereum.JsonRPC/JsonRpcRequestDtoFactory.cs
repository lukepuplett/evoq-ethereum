using System.Collections.Generic;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Provides methods for creating pre-filled common Ethereum JSON-RPC requests.
/// </summary>
internal static class JsonRpcRequestDtoFactory
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
    /// Creates a request for the eth_gasPrice method.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_gasPrice method.</returns>
    public static JsonRpcRequestDto CreateGasPriceRequest(int id)
    {
        return new JsonRpcRequestDto(
            "eth_gasPrice",
            new List<object>(),
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
    /// <param name="ethCallParams">The eth_call parameters.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_call method.</returns>
    public static JsonRpcRequestDto CreateCallRequest(EthCallParamObjectDto ethCallParams, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_call",
            new List<object>
            {
                ethCallParams,
                blockParameter
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

    /// <summary>
    /// Creates a request for the eth_chainId method.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_chainId method.</returns>
    public static JsonRpcRequestDto CreateChainIdRequest(int id)
    {
        return new JsonRpcRequestDto(
            "eth_chainId",
            new List<object>(),
            id);
    }

    /// <summary>
    /// Creates a request for the eth_blockNumber method.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_blockNumber method.</returns>
    public static JsonRpcRequestDto CreateBlockNumberRequest(int id)
    {
        return new JsonRpcRequestDto(
            "eth_blockNumber",
            new List<object>(),
            id);
    }

    /// <summary>
    /// Creates a request for the eth_getBlockByNumber method with transaction hashes.
    /// </summary>
    /// <param name="blockNumberOrTag">The block number (hex) or tag (e.g., "latest", "pending").</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getBlockByNumber method.</returns>
    public static JsonRpcRequestDto CreateGetBlockByNumberWithTxHashesRequest(string blockNumberOrTag, int id)
    {
        return new JsonRpcRequestDto(
            "eth_getBlockByNumber",
            new List<object> { blockNumberOrTag, false },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_getBlockByNumber method with full transaction objects.
    /// </summary>
    /// <param name="blockNumberOrTag">The block number (hex) or tag (e.g., "latest", "pending").</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getBlockByNumber method.</returns>
    public static JsonRpcRequestDto CreateGetBlockByNumberWithTxObjectsRequest(string blockNumberOrTag, int id)
    {
        return new JsonRpcRequestDto(
            "eth_getBlockByNumber",
            new List<object> { blockNumberOrTag, true },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_feeHistory method.
    /// </summary>
    /// <param name="blockCount">Number of blocks to analyze.</param>
    /// <param name="newestBlock">The newest block to consider ("latest" or block number).</param>
    /// <param name="rewardPercentiles">Percentiles to sample for priority fees.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_feeHistory method.</returns>
    public static JsonRpcRequestDto CreateFeeHistoryRequest(string blockCount, string newestBlock, double[] rewardPercentiles, int id)
    {
        return new JsonRpcRequestDto(
            "eth_feeHistory",
            new List<object> { blockCount, newestBlock, rewardPercentiles },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_getCode method.
    /// </summary>
    /// <param name="address">The address of the contract to get the code for.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getCode method.</returns>
    public static JsonRpcRequestDto CreateGetCodeRequest(string address, string blockParameter = "latest", int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_getCode",
            new List<object> { address, blockParameter },
            id);
    }

    /// <summary>
    /// Creates a request for the eth_getTransactionReceipt method.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction to get the receipt for.</param>
    /// <param name="id">The request identifier.</param>
    /// <returns>A JSON-RPC request for the eth_getTransactionReceipt method.</returns>
    public static JsonRpcRequestDto CreateGetTransactionReceiptRequest(string transactionHash, int id = 1)
    {
        return new JsonRpcRequestDto(
            "eth_getTransactionReceipt",
            new List<object> { transactionHash },
            id);
    }
}