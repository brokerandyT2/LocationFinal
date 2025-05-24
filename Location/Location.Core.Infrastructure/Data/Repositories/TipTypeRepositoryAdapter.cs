using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Location.Core.Infrastructure.Data.Repositories
{
    public class TipTypeRepositoryAdapter : ITipTypeRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository _innerRepository;
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository _innerLocationRepository;
        Location.Core.Application.Common.Interfaces.Persistence.ITipRepository _innerTipRepository;
        public TipTypeRepositoryAdapter(
            Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository innerRepository,
            Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository locationRepository,
            Location.Core.Application.Common.Interfaces.Persistence.ITipRepository tipRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
            _innerLocationRepository = locationRepository ?? throw new ArgumentNullException(nameof(locationRepository));
            _innerTipRepository = tipRepository ?? throw new ArgumentNullException(nameof(tipRepository));
        }
        public Task<TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _innerRepository.GetByIdAsync(id, cancellationToken);

        public Task<IEnumerable<TipType>> GetAllAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetAllAsync(cancellationToken);

        public Task<TipType> AddAsync(TipType tipType, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(tipType, cancellationToken);

        public async Task UpdateAsync(TipType tipType, CancellationToken cancellationToken = default)
            => await _innerRepository.UpdateAsync(tipType, cancellationToken);

        public async Task DeleteAsync(TipType tipType, CancellationToken cancellationToken = default)
            => await _innerRepository.DeleteAsync(tipType, cancellationToken);
        public async Task<Result<TipType>> CreateEntityAsync(TipType entity, CancellationToken cancellationToken = default)
        {
            try
            {
                // Call the inner repository's AddAsync method to persist the entity
                var createdEntity = await _innerRepository.AddAsync(entity, cancellationToken);

                // Return a successful result with the created entity
                return Result<TipType>.Success(createdEntity);
            }
            catch (Exception ex)
            {
                // Return a failure result with the exception message
                return Result<TipType>.Failure($"Failed to create TipType: {ex.Message}");
            }
        }

        public async Task<Result<Tip>> CreateEntityAsync(Tip entity, CancellationToken cancellationToken = default)
        {
            try
            {
                // Since this adapter is for TipTypeRepository, we need to use the appropriate repository
                // Assuming we can access the TipRepository through some means like a service provider
                // or through the TipType's relationship

                // If the inner repository has access to tip creation:
                //var createdTip = await _innerRepository.AddAsync(entity, cancellationToken);

                // If there's no direct method, you might need to inject ITipRepository separately
                var createdTip = await _innerTipRepository.AddAsync(entity, cancellationToken);

                return Result<Tip>.Success(createdTip);
            }
            catch (Exception ex)
            {
                return Result<Tip>.Failure($"Failed to create Tip: {ex.Message}");
            }
        }

        public async Task<Result<Location.Core.Domain.Entities.Location>> CreateEntityAsync(Location.Core.Domain.Entities.Location entity, CancellationToken cancellationToken = default)
        {
            try
            {
                // Similar to the Tip case, this adapter is for TipTypeRepository
                // We would need to access the LocationRepository appropriately

                // If the inner repository has access to location creation:
                var createdLocation = await _innerLocationRepository.AddAsync(entity, cancellationToken);

                // If there's no direct method, you might need to inject ILocationRepository separately
                // var createdLocation = await _locationRepository.AddAsync(entity, cancellationToken);

                return Result<Location.Core.Domain.Entities.Location>.Success(createdLocation);
            }
            catch (Exception ex)
            {
                return Result<Location.Core.Domain.Entities.Location>.Failure($"Failed to create Location: {ex.Message}");
            }
        }
    }
}