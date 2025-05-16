using System;

namespace Location.Photography.Application.Common.Models
{
    /// <summary>
    /// Represents the result of an operation with error handling
    /// </summary>
    public class Result<T>
    {
        private Result(bool isSuccess, T data, string message, Exception exception = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            Message = message;
            Exception = exception;
        }

        /// <summary>
        /// Whether the operation was successful
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// The data returned by the operation (if successful)
        /// </summary>
        public T Data { get; }

        /// <summary>
        /// Descriptive message (error message if operation failed)
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// Exception that caused the failure (if applicable)
        /// </summary>
        public Exception Exception { get; }

        /// <summary>
        /// Creates a successful result with data
        /// </summary>
        public static Result<T> Success(T data, string message = null)
        {
            return new Result<T>(true, data, message ?? "Operation completed successfully");
        }

        /// <summary>
        /// Creates a failure result with error details
        /// </summary>
        public static Result<T> Failure(string message, Exception exception = null)
        {
            return new Result<T>(false, default, message, exception);
        }
    }

    /// <summary>
    /// Static class for creating Result objects
    /// </summary>
    public static class Result
    {
        /// <summary>
        /// Creates a successful result with data
        /// </summary>
        public static Result<T> Success<T>(T data, string message = null)
        {
            return Result<T>.Success(data, message);
        }

        /// <summary>
        /// Creates a failure result with error details
        /// </summary>
        public static Result<T> Failure<T>(string message, Exception exception = null)
        {
            return Result<T>.Failure(message, exception);
        }
    }
}