using System;
using System.Collections.Generic;
using System.Linq;

namespace Location.Core.Application.Common.Models
{
    /// <summary>
    /// Represents a paged list of items
    /// </summary>
    /// <typeparam name="T">The type of items in the list</typeparam>
    public class PagedList<T>
    {
        /// <summary>
        /// The items in the current page
        /// </summary>
        public IReadOnlyList<T> Items { get; }

        /// <summary>
        /// Current page number (1-based)
        /// </summary>
        public int PageNumber { get; }

        /// <summary>
        /// Number of items per page
        /// </summary>
        public int PageSize { get; }

        /// <summary>
        /// Total number of items across all pages
        /// </summary>
        public int TotalCount { get; }

        /// <summary>
        /// Total number of pages
        /// </summary>
        public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);

        /// <summary>
        /// Indicates whether there is a previous page
        /// </summary>
        public bool HasPreviousPage => PageNumber > 1;

        /// <summary>
        /// Indicates whether there is a next page
        /// </summary>
        public bool HasNextPage => PageNumber < TotalPages;

        /// <summary>
        /// Creates a new paged list
        /// </summary>
        public PagedList(IEnumerable<T> items, int count, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = count;
            Items = items.ToList().AsReadOnly();
        }

        /// <summary>
        /// Creates a paged list from a queryable source
        /// </summary>
        public static PagedList<T> Create(IQueryable<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count();
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates a paged list from a list source
        /// </summary>
        public static PagedList<T> Create(IList<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count;
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }
    }
}