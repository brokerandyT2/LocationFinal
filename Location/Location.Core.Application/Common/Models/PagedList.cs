using System;
using System.Collections.Generic;
using System.Linq;

namespace Location.Core.Application.Common.Models
{
    /// <summary>
    /// Represents a paged list of items with optimized construction
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
        /// Creates a new paged list with pre-calculated count
        /// </summary>
        public PagedList(IEnumerable<T> items, int totalCount, int pageNumber, int pageSize)
        {
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalCount = totalCount;
            Items = items.ToList().AsReadOnly();
        }

        /// <summary>
        /// Creates a paged list from a queryable source - PERFORMANCE OPTIMIZED
        /// This should only be used when the repository cannot provide optimized paging
        /// </summary>
        [Obsolete("Use repository-level paging with GetPagedAsync for better performance")]
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
        /// Creates a paged list from a list source - AVOID IN PRODUCTION
        /// This materializes the entire list in memory before paging
        /// </summary>
        [Obsolete("Use repository-level paging with GetPagedAsync for better performance")]
        public static PagedList<T> Create(IList<T> source, int pageNumber, int pageSize)
        {
            var count = source.Count;
            var items = source
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new PagedList<T>(items, count, pageNumber, pageSize);
        }

        /// <summary>
        /// Creates an optimized paged list when you already have the paged items and total count
        /// This is the preferred method for repository-level paging
        /// </summary>
        public static PagedList<T> CreateOptimized(IEnumerable<T> pagedItems, int totalCount, int pageNumber, int pageSize)
        {
            return new PagedList<T>(pagedItems, totalCount, pageNumber, pageSize);
        }
    }
}