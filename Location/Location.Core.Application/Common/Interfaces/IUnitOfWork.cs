namespace Location.Core.Application.Common.Interfaces
{
    /// <summary>
    /// Unit of Work pattern interface for managing database transactions
    /// </summary>
    public interface IUnitOfWork : IDisposable
    {
        /// <summary>
        /// Repository for Location entities
        /// </summary>
        ILocationRepository Locations { get; }

        /// <summary>
        /// Repository for Weather entities
        /// </summary>
        IWeatherRepository Weather { get; }

        /// <summary>
        /// Repository for Tip entities
        /// </summary>
        ITipRepository Tips { get; }

        /// <summary>
        /// Repository for TipType entities
        /// </summary>
        ITipTypeRepository TipTypes { get; }



        /// <summary>
        /// Repository for Setting entities
        /// </summary>
        ISettingRepository Settings { get; }
        ISubscriptionRepository Subscriptions { get; }
        /// <summary>
        /// Commits all changes made in this unit of work
        /// </summary>
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Begins a new transaction
        /// </summary>
        Task BeginTransactionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Commits the current transaction
        /// </summary>
        Task CommitAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Rolls back the current transaction
        /// </summary>
        Task RollbackAsync(CancellationToken cancellationToken = default);
    }
}