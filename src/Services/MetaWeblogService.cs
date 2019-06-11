using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Miniblog.Core.Configuration;
using Miniblog.Core.Models;
using WilderMinds.MetaWeblog;
using Post = WilderMinds.MetaWeblog.Post;

namespace Miniblog.Core.Services
{
    /// <summary>
    ///     Defines the <see cref="MetaWeblogService" />
    /// </summary>
    public class MetaWeblogService : IMetaWeblogProvider
    {
        /// <summary>
        ///     Defines the _blog
        /// </summary>
        private readonly IBlogService _blog;

        /// <summary>
        ///     Defines the _blogSettings
        /// </summary>
        private readonly BlogSettings _blogSettings;

        /// <summary>
        ///     Defines the _context
        /// </summary>
        private readonly IHttpContextAccessor _context;

        /// <summary>
        ///     Defines the _userServices
        /// </summary>
        private readonly IUserServices _userServices;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MetaWeblogService" /> class.
        /// </summary>
        /// <param name="blog">The blog<see cref="IBlogService" /></param>
        /// <param name="blogSettings">The blogSettings<see cref="BlogSettings" /></param>
        /// <param name="context">The context<see cref="IHttpContextAccessor" /></param>
        /// <param name="userServices">The userServices<see cref="IUserServices" /></param>
        public MetaWeblogService(IBlogService blog, IOptionsMonitor<BlogSettings> blogSettings, IHttpContextAccessor context, IUserServices userServices)
        {
            _blog = blog;
            _blogSettings = blogSettings.CurrentValue;
            _userServices = userServices;
            _context = context;
        }

        /// <summary>
        ///     The GetUserInfoAsync
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>The <see cref="Task{UserInfo}" /></returns>
        public async Task<UserInfo> GetUserInfoAsync(string key, string userId, string password)
        {
            return await Task.Run(() => GetUserInfo(key, userId, password));
        }

        /// <summary>
        ///     The GetUsersBlogsAsync
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{BlogInfo[]}</cref>
        ///     </see>
        /// </returns>
        public async Task<BlogInfo[]> GetUsersBlogsAsync(string key, string userId, string password)
        {
            return await Task.Run(() => GetUsersBlogs(key, userId, password));
        }

        /// <summary>
        ///     The GetPostAsync
        /// </summary>
        /// <param name="postId">The postId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>The <see cref="Task{Post}" /></returns>
        public async Task<Post> GetPostAsync(string postId, string userId, string password)
        {
            ValidateUser(userId, password);

            Models.Post post = await _blog.GetPostById(postId);

            return post != null ? ToMetaWebLogPost(post) : null;
        }

        /// <summary>
        ///     The GetRecentPostsAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="numberOfPosts">The numberOfPosts<see cref="int" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{Post[]}</cref>
        ///     </see>
        /// </returns>
        public async Task<Post[]> GetRecentPostsAsync(string blogId, string userId, string password, int numberOfPosts)
        {
            ValidateUser(userId, password);

            IEnumerable<Models.Post> result = await _blog.GetPosts(numberOfPosts);

            return result.Select(ToMetaWebLogPost)
                .ToArray();
        }

        /// <summary>
        ///     The AddPostAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="post">The post<see cref="Post" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{string}</cref>
        ///     </see>
        /// </returns>
        public async Task<string> AddPostAsync(
            string blogId,
            string userId,
            string password,
            Post post,
            bool publish)
        {
            ValidateUser(userId, password);

            var newPost = new Models.Post
            {
                Title = post.title,
                Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : PostBase.CreateSlug(post.title),
                Content = post.description,
                IsPublished = publish,
                Categories = post.categories
            };

            if (post.dateCreated != DateTime.MinValue)
            {
                newPost.PubDate = post.dateCreated;
            }

            await _blog.SavePost(newPost);

            return newPost.Id;
        }

        /// <summary>
        ///     The DeletePostAsync
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="postId">The postId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{bool}</cref>
        ///     </see>
        /// </returns>
        public async Task<bool> DeletePostAsync(
            string key,
            string postId,
            string userId,
            string password,
            bool publish)
        {
            ValidateUser(userId, password);

            Models.Post post = await _blog.GetPostById(postId);

            if (post == null)
            {
                return false;
            }

            await _blog.DeletePost(post);
            return true;
        }

        /// <summary>
        ///     The EditPostAsync
        /// </summary>
        /// <param name="postId">The postId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="post">The post<see cref="Post" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{bool}</cref>
        ///     </see>
        /// </returns>
        public async Task<bool> EditPostAsync(
            string postId,
            string userId,
            string password,
            Post post,
            bool publish)
        {
            ValidateUser(userId, password);

            Models.Post existing = await _blog.GetPostById(postId);

            if (existing == null)
            {
                return false;
            }

            existing.Title = post.title;
            existing.Slug = post.wp_slug;
            existing.Content = post.description;
            existing.IsPublished = publish;
            existing.Categories = post.categories;

            if (post.dateCreated != DateTime.MinValue)
            {
                existing.PubDate = post.dateCreated;
            }

            await _blog.SavePost(existing);

            return true;
        }

