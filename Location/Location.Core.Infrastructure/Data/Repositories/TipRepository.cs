using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace Location.Core.Infrastructure.Data.Repositories
{
    public class TipRepository : ITipRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<TipRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        public TipRepository(IDatabaseContext context, ILogger<TipRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        public async Task<Tip?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<TipEntity>(id);
                    return entity != null ? MapToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetById",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<Tip>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.Table<TipEntity>().ToListAsync();
                    return entities.Select(MapToDomain);
                },
                _exceptionMapper,
                "GetAll",
                "tip",
                _logger);
        }

        public async Task<IEnumerable<Tip>> GetByTipTypeIdAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.Table<TipEntity>()
                        .Where(t => t.TipTypeId == tipTypeId)
                        .ToListAsync();
                    return entities.Select(MapToDomain);
                },
                _exceptionMapper,
                "GetByTipTypeId",
                "tip",
                _logger);
        }

        public async Task<Tip> AddAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(tip);
                    await _context.InsertAsync(entity);

                    // Update domain object with generated ID
                    SetPrivateProperty(tip, "Id", entity.Id);

                    _logger.LogInformation("Created tip with ID {TipId}", entity.Id);
                    return tip;
                },
                _exceptionMapper,
                "Add",
                "tip",
                _logger);
        }

        public async Task UpdateAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(tip);
                    await _context.UpdateAsync(entity);
                    _logger.LogInformation("Updated tip with ID {TipId}", tip.Id);
                },
                _exceptionMapper,
                "Update",
                "tip",
                _logger);
        }

        public async Task DeleteAsync(Tip tip, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(tip);
                    await _context.DeleteAsync(entity);
                    _logger.LogInformation("Deleted tip with ID {TipId}", tip.Id);
                },
                _exceptionMapper,
                "Delete",
                "tip",
                _logger);
        }

        public async Task<Tip?> GetByTitleAsync(string title, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.Table<TipEntity>()
                        .Where(t => t.Title == title)
                        .FirstOrDefaultAsync();

                    return entity != null ? MapToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetByTitle",
                "tip",
                _logger);
        }

        public async Task<Tip?> GetRandomByTypeAsync(int tipTypeId, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
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
                },
                _exceptionMapper,
                "GetRandomByType",
                "tip",
                _logger);
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