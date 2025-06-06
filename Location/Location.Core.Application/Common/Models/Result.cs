using Location.Core.Application.Common.Interfaces;

namespace Location.Core.Application.Common.Models
{
    /// <summary>
    /// Implementation of operation result
    /// </summary>
    public class Result : IResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public IEnumerable<Error> Errors { get; }

        protected Result(bool isSuccess, string? errorMessage, IEnumerable<Error>? errors)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Errors = errors ?? new List<Error>();
        }

        /// <summary>
        /// Creates a successful result
        /// </summary>
        public static Result Success() => new Result(true, null, null);

        /// <summary>
        /// Creates a failure result with an error message
        /// </summary>
        public static Result Failure(string errorMessage) => new Result(false, errorMessage, null);

        /// <summary>
        /// Creates a failure result with multiple errors
        /// </summary>
        public static Result Failure(IEnumerable<Error> errors) => new Result(false, null, errors);

        /// <summary>
        /// Creates a failure result with a single error
        /// </summary>
        public static Result Failure(Error error) => new Result(false, null, new[] { error });
    }

    /// <summary>
    /// Generic implementation of operation result with data
    /// </summary>
    /// <typeparam name="T">The type of data returned</typeparam>
    public class Result<T> : Result, IResult<T>
    {
        private object value;

        public T? Data { get; }

        protected Result(bool isSuccess, T? data, string? errorMessage, IEnumerable<Error>? errors)
            : base(isSuccess, errorMessage, errors)
        {
            Data = data;
        }

        public Result(bool isSuccess, string? errorMessage, IEnumerable<Error>? errors, object value) : base(isSuccess, errorMessage, errors)
        {
            this.value = value;
        }

        /// <summary>
        /// Creates a successful result with data
        /// </summary>
        public static Result<T> Success(T data) => new Result<T>(true, data, null, null);

        /// <summary>
        /// Creates a failure result with an error message
        /// </summary>
        public new static Result<T> Failure(string errorMessage) => new Result<T>(false, default, errorMessage, null);

        /// <summary>
        /// Creates a failure result with multiple errors
        /// </summary>
        public new static Result<T> Failure(IEnumerable<Error> errors) => new Result<T>(false, default, null, errors);

        /// <summary>
        /// Creates a failure result with a single error
        /// </summary>
        public new static Result<T> Failure(Error error) => new Result<T>(false, default, null, new[] { error });

        /// <summary>
        /// Creates a failure result from domain exception
        /// </summary>
        public static Result<T> Failure(Domain.Exceptions.LocationDomainException exception)
        {
            var error = new Error(exception.Code, exception.Message);
            return new Result<T>(false, default, exception.Message, new[] { error });
        }
    }
}