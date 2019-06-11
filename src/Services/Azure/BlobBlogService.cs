using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Http;
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

            Mapper.Initialize(config =>
            {
                config.CreateMap<Post, PostBase>()
                    .ReverseMap();
            });
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
                posts.Add(await GetPostById(postBase.Id));
            }

            return posts;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<Post>> GetPostsByCategory(string category)
        {
            bool isAdmin = IsAdmin();

            // Get cached Ids
            List<string> postIdList = _blobStorageService
                .CategoryCache
                .First(c => c.Label == category)
                .Posts;


            IEnumerable<Post> posts =
                await Task.Run(
                    () => Mapper.Map<IEnumerable<Post>>(
                        _blobStorageService
                            .PostCache
                            .Where(
                                p =>
                                    postIdList.Contains(p.Id)
                                    &&
                                    (
                                        isAdmin || p.IsPublished
                                    )
                            )
                    )
                );

            return posts;
        }

        /// <inheritdoc />
        public async Task<Post> GetPostBySlug(string slug)
        {
            if (_blobStorageService.PostCache.All(p => p.Slug != slug))
            {
                return null;
            }

            string postId = _blobStorageService.PostCache.First(p => p.Slug == slug)
                .Id;

            return await GetPostById(postId);
        }

        /// <inheritdoc />
        public async Task<Post> GetPostById(string id)
        {
            return await _blobStorageService.GetPostByIdAsync(id);
        }

        /// <inheritdoc />
        public Task<IEnumerable<string>> GetCategories()
        {
            return Task.FromResult(_blobStorageService.CategoryCache.Select(c => c.Label));
        }

        /// <inheritdoc />
        public async Task SavePost(Post post)
        {
            post.DateModified = DateTime.UtcNow;

            await _blobStorageService.SavePostAsync(post);
        }

        /// <inheritdoc />
        public async Task DeletePost(Post post)
        {
            await _blobStorageService.DeletePostByIdAsync(post.Id);
        }

        /// <inheritdoc />
        public async Task<string> SaveFile(byte[] bytes, string fileName, string suffix = null)
        {
            suffix = RemoveInvalidChars(suffix ?? DateTime.UtcNow.Ticks.ToString());

            string ext = Path.GetExtension(fileName);
            string name = RemoveInvalidChars(Path.GetFileNameWithoutExtension(fileName));

            string fileNameWithSuffix = $"{name}_{suffix}{ext}";

            Uri fileUri = await _blobStorageService.SaveFileAsync(bytes, fileNameWithSuffix);

            return fileUri.ToString();
        }

        private static string RemoveInvalidChars(string input)
        {
            string regexSearch = Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars()));
            var r = new Regex($"[{regexSearch}]");
            return r.Replace(input, string.Empty);
        }

        protected bool IsAdmin() => _contextAccessor.HttpContext?.User?.Identity.IsAuthenticated == true;
    }
}