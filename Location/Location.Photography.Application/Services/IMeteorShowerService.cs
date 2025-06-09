using Location.Photography.Domain.Entities;

namespace Location.Photography.Application.Services
{
    public interface IMeteorShowerService
    {
        Task<List<MeteorShower>> GetActiveShowersAsync(DateTime date);
        Task<MeteorShower> GetShowerByCodeAsync(string code);
    }
}
