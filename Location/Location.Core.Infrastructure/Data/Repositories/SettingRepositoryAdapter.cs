using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Domain.Entities;
namespace Location.Core.Infrastructure.Data.Repositories
{
    public class SettingRepositoryAdapter : ISettingRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository _innerRepository;
        public SettingRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public async Task<Result<Setting>> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var setting = await _innerRepository.GetByKeyAsync(key, cancellationToken);
                return setting != null
                    ? Result<Setting>.Success(setting)
                    : Result<Setting>.Failure($"Setting with key '{key}' not found");
            }
            catch (Exception ex)
            {
                return Result<Setting>.Failure($"Failed to retrieve setting: {ex.Message}");
            }
        }

        public async Task<Result<List<Setting>>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _innerRepository.GetAllAsync(cancellationToken);
                return Result<List<Setting>>.Success(settings.ToList());
            }
            catch (Exception ex)
            {
                return Result<List<Setting>>.Failure($"Failed to retrieve settings: {ex.Message}");
            }
        }

        public async Task<Result<Setting>> CreateAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            try
            {
                var created = await _innerRepository.AddAsync(setting, cancellationToken);
                return Result<Setting>.Success(created);
            }
            catch (Exception ex)
            {
                return Result<Setting>.Failure($"Failed to create setting: {ex.Message}");
            }
        }

        public async Task<Result<Setting>> UpdateAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            try
            {
                await _innerRepository.UpdateAsync(setting, cancellationToken);
                return Result<Setting>.Success(setting);
            }
            catch (Exception ex)
            {
                return Result<Setting>.Failure($"Failed to update setting: {ex.Message}");
            }
        }

        public async Task<Result<bool>> DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            try
            {
                var setting = await _innerRepository.GetByKeyAsync(key, cancellationToken);
                if (setting == null)
                {
                    return Result<bool>.Failure($"Setting with key '{key}' not found");
                }

                _innerRepository.DeleteAsync(setting);
                return Result<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return Result<bool>.Failure($"Failed to delete setting: {ex.Message}");
            }
        }

        public async Task<Result<Setting>> UpsertAsync(Setting setting, CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await _innerRepository.UpsertAsync(setting.Key, setting.Value, setting.Description, cancellationToken);
                return Result<Setting>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<Setting>.Failure($"Failed to upsert setting: {ex.Message}");
            }
        }

        public async Task<Result<Dictionary<string, string>>> GetAllAsDictionaryAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var settings = await _innerRepository.GetAllAsync(cancellationToken);
                var dictionary = settings.ToDictionary(s => s.Key, s => s.Value);
                return Result<Dictionary<string, string>>.Success(dictionary);
            }
            catch (Exception ex)
            {
                return Result<Dictionary<string, string>>.Failure($"Failed to retrieve settings as dictionary: {ex.Message}");
            }
        }
    }
}