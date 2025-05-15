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

                // After updating the TipType, we need to handle the Tips collection
                // Delete existing tips for this type and re-add them
                var existingTips = _context.Table<TipEntity>()
                    .Where(t => t.TipTypeId == tipType.Id)
                    .ToListAsync().GetAwaiter().GetResult();

                foreach (var existingTip in existingTips)
                {
                    _context.DeleteAsync(existingTip).GetAwaiter().GetResult();
                }

                // Add the current tips
                foreach (var tip in tipType.Tips)
                {
                    var tipEntity = MapTipToEntity(tip);
                    tipEntity.TipTypeId = tipType.Id; // Ensure the TipTypeId is set
                    _context.InsertAsync(tipEntity).GetAwaiter().GetResult();
                }

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

        public async Task<TipType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Table<TipTypeEntity>()
                    .Where(t => t.Name == name)
                    .FirstOrDefaultAsync();

                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tip type by name {Name}", name);
                throw;
            }
        }

        public async Task<TipType?> GetWithTipsAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var tipTypeEntity = await _context.GetAsync<TipTypeEntity>(id);
                if (tipTypeEntity == null)
                {
                    return null;
                }

                var tipType = MapToDomain(tipTypeEntity);

                // Load related tips
                var tipEntities = await _context.Table<TipEntity>()
                    .Where(t => t.TipTypeId == id)
                    .ToListAsync();

                foreach (var tipEntity in tipEntities)
                {
                    var tip = CreateTipFromEntity(tipEntity);
                    tipType.AddTip(tip);
                }

                return tipType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tip type with tips for ID {TipTypeId}", id);
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

        private TipEntity MapTipToEntity(Tip tip)
        {
            return new TipEntity
            {
                Id = tip.Id,
                TipTypeId = tip.TipTypeId,
                Title = tip.Title,
                Content = tip.Content,
                Fstop = tip.Fstop,
                ShutterSpeed = tip.ShutterSpeed,
                Iso = tip.Iso,
                I8n = tip.I8n
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

        private Tip CreateTipFromEntity(TipEntity entity)
        {
            var type = typeof(Tip);
            var constructor = type.GetConstructor(
                new[] { typeof(int), typeof(string), typeof(string) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Tip constructor");
            }

            var tip = (Tip)constructor.Invoke(new object[] { entity.TipTypeId, entity.Title, entity.Content });

            // Set properties
            SetPrivateProperty(tip, "Id", entity.Id);
            SetPrivateProperty(tip, "_fstop", entity.Fstop);
            SetPrivateProperty(tip, "_shutterSpeed", entity.ShutterSpeed);
            SetPrivateProperty(tip, "_iso", entity.Iso);
            SetPrivateProperty(tip, "I8n", entity.I8n);

            return tip;
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