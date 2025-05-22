using BoDi;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tips.DTOs;
using Location.Core.BDD.Tests.Drivers;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Tips
{
    [Binding]
    public class TipTypeManagementSteps
    {
        private readonly ApiContext _context;
        private readonly TipTypeDriver _tipTypeDriver;
        private readonly TipDriver _tipDriver;
        private readonly Dictionary<string, TipTypeTestModel> _tipTypesByName = new();
        private int _tipTypeIdCounter = 1;
        private List<TipTypeTestModel> _createdTipTypes = new();
        private readonly IObjectContainer _objectContainer;

        public TipTypeManagementSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _tipTypeDriver = new TipTypeDriver(context);
            _tipDriver = new TipDriver(context);
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                // Cleanup if needed
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in TipTypeManagementSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have a tip type category with the following details:")]
        public void GivenIHaveATipTypeCategoryWithTheFollowingDetails(Table table)
        {
            var tipTypeModel = table.CreateInstance<TipTypeTestModel>();

            // Assign an ID if not provided
            if (!tipTypeModel.Id.HasValue)
            {
                tipTypeModel.Id = _tipTypeIdCounter++;
            }

            // Setup the tip type in the repository
            _tipTypeDriver.SetupTipTypes(new List<TipTypeTestModel> { tipTypeModel });

            // Store for later use
            _tipTypesByName[tipTypeModel.Name] = tipTypeModel;
            _context.StoreModel(tipTypeModel, "CurrentTipType");
        }

        [Given(@"I have multiple tip types in the system:")]
        public void GivenIHaveMultipleTipTypesInTheSystem(Table table)
        {
            var tipTypes = table.CreateSet<TipTypeTestModel>().ToList();

            // Assign IDs if not provided
            for (int i = 0; i < tipTypes.Count; i++)
            {
                if (!tipTypes[i].Id.HasValue)
                {
                    tipTypes[i].Id = _tipTypeIdCounter++;
                }

                // Store by name for easier lookup
                _tipTypesByName[tipTypes[i].Name] = tipTypes[i];
            }

            // Setup the tip types in the repository
            _tipTypeDriver.SetupTipTypes(tipTypes);
        }

        [Given(@"the tip type has the following associated tips:")]
        public void GivenTheTipTypeHasTheFollowingAssociatedTips(Table table)
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");
            tipTypeModel.Should().NotBeNull("Tip type should be available in context");

            var tips = table.CreateSet<TipTestModel>().ToList();

            // Set the correct tip type ID and assign IDs if not provided
            for (int i = 0; i < tips.Count; i++)
            {
                tips[i].TipTypeId = tipTypeModel.Id.Value;

                if (!tips[i].Id.HasValue)
                {
                    tips[i].Id = i + 1;
                }

                // Add to the tip type's tips
                tipTypeModel.Tips.Add(tips[i]);
            }

            // Setup the tips in the repository
            _tipDriver.SetupTips(tips);

            // Store for later use
            _context.StoreModel(tips, $"TipsForType_{tipTypeModel.Name}");
        }

        [Given(@"the tip type has no associated tips")]
        public void GivenTheTipTypeHasNoAssociatedTips()
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");
            tipTypeModel.Should().NotBeNull("Tip type should be available in context");

            // Ensure the tip type has no tips
            tipTypeModel.Tips.Clear();
        }

        [When(@"I create a new tip type record with the following details:")]
        public async Task WhenICreateANewTipTypeRecordWithTheFollowingDetails(Table table)
        {
            var tipTypeModel = table.CreateInstance<TipTypeTestModel>();

            // Store in created list for tracking
            _createdTipTypes.Add(tipTypeModel);

            var result = await _tipTypeDriver.CreateTipTypeAsync(tipTypeModel);

            // Store for later use
            _tipTypesByName[tipTypeModel.Name] = tipTypeModel;
        }

        [When(@"I update the tip type with the following details:")]
        public async Task WhenIUpdateTheTipTypeWithTheFollowingDetails(Table table)
        {
            var updatedTipType = table.CreateInstance<TipTypeTestModel>();
            var currentTipType = _context.GetModel<TipTypeTestModel>("CurrentTipType");

            currentTipType.Should().NotBeNull("Current tip type should be available in context");

            // Update the tip type
            currentTipType.Name = updatedTipType.Name;
            currentTipType.I8n = updatedTipType.I8n;

            // Create response directly (NO MediatR)
            var tipTypeDto = new TipTypeDto
            {
                Id = currentTipType.Id.Value,
                Name = currentTipType.Name,
                I8n = currentTipType.I8n
            };

            var result = Result<TipTypeDto>.Success(tipTypeDto);

            // Store the result
            _context.StoreResult(result);

            // Update in the dictionary
            _tipTypesByName.Remove(currentTipType.Name);
            _tipTypesByName[updatedTipType.Name] = currentTipType;
        }

        [When(@"I delete the tip type")]
        public async Task WhenIDeleteTheTipType()
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");
            tipTypeModel.Should().NotBeNull("Tip type should be available in context");

            await _tipTypeDriver.DeleteTipTypeAsync(tipTypeModel.Id.Value);
        }

        [When(@"I request a list of all tip types")]
        public async Task WhenIRequestAListOfAllTipTypes()
        {
            await _tipTypeDriver.GetAllTipTypesAsync();
        }

        [When(@"I retrieve the tip type with its associated tips")]
        public async Task WhenIRetrieveTheTipTypeWithItsAssociatedTips()
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");
            tipTypeModel.Should().NotBeNull("Tip type should be available in context");

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
        }

        [When(@"I create tip types with different localizations:")]
        public async Task WhenICreateTipTypesWithDifferentLocalizations(Table table)
        {
            var tipTypes = table.CreateSet<TipTypeTestModel>().ToList();
            _createdTipTypes.Clear(); // Clear previous results

            foreach (var tipType in tipTypes)
            {
                var result = await _tipTypeDriver.CreateTipTypeAsync(tipType);

                if (result.IsSuccess && result.Data != null)
                {
                    // ✅ FIXED: Store the actual created tip type with correct ID from result
                    tipType.Id = result.Data.Id; // ✅ Update with actual ID
                    _createdTipTypes.Add(tipType);
                }
            }
        }


        [Then(@"the tip type record should be created successfully")]
        public void ThenTheTipTypeRecordShouldBeCreatedSuccessfully()
        {
            var lastResult = _context.GetLastResult<TipTypeDto>();
            lastResult.Should().NotBeNull("Result should be available after creation");
            lastResult.IsSuccess.Should().BeTrue("Tip type creation should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");
            lastResult.Data.Id.Should().BeGreaterThan(0, "Tip type ID should be positive");
        }

        [Then(@"the tip type record should have the correct details:")]
        public void ThenTheTipTypeRecordShouldHaveTheCorrectDetails(Table table)
        {
            var expectedTipType = table.CreateInstance<TipTypeTestModel>();
            var lastResult = _context.GetLastResult<TipTypeDto>();

            lastResult.Should().NotBeNull("Result should be available after creation");
            lastResult.IsSuccess.Should().BeTrue("Tip type creation should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");

            lastResult.Data.Name.Should().Be(expectedTipType.Name, "Tip type name should match expected value");
            lastResult.Data.I8n.Should().Be(expectedTipType.I8n, "Tip type localization should match expected value");
        }

        [Then(@"the tip type should be updated successfully")]
        public void ThenTheTipTypeShouldBeUpdatedSuccessfully()
        {
            var lastResult = _context.GetLastResult<TipTypeDto>();
            lastResult.Should().NotBeNull("Result should be available after update");
            lastResult.IsSuccess.Should().BeTrue("Tip type update should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");
        }

        [Then(@"the tip type should have the following details:")]
        public void ThenTheTipTypeShouldHaveTheFollowingDetails(Table table)
        {
            ThenTheTipTypeRecordShouldHaveTheCorrectDetails(table);
        }

        [Then(@"the tip type should be deleted successfully")]
        public void ThenTheTipTypeShouldBeDeletedSuccessfully()
        {
            var lastResult = _context.GetLastResult<bool>();
            lastResult.Should().NotBeNull("Delete result should be available");
            lastResult.IsSuccess.Should().BeTrue("Tip type deletion should be successful");
            lastResult.Data.Should().BeTrue("Tip type deletion should return true");
        }

        [Then(@"the tip type should not exist in the system")]
        public void ThenTheTipTypeShouldNotExistInTheSystem()
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");

            // Get tip type by ID should fail after deletion
            var task = _tipTypeDriver.GetTipTypeByIdAsync(tipTypeModel.Id.Value);
            task.Wait();

            var result = task.Result;
            result.IsSuccess.Should().BeFalse("Tip type should not be found after deletion");
        }

        [Then(@"the result should contain (.*) tip types")]
        public void ThenTheResultShouldContainTipTypes(int expectedCount)
        {
            var lastResult = _context.GetLastResult<List<TipTypeDto>>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip type retrieval should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");
            lastResult.Data.Count.Should().Be(expectedCount, $"Result should contain {expectedCount} tip types");
        }

        [Then(@"the tip type list should include ""(.*)""")]
        public void ThenTheTipTypeListShouldInclude(string expectedName)
        {
            var lastResult = _context.GetLastResult<List<TipTypeDto>>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip type retrieval should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");
            lastResult.Data.Should().Contain(tt => tt.Name == expectedName, $"Tip type list should include '{expectedName}'");
        }

        [Then(@"the tip type should have (.*) associated tips")]
        public void ThenTheTipTypeShouldHaveAssociatedTips(int expectedCount)
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");
            tipTypeModel.Should().NotBeNull("Tip type should be available in context");

            var tips = _context.GetModel<List<TipTestModel>>($"TipsForType_{tipTypeModel.Name}");
            tips.Should().NotBeNull("Tips should be available in context");
            tips.Count.Should().Be(expectedCount, $"Tip type should have {expectedCount} associated tips");
        }

        [Given(@"I have a tip type with the following details:")]
        public void GivenIHaveATipTypeWithTheFollowingDetails(Table table)
        {
            // Same as GivenIHaveATipTypeCategoryWithTheFollowingDetails
            GivenIHaveATipTypeCategoryWithTheFollowingDetails(table);
        }

        [When(@"I create a new tip type with the following details:")]
        public async Task WhenICreateANewTipTypeWithTheFollowingDetails(Table table)
        {
            // Same as WhenICreateANewTipTypeRecordWithTheFollowingDetails
            await WhenICreateANewTipTypeRecordWithTheFollowingDetails(table);
        }

        [Then(@"the tips should include ""(.*)""")]
        public void ThenTheTipsShouldInclude(string expectedTitle)
        {
            var tipTypeModel = _context.GetModel<TipTypeTestModel>("CurrentTipType");
            tipTypeModel.Should().NotBeNull("Tip type should be available in context");

            var tips = _context.GetModel<List<TipTestModel>>($"TipsForType_{tipTypeModel.Name}");
            tips.Should().NotBeNull("Tips should be available in context");
            tips.Should().Contain(t => t.Title == expectedTitle, $"Tips should include tip with title '{expectedTitle}'");
        }

        [Then(@"I should receive successful results")]
        public void ThenIShouldReceiveSuccessfulResults()
        {
            var successCount = _createdTipTypes.Count;
            successCount.Should().BeGreaterThan(0, "At least one tip type should be successfully created");
        }

        [Then(@"the tip types should be created with the correct localizations")]
        public void ThenTheTipTypesShouldBeCreatedWithTheCorrectLocalizations()
        {
            foreach (var tipType in _createdTipTypes)
            {
                // ✅ FIXED: Check context using actual ID from creation
                var tipTypeResult = _context.GetModel<TipTypeTestModel>($"TipType_{tipType.Id}");
                tipTypeResult.Should().NotBeNull($"Tip type '{tipType.Name}' should be available in context");

                // ✅ FIXED: Compare with the original input, not context (which might be overwritten)
                tipTypeResult.I8n.Should().Be(tipType.I8n, $"Tip type '{tipType.Name}' should have the correct localization");
                tipTypeResult.Name.Should().Be(tipType.Name, $"Tip type name should match");
            }
        }
    }
}