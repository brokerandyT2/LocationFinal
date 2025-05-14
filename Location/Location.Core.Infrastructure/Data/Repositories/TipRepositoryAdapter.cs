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
    public class TipRepositoryAdapter : ITipRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ITipRepository _innerRepository;
        public TipRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.ITipRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public async Task<Result<Tip>> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var tip = await _innerRepository.GetByIdAsync(id, cancellationToken);
                return tip != null
                    ? Result<Tip>.Success(tip)
                    : Result<Tip>.Failure($"Tip with ID {id} not found");
            }
            catch (Exception ex)
            {
                return Result<Tip>.Failure($"Failed to retrieve tip: {ex.Message}");
            }
        }

        public async Task<Result<List<Tip>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var tips = await _innerRepository.GetAllAsync(cancellationToken);
                return Result<List<Tip>>.Success(tips.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Tip>>.Failure($"Failed to retrieve tips: {ex.Message}");
            }
        }

        public async Task<Result<List<Tip>>> GetByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tips = await _innerRepository.GetByTipTypeIdAsync(tipTypeId, cancellationToken);
                return Result<List<Tip>>.Success(tips.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Tip>>.Failure($"Failed to retrieve tips by type: {ex.Message}");
            }
        }

        public async Task<Result<Tip>> CreateAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _innerRepository.AddAsync(tip, cancellationToken);
                return Result<Tip>.Success(created);
            }
            catch (Exception ex)
            {
                return Result<Tip>.Failure($"Failed to create tip: {ex.Message}");
            }
        }

        public async Task<Result<Tip>> UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            try
            {
                _innerRepository.Update(tip);
                return Result<Tip>.Success(tip);
            }
            catch (Exception ex)
            {
                return Result<Tip>.Failure($"Failed to update tip: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var tip = await _innerRepository.GetByIdAsync(id, cancellationToken);
                if (tip == null)
                {
                    return Result<bool>.Failure($"Tip with ID {id} not found");
                }

                _innerRepository.Delete(tip);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete tip: {ex.Message}");
            }
        }

        public async Task<Result<Tip>> GetRandomByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var tip = await _innerRepository.GetRandomByTypeAsync(tipTypeId, cancellationToken);
                return tip != null
                    ? Result<Tip>.Success(tip)
                    : Result<Tip>.Failure($"No tips found for type ID {tipTypeId}");
            }
            catch (Exception ex)
            {
                return Result<Tip>.Failure($"Failed to retrieve random tip: {ex.Message}");
            }
        }

        public async Task<Result<List<TipType>>> GetTipTypesAsync(CancellationToken cancellationToken = default)
        {
            // This would be implemented if we had a separate TipTypeRepository method
            // For now, returning failure
            return Result<List<TipType>>.Failure("GetTipTypes not implemented in persistence layer");
        }

        public async Task<Result<TipType>> CreateTipTypeAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            // This would be implemented if we had a separate TipTypeRepository method
            // For now, returning failure
            return Result<TipType>.Failure("CreateTipType not implemented in persistence layer");
        }
    }
}