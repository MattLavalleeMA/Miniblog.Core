// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services.Azure
{
    public interface IBlobStorageService
    {
        IEnumerable<Category> CategoryCache { get; }
        bool IsInitialized { get; }
        IEnumerable<PostBase> PostCache { get; }

        Task DeletePostByIdAsync(string postId, CancellationToken cancellationToken = default);

        Task<Post> GetPostByIdAsync(string postId, CancellationToken cancellationToken = default);

        Task InitializeAsync(CancellationToken cancellationToken = default);

        Task InitializeCategoryCache(CancellationToken cancellationToken = default);

        Task InitializeSummaryCache(CancellationToken cancellationToken = default);

        Task<Uri> SaveFileAsync(byte[] dataBytes, string fileName, CancellationToken cancellationToken = default);

        Task<Uri> SavePostAsync(Post post, CancellationToken cancellationToken = default);
    }
}