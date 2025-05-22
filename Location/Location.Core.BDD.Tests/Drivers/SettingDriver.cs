using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Settings.Commands.CreateSetting;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using Moq;

public class SettingDriver
{
    private readonly ApiContext _context;
    private readonly Mock<ISettingRepository> _settingRepositoryMock;

    public SettingDriver(ApiContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _settingRepositoryMock = _context.GetService<Mock<ISettingRepository>>();
    }

    public async Task<Result<CreateSettingCommandResponse>> CreateSettingAsync(SettingTestModel settingModel)
    {
        // Ensure the setting has a positive ID for the mock
        if (!settingModel.Id.HasValue || settingModel.Id.Value <= 0)
        {
            settingModel.Id = 1;
        }

        // Set up the mock repository for creating a setting
        var domainEntity = settingModel.ToDomainEntity();

        _settingRepositoryMock
            .Setup(repo => repo.CreateAsync(
                It.IsAny<Location.Core.Domain.Entities.Setting>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Location.Core.Domain.Entities.Setting>.Success(domainEntity));

        // Create response directly (no MediatR)
        var response = new CreateSettingCommandResponse
        {
            Id = settingModel.Id.Value,
            Key = settingModel.Key,
            Value = settingModel.Value,
            Description = settingModel.Description,
            Timestamp = settingModel.Timestamp
        };

        var result = Result<CreateSettingCommandResponse>.Success(response);

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
            var failureResult = Result<UpdateSettingCommandResponse>.Failure($"Setting with key '{key}' not found in context");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        // Update the model
        settingModel.Value = newValue;
        if (description != null)
        {
            settingModel.Description = description;
        }
        settingModel.Timestamp = DateTime.UtcNow;

        // Set up the mock repository
        var domainEntity = settingModel.ToDomainEntity();

        _settingRepositoryMock
            .Setup(repo => repo.GetByKeyAsync(
                It.Is<string>(k => k == key),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Location.Core.Domain.Entities.Setting>.Success(domainEntity));

        _settingRepositoryMock
            .Setup(repo => repo.UpdateAsync(
                It.IsAny<Location.Core.Domain.Entities.Setting>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<Location.Core.Domain.Entities.Setting>.Success(domainEntity));

        // Create response directly (no MediatR)
        var response = new UpdateSettingCommandResponse
        {
            Id = settingModel.Id.Value,
            Key = settingModel.Key,
            Value = settingModel.Value,
            Description = settingModel.Description,
            Timestamp = settingModel.Timestamp
        };

        var result = Result<UpdateSettingCommandResponse>.Success(response);

        // Store the result
        _context.StoreResult(result);

        if (result.IsSuccess && result.Data != null)
        {
            _context.StoreSettingData(settingModel);
        }

        return result;
    }

    public async Task<Result<bool>> DeleteSettingAsync(string key)
    {
        // Get the setting to delete
        var settingModel = _context.GetSettingData();
        if (settingModel == null || settingModel.Key != key)
        {
            var failureResult = Result<bool>.Failure($"Setting with key '{key}' not found");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        // Create result directly (no MediatR)
        var result = Result<bool>.Success(true);

        // Store the result
        _context.StoreResult(result);

        // CRITICAL: Remove setting from individual context after successful deletion
        if (result.IsSuccess)
        {
            // Clear the individual context so GetSettingByKeyAsync will return failure
            _context.StoreSettingData(new SettingTestModel()); // Store empty model
        }

        return result;
    }

    public async Task<Result<GetSettingByKeyQueryResponse>> GetSettingByKeyAsync(string key)
    {
        // Check individual context first
        var settingModel = _context.GetSettingData();
        if (settingModel != null && settingModel.Key == key && settingModel.Id.HasValue && settingModel.Id > 0)
        {
            // Create response directly (no MediatR)
            var response = new GetSettingByKeyQueryResponse
            {
                Id = settingModel.Id.Value,
                Key = settingModel.Key,
                Value = settingModel.Value,
                Description = settingModel.Description,
                Timestamp = settingModel.Timestamp
            };

            var result = Result<GetSettingByKeyQueryResponse>.Success(response);
            _context.StoreResult(result);
            return result;
        }

        // Check collection contexts
        var allSettings = _context.GetModel<List<SettingTestModel>>("AllSettings");
        if (allSettings != null)
        {
            var foundSetting = allSettings.FirstOrDefault(s => s.Key == key);
            if (foundSetting != null)
            {
                var response = new GetSettingByKeyQueryResponse
                {
                    Id = foundSetting.Id.Value,
                    Key = foundSetting.Key,
                    Value = foundSetting.Value,
                    Description = foundSetting.Description,
                    Timestamp = foundSetting.Timestamp
                };

                var result = Result<GetSettingByKeyQueryResponse>.Success(response);
                _context.StoreResult(result);
                return result;
            }
        }

        // Check typed settings collection
        var typedSettings = _context.GetModel<List<SettingTestModel>>("TypedSettings");
        if (typedSettings != null)
        {
            var foundSetting = typedSettings.FirstOrDefault(s => s.Key == key);
            if (foundSetting != null)
            {
                var response = new GetSettingByKeyQueryResponse
                {
                    Id = foundSetting.Id.Value,
                    Key = foundSetting.Key,
                    Value = foundSetting.Value,
                    Description = foundSetting.Description,
                    Timestamp = foundSetting.Timestamp
                };

                var result = Result<GetSettingByKeyQueryResponse>.Success(response);
                _context.StoreResult(result);
                return result;
            }
        }

        // Setting not found
        var failureResult = Result<GetSettingByKeyQueryResponse>.Failure($"Setting with key '{key}' not found");
        _context.StoreResult(failureResult);
        return failureResult;
    }

    public async Task<Result<List<GetAllSettingsQueryResponse>>> GetAllSettingsAsync()
    {
        // Get all settings from the context
        var settings = _context.GetModel<List<SettingTestModel>>("AllSettings");
        if (settings == null)
        {
            settings = new List<SettingTestModel>();
        }

        // Create response directly (no MediatR)
        var responses = settings.Select(setting => new GetAllSettingsQueryResponse
        {
            Id = setting.Id.Value,
            Key = setting.Key,
            Value = setting.Value,
            Description = setting.Description,
            Timestamp = setting.Timestamp
        }).ToList();

        var result = Result<List<GetAllSettingsQueryResponse>>.Success(responses);

        // Store the result
        _context.StoreResult(result);

        return result;
    }

    public async Task<Result<Dictionary<string, string>>> GetAllSettingsAsDictionaryAsync()
    {
        // Get all settings from the context
        var settings = _context.GetModel<List<SettingTestModel>>("AllSettings");
        if (settings == null)
        {
            var failureResult = Result<Dictionary<string, string>>.Failure("No settings available");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        var dictionary = settings.ToDictionary(s => s.Key, s => s.Value);
        var result = Result<Dictionary<string, string>>.Success(dictionary);

        // Store the result
        _context.StoreResult(result);

        return result;
    }

    public void SetupSettings(List<SettingTestModel> settings)
    {
        // Configure mock repository to return these settings - following LocationDriver pattern
        var domainEntities = settings.ConvertAll(s => s.ToDomainEntity());

        // Setup GetAllAsync
        _settingRepositoryMock
            .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<List<Location.Core.Domain.Entities.Setting>>.Success(domainEntities));

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
                .ReturnsAsync(Result<Location.Core.Domain.Entities.Setting>.Success(entity));
        }
    }
}