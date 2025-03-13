using System;
using System.Net;
using System.Net.Http;
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
    public JsonRpcClient(Uri url, ILoggerFactory loggerFactory)
    {
        this.BaseAddress = url;
        this.httpClient.BaseAddress = url;
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
    /// <returns>The return data from the call as a hex value.</returns>
    /// <exception cref="JsonRpcNullResultException">Thrown when the JSON-RPC response has a null result.</exception>
    public async Task<Hex> CallAsync(EthCallParamObjectDto ethCallParams, string blockParameter = "latest", int id = 1)
    {
        var request = JsonRpcRequestDtoFactory.CreateCallRequest(ethCallParams, blockParameter, id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id));

        var hexString = response.Result;
        if (hexString == null)
        {
            throw new JsonRpcNullResultException("JSON-RPC response has null result");
        }

        return Hex.Parse(hexString, HexParseOptions.AllowOddLength);
    }

    public Task<Hex> EstimateGasAsync(TransactionParamsDto transactionParams, int id = 1)
    {
        throw new NotImplementedException();
    }

    public Task<Hex> GetBalanceAsync(EthereumAddress address, string blockParameter = "latest", int id = 1)
    {
        throw new NotImplementedException();
    }

    public Task<Hex> GetTransactionCountAsync(EthereumAddress address, string blockParameter = "latest", int id = 1)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Sends a raw transaction.
    /// </summary>
    /// <param name="signedTransaction">The signed transaction.</param>
    /// <param name="id">The ID of the request.</param>
    /// <returns>The hash of the transaction.</returns>
    /// <exception cref="JsonRpcNullResultException">
    /// Thrown when the JSON-RPC response has a null result.
    /// </exception>
    public async Task<Hex> SendRawTransactionAsync(Hex signedTransaction, int id = 1)
    {
        var request = JsonRpcRequestDtoFactory.CreateSendRawTransactionRequest(signedTransaction.ToString(), id);

        var response = await this.SendAsync<string>(request, new MethodInfo(request.Method, id));

        var hexString = response.Result;
        if (hexString == null)
        {
            throw new JsonRpcNullResultException("JSON-RPC response has null result");
        }

        return Hex.Parse(hexString, HexParseOptions.AllowOddLength);
    }

    public Task<Hex> SendTransactionAsync(TransactionParamsDto transactionParams, int id = 1)
    {
        throw new NotImplementedException();
    }

    public Task<Hex> GetCodeAsync(EthereumAddress address)
    {
        throw new NotImplementedException();
    }

    //

    private async Task<JsonRpcResponseDto<TResponseResult>> SendAsync<TResponseResult>(
        JsonRpcRequestDto request,
        MethodInfo methodInfo)
        where TResponseResult : class
    {
        var httpClient = this.CreateHttpClient(true);

        var caller = new JsonRpcProviderCaller<TResponseResult>(
            jsonSerializerOptions: new JsonSerializerOptions(),
            loggerFactory: this.loggerFactory);

        var response = await caller.CallAsync(
            request,
            httpClient,
            methodInfo,
            this.ShouldRetry ?? ((faultInfo) => Task.FromResult(false)),
            TimeSpan.FromSeconds(90),
            CancellationToken.None);

        if (response.Error != null)
        {
            throw new JsonRpcProvidedErrorException(response.Error);
        }

        return response;
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
}
