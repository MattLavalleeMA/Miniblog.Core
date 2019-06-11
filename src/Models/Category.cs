// Copyright (c) 2019 All rights reserved.
// Code should follow the .NET Standard Guidelines:
// https://docs.microsoft.com/en-us/dotnet/standard/design-guidelines/

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Miniblog.Core.Models
{
    public class Category
    {
        [Required]
        public string Label { get; set; }

        public readonly List<string> Posts;

        public Category()
        {
            Posts = new List<string>();
        }
    }
}