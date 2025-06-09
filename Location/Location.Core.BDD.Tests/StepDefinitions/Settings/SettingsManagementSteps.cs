using FluentAssertions;
using Location.Core.Application.Settings.Commands.CreateSetting;
using Location.Core.Application.Settings.Commands.UpdateSetting;
using Location.Core.Application.Settings.Queries.GetAllSettings;
using Location.Core.Application.Settings.Queries.GetSettingByKey;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Settings
{
    [Binding]
    public class SettingsManagementSteps
    {
        private readonly ApiContext _context;
        private readonly SettingDriver _settingDriver;

        public SettingsManagementSteps(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _settingDriver = new SettingDriver(context);
        }

        [Given(@"I have a setting with the following details:")]
        public async Task GivenIHaveASettingWithTheFollowingDetails(Table table)
        {
            var settingModel = table.CreateInstance<SettingTestModel>();

            // Assign an ID if not provided
            if (!settingModel.Id.HasValue)
            {
                settingModel.Id = 1;
            }

            // Setup the setting in the repository
            _settingDriver.SetupSettings(new List<SettingTestModel> { settingModel });

            // Store the setting in the context
            _context.StoreSettingData(settingModel);

            // Create the setting
            await _settingDriver.CreateSettingAsync(settingModel);
        }

        [Given(@"I have multiple settings in the system:")]
        public void GivenIHaveMultipleSettingsInTheSystem(Table table)
        {
            var settings = table.CreateSet<SettingTestModel>().ToList();

            // Assign IDs if not provided
            for (int i = 0; i < settings.Count; i++)
            {
                if (!settings[i].Id.HasValue)
                {
                    settings[i].Id = i + 1;
                }
            }

            // Setup the settings in the repository
            _settingDriver.SetupSettings(settings);

            // Store all settings in the context
            _context.StoreModel(settings, "AllSettings");
        }

        [Given(@"I have settings with different value types:")]
        public void GivenIHaveSettingsWithDifferentValueTypes(Table table)
        {
            var settings = table.CreateSet<SettingTestModel>().ToList();

            // Assign IDs if not provided
            for (int i = 0; i < settings.Count; i++)
            {
                if (!settings[i].Id.HasValue)
                {
                    settings[i].Id = i + 1;
                }
            }

            // Setup the settings in the repository
            _settingDriver.SetupSettings(settings);

            // Store all settings in the context
            _context.StoreModel(settings, "TypedSettings");
        }

        [When(@"I create a new setting with the following details:")]
        public async Task WhenICreateANewSettingWithTheFollowingDetails(Table table)
        {
            var settingModel = table.CreateInstance<SettingTestModel>();
            await _settingDriver.CreateSettingAsync(settingModel);
        }

        [When(@"I update the setting with the following value:")]
        public async Task WhenIUpdateTheSettingWithTheFollowingValue(Table table)
        {
            var settingData = table.Rows[0];
            var settingModel = _context.GetSettingData();

            settingModel.Should().NotBeNull("Setting data should be available in context");

            await _settingDriver.UpdateSettingAsync(settingModel.Key, settingData["Value"]);
        }

        [When(@"I delete the setting")]
        public async Task WhenIDeleteTheSetting()
        {
            var settingModel = _context.GetSettingData();
            settingModel.Should().NotBeNull("Setting data should be available in context");

            await _settingDriver.DeleteSettingAsync(settingModel.Key);
        }

        [When(@"I retrieve the setting by its key")]
        public async Task WhenIRetrieveTheSettingByItsKey()
        {
            var settingModel = _context.GetSettingData();
            settingModel.Should().NotBeNull("Setting data should be available in context");

            await _settingDriver.GetSettingByKeyAsync(settingModel.Key);
        }

        [When(@"I request a list of all settings")]
        public async Task WhenIRequestAListOfAllSettings()
        {
            await _settingDriver.GetAllSettingsAsync();
        }

        [When(@"I upsert a setting with the following details:")]
        public async Task WhenIUpsertASettingWithTheFollowingDetails(Table table)
        {
            var settingModel = table.CreateInstance<SettingTestModel>();

            // Check if the setting already exists
            var existingModel = _context.GetSettingData();
            if (existingModel != null && existingModel.Key == settingModel.Key)
            {
                // Update
                await _settingDriver.UpdateSettingAsync(settingModel.Key, settingModel.Value, settingModel.Description);
            }
            else
            {
                // Create
                await _settingDriver.CreateSettingAsync(settingModel);
            }
        }

        [When(@"I retrieve the settings and convert their values")]
        public async Task WhenIRetrieveTheSettingsAndConvertTheirValues()
        {
            var settings = _context.GetModel<List<SettingTestModel>>("TypedSettings");
            settings.Should().NotBeNull("Settings data should be available in context");

            // Store the converted values
            var convertedValues = new Dictionary<string, object>();

            foreach (var setting in settings)
            {
                var result = await _settingDriver.GetSettingByKeyAsync(setting.Key);

                if (result.IsSuccess && result.Data != null)
                {
                    object convertedValue = null;

                    // Try to convert based on key name
                    if (setting.Key.Contains("Boolean", StringComparison.OrdinalIgnoreCase))
                    {
                        convertedValue = bool.TryParse(result.Data.Value, out var boolValue) ? boolValue : null;
                    }
                    else if (setting.Key.Contains("Integer", StringComparison.OrdinalIgnoreCase))
                    {
                        convertedValue = int.TryParse(result.Data.Value, out var intValue) ? intValue : null;
                    }
                    else if (setting.Key.Contains("DateTime", StringComparison.OrdinalIgnoreCase))
                    {
                        convertedValue = DateTime.TryParse(result.Data.Value, out var dateValue) ? dateValue : null;
                    }

                    convertedValues[setting.Key] = convertedValue;
                }
            }

            _context.StoreModel(convertedValues, "ConvertedSettings");
        }

        [When(@"I request all settings as a dictionary")]
        public async Task WhenIRequestAllSettingsAsADictionary()
        {
            await _settingDriver.GetAllSettingsAsDictionaryAsync();
        }

        [Then(@"the setting should be created successfully")]
        public void ThenTheSettingShouldBeCreatedSuccessfully()
        {
            var settingResult = _context.GetLastResult<CreateSettingCommandResponse>();
            settingResult.Should().NotBeNull("Setting result should be available");
            settingResult.IsSuccess.Should().BeTrue("Setting creation should be successful");
            settingResult.Data.Should().NotBeNull("Setting data should be available");
            settingResult.Data.Id.Should().BeGreaterThan(0, "Setting ID should be positive");
        }

        [Then(@"the setting should have the correct details:")]
        public void ThenTheSettingShouldHaveTheCorrectDetails(Table table)
        {
            var expectedSetting = table.CreateInstance<SettingTestModel>();
            var settingResult = _context.GetLastResult<CreateSettingCommandResponse>();

            settingResult.Should().NotBeNull("Setting result should be available");
            settingResult.Data.Should().NotBeNull("Setting data should be available");
            settingResult.Data.Key.Should().Be(expectedSetting.Key, "Setting key should match expected value");
            settingResult.Data.Value.Should().Be(expectedSetting.Value, "Setting value should match expected value");
            settingResult.Data.Description.Should().Be(expectedSetting.Description, "Setting description should match expected value");
        }

        [Then(@"the setting should be updated successfully")]
        public void ThenTheSettingShouldBeUpdatedSuccessfully()
        {
            var lastResult = _context.GetLastResult<UpdateSettingCommandResponse>();
            lastResult.Should().NotBeNull("Update result should be available");
            lastResult.IsSuccess.Should().BeTrue("Setting update should be successful");
        }

        [Then(@"the setting should have the new value ""(.*)""")]
        public void ThenTheSettingShouldHaveTheNewValue(string expectedValue)
        {
            var updateResult = _context.GetLastResult<UpdateSettingCommandResponse>();
            updateResult.Should().NotBeNull("Update result should be available");
            updateResult.Data.Should().NotBeNull("Update data should be available");
            updateResult.Data.Value.Should().Be(expectedValue, "Setting value should be updated to the new value");
        }

        [Then(@"the setting timestamp should be updated")]
        public void ThenTheSettingTimestampShouldBeUpdated()
        {
            var updateResult = _context.GetLastResult<UpdateSettingCommandResponse>();
            updateResult.Should().NotBeNull("Update result should be available");
            updateResult.Data.Should().NotBeNull("Update data should be available");

            var now = DateTime.UtcNow;
            var timeDifference = now - updateResult.Data.Timestamp;

            timeDifference.TotalMinutes.Should().BeLessThan(5, "Setting timestamp should be recent (within 5 minutes)");
        }

        [Then(@"the setting should be deleted successfully")]
        public void ThenTheSettingShouldBeDeletedSuccessfully()
        {
            var lastResult = _context.GetLastResult<bool>();
            lastResult.Should().NotBeNull("Delete result should be available");
            lastResult.IsSuccess.Should().BeTrue("Setting deletion should be successful");
            lastResult.Data.Should().BeTrue("Setting deletion should return true");
        }

        [Then(@"the setting should not exist in the system")]
        public void ThenTheSettingShouldNotExistInTheSystem()
        {
            var settingModel = _context.GetSettingData();
            settingModel.Should().NotBeNull("Setting model should be available in context");

            // Use the existing _settingDriver instead of creating a new one
            var task = _settingDriver.GetSettingByKeyAsync(settingModel.Key);
            task.Wait();

            var result = task.Result;
            result.IsSuccess.Should().BeFalse("Setting should not be found after deletion");
        }

        [Then(@"the retrieved setting should match the original setting details")]
        public void ThenTheRetrievedSettingShouldMatchTheOriginalSettingDetails()
        {
            var originalSetting = _context.GetSettingData();
            originalSetting.Should().NotBeNull("Original setting data should be available in context");

            var lastResult = _context.GetLastResult<GetSettingByKeyQueryResponse>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Setting retrieval should be successful");

            if (lastResult.Data != null)
            {
                lastResult.Data.Key.Should().Be(originalSetting.Key, "Retrieved setting key should match original");
                lastResult.Data.Value.Should().Be(originalSetting.Value, "Retrieved setting value should match original");
                lastResult.Data.Description.Should().Be(originalSetting.Description, "Retrieved setting description should match original");
            }
        }

        [Then(@"the result should contain (.*) settings")]
        public void ThenTheResultShouldContainSettings(int expectedCount)
        {
            var lastResult = _context.GetLastResult<List<GetAllSettingsQueryResponse>>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Settings retrieval should be successful");

            if (lastResult.Data != null)
            {
                lastResult.Data.Count.Should().Be(expectedCount, $"Result should contain {expectedCount} settings");
            }
        }

        [Then(@"the settings list should include ""(.*)""")]
        public void ThenTheSettingsListShouldInclude(string expectedKey)
        {
            var lastResult = _context.GetLastResult<List<GetAllSettingsQueryResponse>>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Settings retrieval should be successful");

            if (lastResult.Data != null)
            {
                lastResult.Data.Should().Contain(s => s.Key == expectedKey, $"Settings list should include '{expectedKey}'");
            }
        }

        [Then(@"the setting description should be ""(.*)""")]
        public void ThenTheSettingDescriptionShouldBe(string expectedDescription)
        {
            var updateResult = _context.GetLastResult<UpdateSettingCommandResponse>();
            updateResult.Should().NotBeNull("Update result should be available");
            updateResult.Data.Should().NotBeNull("Update data should be available");
            updateResult.Data.Description.Should().Be(expectedDescription, "Setting description should be updated to the expected value");
        }

        [Then(@"the ""(.*)"" should be converted to boolean (.*)")]
        public void ThenTheShouldBeConvertedToBoolean(string settingKey, bool expectedValue)
        {
            var convertedSettings = _context.GetModel<Dictionary<string, object>>("ConvertedSettings");
            convertedSettings.Should().NotBeNull("Converted settings should be available in context");
            convertedSettings.Should().ContainKey(settingKey, $"Converted settings should contain '{settingKey}'");

            var value = convertedSettings[settingKey];
            value.Should().BeOfType<bool>($"Setting '{settingKey}' should be converted to boolean");
            ((bool)value).Should().Be(expectedValue, $"Setting '{settingKey}' should be converted to boolean {expectedValue}");
        }

        [Then(@"the ""(.*)"" should be converted to integer (.*)")]
        public void ThenTheShouldBeConvertedToInteger(string settingKey, int expectedValue)
        {
            var convertedSettings = _context.GetModel<Dictionary<string, object>>("ConvertedSettings");
            convertedSettings.Should().NotBeNull("Converted settings should be available in context");
            convertedSettings.Should().ContainKey(settingKey, $"Converted settings should contain '{settingKey}'");

            var value = convertedSettings[settingKey];
            value.Should().BeOfType<int>($"Setting '{settingKey}' should be converted to integer");
            ((int)value).Should().Be(expectedValue, $"Setting '{settingKey}' should be converted to integer {expectedValue}");
        }

        [Then(@"the ""(.*)"" should be converted to a valid date time")]
        public void ThenTheShouldBeConvertedToAValidDateTime(string settingKey)
        {
            var convertedSettings = _context.GetModel<Dictionary<string, object>>("ConvertedSettings");
            convertedSettings.Should().NotBeNull("Converted settings should be available in context");
            convertedSettings.Should().ContainKey(settingKey, $"Converted settings should contain '{settingKey}'");

            var value = convertedSettings[settingKey];
            value.Should().BeOfType<DateTime>($"Setting '{settingKey}' should be converted to DateTime");
        }

        [Then(@"the dictionary should contain (.*) key-value pairs")]
        public void ThenTheDictionaryShouldContainKeyValuePairs(int expectedCount)
        {
            var lastResult = _context.GetLastResult<Dictionary<string, string>>();
            lastResult.Should().NotBeNull("Dictionary result should be available");
            lastResult.IsSuccess.Should().BeTrue("Dictionary retrieval should be successful");
            lastResult.Data.Should().NotBeNull("Dictionary data should be available");
            lastResult.Data.Count.Should().Be(expectedCount, $"Dictionary should contain {expectedCount} key-value pairs");
        }

        [Then(@"the dictionary should have key ""(.*)"" with value ""(.*)""")]
        public void ThenTheDictionaryShouldHaveKeyWithValue(string key, string value)
        {
            var lastResult = _context.GetLastResult<Dictionary<string, string>>();
            lastResult.Should().NotBeNull("Dictionary result should be available");
            lastResult.IsSuccess.Should().BeTrue("Dictionary retrieval should be successful");
            lastResult.Data.Should().NotBeNull("Dictionary data should be available");
            lastResult.Data.Should().ContainKey(key, $"Dictionary should contain key '{key}'");
            lastResult.Data[key].Should().Be(value, $"Dictionary value for key '{key}' should be '{value}'");
        }

        // REMOVED: ThenIShouldReceiveASuccessfulResult() 
        // This step is already handled by CommonSteps to avoid duplication
    }
}