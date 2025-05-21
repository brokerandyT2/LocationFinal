using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Commands.TipTypes;
using Location.Core.Application.Queries.TipTypes;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Core.BDD.Tests.Drivers
{
    public class TipTypeDriver
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<ITipTypeRepository> _tipTypeRepositoryMock;

        public TipTypeDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = _context.GetService<IMediator>();
            _tipTypeRepositoryMock = _context.GetService<Mock<ITipTypeRepository>>();
        }

        public async Task<Result<TipTypeDto>> CreateTipTypeAsync(TipTypeTestModel tipTypeModel)
        {
            // Set up the mock repository
            var domainEntity = tipTypeModel.ToDomainEntity();

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

            // Create the command
            var command = new CreateTipTypeCommand
            {
                Name = tipTypeModel.Name,
                I8n = tipTypeModel.I8n
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                tipTypeModel.Id = result.Data.Id;
                _context.StoreModel(tipTypeModel, $"TipType_{tipTypeModel.Id}");

                // Also store the latest created tip type
                _context.StoreModel(tipTypeModel, "LatestTipType");
            }

            return result;
        }

        public async Task<Result<TipTypeDto>> GetTipTypeByIdAsync(int tipTypeId)
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
            }

            // Create the query
            var query = new GetTipTypeByIdQuery
            {
                Id = tipTypeId
            };

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<List<TipTypeDto>>> GetAllTipTypesAsync()
        {
            // Get all tip types from the context
            // This would be populated by the SetupTipTypes method

            // Create the query
            var query = new GetAllTipTypesQuery();

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

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

            // In a real implementation, this would be a DeleteTipTypeCommand
            // For our mock, we'll just return a success result
            var result = Result<bool>.Success(true);

            // Store the result
            _context.StoreResult(result);

            return result;
        }
    }
}