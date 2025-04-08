using System.Net;
using System.Text;
using Evoq.Ethereum.JsonRPC;

namespace Evoq.Ethereum.Tests.Ethereum.JsonRPC;

[TestClass]
public class DefaultJsonRpcCacheTests
{
    [TestMethod]
    public async Task Cache_StoresAndRetrievesItems()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();
        var key = cache.GetCacheKey("eth_gasPrice", "{}");
        IJsonRpcCacheItem? item;

        // Act
        var created = cache.CreateItem(
            "eth_gasPrice",
            "{\"result\":\"0x1234\"}",
            HttpStatusCode.OK,
            TimeSpan.FromMinutes(5),
            out item);

        Assert.IsTrue(created);
        Assert.IsNotNull(item);

        await cache.SetAsync(key, item!);
        var result = await cache.GetAsync(key);

        // Assert
        Assert.IsTrue(result.IsFound);
        Assert.IsNotNull(result.Item);
        Assert.AreEqual("{\"result\":\"0x1234\"}", result.Item!.Json);
    }

    [TestMethod]
    public async Task Cache_ExpiredItemsAreNotReturned()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();
        var key = cache.GetCacheKey("eth_gasPrice", "{}");

        var expiredItem = new JsonRpcCacheItem
        {
            Json = "{\"result\":\"0x1234\"}",
            StatusCode = HttpStatusCode.OK,
            Expiration = DateTimeOffset.UtcNow.AddMinutes(-1) // Expired 1 minute ago
        };

        // Act
        await cache.SetAsync(key, expiredItem);
        var result = await cache.GetAsync(key);

        // Assert
        Assert.IsFalse(result.IsFound);
        Assert.IsNull(result.Item);
    }

    [TestMethod]
    public void Cache_SaveAndLoadPreservesItems()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();
        var key = cache.GetCacheKey("eth_gasPrice", "{}");

        cache.CreateItem(
            "eth_gasPrice",
            "{\"result\":\"0x1234\"}",
            HttpStatusCode.OK,
            TimeSpan.FromMinutes(5),
            out var item);

        Assert.IsNotNull(item);
        cache.SetAsync(key, item!).Wait();

        using var saveStream = new MemoryStream();

        // Act
        cache.Save(saveStream);
        saveStream.Position = 0;

        var newCache = new DefaultJsonRpcCache();
        newCache.Load(saveStream);

        // Assert
        var result = newCache.GetAsync(key).Result;
        Assert.IsTrue(result.IsFound);
        Assert.IsNotNull(result.Item);
        Assert.AreEqual("{\"result\":\"0x1234\"}", result.Item!.Json);
    }

    [TestMethod]
    public void Cache_LoadingInvalidJsonStartsWithEmptyCache()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();
        var invalidJson = "{ invalid json }";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));

        // Act
        cache.Load(stream);

        // Assert - Should not throw and cache should be empty
        var result = cache.GetAsync("any-key").Result;
        Assert.IsFalse(result.IsFound);
    }

    [TestMethod]
    public async Task Cache_ClearRemovesAllItems()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();
        var key = cache.GetCacheKey("eth_gasPrice", "{}");

        cache.CreateItem(
            "eth_gasPrice",
            "{\"result\":\"0x1234\"}",
            HttpStatusCode.OK,
            TimeSpan.FromMinutes(5),
            out var item);

        Assert.IsNotNull(item);
        await cache.SetAsync(key, item!);

        // Act
        cache.Clear();

        // Assert
        var result = await cache.GetAsync(key);
        Assert.IsFalse(result.IsFound);
    }

    [TestMethod]
    public async Task Cache_ClearExpiredRemovesOnlyExpiredItems()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();
        var expiredKey = cache.GetCacheKey("eth_gasPrice", "{expired}");
        var validKey = cache.GetCacheKey("eth_gasPrice", "{valid}");

        var expiredItem = new JsonRpcCacheItem
        {
            Json = "{\"result\":\"expired\"}",
            StatusCode = HttpStatusCode.OK,
            Expiration = DateTimeOffset.UtcNow.AddMinutes(-1)
        };

        var validItem = new JsonRpcCacheItem
        {
            Json = "{\"result\":\"valid\"}",
            StatusCode = HttpStatusCode.OK,
            Expiration = DateTimeOffset.UtcNow.AddMinutes(5)
        };

        await cache.SetAsync(expiredKey, expiredItem);
        await cache.SetAsync(validKey, validItem);

        // Act
        cache.ClearExpired();

        // Assert
        var expiredResult = await cache.GetAsync(expiredKey);
        var validResult = await cache.GetAsync(validKey);

        Assert.IsFalse(expiredResult.IsFound);
        Assert.IsTrue(validResult.IsFound);
        Assert.AreEqual("{\"result\":\"valid\"}", validResult.Item!.Json);
    }

    [TestMethod]
    public void Cache_OnlyAllowsConfiguredMethods()
    {
        // Arrange
        var cache = new DefaultJsonRpcCache();

        // Act & Assert - Valid method
        var validCreated = cache.CreateItem(
            "eth_gasPrice",
            "{}",
            HttpStatusCode.OK,
            TimeSpan.FromMinutes(5),
            out var validItem);

        Assert.IsTrue(validCreated);
        Assert.IsNotNull(validItem);

        // Act & Assert - Invalid method
        var invalidCreated = cache.CreateItem(
            "eth_blockNumber",
            "{}",
            HttpStatusCode.OK,
            TimeSpan.FromMinutes(5),
            out var invalidItem);

        Assert.IsFalse(invalidCreated);
        Assert.IsNull(invalidItem);
    }
}
