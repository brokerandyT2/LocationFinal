using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Location.Core.Infrastructure.Data.Repositories
{
    public class TipTypeRepository : ITipTypeRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<TipTypeRepository> _logger;
        public TipTypeRepository(IDatabaseContext context, ILogger<TipTypeRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.GetAsync<TipTypeEntity>(id);
                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tip type with ID {TipTypeId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<TipType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<TipTypeEntity>().ToListAsync();
                return entities.Select(MapToDomain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tip types");
                throw;
            }
        }

        public async Task<TipType> AddAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = MapToEntity(tipType);
                await _context.InsertAsync(entity);

                // Update domain object with generated ID
                SetPrivateProperty(tipType, "Id", entity.Id);

                _logger.LogInformation("Created tip type with ID {TipTypeId}", entity.Id);
                return tipType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tip type");
                throw;
            }
        }

        public void Update(TipType tipType)
        {
            try
            {
                var entity = MapToEntity(tipType);
                _context.UpdateAsync(entity).GetAwaiter().GetResult();
                _logger.LogInformation("Updated tip type with ID {TipTypeId}", tipType.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tip type with ID {TipTypeId}", tipType.Id);
                throw;
            }
        }

        public void Delete(TipType tipType)
        {
            try
            {
                var entity = MapToEntity(tipType);
                _context.DeleteAsync(entity).GetAwaiter().GetResult();
                _logger.LogInformation("Deleted tip type with ID {TipTypeId}", tipType.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tip type with ID {TipTypeId}", tipType.Id);
                throw;
            }
        }

        #region Mapping Methods

        private TipType MapToDomain(TipTypeEntity entity)
        {
            // Create tip type using reflection
            var tipType = CreateTipTypeViaReflection(entity.Name);

            // Set properties
            SetPrivateProperty(tipType, "Id", entity.Id);
            SetPrivateProperty(tipType, "I8n", entity.I8n);

            return tipType;
        }

        private TipTypeEntity MapToEntity(TipType tipType)
        {
            return new TipTypeEntity
            {
                Id = tipType.Id,
                Name = tipType.Name,
                I8n = tipType.I8n
            };
        }

        private TipType CreateTipTypeViaReflection(string name)
        {
            var type = typeof(TipType);
            var constructor = type.GetConstructor(new[] { typeof(string) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find TipType constructor");
            }

            return (TipType)constructor.Invoke(new object[] { name });
        }

        private void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property == null)
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
            else
            {
                property.SetValue(obj, value);
            }
        }

        #endregion
    }
}