        /// <summary>
        ///     The GetCategoriesAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{CategoryInfo[]}</cref>
        ///     </see>
        /// </returns>
        public async Task<CategoryInfo[]> GetCategoriesAsync(string blogId, string userId, string password)
        {
            ValidateUser(userId, password);

            IEnumerable<string> result = await _blog.GetCategories();

            return result.Select(cat => new CategoryInfo
                {
                    categoryid = cat,
                    title = cat
                })
                .OrderBy(categoryInfo => categoryInfo.title)
                .ToArray();
        }

        /// <summary>
        ///     The AddCategoryAsync
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="category">The category<see cref="NewCategory" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{int}</cref>
        ///     </see>
        /// </returns>
        public Task<int> AddCategoryAsync(string key, string userId, string password, NewCategory category) => throw new NotImplementedException();

        /// <summary>
        ///     The NewMediaObjectAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="mediaObject">The mediaObject<see cref="MediaObject" /></param>
        /// <returns>The <see cref="Task{MediaObjectInfo}" /></returns>
        public async Task<MediaObjectInfo> NewMediaObjectAsync(string blogId, string userId, string password, MediaObject mediaObject)
        {
            ValidateUser(userId, password);
            byte[] bytes = Convert.FromBase64String(mediaObject.bits);
            string path = await _blog.SaveFile(bytes, mediaObject.name);

            return new MediaObjectInfo {url = path};
        }

        /// <summary>
        ///     The GetPageAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="pageId">The pageId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>The <see cref="Task{Page}" /></returns>
        public Task<Page> GetPageAsync(string blogId, string pageId, string userId, string password) => throw new NotImplementedException();

        /// <summary>
        ///     The GetPagesAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="numPages">The numPages<see cref="int" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{Page[]}</cref>
        ///     </see>
        /// </returns>
        public Task<Page[]> GetPagesAsync(string blogId, string userId, string password, int numPages) => throw new NotImplementedException();

        /// <summary>
        ///     The GetAuthorsAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{Author[]}</cref>
        ///     </see>
        /// </returns>
        public Task<Author[]> GetAuthorsAsync(string blogId, string userId, string password) => throw new NotImplementedException();

        /// <summary>
        ///     The AddPageAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="page">The page<see cref="Page" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{string}</cref>
        ///     </see>
        /// </returns>
        public Task<string> AddPageAsync(
            string blogId,
            string userId,
            string password,
            Page page,
            bool publish) =>
            throw new NotImplementedException();

        /// <summary>
        ///     The EditPageAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="pageId">The pageId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="page">The page<see cref="Page" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{bool}</cref>
        ///     </see>
        /// </returns>
        public Task<bool> EditPageAsync(
            string blogId,
            string pageId,
            string userId,
            string password,
            Page page,
            bool publish) =>
            throw new NotImplementedException();

        /// <summary>
        ///     The DeletePageAsync
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="pageId">The pageId<see cref="string" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Task{bool}</cref>
        ///     </see>
        /// </returns>
        public Task<bool> DeletePageAsync(string blogId, string userId, string password, string pageId) => throw new NotImplementedException();

        /// <summary>
        ///     The AddPost
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="post">The post<see cref="Post" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>The <see cref="string" /></returns>
        public string AddPost(
            string blogId,
            string userId,
            string password,
            Post post,
            bool publish)
        {
            ValidateUser(userId, password);

            var newPost = new Models.Post
            {
                Title = post.title,
                Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : PostBase.CreateSlug(post.title),
                Content = post.description,
                IsPublished = publish,
                Categories = post.categories
            };

            if (post.dateCreated != DateTime.MinValue)
            {
                newPost.PubDate = post.dateCreated;
            }

            _blog.SavePost(newPost)
                .GetAwaiter()
                .GetResult();

            return newPost.Id;
        }

        /// <summary>
        ///     The DeletePost
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="postId">The postId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>The <see cref="bool" /></returns>
        public bool DeletePost(
            string key,
            string postId,
            string userId,
            string password,
            bool publish)
        {
            ValidateUser(userId, password);

            Models.Post post = _blog.GetPostById(postId)
                .GetAwaiter()
                .GetResult();

            if (post != null)
            {
                _blog.DeletePost(post)
                    .GetAwaiter()
                    .GetResult();
                return true;
            }

            return false;
        }

        /// <summary>
        ///     The EditPost
        /// </summary>
        /// <param name="postId">The postId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="post">The post<see cref="Post" /></param>
        /// <param name="publish">The publish<see cref="bool" /></param>
        /// <returns>The <see cref="bool" /></returns>
        public bool EditPost(
            string postId,
            string userId,
            string password,
            Post post,
            bool publish)
        {
            ValidateUser(userId, password);

            Models.Post existing = _blog.GetPostById(postId)
                .GetAwaiter()
                .GetResult();

            if (existing != null)
            {
                existing.Title = post.title;
                existing.Slug = post.wp_slug;
                existing.Content = post.description;
                existing.IsPublished = publish;
                existing.Categories = post.categories;

                if (post.dateCreated != DateTime.MinValue)
                {
                    existing.PubDate = post.dateCreated;
                }

                _blog.SavePost(existing)
                    .GetAwaiter()
                    .GetResult();

                return true;
            }

            return false;
        }

