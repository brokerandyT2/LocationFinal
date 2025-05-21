using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Commands.CreateSetting;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Commands.DeleteSetting;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.Application.Settings.Queries.GetAllSettings;
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
    public class SettingDriver
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<ISettingRepository> _settingRepositoryMock;

        public SettingDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _mediator = _context.GetService<IMediator>();
            _settingRepositoryMock = _context.GetService<Mock<ISettingRepository>>();
        }

        public async Task<Result<CreateSettingCommandResponse>> CreateSettingAsync(SettingTestModel settingModel)
        {
            // Set up the mock repository
            var domainEntity = settingModel.ToDomainEntity();

            _settingRepositoryMock
                .Setup(repo => repo.GetByKeyAsync(
                    It.Is<string>(key => key == settingModel.Key),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Failure($"Setting with key '{settingModel.Key}' not found"));

            _settingRepositoryMock
                .Setup(repo => repo.CreateAsync(
                    It.IsAny<Domain.Entities.Setting>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(domainEntity));

            // Create the command
            var command = new CreateSettingCommand
            {
                Key = settingModel.Key,
                Value = settingModel.Value,
                Description = settingModel.Description
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                settingModel.Id = result.Data.Id;
                settingModel.Timestamp = result.Data.Timestamp;
                _context.StoreSettingData(settingModel);
            }

            return result;
        }

        public async Task<Result<UpdateSettingCommandResponse>> UpdateSettingAsync(string key, string newValue, string description = null)
        {
            // Get the existing setting model
            var settingModel = _context.GetSettingData();
            if (settingModel == null || settingModel.Key != key)
            {
                throw new InvalidOperationException($"Setting with key '{key}' not found in context");
            }

            // Update the model
            settingModel.Value = newValue;
            if (description != null)
            {
                settingModel.Description = description;
            }

            // Set up the mock repository
            var domainEntity = settingModel.ToDomainEntity();

            _settingRepositoryMock
                .Setup(repo => repo.GetByKeyAsync(
                    It.Is<string>(k => k == key),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(domainEntity));

            _settingRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Setting>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Setting>.Success(domainEntity));

            // Create the command
            var command = new UpdateSettingCommand
            {
                Key = key,
                Value = newValue,
                Description = description
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                settingModel.Timestamp = result.Data.Timestamp;
                _context.StoreSettingData(settingModel);
            }

            return result;
        }

        public async Task<Result<bool>> DeleteSettingAsync(string key)
        {
            // Set up the mock repository
            var settingModel = _context.GetSettingData();
            if (settingModel != null && settingModel.Key == key)
            {
                var domainEntity = settingModel.ToDomainEntity();

                _settingRepositoryMock
                    .Setup(repo => repo.GetByKeyAsync(
                        It.Is<string>(k => k == key),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Setting>.Success(domainEntity));

                _settingRepositoryMock
                    .Setup(repo => repo.DeleteAsync(
                        It.Is<string>(k => k == key),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<bool>.Success(true));
            }

            // Create the command
            var command = new DeleteSettingCommand
            {
                Key = key
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<GetSettingByKeyQueryResponse>> GetSettingByKeyAsync(string key)
        {
            // Set up the mock repository
            var settingModel = _context.GetSettingData();
            if (settingModel != null && settingModel.Key == key)
            {
                var domainEntity = settingModel.ToDomainEntity();

                _settingRepositoryMock
                    .Setup(repo => repo.GetByKeyAsync(
                        It.Is<string>(k => k == key),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Setting>.Success(domainEntity));
            }

            // Create the query
            var query = new GetSettingByKeyQuery
            {
                Key = key
            };

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public async Task<Result<List<GetAllSettingsQueryResponse>>> GetAllSettingsAsync()
        {
            // Get all settings from the context
            // This would be populated by the SetupSettings method

            // Create the query
            var query = new GetAllSettingsQuery();

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

            return result;
        }

        public void SetupSettings(List<SettingTestModel> settings)
        {
            // Configure mock repository to return these settings
            var domainEntities = settings.ConvertAll(s => s.ToDomainEntity());

            // Setup GetAllAsync
            _settingRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Success(domainEntities));

            // Setup GetAllAsDictionaryAsync
            var dictionary = settings.ToDictionary(s => s.Key, s => s.Value);
            _settingRepositoryMock
                .Setup(repo => repo.GetAllAsDictionaryAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Dictionary<string, string>>.Success(dictionary));

            // Setup individual GetByKeyAsync for each setting
            foreach (var setting in settings)
            {
                var entity = setting.ToDomainEntity();
                _settingRepositoryMock
                    .Setup(repo => repo.GetByKeyAsync(
                        It.Is<string>(key => key == setting.Key),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Setting>.Success(entity));
            }
        }

        public async Task<Result<Dictionary<string, string>>> GetAllSettingsAsDictionaryAsync()
        {
            // We'll mock getting settings as a dictionary here
            // In a real implementation, this would be a separate query

            var settings = _context.GetModel<List<SettingTestModel>>("AllSettings");
            if (settings == null)
            {
                return Result<Dictionary<string, string>>.Failure("No settings available");
            }

            var dictionary = settings.ToDictionary(s => s.Key, s => s.Value);
            var result = Result<Dictionary<string, string>>.Success(dictionary);

            // Store the result
            _context.StoreResult(result);

            return result;
        }
    }
}