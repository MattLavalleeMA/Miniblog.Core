﻿// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Miniblog.Core.Configuration;
using Miniblog.Core.Models;
using Miniblog.Core.Services;
using WebEssentials.AspNetCore.Pwa;

namespace Miniblog.Core.Controllers
{
    public class BlogController : Controller
    {
        private readonly IBlogService _blog;
        private readonly WebManifest _manifest;
        private readonly IOptionsSnapshot<BlogSettings> _settings;

        public BlogController(IBlogService blog, IOptionsSnapshot<BlogSettings> settings, WebManifest manifest)
        {
            _blog = blog;
            _settings = settings;
            _manifest = manifest;
        }

        private async Task SaveFilesToDisk(Post post)
        {
            var imgRegex = new Regex("<img[^>].+ />", RegexOptions.IgnoreCase | RegexOptions.Compiled);
            var base64Regex = new Regex("data:[^/]+/(?<ext>[a-z]+);base64,(?<base64>.+)", RegexOptions.IgnoreCase);

            foreach (Match match in imgRegex.Matches(post.Content))
            {
                var doc = new XmlDocument();
                doc.LoadXml("<root>" + match.Value + "</root>");

                XmlNode img = doc.FirstChild.FirstChild;
                XmlAttribute srcNode = img.Attributes["src"];
                XmlAttribute fileNameNode = img.Attributes["data-filename"];

                // The HTML editor creates base64 DataURIs which we'll have to convert to image files on disk
                if (srcNode == null || fileNameNode == null)
                {
                    continue;
                }

                Match base64Match = base64Regex.Match(srcNode.Value);
                if (!base64Match.Success)
                {
                    continue;
                }

                byte[] bytes = Convert.FromBase64String(base64Match.Groups["base64"]
                    .Value);
                srcNode.Value = await _blog.SaveFile(bytes, fileNameNode.Value)
                    .ConfigureAwait(false);

                img.Attributes.Remove(fileNameNode);
                post.Content = post.Content.Replace(match.Value, img.OuterXml, StringComparison.CurrentCulture);
            }
        }

        [Route("/blog/comment/{postId}")]
        [HttpPost]
        public async Task<IActionResult> AddComment(string postId, Comment comment)
        {
            Post post = await _blog.GetPostById(postId)
                .ConfigureAwait(false);

            if (!ModelState.IsValid)
            {
                return View("Post", post);
            }

            if (post == null || !post.AreCommentsOpen(_settings.Value.CommentsCloseAfterDays))
            {
                return NotFound();
            }

            if (comment != null)
            {
                comment.IsAdmin = User.Identity.IsAuthenticated;
                comment.Content = comment.Content.Trim();
                comment.Author = comment.Author.Trim();
                comment.Email = comment.Email.Trim();

                // the website form key should have been removed by javascript
                // unless the comment was posted by a spam robot
                if (!Request.Form.ContainsKey("website"))
                {
                    post.Comments.Add(comment);
                    await _blog.SavePost(post)
                        .ConfigureAwait(false);
                }

                return Redirect(post.GetEncodedLink() + "#" + comment.Id);
            }

            return Redirect("/");
        }

