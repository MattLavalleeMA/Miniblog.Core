using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Miniblog.Core.Serialization;
using Newtonsoft.Json;

namespace Miniblog.Core.Services.Azure
{
    /// <summary>
    /// Defines the <see cref="CacheService" />, a strongly-typed wrapper for <see cref="IDistributedCache"/>.
    /// </summary>
    public class CacheService : ICacheService
    {
        /// <inheritdoc />
        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <inheritdoc />
        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }

        /// <summary>
        /// Defines the _cache
        /// </summary>
        private readonly IDistributedCache _cache;

        /// <summary>
        /// Defines the _telemetry
        /// </summary>
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="CacheService"/> class.
        /// </summary>
        /// <param name="serviceProvider"></param>
        /// <param name="logger"></param>
        public CacheService(IServiceProvider serviceProvider, ILogger<CacheService> logger)
        {
            _cache = serviceProvider.GetService<IDistributedCache>();
            _logger = logger;

            SerializationSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter()
                }
            };
            DeserializationSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                {
                    new Iso8601TimeSpanConverter()
                }
            };
        }

        /// <summary>
        /// Set a cache value
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="value">The value<see cref="T"/></param>
        public void Set<T>(string key, T value)
        {
            key = $"{typeof(T).Name}_{key}";
            string obj = JsonConvert.SerializeObject(value, SerializationSettings);
            try
            {
                _cache.Set(key, Encoding.UTF8.GetBytes(obj));
                _logger.LogTrace($"Cache Set: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cache Set: {key}", ex);
            }
        }

        /// <summary>
        /// SetAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="value">The value<see cref="T"/></param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default)
        {
            try
            {
                key = $"{typeof(T).Name}_{key}";
                string obj = JsonConvert.SerializeObject(value, SerializationSettings);
                await _cache.SetAsync(key, Encoding.UTF8.GetBytes(obj), cancellationToken);
                _logger.LogTrace($"Cache Set: {key}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cache Set: {key}", ex);
            }
        }

        /// <summary>
        /// Get
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <returns>The <see cref="T"/></returns>
        public T Get<T>(string key)
        {
            try
            {
                key = $"{typeof(T).Name}_{key}";
                _logger.LogTrace($"Cache Get: {key}");
                byte[] result = _cache.Get(key);
                if (result == null)
                {
                    return default;
                }

                string obj = Encoding.UTF8.GetString(result);
                return JsonConvert.DeserializeObject<T>(obj, DeserializationSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cache Get: {key}", ex);
                return default;
            }
        }

        /// <summary>
        /// GetAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Task{T}"/></returns>
        public async Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                key = $"{typeof(T).Name}_{key}";
                _logger.LogTrace($"Cache Set: {key}");
                byte[] result = await _cache.GetAsync(key, cancellationToken);
                if (result == null)
                {
                    return default;
                }

                string obj = Encoding.UTF8.GetString(result);
                return JsonConvert.DeserializeObject<T>(obj, DeserializationSettings);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Cache Get: {key}", ex);
                return default;
            }
        }

        /// <summary>
        /// The TryGetValue
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="result">The result<see cref="T"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public bool TryGetValue<T>(string key, out T result)
        {
            result = Get<T>(key);
            return result != null;
        }

        /// <summary>
        /// The GetOrAdd
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="addMethod">The addMethod<see cref="Func{T}"/></param>
        /// <returns>The <see cref="Task{T}"/></returns>
        public T GetOrAdd<T>(string key, Func<T> addMethod)
        {
            if (TryGetValue(key, out T result))
            {
                return result;
            }

            result = addMethod();
            if (result != null)
            {
                Set(key, result);
            }

            return result;
        }

        /// <summary>
        /// Refresh
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        public void Refresh<T>(string key)
        {
            key = $"{typeof(T).Name}_{key}";
            _logger.LogTrace($"Cache Refresh: {key}");
            _cache.Refresh(key);
        }

        /// <summary>
        /// RefreshAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task RefreshAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            key = $"{typeof(T).Name}_{key}";
            _logger.LogTrace($"Cache Refresh: {key}");
            return _cache.RefreshAsync(key, cancellationToken);
        }

        /// <summary>
        /// Remove
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        public void Remove<T>(string key)
        {
            key = $"{typeof(T).Name}_{key}";
            _logger.LogTrace($"Cache Remove: {key}");
            _cache.Remove(key);
        }

        /// <summary>
        /// RemoveAsync
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key">The key<see cref="string"/></param>
        /// <param name="cancellationToken">The cancellationToken<see cref="CancellationToken"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public Task RemoveAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            key = $"{typeof(T).Name}_{key}";
            _logger.LogTrace($"Cache Remove: {key}");
            return _cache.RemoveAsync(key, cancellationToken);
        }
    }
}