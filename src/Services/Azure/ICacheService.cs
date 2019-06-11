// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Miniblog.Core.Services
{
    public interface ICacheService
    {
        /// <summary>
        /// Gets or sets the JSON serialization settings.
        /// </summary>
        JsonSerializerSettings SerializationSettings { get; }

        /// <summary>
        /// Gets or sets the JSON deserialization settings.
        /// </summary>
        JsonSerializerSettings DeserializationSettings { get; }

        void Set<T>(string key, T value);
        Task SetAsync<T>(string key, T value, CancellationToken cancellationToken = default(CancellationToken));
        bool TryGetValue<T>(string key, out T result);
        T GetOrAdd<T>(string key, Func<T> addMethod);
        T Get<T>(string key);
        Task<T> GetAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken));
        void Refresh<T>(string key);
        Task RefreshAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken));
        void Remove<T>(string key);
        Task RemoveAsync<T>(string key, CancellationToken cancellationToken = default(CancellationToken));
    }
}