using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using MimeMapping;
using Miniblog.Core.Configuration;
using Miniblog.Core.Extensions;
using Miniblog.Core.Models;
using Newtonsoft.Json;

namespace Miniblog.Core.Services.Azure
{
    public class BlobStorageService : IBlobStorageService
    {
        private const string CATEGORY_CACHE_FILE_NAME = "category-cache.json";
        private const string JSON_FILE_EXT = ".json";
        private const string POST_BLOB_PREFIX = "post-";
        private const string SUMMARY_CACHE_FILE_NAME = "summary-cache.json";

        private readonly ICacheService _cacheService;
        private readonly CloudBlobContainer _filesContainer;
        private readonly CloudBlobContainer _postsContainer;

        private ConcurrentDictionary<string, Category> _categoryCache = new ConcurrentDictionary<string, Category>();
        private ConcurrentDictionary<string, PostBase> _postSummaryCache = new ConcurrentDictionary<string, PostBase>();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Checked with Ensure")]
        public BlobStorageService(IOptionsMonitor<BlobStorageSettings> blobStorageSettings, ICacheService cacheService)
        {
            Ensure.Argument.IsNotNull(blobStorageSettings);

            BlobStorageSettings blobStorageSettings1 = blobStorageSettings.CurrentValue;
            _cacheService = cacheService;

            CloudBlobClient blobClient = CloudStorageAccount.Parse(blobStorageSettings1.ConnectionString)
                .CreateCloudBlobClient();

            _postsContainer = blobClient.GetContainerReference(blobStorageSettings1.PostsContainerName);
            _filesContainer = blobClient.GetContainerReference(blobStorageSettings1.FilesContainerName);

            InitializeAsync()
                .GetAwaiter()
                .GetResult();
        }

        public IEnumerable<Category> CategoryCache => _categoryCache.Values;
        public bool IsInitialized { get; private set; }

        public IEnumerable<PostBase> PostCache => _postSummaryCache.Values;

        private static string GetPostBlobName(string postId) => $"{POST_BLOB_PREFIX}{postId}{JSON_FILE_EXT}";

