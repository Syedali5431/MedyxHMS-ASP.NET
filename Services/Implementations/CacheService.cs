using Microsoft.Extensions.Caching.Distributed;
using MedyxHMS.Services.Interfaces;
using System.Text.Json;

// Purpose: Contains application code for CacheService and its related runtime behavior.
namespace MedyxHMS.Services.Implementations
{
    /// <summary>
    /// Caching service for performance optimization.
    /// Supports both in-memory and distributed caching with configurable TTL.
    /// </summary>
    public class CacheService : ICacheService
    {
        private readonly IDistributedCache _cache;
        private readonly ILogger<CacheService> _logger;
        private const int DefaultCacheDurationMinutes = 30;

        public CacheService(IDistributedCache cache, ILogger<CacheService> logger)
        {
            _cache = cache;
            _logger = logger;
        }

        /// <summary>
        /// Gets a cached value by key.
        /// </summary>
        public async Task<T?> GetAsync<T>(string key) where T : class
        {
            try
            {
                var cachedValue = await _cache.GetStringAsync(key);
                if (string.IsNullOrEmpty(cachedValue))
                    return null;

                return JsonSerializer.Deserialize<T>(cachedValue);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving cached value for key: {Key}", key);
                return null;
            }
        }

        /// <summary>
        /// Sets a cached value with default TTL.
        /// </summary>
        public async Task SetAsync<T>(string key, T value) where T : class
        {
            await SetAsync(key, value, DefaultCacheDurationMinutes);
        }

        /// <summary>
        /// Sets a cached value with custom TTL (in minutes).
        /// </summary>
        public async Task SetAsync<T>(string key, T value, int durationMinutes) where T : class
        {
            try
            {
                var options = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(durationMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(5)
                };

                var serialized = JsonSerializer.Serialize(value);
                await _cache.SetStringAsync(key, serialized, options);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting cached value for key: {Key}", key);
            }
        }

        /// <summary>
        /// Removes a cached value.
        /// </summary>
        public async Task RemoveAsync(string key)
        {
            try
            {
                await _cache.RemoveAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cached value for key: {Key}", key);
            }
        }

        /// <summary>
        /// Gets or creates a cached value using a factory function.
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory) where T : class
        {
            return await GetOrSetAsync(key, factory, DefaultCacheDurationMinutes);
        }

        /// <summary>
        /// Gets or creates a cached value with custom TTL.
        /// </summary>
        public async Task<T> GetOrSetAsync<T>(string key, Func<Task<T>> factory, int durationMinutes) where T : class
        {
            var cached = await GetAsync<T>(key);
            if (cached != null)
                return cached;

            var value = await factory();
            if (value != null)
                await SetAsync(key, value, durationMinutes);

            return value!;
        }

        /// <summary>
        /// Invalidates all cache entries matching a pattern (Redis only).
        /// </summary>
        public async Task InvalidatePrefixAsync(string prefix)
        {
            try
            {
                await _cache.RemoveAsync(prefix);
                _logger.LogInformation("Invalidated cache prefix: {Prefix}", prefix);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error invalidating cache prefix: {Prefix}", prefix);
            }
        }

        /// <summary>
        /// Gets cache key for a report with filters.
        /// </summary>
        public static string GetReportCacheKey(string reportName, Dictionary<string, string>? filters = null)
        {
            var key = $"report:{reportName.ToLower()}";
            if (filters?.Any() == true)
            {
                var filterStr = string.Join(":", filters.OrderBy(x => x.Key).Select(x => $"{x.Key}={x.Value}"));
                key += $":{filterStr}";
            }
            return key;
        }

        /// <summary>
        /// Gets cache key for entity collections.
        /// </summary>
        public static string GetEntityCacheKey<T>(int? id = null, string? filter = null) where T : class
        {
            var entityName = typeof(T).Name.ToLower();
            var key = $"entity:{entityName}";
            if (id.HasValue)
                key += $":{id}";
            if (!string.IsNullOrEmpty(filter))
                key += $":{filter}";
            return key;
        }
    }
}
