﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Miniblog.Core.Models
{
    public class Post : PostBase
    {
        [Required]
        public string Content { get; set; }

        public IList<Comment> Comments { get; } = new List<Comment>();

        public bool AreCommentsOpen(int commentsCloseAfterDays) => PubDate.AddDays(commentsCloseAfterDays) >= DateTime.UtcNow;

        public string RenderContent()
        {
            string result = Content;
            if (string.IsNullOrEmpty(result))
            {
                return result;
            }

            // Set up lazy loading of images/iframes
            result = result.Replace(" src=\"", " src=\"data:image/gif;base64,R0lGODlhAQABAIAAAP///wAAACH5BAEAAAAALAAAAAABAAEAAAICRAEAOw==\" data-src=\"");

            // Youtube content embedded using this syntax: [youtube:xyzAbc123]
            const string video = "<div class=\"video\"><iframe width=\"560\" height=\"315\" title=\"YouTube embed\" src=\"about:blank\" data-src=\"https://www.youtube-nocookie.com/embed/{0}?modestbranding=1&amp;hd=1&amp;rel=0&amp;theme=light\" allowfullscreen></iframe></div>";
            result = Regex.Replace(result, @"\[youtube:(.*?)\]", m => string.Format(video, m.Groups[1].Value));

            return result;
        }
    }
}