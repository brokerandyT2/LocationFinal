using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.Infrastructure.Extensions
{
    /// <summary>
    /// Extension methods for repository interfaces to match expected method signatures
    /// </summary>
    public static class RepositoryExtensions
    {
        /// <summary>
        /// Extension method to provide CreateAsync functionality for repositories that have AddAsync
        /// </summary>
        public static Task<Result<Core.Domain.Entities.TipType>> CreateAsync(
            this ITipTypeRepository repository,
            Core.Domain.Entities.TipType entity,
            CancellationToken cancellationToken = default)
        {
            // Call the actual method available on the repository
            // Replace with the actual method name your repository implements
            return repository.CreateAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Extension method to provide CreateAsync functionality for ITipRepository
        /// </summary>
        public static Task<Result<Core.Domain.Entities.Tip>> CreateAsync(
            this ITipRepository repository,
            Core.Domain.Entities.Tip entity,
            CancellationToken cancellationToken = default)
        {
            // Call the actual method available on the repository
            return repository.CreateAsync(entity, cancellationToken);
        }

        /// <summary>
        /// Extension method to provide CreateAsync functionality for ILocationRepository
        /// </summary>
        public static Task<Result<Core.Domain.Entities.Location>> CreateAsync(
            this ILocationRepository repository,
            Core.Domain.Entities.Location entity,
            CancellationToken cancellationToken = default)
        {
            // Call the actual method available on the repository
            return repository.CreateAsync(entity, cancellationToken);
        }
    }
}