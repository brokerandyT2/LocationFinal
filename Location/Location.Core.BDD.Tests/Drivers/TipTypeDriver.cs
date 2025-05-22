using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using Moq;

namespace Location.Core.BDD.Tests.Drivers
{
    // TipTypeDriver.cs - Fix the CreateTipTypeAsync method

    public class TipTypeDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<ITipTypeRepository> _tipTypeRepositoryMock;
        private static int _idCounter = 1; // ✅ Add static counter like LocationDriver

        public TipTypeDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tipTypeRepositoryMock = _context.GetService<Mock<ITipTypeRepository>>();
        }

        public async Task<Result<TipTypeDto>> CreateTipTypeAsync(TipTypeTestModel tipTypeModel)
        {
            // ✅ FIXED: Use counter instead of always assigning 1
            if (!tipTypeModel.Id.HasValue || tipTypeModel.Id.Value <= 0)
            {
                tipTypeModel.Id = _idCounter++; // ✅ Increment counter
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
                // ✅ FIXED: Use unique keys with actual ID
                _context.StoreModel(tipTypeModel, $"TipType_{tipTypeModel.Id}");
                _context.StoreModel(tipTypeModel, "LatestTipType");
            }

            return result;
        }

        // ... rest of methods unchanged
    }
}