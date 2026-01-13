using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Evoq.Blockchain;
using Evoq.Ethereum.Chains;
using Evoq.Ethereum.JsonRPC;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace Evoq.Ethereum.Tests.Ethereum.Chains;

/// <summary>
/// Mock implementation of IHttpClientFactory for testing.
/// </summary>
public class MockHttpClientFactory : IHttpClientFactory
{
    private readonly HttpMessageHandler handler;
    public int CreateClientCallCount { get; private set; }

    public MockHttpClientFactory(HttpMessageHandler handler)
    {
        this.handler = handler;
    }

    public HttpClient CreateClient(string? name = null)
    {
        CreateClientCallCount++;
        return new HttpClient(handler) { BaseAddress = new Uri("http://test.example.com") };
    }
}

/// <summary>
/// Mock HTTP message handler that intercepts requests and returns predefined responses.
/// </summary>
public class MockJsonRpcHandler : DelegatingHandler
{
    private readonly Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc;
    public List<HttpRequestMessage> Requests { get; } = new();

    public MockJsonRpcHandler(Func<HttpRequestMessage, Task<HttpResponseMessage>> handlerFunc)
    {
        this.handlerFunc = handlerFunc;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        Requests.Add(request);
        var response = await handlerFunc(request);
        return response;
    }
}

[TestClass]
public class ChainHttpClientFactoryTests
{
    [TestMethod]
    public async Task CreateDefault_WithHttpClientFactory_UsesFactory()
    {
        // Arrange
        var mockHandler = new MockJsonRpcHandler(async request =>
        {
            // Read the request body to extract the JSON-RPC ID
            var requestBody = await request.Content!.ReadAsStringAsync();
            var requestJson = JsonSerializer.Deserialize<JsonElement>(requestBody);
            var requestId = requestJson.GetProperty("id").GetInt32();

            // Create response with matching ID
            var responseJson = $"{{\"jsonrpc\":\"2.0\",\"id\":{requestId},\"result\":\"0x1\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var mockFactory = new MockHttpClientFactory(mockHandler);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Act
        var chain = Chain.CreateDefault(1, new Uri("http://test.example.com"), loggerFactory, mockFactory);
        var context = new JsonRpcContext();
        var chainId = await chain.GetChainIdAsync(context);

        // Assert
        Assert.AreEqual(1, mockFactory.CreateClientCallCount, "Factory should be called once to create the HttpClient");
        Assert.IsTrue(mockHandler.Requests.Count > 0, "Mock handler should receive HTTP requests");
        Assert.AreEqual(1ul, chainId, "Should get chain ID 1 from mock response");
    }

    [TestMethod]
    public async Task CreateDefault_WithHttpClientFactory_FromEndpoint_UsesFactory()
    {
        // Arrange
        var mockHandler = new MockJsonRpcHandler(async request =>
        {
            // Read the request body to extract the JSON-RPC ID
            var requestBody = await request.Content!.ReadAsStringAsync();
            var requestJson = JsonSerializer.Deserialize<JsonElement>(requestBody);
            var requestId = requestJson.GetProperty("id").GetInt32();

            // Create response with matching ID and result
            var responseJson = $"{{\"jsonrpc\":\"2.0\",\"id\":{requestId},\"result\":\"0x89\"}}";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseJson, Encoding.UTF8, "application/json")
            };
        });

        var mockFactory = new MockHttpClientFactory(mockHandler);
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));
        var endpoint = new Endpoint("Hardhat", "Hardhat", "http://test.example.com", loggerFactory);

        // Act
        var chain = Chain.CreateDefault(endpoint, mockFactory);
        var context = new JsonRpcContext();
        var chainId = await chain.GetChainIdAsync(context);

        // Assert
        Assert.AreEqual(1, mockFactory.CreateClientCallCount, "Factory should be called once");
        Assert.IsTrue(mockHandler.Requests.Count > 0, "Mock handler should receive HTTP requests");
        Assert.AreEqual(137ul, chainId, "Should get chain ID 137 (0x89) from mock response");
    }

    [TestMethod]
    public async Task CreateDefault_WithoutHttpClientFactory_WorksNormally()
    {
        // Arrange
        // This test verifies backward compatibility - when no factory is provided,
        // the chain should still work (though it will fail without a real endpoint)
        // We'll test that it doesn't throw when creating the chain
        var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Warning));

        // Act & Assert - should not throw when creating the chain
        var chain = Chain.CreateDefault(1, new Uri("http://test.example.com"), loggerFactory, httpClientFactory: null);
        Assert.IsNotNull(chain, "Chain should be created successfully without factory");
        Assert.AreEqual(1ul, chain.ChainId, "Chain ID should be set correctly");
    }
}
