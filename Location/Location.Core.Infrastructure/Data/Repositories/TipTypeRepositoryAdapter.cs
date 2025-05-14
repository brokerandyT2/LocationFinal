using Location.Core.Application.Common.Interfaces;
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

        public TipTypeRepositoryAdapter(Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository innerRepository)
        {
            _innerRepository = innerRepository ?? throw new ArgumentNullException(nameof(innerRepository));
        }

        public Task<TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
            => _innerRepository.GetByIdAsync(id, cancellationToken);

        public Task<IEnumerable<TipType>> GetAllAsync(CancellationToken cancellationToken = default)
            => _innerRepository.GetAllAsync(cancellationToken);

        public Task<TipType> AddAsync(TipType tipType, CancellationToken cancellationToken = default)
            => _innerRepository.AddAsync(tipType, cancellationToken);

        public void Update(TipType tipType)
            => _innerRepository.Update(tipType);

        public void Delete(TipType tipType)
            => _innerRepository.Delete(tipType);

        public Task<TipType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
            => _innerRepository.GetByNameAsync(name, cancellationToken);

        public Task<TipType?> GetWithTipsAsync(int id, CancellationToken cancellationToken = default)
            => _innerRepository.GetWithTipsAsync(id, cancellationToken);
    }
}