        private async Task UpdateCategoryCacheBlobAsync(CancellationToken cancellationToken = default)
        {
            CloudBlockBlob index = _postsContainer.GetBlockBlobReference(CATEGORY_CACHE_FILE_NAME);
            index.Properties.ContentType = KnownMimeTypes.Json;
            await index.UploadTextAsync(JsonConvert.SerializeObject(_categoryCache),
                    Encoding.UTF8,
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private async Task UpdatePostCategoriesAsync(PostBase post, bool saveCache, CancellationToken cancellationToken)
        {
            // Remove existing categories that are no longer associated or used
            foreach (Category category in _categoryCache.Values.Where(c => c.Posts.Contains(post.Id)))
            {
                if (post.Categories.Contains(category.Label))
                {
                    continue;
                }

                category.Posts.Remove(post.Id);

                // If there are no more posts for this category, remove it entirely
                if (category.Posts.Count == 0)
                {
                    _categoryCache.TryRemove(category.Label, out _);
                }
                else
                {
                    _categoryCache[category.Label] = category;
                }
            }

            // Update categories for this post
            foreach (string category in post.Categories)
            {
                if (!_categoryCache.ContainsKey(category))
                {
                    _categoryCache.TryAdd(category, new Category { Label = category });
                }

                if (!_categoryCache[category]
                    .Posts.Contains(post.Id))
                {
                    _categoryCache[category]
                        .Posts.Add(post.Id);
                }
            }

            if (saveCache)
            {
                await UpdateCategoryCacheBlobAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
        }

        private async Task UpdateSummaryCacheBlobAsync(CancellationToken cancellationToken = default)
        {
            CloudBlockBlob index = _postsContainer.GetBlockBlobReference(SUMMARY_CACHE_FILE_NAME);
            index.Properties.ContentType = KnownMimeTypes.Json;
            await index.UploadTextAsync(JsonConvert.SerializeObject(_postSummaryCache),
                    Encoding.UTF8,
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task DeletePostByIdAsync(string postId, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob blob = _postsContainer.GetBlockBlobReference(GetPostBlobName(postId));
            if (await blob.ExistsAsync(null, null, cancellationToken)
                .ConfigureAwait(false))
            {
                await blob.DeleteAsync(DeleteSnapshotsOption.None,
                        null,
                        null,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
            }

            if (_cacheService != null)
            {
                await _cacheService.RemoveAsync<Post>(postId, cancellationToken)
                    .ConfigureAwait(false);
            }

            await UpdatePostCategoriesAsync(new PostBase { Id = postId }, true, cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task<Post> GetPostByIdAsync(string postId, CancellationToken cancellationToken = default)
        {
            if (await _postsContainer.GetBlockBlobReference(GetPostBlobName(postId))
                .ExistsAsync(null, null, cancellationToken)
                .ConfigureAwait(false))
            {
                return JsonConvert.DeserializeObject<Post>(await _postsContainer.GetBlockBlobReference(GetPostBlobName(postId))
                    .DownloadTextAsync(Encoding.UTF8,
                        null,
                        null,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false));
            }

            return null;
        }

        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (IsInitialized)
            {
                return;
            }

            await _postsContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Off, null, null, cancellationToken)
                .ConfigureAwait(false);
            await _filesContainer.CreateIfNotExistsAsync(BlobContainerPublicAccessType.Blob, null, null, cancellationToken)
                .ConfigureAwait(false);

            await InitializeSummaryCache(cancellationToken)
                .ConfigureAwait(false);
            await InitializeCategoryCache(true, cancellationToken)
                .ConfigureAwait(false);

            IsInitialized = true;
        }

        /// <inheritdoc />
        public async Task InitializeCategoryCache(CancellationToken cancellationToken = default) => await InitializeCategoryCache(false, cancellationToken)
            .ConfigureAwait(false);

        public async Task InitializeCategoryCache(bool refreshCache = false, CancellationToken cancellationToken = default)
        {
            CloudBlockBlob index = _postsContainer.GetBlockBlobReference(CATEGORY_CACHE_FILE_NAME);

            if (!refreshCache)
            {
                if (await index.ExistsAsync(null, null, cancellationToken)
                    .ConfigureAwait(false))
                {
                    _categoryCache = JsonConvert.DeserializeObject<ConcurrentDictionary<string, Category>>(await index.DownloadTextSimpleAsync(cancellationToken)
                        .ConfigureAwait(false));
                    return;
                }
            }

            foreach (PostBase postBase in _postSummaryCache.Values)
            {
                foreach (string categoryLabel in postBase.Categories)
                {
                    if (!_categoryCache.TryGetValue(categoryLabel, out Category category))
                    {
                        category = new Category { Label = categoryLabel };
                    }

                    category.Posts.Add(postBase.Id);

                    _categoryCache.AddOrUpdate(categoryLabel, category, (k, v) => category);
                }
            }

            await UpdateCategoryCacheBlobAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        public async Task InitializeSummaryCache(CancellationToken cancellationToken = default)
        {
            CloudBlockBlob indexBlob = _postsContainer.GetBlockBlobReference(SUMMARY_CACHE_FILE_NAME);

            if (await indexBlob.ExistsAsync(null, null, cancellationToken)
                .ConfigureAwait(false))
            {
                _postSummaryCache = JsonConvert.DeserializeObject<ConcurrentDictionary<string, PostBase>>(await indexBlob.DownloadTextSimpleAsync(cancellationToken)
                    .ConfigureAwait(false));
                return;
            }

            BlobContinuationToken token = null;
            do
            {
                BlobResultSegment resultSegment = await _postsContainer.ListBlobsSegmentedAsync(POST_BLOB_PREFIX,
                        false,
                        BlobListingDetails.Metadata,
                        20,
                        token,
                        null,
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);

                token = resultSegment.ContinuationToken;
                foreach (IListBlobItem blob in resultSegment.Results)
                {
                    var post = JsonConvert.DeserializeObject<Post>(await ((CloudBlockBlob)blob).DownloadTextSimpleAsync(cancellationToken)
                        .ConfigureAwait(false));
                    _postSummaryCache.TryAdd(post.Id, post);
                }
            } while (token != null);

            await UpdateSummaryCacheBlobAsync(cancellationToken)
                .ConfigureAwait(false);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Checked with Ensure")]
        public async Task<Uri> SaveFileAsync(byte[] dataBytes, string fileName, CancellationToken cancellationToken = default)
        {
            Ensure.Argument.IsNotNull(dataBytes, nameof(dataBytes));

            CloudBlockBlob blob = _filesContainer.GetBlockBlobReference(fileName);

            await blob.DeleteIfExistsAsync(DeleteSnapshotsOption.None,
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            await blob.UploadFromByteArrayAsync(dataBytes,
                    0,
                    dataBytes.Length,
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return blob.Uri;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Checked with Ensure")]
        public async Task<Uri> SavePostAsync(Post post, CancellationToken cancellationToken = default)
        {
            Ensure.Argument.IsNotNull(post);

            string jsonPost = JsonConvert.SerializeObject(post);

            CloudBlockBlob blob = _postsContainer.GetBlockBlobReference(GetPostBlobName(post.Id));
            blob.Properties.ContentType = KnownMimeTypes.Json;

            await blob.UploadTextAsync(jsonPost,
                    Encoding.UTF8,
                    null,
                    null,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            await UpdatePostCategoriesAsync(post, true, cancellationToken)
                .ConfigureAwait(false);

            return blob.Uri;
        }
    }
}