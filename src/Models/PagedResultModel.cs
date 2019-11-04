using System.Collections.Generic;

// ReSharper disable UnusedAutoPropertyAccessor.Global

namespace Miniblog.Core.Models
{
    public class PagedResultModel<T>
    {
        public bool HasNextPage { get; set; }
        public bool HasPreviousPage { get; set; }
        public IEnumerable<T> Items { get; set; }
        public int NextPageNumber { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int PreviousPageNumber { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
    }
}