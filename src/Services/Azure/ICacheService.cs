using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

// ReSharper disable UnusedMemberInSuper.Global

namespace Miniblog.Core.Services.Azure
{
    public interface ICacheService
    {
        /// <summary>
        /// Gets or sets the JSON deserialization settings.
        /// </summary>
        JsonSerializerSettings DeserializationSettings { get; }

        /// <summary>
        /// Gets or sets the JSON serialization settings.
        /// </summary>
        JsonSerializerSettings SerializationSettings { get; }

        T Get<T>(string key);

        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default);

        T GetOrAdd<T>(string key, Func<T> addMethod);

        void Refresh<T>(string key);

        Task RefreshAsync<T>(string key, CancellationToken cancellationToken = default);

        void Remove<T>(string key);

        Task RemoveAsync<T>(string key, CancellationToken cancellationToken = default);

        void Set<T>(string key, T value);

        Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default);

        bool TryGetValue<T>(string key, out T result);
    }
}