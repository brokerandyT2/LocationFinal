using Location.Core.Application.Common.Models;

namespace Location.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Base interface for operation results
    /// </summary>
    public interface IResult
    {
        /// <summary>
        /// Indicates whether the operation was successful
        /// </summary>
        bool IsSuccess { get; }

        /// <summary>
        /// Error message if the operation failed
        /// </summary>
        string? ErrorMessage { get; }

        /// <summary>
        /// Collection of detailed errors
        /// </summary>
        IEnumerable<Error> Errors { get; }
    }

    /// <summary>
    /// Generic result interface with data
    /// </summary>
    /// <typeparam name="T">The type of data returned</typeparam>
    public interface IResult<T> : IResult
    {
        /// <summary>
        /// The data returned by the operation
        /// </summary>
        T? Data { get; }
    }
}