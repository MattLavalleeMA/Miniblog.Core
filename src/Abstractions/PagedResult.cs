// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using System;
using System.Collections.Generic;
using System.Linq;

namespace Miniblog.Core.Abstractions
{
    /// <summary>
    ///     Defines the <see cref="PagedResult{T}" />
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PagedResult<T>
    {
        private readonly IEnumerable<T> _source;

        /// <summary>
        ///     Initializes a new instance of the <see cref="PagedResult{T}" /> class.
        /// </summary>
        /// <param name="source">The source<see cref="IQueryable{T}" /></param>
        /// <param name="pageSize">The pageSize<see cref="int" /></param>
        /// <param name="pageNumber">The pageNumber<see cref="int" /></param>
        public PagedResult(IEnumerable<T> source, int pageSize = 5, int pageNumber = 1)
        {
            _source = source;

            PageSize = pageSize;
            PageNumber = pageNumber;
        }

        /// <summary>
        ///     Gets a value indicating whether HasNextPage
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        ///     Gets a value indicating whether HasPreviousPage
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        ///     Gets the Items
        /// </summary>
        public List<T> Items => _source
            .Skip(PageSize * (PageNumber - 1))
            .Take(PageSize)
            .ToList();

        /// <summary>
        ///     Gets the NextPageNumber
        /// </summary>
        public int NextPageNumber => HasNextPage ? PageNumber + 1 : 0;

        /// <summary>
        ///     Gets the PageNumber
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        ///     Gets the PageSize
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        ///     Gets the PreviousPageNumber
        /// </summary>
        public int PreviousPageNumber => HasPreviousPage ? PageNumber - 1 : 0;

        /// <summary>
        ///     Gets the TotalItems
        /// </summary>
        public int TotalItems => _source.Count();

        /// <summary>
        ///     Gets the TotalPages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalItems / (double)PageSize);
    }
}