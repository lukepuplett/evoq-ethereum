using System;
using System.Net;
using System.Net.Http;
using System.Numerics;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A record that contains the information needed to send a JSON-RPC request.
/// </summary>
/// <param name="MethodName">The name of the method to call.</param>
/// <param name="Id">The ID of the request.</param>
public readonly record struct MethodInfo(
    string MethodName,
    int Id);

/// <summary>
/// A record that contains the information needed to send a JSON-RPC request.
/// </summary>
/// <param name="MethodInfo">The method info.</param>
/// <param name="Exception">The exception.</param>
/// <param name="HttpStatusCode">The HTTP status code.</param>
public readonly record struct MethodFaultInfo(
    MethodInfo MethodInfo,
    Exception Exception,
    HttpStatusCode HttpStatusCode);

/// <summary>
/// A JSON-RPC client.
/// </summary>
public class JsonRpcClient : IEthereumJsonRpc
{
    private HttpClient httpClient = new HttpClient();
    private ILoggerFactory loggerFactory;

    //

    /// <summary>
    /// Initializes a new instance of the JsonRpcClient class.
    /// </summary>
    public JsonRpcClient(Uri baseUrl, ILoggerFactory loggerFactory)
    {
        this.BaseAddress = baseUrl;
        this.httpClient.BaseAddress = baseUrl;
        this.loggerFactory = loggerFactory;
    }

    //

    /// <summary>
    /// Gets or sets the HTTP client factory.
    /// </summary>
    public IHttpClientFactory? HttpClientFactory { get; init; }

    /// <summary>
    /// Gets or sets the function to determine if a request should be retried.
    /// </summary>
    /// <remarks>
    /// The function takes the MethodFaultInfo of the request and returns a boolean indicating if the request should be retried.
    /// </remarks>
    public Func<MethodFaultInfo, Task<bool>>? ShouldRetry { get; init; }

    /// <summary>
    /// Gets or sets the base address of the JSON-RPC client.
    /// </summary>
    public Uri BaseAddress { get; }

    //

