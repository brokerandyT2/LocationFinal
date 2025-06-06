// Location.Photography.Application/Common/Models/Result.cs
namespace Location.Photography.Application.Common.Models
{
    public class Result<T>
    {
        public bool IsSuccess { get; private set; }
        public T Data { get; private set; }
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        private Result(bool isSuccess, T data, string errorMessage, Exception exception = null)
        {
            IsSuccess = isSuccess;
            Data = data;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result<T> Success(T data)
        {
            return new Result<T>(true, data, string.Empty, null);
        }

        public static Result<T> Failure(string errorMessage)
        {
            return new Result<T>(false, default, errorMessage, null);
        }

        public static Result<T> Failure(string errorMessage, Exception exception)
        {
            return new Result<T>(false, default, errorMessage, exception);
        }

        public static implicit operator Result(Result<T> result)
        {
            return result.IsSuccess
                ? Result.Success()
                : Result.Failure(result.ErrorMessage, result.Exception);
        }

        public Result<TDestination> MapTo<TDestination>(Func<T, TDestination> mapper)
        {
            if (!IsSuccess)
                return Result<TDestination>.Failure(ErrorMessage, Exception);

            try
            {
                var destination = mapper(Data);
                return Result<TDestination>.Success(destination);
            }
            catch (Exception ex)
            {
                return Result<TDestination>.Failure($"Error mapping data: {ex.Message}", ex);
            }
        }
    }

    public class Result
    {
        public bool IsSuccess { get; private set; }
        public string ErrorMessage { get; private set; }
        public Exception Exception { get; private set; }

        private Result(bool isSuccess, string errorMessage, Exception exception = null)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static Result Success()
        {
            return new Result(true, string.Empty, null);
        }

        public static Result Failure(string errorMessage)
        {
            return new Result(false, errorMessage, null);
        }

        public static Result Failure(string errorMessage, Exception exception)
        {
            return new Result(false, errorMessage, exception);
        }
    }
}