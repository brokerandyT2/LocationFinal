namespace Location.Photography.Application.Common.Models
{
    public interface IResult
    {
        bool IsSuccess { get; }
        string? ErrorMessage { get; }
    }

    public interface IResult<T> : IResult
    {
        T? Data { get; }
    }
}