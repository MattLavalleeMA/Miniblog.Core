using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Miniblog.Core.Models
{
    public class PagedResultModel<T>
    {
        public IEnumerable<T> Items { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int PageNumber { get; set; }

        public bool HasPreviousPage { get; set; }
        public bool HasNextPage { get; set; }

        public int NextPageNumber { get; set; }
        public int PreviousPageNumber { get; set; }
    }
}