using Location.Core.Application.Commands.Tips;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.Commands.CreateTip;
using Location.Core.Application.Tips.Commands.DeleteTip;
using Location.Core.Application.Tips.Commands.UpdateTip;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Queries.GetAllTips;
using Location.Core.Application.Tips.Queries.GetTipById;
using Location.Core.Application.Tips.Queries.GetTipsByType;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.BDD.Tests.Drivers
{
    public class TipDriver
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<ITipRepository> _tipRepositoryMock;
        private readonly Mock<ITipTypeRepository> _tipTypeRepositoryMock;

        public TipDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = _context.GetService<IMediator>();
            _tipRepositoryMock = _context.GetService<Mock<ITipRepository>>();
            _tipTypeRepositoryMock = _context.GetService<Mock<ITipTypeRepository>>();
        }

        public async Task<Result<TipDto>> CreateTipAsync(TipTestModel tipModel)
        {
            // Set up the mock repository
            var domainEntity = tipModel.ToDomainEntity();

            _tipRepositoryMock
                .Setup(repo => repo.CreateAsync(
                    It.IsAny<Domain.Entities.Tip>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(domainEntity));

            // Create the command
            var command = new CreateTipCommand
            {
                TipTypeId = tipModel.TipTypeId,
                Title = tipModel.Title,
                Content = tipModel.Content,
                Fstop = tipModel.Fstop,
                ShutterSpeed = tipModel.ShutterSpeed,
                Iso = tipModel.Iso,
                I8n = tipModel.I8n
            };

            // Send the command
            var listResult = await _mediator.Send(command);
            if (listResult.IsSuccess && listResult.Data.Any())
            {
                return Result<TipDto>.Success(listResult.Data.First());
            }
            return Result<TipDto>.Failure(listResult.ErrorMessage ?? "No tips found");
        }

        public async Task<Result<TipDto>> UpdateTipAsync(TipTestModel tipModel)
        {
            // Ensure we have an ID
            if (!tipModel.Id.HasValue || tipModel.Id.Value <= 0)
            {
                throw new InvalidOperationException("Cannot update a tip without a valid ID");
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

            // Create the command
            var command = new UpdateTipCommand
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

            // Send the command
            var updateResult = await _mediator.Send(command);
            if (updateResult.IsSuccess && updateResult.Data != null)
            {
                var tipDto = new TipDto
                {
                    Id = updateResult.Data.Id,
                    TipTypeId = updateResult.Data.TipTypeId,
                    Title = updateResult.Data.Title,
                    Content = updateResult.Data.Content,
                    Fstop = updateResult.Data.Fstop,
                    ShutterSpeed = updateResult.Data.ShutterSpeed,
                    Iso = updateResult.Data.Iso,
                    I8n = updateResult.Data.I8n
                };
                return Result<TipDto>.Success(tipDto);
            }
            return Result<TipDto>.Failure(updateResult.ErrorMessage ?? "Failed to update tip");
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

            // Create the command
            var command = new DeleteTipCommand
            {
                Id = tipId
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<GetTipByIdQueryResponse>> GetTipByIdAsync(int tipId)
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
            }

            // Create the query
            var query = new GetTipByIdQuery
            {
                Id = tipId
            };

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<List<TipDto>>> GetTipsByTypeAsync(int tipTypeId)
        {
            // Get all tips for this type from the context
            // This would be populated by the SetupTips method

            // Create the query
            var query = new GetTipsByTypeQuery
            {
                TipTypeId = tipTypeId
            };

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<TipDto>> GetRandomTipByTypeAsync(int tipTypeId)
        {
            // Get all tips for this type from the context
            // This would be populated by the SetupTips method

            // Set up the mock repository
            var tips = _context.GetModel<List<TipTestModel>>($"TipsByType_{tipTypeId}");
            if (tips != null && tips.Count > 0)
            {
                // Pick a random tip
                var random = new Random();
                var tipModel = tips[random.Next(tips.Count)];
                var domainEntity = tipModel.ToDomainEntity();

                _tipRepositoryMock
                    .Setup(repo => repo.GetRandomByTypeAsync(
                        It.Is<int>(id => id == tipTypeId),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Tip>.Success(domainEntity));
            }

            // Create the command
            var command = new GetRandomTipCommand
            {
                TipTypeId = tipTypeId
            };

            // Send the command
            var result = await _mediator.Send(command);

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
        }
    }
}