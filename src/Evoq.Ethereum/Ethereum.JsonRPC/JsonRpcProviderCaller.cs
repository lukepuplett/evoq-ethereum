using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// Makes the actual JSON-RPC request to the provider and deserializes the response.
/// </summary>
public class JsonRpcProviderCaller<TResponseResult>
    where TResponseResult : class
{
    private readonly JsonSerializerOptions jsonSerializerOptions;
    private readonly ILogger? logger;
    private readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(30);
    private readonly IJsonRpcCache cache = new DefaultJsonRpcCache();

    //

    /// <summary>
    /// Initializes a new instance of the JsonRpcProviderCaller class.
    /// </summary>
    /// <param name="jsonSerializerOptions">The JSON serializer options.</param>
    /// <param name="loggerFactory">The logger factory.</param>
    /// <param name="cache">The cache to use for the interaction.</param>
    public JsonRpcProviderCaller(
        JsonSerializerOptions? jsonSerializerOptions = null,
        ILoggerFactory? loggerFactory = null,
        IJsonRpcCache? cache = null)
    {
        this.jsonSerializerOptions = jsonSerializerOptions ?? new JsonSerializerOptions();
        this.logger = loggerFactory?.CreateLogger("JsonRpcProviderCaller");

        if (cache != null)
        {
            this.cache = cache;
        }
    }

    //

    /// <summary>
    /// Calls a JSON-RPC method with the specified request.
    /// </summary>
    /// <param name="request">The JSON-RPC request.</param>
    /// <param name="httpClient">The HTTP client to use for the request.</param>
    /// <param name="shouldRetry">A function that determines whether a failed request should be retried.</param>
    /// <param name="timeout">The timeout for the request. If null, the default timeout of 30 seconds is used.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>The JSON-RPC response.</returns>
    public async Task<JsonRpcResponseDto<TResponseResult>> CallAsync(
        JsonRpcRequestDto request,
        HttpClient httpClient,
        Func<MethodFaultInfo, Task<bool>> shouldRetry,
        TimeSpan? timeout,
        CancellationToken cancellationToken)
    {
        var methodInfo = new MethodInfo(request.Method, request.Id);
        int attemptCount = 0;

        using (this.logger?.BeginScope("[Method: {MethodName}, ID: {Id}]", methodInfo.MethodName, methodInfo.Id))
        {
            // Set up timeout
            TimeSpan actualTimeout = timeout ?? defaultTimeout;
            this.logger?.LogDebug("Using timeout of {TimeoutSeconds} seconds", actualTimeout.TotalSeconds);

            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();

                attemptCount++;

                this.logger?.LogDebug("Attempt {AttemptCount} for JSON-RPC method {MethodName} (ID: {Id})",
                     attemptCount, methodInfo.MethodName, methodInfo.Id);

                // Create a timeout cancellation token source
                using var timeoutCts = new CancellationTokenSource(actualTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    timeoutCts.Token, cancellationToken);

                try
                {
                    // Serialize the request
                    var requestJson = JsonSerializer.Serialize(request, this.jsonSerializerOptions);
                    this.logger?.LogTrace("JSON-RPC request: {RequestJson}", requestJson);

                    var cacheKey = this.cache.GetCacheKey(methodInfo.MethodName, requestJson);
                    var (cacheResult, isFound) = await this.cache.GetAsync(cacheKey);

                    if (isFound && cacheResult!.Expiration.IsInTheFuture())
                    {
                        this.logger?.LogDebug("Cache hit for method {MethodName}: {StatusCode}", methodInfo.MethodName, cacheResult.StatusCode);

                        if (cacheResult.StatusCode == HttpStatusCode.OK) // Could also support other non-negative status codes
                        {
                            var cachedDto = JsonSerializer.Deserialize<JsonRpcResponseDto<TResponseResult>>(
                                cacheResult.Json, this.jsonSerializerOptions);

                            if (cachedDto != null)
                            {
                                this.logger?.LogDebug("Returning cached response for method {MethodName}", methodInfo.MethodName);

                                return cachedDto;
                            }
                        }
                    }

                    var content = new StringContent(requestJson, Encoding.UTF8, "application/json");

                    // Add compression headers
                    if (!httpClient.DefaultRequestHeaders.Contains("Accept-Encoding"))
                    {
                        httpClient.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
                        this.logger?.LogDebug("Added compression headers to request");
                    }

                    // Send the request
                    var requestRelativeUrl = "";
                    var requestRelativeUri = new Uri(requestRelativeUrl, UriKind.Relative);

                    this.logger?.LogDebug("Sending request to {BaseAddress}{RelativeUri}",
                        httpClient.BaseAddress, requestRelativeUri);

                    var response = await httpClient.PostAsync(requestRelativeUri, content, linkedCts.Token);

                    // Handle HTTP error status codes
                    if (!response.IsSuccessStatusCode)
                    {
                        this.logger?.LogWarning("HTTP error response: {StatusCode} for method {MethodName}",
                            response.StatusCode, methodInfo.MethodName);

                        var faultInfo = new MethodFaultInfo(
                            methodInfo,
                            new HttpRequestException($"Request failed with status code {response.StatusCode}"),
                            response.StatusCode);

                        if (shouldRetry != null && await shouldRetry(faultInfo))
                        {
                            this.logger?.LogInformation("Retrying after HTTP error {StatusCode} for method {MethodName}",
                                response.StatusCode, methodInfo.MethodName);

                            continue; // Retry the request
                        }

                        this.logger?.LogError("Request failed with status code {StatusCode} for method {MethodName}",
                            response.StatusCode, methodInfo.MethodName);

                        throw new JsonRpcRequestFailedException($"Request failed with status code {response.StatusCode}");
                    }

                    // Log compression info if available
                    if (response.Content.Headers.ContentEncoding.Count > 0)
                    {
                        this.logger?.LogDebug("Response compressed using: {CompressionMethod}",
                            string.Join(", ", response.Content.Headers.ContentEncoding));
                    }

                    // Read and decompress the response if needed
                    string responseBodyStr;
                    if (response.Content.Headers.ContentEncoding.Contains("gzip"))
                    {
                        using var compressedStream = await response.Content.ReadAsStreamAsync();
                        using var gzipStream = new GZipStream(compressedStream, CompressionMode.Decompress);
                        using var reader = new StreamReader(gzipStream);
                        responseBodyStr = await reader.ReadToEndAsync();
                    }
                    else
                    {
                        responseBodyStr = await response.Content.ReadAsStringAsync();
                    }

                    this.logger?.LogTrace("JSON-RPC response: {ResponseJson}", responseBodyStr);
                    this.logger?.LogInformation(
                        "MethodName: {MethodName} (ID: {Id}) Response: Status={StatusCode} {StatusReason}, Type={ContentType}, Length={Length} bytes",
                        methodInfo.MethodName,
                        methodInfo.Id,
                        (int)response.StatusCode,
                        response.ReasonPhrase,
                        response.Content.Headers.ContentType,
                        responseBodyStr.Length);

                    if (this.cache.CreateItem(
                        methodInfo.MethodName,
                        responseBodyStr,
                        response.StatusCode,
                        TimeSpan.FromMinutes(5),
                        out var item))
                    {
                        await this.cache.SetAsync(cacheKey, item!);
                    }

                    var responseDto = JsonSerializer.Deserialize<JsonRpcResponseDto<TResponseResult>>(
                        responseBodyStr, this.jsonSerializerOptions);

                    // After deserialization, add more detailed logging:
                    if (responseDto == null)
                    {
                        this.logger?.LogWarning("Null response for method {MethodName}", methodInfo.MethodName);

                        var faultInfo = new MethodFaultInfo(
                            methodInfo,
                            new JsonRpcRequestFailedException("Request failed with null response"),
                            response.StatusCode);

                        if (shouldRetry != null && await shouldRetry(faultInfo))
                        {
                            this.logger?.LogInformation("Retrying after null response for method {MethodName}",
                                methodInfo.MethodName);

                            continue; // Retry the request
                        }

                        this.logger?.LogError("Request failed with null response for method {MethodName}",
                            methodInfo.MethodName);

                        throw new JsonRpcRequestFailedException("Request failed with null response");
                    }
                    else
                    {
                        this.logger?.LogDebug("Deserialized Response - ID: {Id}, Has Error: {HasError}, Has Result: {HasResult}",
                            responseDto.Id,
                            responseDto.Error != null,
                            responseDto.Result != null);

                        if (responseDto.Result != null)
                        {
                            this.logger?.LogDebug("Result Type: {ResultType}", responseDto.Result.GetType().Name);
                            this.logger?.LogDebug("Result Value: {@Result}", responseDto.Result);
                        }
                    }

                    // Validate response ID matches request ID
                    if (responseDto.Id != methodInfo.Id)
                    {
                        this.logger?.LogWarning("Response ID {ResponseId} doesn't match request ID {RequestId} for method {MethodName}",
                            responseDto.Id, methodInfo.Id, methodInfo.MethodName);

                        var idMismatchException = new JsonRpcRequestFailedException(
                            $"Response ID {responseDto.Id} doesn't match request ID {methodInfo.Id}");

                        var faultInfo = new MethodFaultInfo(
                            methodInfo,
                            idMismatchException,
                            response.StatusCode);

                        if (shouldRetry != null && await shouldRetry(faultInfo))
                        {
                            this.logger?.LogInformation("Retrying after ID mismatch for method {MethodName}",
                                methodInfo.MethodName);

                            continue; // Retry the request
                        }

                        this.logger?.LogError("Response ID mismatch for method {MethodName}: expected {ExpectedId}, got {ActualId}",
                            methodInfo.MethodName, methodInfo.Id, responseDto.Id);

                        throw idMismatchException;
                    }

                    // Check if the response contains an error
                    if (responseDto.Error != null)
                    {
                        this.logger?.LogWarning(
                            "JSON-RPC Error | Method: {MethodName} | Code: {ErrorCode} | Message: {ErrorMessage}",
                            methodInfo.MethodName,
                            responseDto.Error.Code,
                            responseDto.Error.Message);

                        var providerException = new JsonRpcProviderErrorException(responseDto.Error);

                        var faultInfo = new MethodFaultInfo(
                            methodInfo,
                            providerException,
                            response.StatusCode);

                        if (shouldRetry != null && await shouldRetry(faultInfo))
                        {
                            this.logger?.LogInformation(
                                "Retrying request | Method: {MethodName} | Previous error code: {ErrorCode}",
                                methodInfo.MethodName,
                                responseDto.Error.Code);

                            continue; // Retry the request
                        }

                        // If we shouldn't retry, throw the exception
                        this.logger?.LogError(
                            "JSON-RPC Error | Method: {MethodName} | Code: {ErrorCode} | Message: {ErrorMessage}",
                            methodInfo.MethodName,
                            responseDto.Error.Code,
                            responseDto.Error.Message);

                        throw providerException;
                    }

                    this.logger?.LogDebug("Successfully completed JSON-RPC method {MethodName} (ID: {Id}) in {AttemptCount} attempts",
                        methodInfo.MethodName, methodInfo.Id, attemptCount);

                    return responseDto;
                }
                catch (OperationCanceledException ex)
                {
                    // Check if it was a timeout or a regular cancellation
                    if (timeoutCts.Token.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
                    {
                        this.logger?.LogWarning("Request timed out after {TimeoutSeconds} seconds for method {MethodName}",
                            actualTimeout.TotalSeconds, methodInfo.MethodName);

                        var timeoutException = new TimeoutException(
                            $"Request timed out after {actualTimeout.TotalSeconds} seconds", ex);

                        var faultInfo = new MethodFaultInfo(
                            methodInfo,
                            timeoutException,
                            HttpStatusCode.RequestTimeout);

                        if (shouldRetry != null && await shouldRetry(faultInfo))
                        {
                            this.logger?.LogInformation("Retrying after timeout for method {MethodName}",
                                methodInfo.MethodName);

                            continue; // Retry the request
                        }

                        this.logger?.LogError("Request timed out for method {MethodName}", methodInfo.MethodName);
                        throw new JsonRpcRequestFailedException($"Request timed out after {actualTimeout.TotalSeconds} seconds", timeoutException);
                    }
                    else
                    {
                        this.logger?.LogInformation("Request cancelled for method {MethodName}", methodInfo.MethodName);
                        // Propagate regular cancellation exceptions directly
                        throw;
                    }
                }
                catch (HttpRequestException ex)
                {
                    this.logger?.LogWarning(ex, "HTTP request exception for method {MethodName}: {Message}",
                        methodInfo.MethodName, ex.Message);

                    var faultInfo = new MethodFaultInfo(
                        methodInfo,
                        ex,
                        HttpStatusCode.InternalServerError); // Default status code for network errors

                    if (shouldRetry != null && !cancellationToken.IsCancellationRequested && await shouldRetry(faultInfo))
                    {
                        this.logger?.LogInformation("Retrying after HTTP request exception for method {MethodName}",
                            methodInfo.MethodName);
                        continue; // Retry the request
                    }

                    this.logger?.LogError(ex, "Request failed due to network error for method {MethodName}",
                        methodInfo.MethodName);

                    throw new JsonRpcRequestFailedException("Request failed due to network error", ex);
                }
                catch (JsonException ex)
                {
                    this.logger?.LogWarning(ex, "JSON deserialization exception for method {MethodName}: {Message}",
                        methodInfo.MethodName, ex.Message);

                    var faultInfo = new MethodFaultInfo(
                        methodInfo,
                        ex,
                        HttpStatusCode.OK); // We got a response but couldn't parse it

                    if (shouldRetry != null && !cancellationToken.IsCancellationRequested && await shouldRetry(faultInfo))
                    {
                        this.logger?.LogInformation("Retrying after JSON deserialization exception for method {MethodName}",
                            methodInfo.MethodName);
                        continue; // Retry the request
                    }

                    this.logger?.LogError(ex, "Failed to deserialize JSON-RPC response for method {MethodName}",
                        methodInfo.MethodName);

                    throw new JsonRpcRequestFailedException("Failed to deserialize JSON-RPC response", ex);
                }
                catch (Exception ex) when (ex is not JsonRpcRequestFailedException)
                {
                    this.logger?.LogWarning(ex, "Unexpected exception for method {MethodName}: {Message}",
                        methodInfo.MethodName, ex.Message);

                    var faultInfo = new MethodFaultInfo(
                        methodInfo,
                        ex,
                        HttpStatusCode.InternalServerError); // Default status code for unexpected errors

                    if (shouldRetry != null && !cancellationToken.IsCancellationRequested && await shouldRetry(faultInfo))
                    {
                        this.logger?.LogInformation("Retrying after unexpected exception for method {MethodName}",
                            methodInfo.MethodName);
                        continue; // Retry the request
                    }

                    this.logger?.LogError(ex, "Unexpected error during JSON-RPC request for method {MethodName}",
                        methodInfo.MethodName);

                    throw new JsonRpcRequestFailedException("Unexpected error during JSON-RPC request", ex);
                }
            }
        }
    }
}
