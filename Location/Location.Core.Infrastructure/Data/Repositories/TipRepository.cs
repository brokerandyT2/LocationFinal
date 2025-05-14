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
    public class TipRepository : Location.Core.Application.Common.Interfaces.Persistence.ITipRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<TipRepository> _logger;

        public TipRepository(IDatabaseContext context, ILogger<TipRepository> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<Tip?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.GetAsync<TipEntity>(id);
                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tip with ID {TipId}", id);
                throw;
            }
        }

        public async Task<IEnumerable<Tip>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<TipEntity>().ToListAsync();
                return entities.Select(MapToDomain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all tips");
                throw;
            }
        }

        public async Task<IEnumerable<Tip>> GetByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<TipEntity>()
                    .Where(t => t.TipTypeId == tipTypeId)
                    .ToListAsync();

                return entities.Select(MapToDomain);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tips for type {TipTypeId}", tipTypeId);
                throw;
            }
        }

        public async Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = MapToEntity(tip);
                await _context.InsertAsync(entity);

                // Update domain object with generated ID
                SetPrivateProperty(tip, "Id", entity.Id);

                _logger.LogInformation("Created tip with ID {TipId}", entity.Id);
                return tip;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating tip");
                throw;
            }
        }

        public void Update(Tip tip)
        {
            try
            {
                var entity = MapToEntity(tip);
                _context.UpdateAsync(entity).GetAwaiter().GetResult();
                _logger.LogInformation("Updated tip with ID {TipId}", tip.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating tip with ID {TipId}", tip.Id);
                throw;
            }
        }

        public void Delete(Tip tip)
        {
            try
            {
                var entity = MapToEntity(tip);
                _context.DeleteAsync(entity).GetAwaiter().GetResult();
                _logger.LogInformation("Deleted tip with ID {TipId}", tip.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting tip with ID {TipId}", tip.Id);
                throw;
            }
        }

        public async Task<Tip?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            try
            {
                var entity = await _context.Table<TipEntity>()
                    .Where(t => t.Title == title)
                    .FirstOrDefaultAsync();

                return entity != null ? MapToDomain(entity) : null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving tip by title {Title}", title);
                throw;
            }
        }

        public async Task<Tip?> GetRandomByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            try
            {
                var entities = await _context.Table<TipEntity>()
                    .Where(t => t.TipTypeId == tipTypeId)
                    .ToListAsync();

                if (!entities.Any())
                {
                    return null;
                }

                var random = new Random();
                var randomEntity = entities[random.Next(entities.Count)];
                return MapToDomain(randomEntity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving random tip for type {TipTypeId}", tipTypeId);
                throw;
            }
        }

        #region Mapping Methods

        private Tip MapToDomain(TipEntity entity)
        {
            // Create tip using reflection
            var tip = CreateTipViaReflection(entity.TipTypeId, entity.Title, entity.Content);

            // Set properties
            SetPrivateProperty(tip, "Id", entity.Id);
            SetPrivateProperty(tip, "_fstop", entity.Fstop);
            SetPrivateProperty(tip, "_shutterSpeed", entity.ShutterSpeed);
            SetPrivateProperty(tip, "_iso", entity.Iso);
            SetPrivateProperty(tip, "I8n", entity.I8n);

            return tip;
        }

        private TipEntity MapToEntity(Tip tip)
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

        private Tip CreateTipViaReflection(int tipTypeId, string title, string content)
        {
            var type = typeof(Tip);
            var constructor = type.GetConstructor(
                new[] { typeof(int), typeof(string), typeof(string) });

            if (constructor == null)
            {
                throw new InvalidOperationException("Cannot find Tip constructor");
            }

            return (Tip)constructor.Invoke(new object[] { tipTypeId, title, content });
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