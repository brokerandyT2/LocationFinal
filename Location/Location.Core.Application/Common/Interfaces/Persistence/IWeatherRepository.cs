namespace Location.Core.Application.Common.Interfaces.Persistence
{
    /// <summary>
    /// Repository interface for Weather aggregate root
    /// </summary>
    public interface IWeatherRepository
    {
        Task<Domain.Entities.Weather?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Weather?> GetByLocationIdAsync(int locationId, CancellationToken cancellationToken = default);
        Task<Domain.Entities.Weather> AddAsync(Domain.Entities.Weather weather, CancellationToken cancellationToken = default);
        Task UpdateAsync(Domain.Entities.Weather setting, CancellationToken cancellationToken = default);
        Task DeleteAsync(Domain.Entities.Weather setting, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Weather>> GetRecentAsync(int count = 10, CancellationToken cancellationToken = default);
        Task<IEnumerable<Domain.Entities.Weather>> GetExpiredAsync(TimeSpan maxAge, CancellationToken cancellationToken = default);
    }
}