namespace Location.Core.Domain.Interfaces
{
    /// <summary>
    /// Marker interface for domain events
    /// </summary>
    public interface IDomainEvent
    {
        DateTimeOffset DateOccurred { get; }
    }
}