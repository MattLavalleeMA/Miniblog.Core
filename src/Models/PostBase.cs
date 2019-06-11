// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Net;
using System.Text;

namespace Miniblog.Core.Models
{
    public class PostBase
    {
        public IList<string> Categories { get; set; } = new List<string>();

        [Required]
        public string Excerpt { get; set; }

        [Required]
        public string Id { get; set; } = DateTime.UtcNow.Ticks.ToString();

        public bool IsPublished
        {
            get => PubDate <= DateTime.UtcNow;
            set
            {
                if (value)
                {
                    PubDate = PubDate < DateTime.MaxValue ? PubDate : DateTime.UtcNow;
                }
            }
        }

        public DateTime DateCreated { get; set; } = DateTime.UtcNow;
        public DateTime DateModified { get; set; } = DateTime.UtcNow;

        public DateTime PubDate { get; set; } = DateTime.MaxValue;

        public string Slug { get; set; }

        [Required]
        public string Title { get; set; }

        private static string RemoveDiacritics(string text)
        {
            string normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                UnicodeCategory unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString()
                .Normalize(NormalizationForm.FormC);
        }

        private static string RemoveReservedUrlCharacters(string text)
        {
            var sb = new StringBuilder();
            foreach (char c in text)
            {
                if (c >= '0' && c <= '9' || c >= 'A' && c <= 'Z' || c >= 'a' && c <= 'z' || c == '.' || c == '_')
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        public static string CreateSlug(string title)
        {
            title = title.ToLowerInvariant()
                .Replace(" ", "-");
            title = RemoveDiacritics(title);
            title = RemoveReservedUrlCharacters(title);

            return title.ToLowerInvariant();
        }

        public string GetEncodedLink() => $"/blog/{WebUtility.UrlEncode(Slug)}/";

        public string GetLink() => $"/blog/{Slug}/";
    }
}