        /// <summary>
        ///     The GetCategories
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>CategoryInfo[]</cref>
        ///     </see>
        /// </returns>
        public CategoryInfo[] GetCategories(string blogId, string userId, string password)
        {
            ValidateUser(userId, password);

            return _blog.GetCategories()
                .GetAwaiter()
                .GetResult()
                .Select(cat => new CategoryInfo
                {
                    categoryid = cat,
                    title = cat
                })
                .ToArray();
        }

        /// <summary>
        ///     The GetPost
        /// </summary>
        /// <param name="postId">The postId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>The <see cref="Post" /></returns>
        public Post GetPost(string postId, string userId, string password)
        {
            ValidateUser(userId, password);

            Models.Post post = _blog.GetPostById(postId)
                .GetAwaiter()
                .GetResult();

            if (post != null)
            {
                return ToMetaWebLogPost(post);
            }

            return null;
        }

        /// <summary>
        ///     The GetRecentPosts
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="numberOfPosts">The numberOfPosts<see cref="int" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>Post[]</cref>
        ///     </see>
        /// </returns>
        public Post[] GetRecentPosts(string blogId, string userId, string password, int numberOfPosts)
        {
            ValidateUser(userId, password);

            return _blog.GetPosts(numberOfPosts)
                .GetAwaiter()
                .GetResult()
                .Select(ToMetaWebLogPost)
                .ToArray();
        }

        /// <summary>
        ///     The GetUsersBlogs
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>
        ///     The
        ///     <see>
        ///         <cref>BlogInfo[]</cref>
        ///     </see>
        /// </returns>
        public BlogInfo[] GetUsersBlogs(string key, string userId, string password)
        {
            ValidateUser(userId, password);

            HttpRequest request = _context.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host;

            return new[]
            {
                new BlogInfo
                {
                    blogid = "1",
                    blogName = _blogSettings.Name ?? nameof(MetaWeblogService),
                    url = url
                }
            };
        }

        /// <summary>
        ///     The NewMediaObject
        /// </summary>
        /// <param name="blogId">The blogId<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="mediaObject">The mediaObject<see cref="MediaObject" /></param>
        /// <returns>The <see cref="MediaObjectInfo" /></returns>
        public MediaObjectInfo NewMediaObject(string blogId, string userId, string password, MediaObject mediaObject)
        {
            ValidateUser(userId, password);
            byte[] bytes = Convert.FromBase64String(mediaObject.bits);
            string path = _blog.SaveFile(bytes, mediaObject.name)
                .GetAwaiter()
                .GetResult();

            return new MediaObjectInfo {url = path};
        }

        /// <summary>
        ///     The GetUserInfo
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <returns>The <see cref="UserInfo" /></returns>
        public UserInfo GetUserInfo(string key, string userId, string password)
        {
            ValidateUser(userId, password);
            return _userServices.GetUser(userId);
        }

        /// <summary>
        ///     The AddCategory
        /// </summary>
        /// <param name="key">The key<see cref="string" /></param>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        /// <param name="category">The category<see cref="NewCategory" /></param>
        /// <returns>The <see cref="int" /></returns>
        public int AddCategory(string key, string userId, string password, NewCategory category)
        {
            ValidateUser(userId, password);
            throw new NotImplementedException();
        }

        /// <summary>
        ///     The ValidateUser
        /// </summary>
        /// <param name="userId">The userId<see cref="string" /></param>
        /// <param name="password">The password<see cref="string" /></param>
        private void ValidateUser(string userId, string password)
        {
            if (_userServices.ValidateUser(userId, password) == false)
            {
                throw new MetaWeblogException("Unauthorized");
            }

            UserInfo user = _userServices.GetUser(userId);

            var identity = new ClaimsIdentity(CookieAuthenticationDefaults.AuthenticationScheme);
            identity.AddClaim(new Claim(ClaimTypes.Name, user.userid));
            identity.AddClaim(new Claim(ClaimTypes.Surname, user.lastname));
            identity.AddClaim(new Claim(ClaimTypes.GivenName, user.firstname));
            identity.AddClaim(new Claim(ClaimTypes.Email, user.email));

            _context.HttpContext.User = new ClaimsPrincipal(identity);
        }

        /// <summary>
        ///     The ToMetaWebLogPost
        /// </summary>
        /// <param name="post">The post<see cref="Models.Post" /></param>
        /// <returns>The <see cref="Post" /></returns>
        private Post ToMetaWebLogPost(Models.Post post)
        {
            HttpRequest request = _context.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host;

            return new Post
            {
                postid = post.Id,
                title = post.Title,
                wp_slug = post.Slug,
                permalink = url + post.GetLink(),
                dateCreated = post.PubDate,
                description = post.Content,
                categories = post.Categories.ToArray()
            };
        }
    }
}