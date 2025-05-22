using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Queries.GetTipById;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using Moq;

namespace Location.Core.BDD.Tests.Drivers
{
    public class TipDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<ITipRepository> _tipRepositoryMock;
        private readonly Mock<ITipTypeRepository> _tipTypeRepositoryMock;

        public TipDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tipRepositoryMock = _context.GetService<Mock<ITipRepository>>();
            _tipTypeRepositoryMock = _context.GetService<Mock<ITipTypeRepository>>();
        }

        public async Task<Result<TipDto>> CreateTipAsync(TipTestModel tipModel)
        {
            // Ensure ID is assigned BEFORE creating domain entity
            if (!tipModel.Id.HasValue || tipModel.Id.Value <= 0)
            {
                tipModel.Id = 1;
            }

            // Create domain entity AFTER ID assignment
            var domainEntity = tipModel.ToDomainEntity();

            // Set up the mock repository
            _tipRepositoryMock
                .Setup(repo => repo.CreateAsync(
                    It.IsAny<Domain.Entities.Tip>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(domainEntity));

            // Create response directly (NO MediatR)
            var tipDto = new TipDto
            {
                Id = tipModel.Id.Value,
                TipTypeId = tipModel.TipTypeId,
                Title = tipModel.Title,
                Content = tipModel.Content,
                Fstop = tipModel.Fstop,
                ShutterSpeed = tipModel.ShutterSpeed,
                Iso = tipModel.Iso,
                I8n = tipModel.I8n
            };

            var result = Result<TipDto>.Success(tipDto);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                _context.StoreTipData(tipModel);
            }

            return result;
        }

        public async Task<Result<TipDto>> UpdateTipAsync(TipTestModel tipModel)
        {
            // Ensure we have an ID
            if (!tipModel.Id.HasValue || tipModel.Id.Value <= 0)
            {
                var failureResult = Result<TipDto>.Failure("Cannot update a tip without a valid ID");
                _context.StoreResult(failureResult);
                return failureResult;
            }

            // Set up the mock repository
            var domainEntity = tipModel.ToDomainEntity();

            _tipRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == tipModel.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(domainEntity));

            _tipRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Tip>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(domainEntity));

            // Create response directly (NO MediatR)
            var tipDto = new TipDto
            {
                Id = tipModel.Id.Value,
                TipTypeId = tipModel.TipTypeId,
                Title = tipModel.Title,
                Content = tipModel.Content,
                Fstop = tipModel.Fstop,
                ShutterSpeed = tipModel.ShutterSpeed,
                Iso = tipModel.Iso,
                I8n = tipModel.I8n
            };

            var result = Result<TipDto>.Success(tipDto);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                _context.StoreTipData(tipModel);
            }

            return result;
        }

        public async Task<Result<bool>> DeleteTipAsync(int tipId)
        {
            // Set up the mock repository
            var tipModel = _context.GetTipData();
            if (tipModel != null && tipModel.Id == tipId)
            {
                var domainEntity = tipModel.ToDomainEntity();

                _tipRepositoryMock
                    .Setup(repo => repo.GetByIdAsync(
                        It.Is<int>(id => id == tipId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Tip>.Success(domainEntity));

                _tipRepositoryMock
                    .Setup(repo => repo.DeleteAsync(
                        It.Is<int>(id => id == tipId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<bool>.Success(true));
            }

            // Create response directly (NO MediatR)
            var result = Result<bool>.Success(true);

            // Store the result
            _context.StoreResult(result);

            // Clear individual context after successful deletion
            if (result.IsSuccess)
            {
                _context.StoreTipData(new TipTestModel()); // Clear individual context
            }

            return result;
        }

        public async Task<Result<GetTipByIdQueryResponse>> GetTipByIdAsync(int tipId)
        {
            // Check individual context first
            var tipModel = _context.GetTipData();
            if (tipModel != null && tipModel.Id == tipId && tipModel.Id.HasValue && tipModel.Id > 0)
            {
                var response = new GetTipByIdQueryResponse
                {
                    Id = tipModel.Id.Value,
                    TipTypeId = tipModel.TipTypeId,
                    Title = tipModel.Title,
                    Content = tipModel.Content,
                    Fstop = tipModel.Fstop,
                    ShutterSpeed = tipModel.ShutterSpeed,
                    Iso = tipModel.Iso,
                    I8n = tipModel.I8n
                };

                var result = Result<GetTipByIdQueryResponse>.Success(response);
                _context.StoreResult(result);
                return result;
            }

            // Check collection contexts
            var collectionKeys = new[] { "AllTips", "SetupTips" };
            foreach (var collectionKey in collectionKeys)
            {
                var tips = _context.GetModel<List<TipTestModel>>(collectionKey);
                if (tips != null)
                {
                    var foundTip = tips.FirstOrDefault(t => t.Id == tipId);
                    if (foundTip != null)
                    {
                        var response = new GetTipByIdQueryResponse
                        {
                            Id = foundTip.Id.Value,
                            TipTypeId = foundTip.TipTypeId,
                            Title = foundTip.Title,
                            Content = foundTip.Content,
                            Fstop = foundTip.Fstop,
                            ShutterSpeed = foundTip.ShutterSpeed,
                            Iso = foundTip.Iso,
                            I8n = foundTip.I8n
                        };

                        var result = Result<GetTipByIdQueryResponse>.Success(response);
                        _context.StoreResult(result);
                        return result;
                    }
                }
            }

            // Tip not found
            var failureResult = Result<GetTipByIdQueryResponse>.Failure($"Tip with ID {tipId} not found");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        public async Task<Result<List<TipDto>>> GetTipsByTypeAsync(int tipTypeId)
        {
            // Get tips by type from context
            var tips = _context.GetModel<List<TipTestModel>>($"TipsByType_{tipTypeId}");
            if (tips == null)
            {
                // Check all tips and filter by type
                var allTips = _context.GetModel<List<TipTestModel>>("AllTips");
                if (allTips != null)
                {
                    tips = allTips.Where(t => t.TipTypeId == tipTypeId).ToList();
                }
            }

            if (tips == null || !tips.Any())
            {
                var emptyResult = Result<List<TipDto>>.Success(new List<TipDto>());
                _context.StoreResult(emptyResult);
                return emptyResult;
            }

            // Create response directly (NO MediatR)
            var tipDtos = tips.Select(t => new TipDto
            {
                Id = t.Id.Value,
                TipTypeId = t.TipTypeId,
                Title = t.Title,
                Content = t.Content,
                Fstop = t.Fstop,
                ShutterSpeed = t.ShutterSpeed,
                Iso = t.Iso,
                I8n = t.I8n
            }).ToList();

            var result = Result<List<TipDto>>.Success(tipDtos);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<TipDto>> GetRandomTipByTypeAsync(int tipTypeId)
        {
            // Get tips by type from context
            var tips = _context.GetModel<List<TipTestModel>>($"TipsByType_{tipTypeId}");
            if (tips == null)
            {
                // Check all tips and filter by type
                var allTips = _context.GetModel<List<TipTestModel>>("AllTips");
                if (allTips != null)
                {
                    tips = allTips.Where(t => t.TipTypeId == tipTypeId).ToList();
                }
            }

            if (tips == null || !tips.Any())
            {
                var failureResult = Result<TipDto>.Failure($"No tips found for type {tipTypeId}");
                _context.StoreResult(failureResult);
                return failureResult;
            }

            // Pick a random tip
            var random = new Random();
            var randomTip = tips[random.Next(tips.Count)];

            // Create response directly (NO MediatR)
            var tipDto = new TipDto
            {
                Id = randomTip.Id.Value,
                TipTypeId = randomTip.TipTypeId,
                Title = randomTip.Title,
                Content = randomTip.Content,
                Fstop = randomTip.Fstop,
                ShutterSpeed = randomTip.ShutterSpeed,
                Iso = randomTip.Iso,
                I8n = randomTip.I8n
            };

            var result = Result<TipDto>.Success(tipDto);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public void SetupTips(List<TipTestModel> tips)
        {
            // Configure mock repository to return these tips
            var domainEntities = tips.ConvertAll(t => t.ToDomainEntity());

            // Setup GetAllAsync
            _tipRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(domainEntities));

            // Setup tips by type
            var tipsByType = tips.GroupBy(t => t.TipTypeId)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var entry in tipsByType)
            {
                var typeId = entry.Key;
                var typeTips = entry.Value;
                var typeDomainEntities = typeTips.ConvertAll(t => t.ToDomainEntity());

                _tipRepositoryMock
                    .Setup(repo => repo.GetByTypeAsync(
                        It.Is<int>(id => id == typeId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(typeDomainEntities));

                // Store for later use
                _context.StoreModel(typeTips, $"TipsByType_{typeId}");
            }

            // Setup individual GetByIdAsync for each tip
            foreach (var tip in tips)
            {
                if (tip.Id.HasValue)
                {
                    var entity = tip.ToDomainEntity();
                    _tipRepositoryMock
                        .Setup(repo => repo.GetByIdAsync(
                            It.Is<int>(id => id == tip.Id.Value),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<Domain.Entities.Tip>.Success(entity));
                }
            }

            // Store all tips
            _context.StoreModel(tips, "AllTips");
        }
    }
}