    /// <summary>
    /// Executes a call without creating a transaction on the blockchain.
    /// </summary>
    /// <param name="ethCallParams">The eth_call parameters, which looks similar to a transaction object but is not a transaction</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The return data from the call as a hex value.</returns>
    /// <exception cref="JsonRpcNullResultException">Thrown when the JSON-RPC response has a null result.</exception>
    public async Task<Hex> CallAsync(
        EthCallParamObjectDto ethCallParams,
        string blockParameter = "latest",
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateCallRequest(ethCallParams, blockParameter, id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Estimates the gas required to invoke a method on a contract.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The estimated gas required to invoke the method.</returns>
    public async Task<Hex> EstimateGasAsync(
        TransactionParamsDto transactionParams,
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateEstimateGasRequest(transactionParams, id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Gets the current gas price.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current gas price.</returns>
    public async Task<Hex> GasPriceAsync(int id = 1, CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateGasPriceRequest(id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Gets the balance of an Ethereum address at a specific block.
    /// </summary>
    /// <param name="address">The Ethereum address.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The balance of the Ethereum address as an EtherAmount in Wei.</returns>
    public async Task<EtherAmount> GetBalanceAsync(
        EthereumAddress address,
        string blockParameter = "latest",
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateGetBalanceRequest(
            address.ToString(),
            blockParameter,
            id);

        var response = await this.SendAsync<string>(
            request,
            new MethodInfo(request.Method, id),
            cancellationToken);

        // Parse the hex response and convert to EtherAmount
        var hexResponse = ParseHexResponse(response);

        return EtherAmount.FromWei(hexResponse);
    }

    /// <summary>
    /// Gets the transaction count of an Ethereum address at a specific block.
    /// </summary>
    /// <param name="address">The Ethereum address.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction count of the Ethereum address.</returns>
    public async Task<Hex> GetTransactionCountAsync(
        EthereumAddress address,
        string blockParameter = "latest",
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateGetTransactionCountRequest(address.ToString(), blockParameter, id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Sends a raw transaction.
    /// </summary>
    /// <param name="signedTransaction">The signed transaction.</param>
    /// <param name="id">The ID of the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The hash of the transaction.</returns>
    /// <exception cref="JsonRpcNullResultException">
    /// Thrown when the JSON-RPC response has a null result.
    /// </exception>
    public async Task<Hex> SendRawTransactionAsync(
        Hex signedTransaction,
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateSendRawTransactionRequest(signedTransaction.ToString(), id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Sends a transaction.
    /// </summary>
    /// <param name="transactionParams">The transaction parameters.</param>
    /// <param name="id">The ID of the request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The hash of the transaction.</returns>
    /// <exception cref="JsonRpcNullResultException">
    /// Thrown when the JSON-RPC response has a null result.
    /// </exception>
    public async Task<Hex> SendTransactionAsync(
        TransactionParamsDto transactionParams,
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateSendTransactionRequest(
            transactionParams,
            id);

        var response = await this.SendAsync<string>(
            request,
            new MethodInfo(request.Method, id),
            cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Gets the code of a contract at a specific Ethereum address.
    /// </summary>
    /// <param name="address">The Ethereum address.</param>
    /// <param name="blockParameter">The block parameter, defaults to "latest".</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The code of the contract.</returns>
    public async Task<Hex> GetCodeAsync(
        EthereumAddress address,
        string blockParameter = "latest",
        int id = 1,
        CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateGetCodeRequest(address.ToString(), blockParameter, id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Gets the chain ID.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The chain ID.</returns>
    public async Task<Hex> ChainIdAsync(
        int id = 1, CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateChainIdRequest(id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Gets the block number.
    /// </summary>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The block number.</returns>
    public async Task<Hex> BlockNumberAsync(
        int id = 1, CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateBlockNumberRequest(id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id), cancellationToken);

        return ParseHexResponse(response);
    }

    /// <summary>
    /// Gets a block by number or tag with transaction hashes.
    /// </summary>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The block information.</returns>
    public async Task<BlockDataDto<string>?> GetBlockByNumberWithTxHashesAsync(
        string blockNumberOrTag, int id = 1, CancellationToken cancellationToken = default)
    {
        // Ensure blockNumberOrTag is properly formatted
        blockNumberOrTag = FormatBlockParameter(blockNumberOrTag);

        var request = JsonRpcRequestDtoFactory.CreateGetBlockByNumberWithTxHashesRequest(blockNumberOrTag, id);

        var response = await this.SendAsync<BlockDataDto<string>>(request, new MethodInfo(request.Method, id), cancellationToken);

        return response.Result; // This can be null if the block doesn't exist
    }

    /// <summary>
    /// Gets a block by number or tag with full transaction objects.
    /// </summary>
    /// <param name="blockNumberOrTag">The block number or tag.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The block information.</returns>   
    public async Task<BlockDataDto<TransactionDataDto>?> GetBlockByNumberWithTxObjectsAsync(
        string blockNumberOrTag, int id = 1, CancellationToken cancellationToken = default)
    {
        // Ensure blockNumberOrTag is properly formatted
        blockNumberOrTag = FormatBlockParameter(blockNumberOrTag);

        var request = JsonRpcRequestDtoFactory.CreateGetBlockByNumberWithTxObjectsRequest(blockNumberOrTag, id);

        var response = await this.SendAsync<BlockDataDto<TransactionDataDto>>(request, new MethodInfo(request.Method, id), cancellationToken);

        return response.Result; // This can be null if the block doesn't exist
    }

    /// <summary>
    /// Gets the fee history.
    /// </summary>
    /// <param name="blockCount">The block count.</param>
    /// <param name="newestBlock">The newest block.</param>
    /// <param name="rewardPercentiles">The reward percentiles.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The fee history.</returns>
    public async Task<FeeHistoryDto?> FeeHistoryAsync(
        Hex blockCount, string newestBlock, double[] rewardPercentiles, int id = 1, CancellationToken cancellationToken = default)
    {
        // Ensure newestBlock is properly formatted
        newestBlock = FormatBlockParameter(newestBlock);

        var request = JsonRpcRequestDtoFactory.CreateFeeHistoryRequest(
            blockCount.ToString(trimLeadingZeroDigits: true),
            newestBlock,
            rewardPercentiles,
            id);

        var response = await this.SendAsync<FeeHistoryDto>(request, new MethodInfo(request.Method, id), cancellationToken);

        return response.Result;
    }

    /// <summary>
    /// Gets the transaction receipt for a given transaction hash.
    /// </summary>
    /// <param name="transactionHash">The hash of the transaction.</param>
    /// <param name="id">The request identifier.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The transaction receipt, or null if the transaction is not found or pending.</returns>
    public async Task<TransactionReceiptDto?> GetTransactionReceiptAsync(
        Hex transactionHash, int id = 1, CancellationToken cancellationToken = default)
    {
        var request = JsonRpcRequestDtoFactory.CreateGetTransactionReceiptRequest(
            transactionHash.ToString(), id);

        var response = await this.SendAsync<TransactionReceiptDto>(
            request, new MethodInfo(request.Method, id), cancellationToken);

        return response.Result; // Can be null if transaction is pending or not found
    }

    //

    private async Task<JsonRpcResponseDto<TResponseResult>> SendAsync<TResponseResult>(
        JsonRpcRequestDto request,
        MethodInfo methodInfo,
        CancellationToken cancellationToken = default)
        where TResponseResult : class
    {
        var httpClient = this.CreateHttpClient(true);

        var caller = new JsonRpcProviderCaller<TResponseResult>(
            jsonSerializerOptions: new JsonSerializerOptions(),
            loggerFactory: this.loggerFactory);

        try
        {
            return await caller.CallAsync(
                request,
                httpClient,
                methodInfo,
                this.ShouldRetry ?? ((faultInfo) => Task.FromResult(false)),
                TimeSpan.FromSeconds(90),
                cancellationToken);
        }
        catch (Exception ex)
            when (JsonRpcErrorHandler.IsExpectedException(ex, out var result))
        {
            throw result!;
        }
    }

    private static Hex ParseHexResponse(JsonRpcResponseDto<string> response)
    {
        var hexString = response.Result;
        if (hexString == null)
        {
            throw new JsonRpcNullResultException("JSON-RPC response has null result");
        }

        return Hex.Parse(hexString, HexParseOptions.AllowOddLength);
    }

    private HttpClient CreateHttpClient(bool fresh)
    {
        if (fresh)
        {
            if (this.HttpClientFactory == null)
            {
                this.httpClient = new HttpClient();
                this.httpClient.BaseAddress = this.BaseAddress;
            }
            else
            {
                this.httpClient = this.HttpClientFactory.CreateClient();
                this.httpClient.BaseAddress = this.BaseAddress;
            }
        }

        return this.httpClient;
    }

    private string FormatBlockParameter(string blockParameter)
    {
        // If it's a known tag, return it as is
        if (blockParameter == "latest" || blockParameter == "pending" || blockParameter == "earliest" || blockParameter == "safe" || blockParameter == "finalized")
        {
            return blockParameter;
        }

        // If it's a number without 0x prefix, add it
        if (!blockParameter.StartsWith("0x"))
        {
            // Try to parse as a number
            if (BigInteger.TryParse(blockParameter, out var bigInteger))
            {
                return "0x" + bigInteger.ToHexString(true);
            }

            // If it's already a hex string without 0x prefix
            return "0x" + blockParameter;
        }

        return blockParameter;
    }
}
