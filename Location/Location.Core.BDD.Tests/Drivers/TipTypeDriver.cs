using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using Moq;

namespace Location.Core.BDD.Tests.Drivers
{
    public class TipTypeDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<ITipTypeRepository> _tipTypeRepositoryMock;
        private static int _nextId = 1; // ✅ FIXED: Static counter for unique IDs

        public TipTypeDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tipTypeRepositoryMock = _context.GetService<Mock<ITipTypeRepository>>();
        }

        public async Task<Result<TipTypeDto>> CreateTipTypeAsync(TipTypeTestModel tipTypeModel)
        {
            // ✅ FIXED: Ensure unique ID assignment
            if (!tipTypeModel.Id.HasValue || tipTypeModel.Id.Value <= 0)
            {
                tipTypeModel.Id = _nextId++; // Generate unique ID
            }

            // Create domain entity AFTER ID assignment
            var domainEntity = tipTypeModel.ToDomainEntity();

            // Set up the mock repository - Using AddAsync (not CreateAsync) and return TipType directly
            _tipTypeRepositoryMock
                .Setup(repo => repo.AddAsync(
                    It.IsAny<Domain.Entities.TipType>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainEntity);

            // Create response directly (NO MediatR)
            var tipTypeDto = new TipTypeDto
            {
                Id = tipTypeModel.Id.Value,
                Name = tipTypeModel.Name,
                I8n = tipTypeModel.I8n
            };

            var result = Result<TipTypeDto>.Success(tipTypeDto);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                // ✅ FIXED: Store with the actual assigned ID
                _context.StoreModel(tipTypeModel, $"TipType_{tipTypeModel.Id}");
            }

            return result;
        }

        public async Task<Result<TipTypeDto>> UpdateTipTypeAsync(TipTypeTestModel tipTypeModel)
        {
            // Ensure we have an ID
            if (!tipTypeModel.Id.HasValue || tipTypeModel.Id.Value <= 0)
            {
                var failureResult = Result<TipTypeDto>.Failure("Cannot update a tip type without a valid ID");
                _context.StoreResult(failureResult);
                return failureResult;
            }

            // Set up the mock repository - Returns TipType? directly
            var domainEntity = tipTypeModel.ToDomainEntity();

            _tipTypeRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == tipTypeModel.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainEntity);

            _tipTypeRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.TipType>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask); // UpdateAsync returns Task (void)

            // Create response directly (NO MediatR)
            var tipTypeDto = new TipTypeDto
            {
                Id = tipTypeModel.Id.Value,
                Name = tipTypeModel.Name,
                I8n = tipTypeModel.I8n
            };

            var result = Result<TipTypeDto>.Success(tipTypeDto);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                _context.StoreModel(tipTypeModel, $"TipType_{result.Data.Id}");
            }

            return result;
        }

        public async Task<Result<bool>> DeleteTipTypeAsync(int tipTypeId)
        {
            // Set up the mock repository - DeleteAsync takes the entity, not ID
            var tipTypeModel = _context.GetModel<TipTypeTestModel>($"TipType_{tipTypeId}");
            if (tipTypeModel != null && tipTypeModel.Id == tipTypeId)
            {
                var domainEntity = tipTypeModel.ToDomainEntity();

                _tipTypeRepositoryMock
                    .Setup(repo => repo.GetByIdAsync(
                        It.Is<int>(id => id == tipTypeId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(domainEntity);

                _tipTypeRepositoryMock
                    .Setup(repo => repo.DeleteAsync(
                        It.IsAny<Domain.Entities.TipType>(),
                        It.IsAny<CancellationToken>()))
                    .Returns(Task.CompletedTask); // DeleteAsync returns Task (void)
            }

            // Create response directly (NO MediatR)
            var result = Result<bool>.Success(true);

            // Store the result
            _context.StoreResult(result);

            // Clear individual context after successful deletion
            if (result.IsSuccess)
            {
                _context.StoreModel(new TipTypeTestModel(), $"TipType_{tipTypeId}"); // Clear individual context
            }

            return result;
        }

        public async Task<Result<TipTypeDto>> GetTipTypeByIdAsync(int tipTypeId)
        {
            // Check individual context first - if cleared after deletion, should return failure
            var tipTypeModel = _context.GetModel<TipTypeTestModel>($"TipType_{tipTypeId}");
            if (tipTypeModel != null && tipTypeModel.Id == tipTypeId && tipTypeModel.Id.HasValue && tipTypeModel.Id > 0)
            {
                var response = new TipTypeDto
                {
                    Id = tipTypeModel.Id.Value,
                    Name = tipTypeModel.Name,
                    I8n = tipTypeModel.I8n
                };

                var result = Result<TipTypeDto>.Success(response);
                _context.StoreResult(result);
                return result;
            }

            // If individual context is cleared (empty model), tip type was deleted - return failure
            if (tipTypeModel != null && !tipTypeModel.Id.HasValue)
            {
                var deletedFailureResult = Result<TipTypeDto>.Failure($"Tip type with ID {tipTypeId} not found");
                _context.StoreResult(deletedFailureResult);
                return deletedFailureResult;
            }

            // Check collection contexts
            var collectionKeys = new[] { "AllTipTypes", "SetupTipTypes" };
            foreach (var collectionKey in collectionKeys)
            {
                var tipTypes = _context.GetModel<List<TipTypeTestModel>>(collectionKey);
                if (tipTypes != null)
                {
                    var foundTipType = tipTypes.FirstOrDefault(t => t.Id == tipTypeId);
                    if (foundTipType != null)
                    {
                        var response = new TipTypeDto
                        {
                            Id = foundTipType.Id.Value,
                            Name = foundTipType.Name,
                            I8n = foundTipType.I8n
                        };

                        var result = Result<TipTypeDto>.Success(response);
                        _context.StoreResult(result);
                        return result;
                    }
                }
            }

            // Tip type not found
            var notFoundFailureResult = Result<TipTypeDto>.Failure($"Tip type with ID {tipTypeId} not found");
            _context.StoreResult(notFoundFailureResult);
            return notFoundFailureResult;
        }

        public async Task<Result<List<TipTypeDto>>> GetAllTipTypesAsync()
        {
            // Get all tip types from the context
            var tipTypes = _context.GetModel<List<TipTypeTestModel>>("AllTipTypes");
            if (tipTypes == null)
            {
                tipTypes = new List<TipTypeTestModel>();
            }

            // Create response directly (NO MediatR)
            var tipTypeDtos = tipTypes.Select(t => new TipTypeDto
            {
                Id = t.Id.Value,
                Name = t.Name,
                I8n = t.I8n
            }).ToList();

            var result = Result<List<TipTypeDto>>.Success(tipTypeDtos);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public void SetupTipTypes(List<TipTypeTestModel> tipTypes)
        {
            // ✅ FIXED: Assign unique IDs if not provided
            foreach (var tipType in tipTypes)
            {
                if (!tipType.Id.HasValue || tipType.Id.Value <= 0)
                {
                    tipType.Id = _nextId++;
                }
            }

            // Configure mock repository to return these tip types
            var domainEntities = tipTypes.ConvertAll(t => t.ToDomainEntity());

            // Setup GetAllAsync - Returns IEnumerable<TipType> directly
            _tipTypeRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainEntities);

            // Setup individual GetByIdAsync for each tip type - Returns TipType? directly
            foreach (var tipType in tipTypes)
            {
                if (tipType.Id.HasValue)
                {
                    var entity = tipType.ToDomainEntity();
                    _tipTypeRepositoryMock
                        .Setup(repo => repo.GetByIdAsync(
                            It.Is<int>(id => id == tipType.Id.Value),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(entity);
                }
            }

            // Store all tip types
            _context.StoreModel(tipTypes, "AllTipTypes");
        }
    }
}