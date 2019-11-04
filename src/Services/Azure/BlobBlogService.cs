using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Miniblog.Core.Abstractions;
using Miniblog.Core.Models;

namespace Miniblog.Core.Services.Azure
{
    public class BlobBlogService : IBlogService
    {
        private readonly IBlobStorageService _blobStorageService;
        private readonly IHttpContextAccessor _contextAccessor;

        public BlobBlogService(IBlobStorageService blobStorageService, IHttpContextAccessor contextAccessor)
        {
            _blobStorageService = blobStorageService;

            _contextAccessor = contextAccessor;
        }

        private static string RemoveInvalidChars(string input)
        {
            string regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var r = new Regex($"[{regexSearch}]");
            return r.Replace(input, string.Empty);
        }

        protected bool IsAdmin() => _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;

        /// <inheritdoc />
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Checked with Ensure")]
        public async Task DeletePost(Post post)
        {
            Ensure.Argument.IsNotNull(post, nameof(post));
            await _blobStorageService.DeletePostByIdAsync(post.Id)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetCategories()
        {
            return Task.FromResult(_blobStorageService.CategoryCache.Select(c => c.Label));
        }

        /// <inheritdoc />
        public async Task<Post> GetPostById(string id) => await _blobStorageService.GetPostByIdAsync(id)
            .ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<Post> GetPostBySlug(string slug)
        {
            if (_blobStorageService.PostCache.All(p => p.Slug != slug))
            {
                return null;
            }

            string postId = _blobStorageService.PostCache.First(p => p.Slug == slug)
                .Id;

            return await GetPostById(postId)
                .ConfigureAwait(false);
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Post>> GetPosts(int count, int skip = 0)
        {
            bool isAdmin = IsAdmin();

            IEnumerable<PostBase> postSummaries = _blobStorageService.PostCache.Where(p => p.PubDate <= DateTime.UtcNow && (p.IsPublished || isAdmin))
                .Skip(skip)
                .Take(count);
            List<Post> posts = new List<Post>();

            foreach (PostBase postBase in postSummaries)
            {
                posts.Add(await GetPostById(postBase.Id)
                    .ConfigureAwait(false));
            }

            return posts;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Post>> GetPostsByCategory(string category)
        {
            bool isAdmin = IsAdmin();

            // Get cached Ids
            List<string> postIdList = _blobStorageService.CategoryCache.First(c => c.Label == category)
                .Posts;

            IEnumerable<Post> posts = await Task.Run(() => (IEnumerable<Post>)_blobStorageService.PostCache.Where(p => postIdList.Contains(p.Id) && (isAdmin || p.IsPublished)))
                .ConfigureAwait(false);

            return posts;
        }

        public async Task<PagedResultModel<Post>> GetPostsPaged(int pageSize, int pageNumber = 1, string category = "", bool isAdmin = false)
        {
            isAdmin |= IsAdmin();

            IEnumerable<PostBase> postSummaries = _blobStorageService.PostCache.Where(p => string.IsNullOrEmpty(category) || p.Categories.Contains(category))
                // ReSharper disable once ArrangeRedundantParentheses
                .Where(p => isAdmin || (p.PubDate <= DateTime.UtcNow && p.IsPublished))
                .OrderByDescending(p => p.PubDate)
                .ToArray();

            PagedResult<PostBase> pagedResult = new PagedResult<PostBase>(postSummaries, pageSize, pageNumber);

            PagedResultModel<Post> result = new PagedResultModel<Post>
            {
                HasNextPage = pagedResult.HasNextPage,
                HasPreviousPage = pagedResult.HasPreviousPage,
                NextPageNumber = pagedResult.NextPageNumber,
                PreviousPageNumber = pagedResult.PreviousPageNumber,
                PageNumber = pagedResult.PageNumber,
                PageSize = pagedResult.PageSize,
                TotalPages = pagedResult.TotalPages,
                TotalItems = pagedResult.TotalItems
            };

            List<Post> posts = new List<Post>();

            foreach (PostBase postBase in pagedResult.Items)
            {
                posts.Add(await GetPostById(postBase.Id)
                    .ConfigureAwait(false));
            }

            if (posts.Any(p => p == null))
            {
                await _blobStorageService.InitializeSummaryCache()
                    .ConfigureAwait(false);
            }

            result.Items = posts.Where(p => p != null);

            return result;
        }

        /// <inheritdoc />
        [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "<Pending>")]
        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = RemoveInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString());

            string ext = Path.GetExtension(fileName);
            string name = RemoveInvalidChars(Path.GetFileNameWithoutExtension(fileName));

            string fileNameWithSuffix = $"{name}_{suffix}{ext}";

            Uri fileUri = await _blobStorageService.SaveFileAsync(bytes, fileNameWithSuffix)
                .ConfigureAwait(false);

            return fileUri.ToString();
        }

        /// <inheritdoc />
        [SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "Checked with Ensure")]
        public async Task SavePost(Post post)
        {
            Ensure.Argument.IsNotNull(post, nameof(post));

            post.DateModified = DateTime.UtcNow;

            await _blobStorageService.SavePostAsync(post)
                .ConfigureAwait(false);
        }
    }
}