        [Route("/category/{category}/{page:int?}")]
        [Route("/blog/category/{category}/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Category(string category, int page = 1)
        {
            PagedResultModel<Post> posts = await _blog.GetPostsPaged(_settings.Value.PostsPerPage, page, category)
                .ConfigureAwait(false);
            ViewData["Title"] = _manifest.Name + " " + category;
            ViewData["Description"] = $"Articles posted in the {category} category";
            ViewData["prev"] = posts.HasPreviousPage ? $"/category/{category}/{page - 1}/" : string.Empty;
            ViewData["next"] = posts.HasNextPage ? $"/category/{category}/{page + 1}/" : string.Empty;
            //ViewData["prev"] = $"/blog/category/{category}/{page + 1}/";
            //ViewData["next"] = $"/blog/category/{category}/{(page <= 1 ? null : page - 1 + "/")}";
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        [Route("/blog/comment/{postId}/{commentId}")]
        [Authorize]
        public async Task<IActionResult> DeleteComment(string postId, string commentId)
        {
            Post post = await _blog.GetPostById(postId)
                .ConfigureAwait(false);

            if (post == null)
            {
                return NotFound();
            }

            Comment comment = post.Comments.FirstOrDefault(c => c.Id.Equals(commentId, StringComparison.OrdinalIgnoreCase));

            if (comment == null)
            {
                return NotFound();
            }

            post.Comments.Remove(comment);
            await _blog.SavePost(post)
                .ConfigureAwait(false);

            return Redirect(post.GetEncodedLink() + "#comments");
        }

        [Route("/blog/deletepost/{id}")]
        [HttpPost]
        [Authorize]
        [AutoValidateAntiforgeryToken]
        public async Task<IActionResult> DeletePost(string id)
        {
            Post existing = await _blog.GetPostById(id)
                .ConfigureAwait(false);

            if (existing != null)
            {
                await _blog.DeletePost(existing)
                    .ConfigureAwait(false);
                return Redirect("/");
            }

            return NotFound();
        }

        [Route("/blog/edit/{id?}")]
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Edit(string id)
        {
            ViewData["AllCats"] = (await _blog.GetCategories()
                .ConfigureAwait(false)).ToList();

            if (string.IsNullOrEmpty(id))
            {
                return View(new Post());
            }

            Post post = await _blog.GetPostById(id)
                .ConfigureAwait(false);

            if (post != null)
            {
                return View(post);
            }

            return NotFound();
        }

        [Route("/{page:int?}")]
        [Route("/page/{page:int?}")]
        [Route("/blog/page/{page:int?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Index([FromRoute] int page = 1)
        {
            PagedResultModel<Post> posts = await _blog.GetPostsPaged(_settings.Value.PostsPerPage, page)
                .ConfigureAwait(false);
            ViewData["Title"] = _manifest.Name;
            ViewData["Description"] = _manifest.Description;
            ViewData["prev"] = posts.HasPreviousPage ? $"/page/{page - 1}/" : string.Empty;
            ViewData["next"] = posts.HasNextPage ? $"/page/{page + 1}/" : string.Empty;
            return View("~/Views/Blog/Index.cshtml", posts);
        }

        [Route("/blog/{slug?}")]
        [OutputCache(Profile = "default")]
        public async Task<IActionResult> Post(string slug)
        {
            Post post = await _blog.GetPostBySlug(slug)
                .ConfigureAwait(false);

            if (post != null)
            {
                return View(post);
            }

            return NotFound();
        }

        // This is for redirecting potential existing URLs from the old Miniblog URL format
        [Route("/post/{slug}")]
        [HttpGet]
        public IActionResult Redirects(string slug) => LocalRedirectPermanent($"/blog/{slug}");

        [Route("/blog/{slug?}")]
        [HttpPost]
        [Authorize]
        [AutoValidateAntiforgeryToken]
        [SuppressMessage("Globalization", "CA1308:Normalize strings to uppercase", Justification = "<Pending>")]
        public async Task<IActionResult> UpdatePost(Post post)
        {
            if (!ModelState.IsValid)
            {
                return View("Edit", post);
            }

            if (post == null)
            {
                return Redirect("/");
            }

            Post existing = await _blog.GetPostById(post.Id)
                    .ConfigureAwait(false) ??
                post;
            string categories = Request.Form["categories"];

            foreach (string category in categories.Split(",", StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim()
                    .ToLowerInvariant()))
            {
                existing.Categories.Add(category);
            }

            existing.Title = post.Title.Trim();
            existing.Slug = !string.IsNullOrWhiteSpace(post.Slug) ? post.Slug.Trim() : PostBase.CreateSlug(post.Title);
            existing.IsPublished = post.IsPublished;
            existing.Content = post.Content.Trim();
            existing.Excerpt = post.Excerpt.Trim();

            await SaveFilesToDisk(existing)
                .ConfigureAwait(false);

            await _blog.SavePost(existing)
                .ConfigureAwait(false);

            return Redirect(post.GetEncodedLink());
        }
    }
}