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

        public TipTypeDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tipTypeRepositoryMock = _context.GetService<Mock<ITipTypeRepository>>();
        }

        public async Task<Result<TipTypeDto>> CreateTipTypeAsync(TipTypeTestModel tipTypeModel)
        {
            // Ensure ID is assigned BEFORE creating domain entity
            if (!tipTypeModel.Id.HasValue || tipTypeModel.Id.Value <= 0)
            {
                tipTypeModel.Id = 1;
            }

            // Create domain entity AFTER ID assignment
            var domainEntity = tipTypeModel.ToDomainEntity();

            // Set up the mock repository
            _tipTypeRepositoryMock
                .Setup(repo => repo.AddAsync(
                    It.IsAny<Domain.Entities.TipType>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainEntity);

            _tipTypeRepositoryMock
                .Setup(repo => repo.CreateEntityAsync(
                    It.IsAny<Domain.Entities.TipType>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.TipType>.Success(domainEntity));

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
                _context.StoreModel(tipTypeModel, $"TipType_{tipTypeModel.Id}");
                _context.StoreModel(tipTypeModel, "LatestTipType");
            }

            return result;
        }

        public async Task<Result<TipTypeDto>> GetTipTypeByIdAsync(int tipTypeId)
        {
            // Check individual context first
            var tipTypeModel = _context.GetModel<TipTypeTestModel>($"TipType_{tipTypeId}");
            if (tipTypeModel != null && tipTypeModel.Id.HasValue && tipTypeModel.Id > 0)
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

            // Check collection contexts
            var collectionKeys = new[] { "AllTipTypes", "SetupTipTypes" };
            foreach (var collectionKey in collectionKeys)
            {
                var tipTypes = _context.GetModel<List<TipTypeTestModel>>(collectionKey);
                if (tipTypes != null)
                {
                    var foundTipType = tipTypes.FirstOrDefault(tt => tt.Id == tipTypeId);
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
            var failureResult = Result<TipTypeDto>.Failure($"Tip type with ID {tipTypeId} not found");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        public async Task<Result<List<TipTypeDto>>> GetAllTipTypesAsync()
        {
            // Get all tip types from context
            var tipTypes = _context.GetModel<List<TipTypeTestModel>>("AllTipTypes");
            if (tipTypes == null)
            {
                tipTypes = new List<TipTypeTestModel>();
            }

            // Create response directly (NO MediatR)
            var tipTypeDtos = tipTypes.Select(tt => new TipTypeDto
            {
                Id = tt.Id.Value,
                Name = tt.Name,
                I8n = tt.I8n
            }).ToList();

            var result = Result<List<TipTypeDto>>.Success(tipTypeDtos);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<bool>> DeleteTipTypeAsync(int tipTypeId)
        {
            // Set up the mock repository
            var tipTypeModel = _context.GetModel<TipTypeTestModel>($"TipType_{tipTypeId}");
            if (tipTypeModel != null)
            {
                var domainEntity = tipTypeModel.ToDomainEntity();

                _tipTypeRepositoryMock
                    .Setup(repo => repo.GetByIdAsync(
                        It.Is<int>(id => id == tipTypeId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(domainEntity);

                _tipTypeRepositoryMock
                    .Setup(repo => repo.Delete(It.IsAny<Domain.Entities.TipType>()));
            }

            // Create response directly (NO MediatR)
            var result = Result<bool>.Success(true);

            // Store the result
            _context.StoreResult(result);

            // Clear individual context after successful deletion
            if (result.IsSuccess)
            {
                _context.StoreModel(new TipTypeTestModel(), $"TipType_{tipTypeId}");
            }

            return result;
        }

        public void SetupTipTypes(List<TipTypeTestModel> tipTypes)
        {
            // Configure mock repository to return these tip types
            var domainEntities = tipTypes.ConvertAll(tt => tt.ToDomainEntity());

            // Setup GetAllAsync
            _tipTypeRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(domainEntities);

            // Setup individual GetByIdAsync for each tip type
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

                    // Store for later use
                    _context.StoreModel(tipType, $"TipType_{tipType.Id}");
                }
            }

            // Store all tip types in the context
            _context.StoreModel(tipTypes, "AllTipTypes");
        }
    }
}