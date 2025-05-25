using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure.Data.Entities;
using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Location.Core.Infrastructure.Data.Repositories
{
    public class TipTypeRepository : ITipTypeRepository
    {
        private readonly IDatabaseContext _context;
        private readonly ILogger<TipTypeRepository> _logger;
        private readonly IInfrastructureExceptionMappingService _exceptionMapper;

        public TipTypeRepository(IDatabaseContext context, ILogger<TipTypeRepository> logger, IInfrastructureExceptionMappingService exceptionMapper)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _exceptionMapper = exceptionMapper ?? throw new ArgumentNullException(nameof(exceptionMapper));
        }

        public async Task<TipType?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.GetAsync<TipTypeEntity>(id);
                    return entity != null ? MapToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetById",
                "tiptype",
                _logger);
        }

        public async Task<IEnumerable<TipType>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entities = await _context.Table<TipTypeEntity>().ToListAsync();
                    return entities.Select(MapToDomain);
                },
                _exceptionMapper,
                "GetAll",
                "tiptype",
                _logger);
        }

        public async Task<TipType> AddAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(tipType);
                    await _context.InsertAsync(entity);

                    // Update domain object with generated ID
                    SetPrivateProperty(tipType, "Id", entity.Id);

                    _logger.LogInformation("Created tip type with ID {TipTypeId}", entity.Id);
                    return tipType;
                },
                _exceptionMapper,
                "Add",
                "tiptype",
                _logger);
        }

        public async Task UpdateAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(tipType);
                    await _context.UpdateAsync(entity);

                    // After updating the TipType, we need to handle the Tips collection
                    // Delete existing tips for this type and re-add them
                    var existingTips = await _context.Table<TipEntity>()
                        .Where(t => t.TipTypeId == tipType.Id)
                        .ToListAsync();

                    foreach (var existingTip in existingTips)
                    {
                        await _context.DeleteAsync(existingTip);
                    }

                    // Add the current tips
                    foreach (var tip in tipType.Tips)
                    {
                        var tipEntity = MapTipToEntity(tip);
                        tipEntity.TipTypeId = tipType.Id; // Ensure the TipTypeId is set
                        await _context.InsertAsync(tipEntity);
                    }

                    _logger.LogInformation("Updated tip type with ID {TipTypeId}", tipType.Id);
                },
                _exceptionMapper,
                "Update",
                "tiptype",
                _logger);
        }

        public async Task DeleteAsync(TipType tipType, CancellationToken cancellationToken = default)
        {
            await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = MapToEntity(tipType);
                    await _context.DeleteAsync(entity);
                    _logger.LogInformation("Deleted tip type with ID {TipTypeId}", tipType.Id);
                },
                _exceptionMapper,
                "Delete",
                "tiptype",
                _logger);
        }

        public async Task<TipType?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
                {
                    var entity = await _context.Table<TipTypeEntity>()
                        .Where(t => t.Name == name)
                        .FirstOrDefaultAsync();

                    return entity != null ? MapToDomain(entity) : null;
                },
                _exceptionMapper,
                "GetByName",
                "tiptype",
                _logger);
        }

        public async Task<TipType?> GetWithTipsAsync(int id, CancellationToken cancellationToken = default)
        {
            return await RepositoryExceptionWrapper.ExecuteWithExceptionMappingAsync(
                async () =>
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
                },
                _exceptionMapper,
                "GetWithTips",
                "tiptype",
                _logger);
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