using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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
/// A result from a cache lookup.
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
    private readonly ILogger<DefaultJsonRpcCache>? logger;

    //

    /// <summary>
    /// Initializes a new instance.
    /// </summary>
    public DefaultJsonRpcCache(ILogger<DefaultJsonRpcCache>? logger = null)
    {
        this.logger = logger;
    }

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
            logger?.LogDebug("Cache lookup for key: {Key}", key);

            if (cache.TryGetValue(key, out var item))
            {
                if (item.Expiration > DateTimeOffset.UtcNow)
                {
                    logger?.LogDebug("Cache hit for key: {Key}, expires: {Expiration}", key, item.Expiration);
                    return Task.FromResult(new JsonRpcCacheResult(item, true));
                }

                logger?.LogDebug("Cache item expired for key: {Key}, expired: {Expiration}", key, item.Expiration);
                cache.Remove(key);
            }
            else
            {
                logger?.LogDebug("Cache miss for key: {Key}", key);
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
            logger?.LogDebug("Setting cache item - Key: {Key}, Expires: {Expiration}", key, value.Expiration);
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
        var key = $"{functionName}:{requestJson}";
        logger?.LogDebug("Generated cache key: {Key}", key);
        return key;
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
            var expiration = DateTimeOffset.UtcNow + ttl;
            logger?.LogDebug(
                "Creating cache item - Method: {Method}, TTL: {TTL}, Expires: {Expiration}",
                functionName, ttl, expiration);

            item = new JsonRpcCacheItem
            {
                Json = json,
                StatusCode = statusCode,
                Expiration = expiration
            };

            return true;
        }

        logger?.LogDebug("Method not cacheable: {Method}", functionName);
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
                logger?.LogDebug("Loading cache from stream");
                using var reader = new StreamReader(stream);
                var json = reader.ReadToEnd();
                var items = JsonSerializer.Deserialize<List<CacheEntry>>(json);

                if (items == null)
                {
                    logger?.LogDebug("No items found in cache stream");
                    return;
                }

                cache.Clear();
                var now = DateTimeOffset.UtcNow;
                var loadedCount = 0;
                var expiredCount = 0;

                foreach (var entry in items)
                {
                    if (entry.Expiration > now)
                    {
                        cache[entry.Key] = new JsonRpcCacheItem
                        {
                            Json = entry.Json,
                            StatusCode = entry.StatusCode,
                            Expiration = entry.Expiration
                        };
                        loadedCount++;
                    }
                    else
                    {
                        expiredCount++;
                    }
                }

                logger?.LogDebug(
                    "Cache loaded - Items loaded: {LoadedCount}, Items expired: {ExpiredCount}",
                    loadedCount, expiredCount);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to load cache from stream");
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
                logger?.LogDebug("Saving cache to stream");
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

                logger?.LogDebug("Saving {Count} cache entries", entries.Count);
                var json = JsonSerializer.Serialize(entries);
                var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true);
                writer.Write(json);
                writer.Flush();
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to save cache to stream");
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
            var count = cache.Count;
            cache.Clear();
            logger?.LogDebug("Cache cleared - Removed {Count} items", count);
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

            logger?.LogDebug("Cleared {Count} expired cache items", expiredKeys.Count);
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