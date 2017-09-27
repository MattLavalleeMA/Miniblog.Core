﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Linq;
using WilderMinds.MetaWeblog;

namespace Miniblog.Core
{
    public class MetaWeblogService : IMetaWeblogProvider
    {
        private IBlogStorage _storage;
        private IConfiguration _config;
        private IHttpContextAccessor _context;

        public MetaWeblogService(IBlogStorage storage, IConfiguration config, IHttpContextAccessor context)
        {
            _storage = storage;
            _config = config;
            _context = context;
        }

        public int AddCategory(string key, string username, string password, NewCategory category)
        {
            throw new NotImplementedException();
        }

        public string AddPost(string blogid, string username, string password, WilderMinds.MetaWeblog.Post post, bool publish)
        {
            ValidateUser(username, password);

            var newPost = new Post
            {
                Title = post.title,
                Slug = !string.IsNullOrWhiteSpace(post.wp_slug) ? post.wp_slug : Post.CreateSlug(post.title),
                Content = post.description,
                IsPublished = publish,
                Categories = post.categories
            };

            _storage.Save(newPost);

            return newPost.ID;
        }

        public bool DeletePost(string key, string postid, string username, string password, bool publish)
        {
            ValidateUser(username, password);

            var post = _storage.GetPostById(postid);

            if (post != null)
            {
                _storage.Delete(post);
                return true;
            }

            return false;
        }

        public bool EditPost(string postid, string username, string password, WilderMinds.MetaWeblog.Post post, bool publish)
        {
            ValidateUser(username, password);

            var existing = _storage.GetPostById(postid);

            if (existing != null)
            {
                existing.Title = post.title;
                existing.Slug = post.wp_slug;
                existing.Content = post.description;
                existing.IsPublished = publish;
                existing.Categories = post.categories;

                _storage.Save(existing);

                return true;
            }

            return false;
        }

        public CategoryInfo[] GetCategories(string blogid, string username, string password)
        {
            throw new NotImplementedException();
        }

        public WilderMinds.MetaWeblog.Post GetPost(string postid, string username, string password)
        {
            ValidateUser(username, password);

            var post = _storage.GetPostById(postid);

            if (post != null)
            {
                return ToMetaWebLogPost(post);
            }

            return null;
        }

        public WilderMinds.MetaWeblog.Post[] GetRecentPosts(string blogid, string username, string password, int numberOfPosts)
        {
            ValidateUser(username, password);

            return _storage.GetPosts(numberOfPosts).Select(p => ToMetaWebLogPost(p)).ToArray();
        }

        public UserInfo GetUserInfo(string key, string username, string password)
        {
            throw new NotImplementedException();
        }

        public BlogInfo[] GetUsersBlogs(string key, string username, string password)
        {
            ValidateUser(username, password);

            var request = _context.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host;

            return new[] { new BlogInfo {
                blogid ="1",
                blogName = _config["blog:name"],
                url = url
            }};
        }

        public MediaObjectInfo NewMediaObject(string blogid, string username, string password, MediaObject mediaObject)
        {
            throw new NotImplementedException();
        }

        private void ValidateUser(string username, string password)
        {
            if (username != _config["user:username"] || !Pages.LoginModel.VerifyHashedPassword(password, _config))
            {
                throw new MetaWeblogException("Unauthorized");
            }
        }

        private WilderMinds.MetaWeblog.Post ToMetaWebLogPost(Post post)
        {
            var request = _context.HttpContext.Request;
            string url = request.Scheme + "://" + request.Host;

            return new WilderMinds.MetaWeblog.Post
            {
                postid = post.ID,
                title = post.Title,
                wp_slug = post.Slug,
                permalink = url + post.GetLink(),
                dateCreated = post.PubDate,
                description = post.Excerpt,
                categories = post.Categories.ToArray()
            };
        }
    }
}
