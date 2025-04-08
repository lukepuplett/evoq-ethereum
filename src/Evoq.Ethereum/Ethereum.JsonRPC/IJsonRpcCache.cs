using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;

namespace Evoq.Ethereum.JsonRPC;

/// <summary>
/// A cache item for JSON-RPC responses.
/// </summary>
public interface IJsonRpcCacheItem
{
    /// <summary>
    /// The expiration time of the cache item.
    /// </summary>
    public DateTimeOffset Expiration { get; }

    /// <summary>
    /// The status code of the response.
    /// </summary>
    public HttpStatusCode StatusCode { get; }

    /// <summary>
    /// The JSON-RPC response.
    /// </summary>
    public string Json { get; }
}

/// <summary>
/// 
/// </summary>
/// <param name="Item">The item from the cache.</param>
/// <param name="IsFound">Whether the item was found in the cache.</param>
public record struct JsonRpcCacheResult(IJsonRpcCacheItem? Item, bool IsFound);

/// <summary>
/// A cache for JSON-RPC responses.
/// </summary>
public interface IJsonRpcCache
{
    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <param name="key">The key to get the value for.</param>
    /// <returns>The value from the cache.</returns>
    Task<JsonRpcCacheResult> GetAsync(string key);

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <param name="key">The key to set the value for.</param>
    /// <param name="value">The value to set in the cache.</param>
    Task SetAsync(string key, IJsonRpcCacheItem value);

    /// <summary>
    /// Gets the cache key for a request.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="requestJson">The JSON-RPC request.</param>
    /// <returns>The cache key.</returns>
    string GetCacheKey(string functionName, string requestJson);

    /// <summary>
    /// Creates a cache item.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="json">The JSON-RPC response.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="suggestedTTL">The expiration.</param>
    /// <param name="item">The cache item.</param>
    /// <returns>Whether the item was created.</returns>
    bool CreateItem(
        string functionName,
        string json,
        HttpStatusCode statusCode,
        TimeSpan suggestedTTL,
        out IJsonRpcCacheItem? item);
}

/// <summary>
/// A default implementation of <see cref="IJsonRpcCache"/> based on a dictionary with Load and Save methods.
/// </summary>
public class DefaultJsonRpcCache : IJsonRpcCache
{
    private readonly Dictionary<string, TimeSpan> cacheableMethods = new()
{
    { "eth_estimateGas".ToLowerInvariant(), TimeSpan.FromMinutes(3) },
    { "eth_gasPrice".ToLowerInvariant(), TimeSpan.FromMinutes(3) },    // Fixed from eth_getGasPrice
    { "eth_feeHistory".ToLowerInvariant(), TimeSpan.FromMinutes(3) },
};

    private readonly Dictionary<string, IJsonRpcCacheItem> cache = new();
    private readonly object lockObject = new();

    //

    /// <summary>
    /// Gets a value from the cache.
    /// </summary>
    /// <param name="key">The key to get the value for.</param>
    /// <returns>The value from the cache.</returns>
    public Task<JsonRpcCacheResult> GetAsync(string key)
    {
        lock (lockObject)
        {
            if (cache.TryGetValue(key, out var item))
            {
                if (item.Expiration > DateTimeOffset.UtcNow)
                {
                    return Task.FromResult(new JsonRpcCacheResult(item, true));
                }

                // Remove expired item
                cache.Remove(key);
            }

            return Task.FromResult(new JsonRpcCacheResult(null, false));
        }
    }

    /// <summary>
    /// Sets a value in the cache.
    /// </summary>
    /// <param name="key">The key to set the value for.</param>
    /// <param name="value">The value to set in the cache.</param>
    /// <returns>A task that completes when the value is set.</returns>
    public Task SetAsync(string key, IJsonRpcCacheItem value)
    {
        lock (lockObject)
        {
            cache[key] = value;
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// Gets the cache key for a request.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="requestJson">The JSON-RPC request.</param>
    /// <returns>The cache key.</returns>
    public string GetCacheKey(string functionName, string requestJson)
    {
        return $"{functionName}:{requestJson}";
    }

    /// <summary>
    /// Creates a cache item.
    /// </summary>
    /// <param name="functionName">The name of the function.</param>
    /// <param name="json">The JSON-RPC response.</param>
    /// <param name="statusCode">The status code.</param>
    /// <param name="suggestedTTL">The expiration.</param>
    /// <param name="item">The cache item.</param>
    /// <returns>Whether the item was created.</returns>
    public bool CreateItem(
        string functionName,
        string json,
        HttpStatusCode statusCode,
        TimeSpan suggestedTTL,
        out IJsonRpcCacheItem? item)
    {
        if (this.cacheableMethods.TryGetValue(functionName.ToLowerInvariant(), out var ttl))
        {
            item = new JsonRpcCacheItem
            {
                Json = json,
                StatusCode = statusCode,
                Expiration = DateTimeOffset.UtcNow + ttl
            };

            return true;
        }

        item = null;
        return false;
    }

    //

    /// <summary>
    /// Loads the cache from a stream.
    /// </summary>
    /// <param name="stream">The stream to load from.</param>
    public void Load(Stream stream)
    {
        lock (lockObject)
        {
            try
            {
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var items = JsonSerializer.Deserialize<List<CacheEntry>>(json);

                if (items == null) return;

                cache.Clear();
                foreach (var entry in items)
                {
                    if (entry.Expiration > DateTimeOffset.UtcNow)
                    {
                        cache[entry.Key] = new JsonRpcCacheItem
                        {
                            Json = entry.Json,
                            StatusCode = entry.StatusCode,
                            Expiration = entry.Expiration
                        };
                    }
                }
            }
            catch
            {
                // If loading fails, start with empty cache
                cache.Clear();
            }
        }
    }

    /// <summary>
    /// Saves the cache to a stream.
    /// </summary>
    /// <param name="stream">The stream to save to.</param>
    public void Save(Stream stream)
    {
        lock (lockObject)
        {
            try
            {
                var entries = new List<CacheEntry>();
                foreach (var kvp in cache)
                {
                    entries.Add(new CacheEntry
                    {
                        Key = kvp.Key,
                        Json = kvp.Value.Json,
                        StatusCode = kvp.Value.StatusCode,
                        Expiration = kvp.Value.Expiration
                    });
                }

                var json = JsonSerializer.Serialize(entries);
                var writer = new StreamWriter(stream);
                writer.Write(json);
                writer.Flush();
            }
            catch
            {
                // If saving fails, continue with in-memory cache
            }
        }
    }

    /// <summary>
    /// Clears the cache.
    /// </summary>
    public void Clear()
    {
        lock (lockObject)
        {
            cache.Clear();
        }
    }

    /// <summary>
    /// Clears expired items from the cache.
    /// </summary>
    public void ClearExpired()
    {
        lock (lockObject)
        {
            var now = DateTimeOffset.UtcNow;
            var expiredKeys = cache
                .Where(kvp => kvp.Value.Expiration <= now)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                cache.Remove(key);
            }
        }
    }

    private class CacheEntry
    {
        public string Key { get; set; } = "";
        public string Json { get; set; } = "";
        public HttpStatusCode StatusCode { get; set; }
        public DateTimeOffset Expiration { get; set; }
    }
}

internal class JsonRpcCacheItem : IJsonRpcCacheItem
{
    public DateTimeOffset Expiration { get; init; }

    public HttpStatusCode StatusCode { get; init; }

    public string Json { get; init; } = "";
}