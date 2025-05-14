using Location.Core.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces.Persistence;
namespace Location.Core.Infrastructure.Data.Repositories
{
    public class SettingRepositoryAdapter : ISettingRepository
    {
        private readonly Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository _innerRepository;

        public SettingRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public Task<Setting?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _innerRepository.GetByIdAsync(id, cancellationToken);

        public Task<Setting?> GetByKeyAsync(string key, CancellationToken cancellationToken = default)
            => _innerRepository.GetByKeyAsync(key, cancellationToken);

        public Task<IEnumerable<Setting>> GetAllAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetAllAsync(cancellationToken);

        public Task<IEnumerable<Setting>> GetByKeysAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
            => _innerRepository.GetByKeysAsync(keys, cancellationToken);

        public Task<Setting> AddAsync(Setting setting, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(setting, cancellationToken);

        public void Update(Setting setting)
            => _innerRepository.Update(setting);

        public void Delete(Setting setting)
            => _innerRepository.Delete(setting);

        public Task<Setting> UpsertAsync(string key, string value, string? description = null, CancellationToken cancellationToken = default)
            => _innerRepository.UpsertAsync(key, value, description, cancellationToken);
    }
}
