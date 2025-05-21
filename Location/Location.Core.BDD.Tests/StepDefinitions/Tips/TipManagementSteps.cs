using FluentAssertions;
using Location.Core.Application.Tips.DTOs;
using Location.Core.Application.Tips.Queries.GetTipById;
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
    public class TipManagementSteps
    {
        private readonly ApiContext _context;
        private readonly TipDriver _tipDriver;
        private readonly TipTypeDriver _tipTypeDriver;
        private readonly Dictionary<string, TipTypeTestModel> _tipTypesByName = new();
        private int _tipTypeIdCounter = 1;

        public TipManagementSteps(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _tipDriver = new TipDriver(context);
            _tipTypeDriver = new TipTypeDriver(context);
        }

        [Given(@"I have the following tip types in the system:")]
        public void GivenIHaveTheFollowingTipTypesInTheSystem(Table table)
        {
            var tipTypes = table.CreateSet<TipTypeTestModel>().ToList();

            // Assign IDs if not provided
            foreach (var tipType in tipTypes)
            {
                if (!tipType.Id.HasValue)
                {
                    tipType.Id = _tipTypeIdCounter++;
                }

                // Store by name for easier lookup
                _tipTypesByName[tipType.Name] = tipType;
            }

            // Setup the tip types in the repository
            _tipTypeDriver.SetupTipTypes(tipTypes);
        }

        [Given(@"I have a tip with the following details:")]
        public async Task GivenIHaveATipWithTheFollowingDetails(Table table)
        {
            var tipModel = table.CreateInstance<TipTestModel>();

            // Assign an ID if not provided
            if (!tipModel.Id.HasValue)
            {
                tipModel.Id = 1;
            }

            // Setup the tip in the repository
            _tipDriver.SetupTips(new List<TipTestModel> { tipModel });

            // Store the tip in the context
            _context.StoreTipData(tipModel);

            // Create the tip
            await _tipDriver.CreateTipAsync(tipModel);
        }

        [Given(@"I have multiple tips for each type:")]
        public void GivenIHaveMultipleTipsForEachType(Table table)
        {
            var tips = table.CreateSet<TipTestModel>().ToList();

            // Assign IDs if not provided
            for (int i = 0; i < tips.Count; i++)
            {
                if (!tips[i].Id.HasValue)
                {
                    tips[i].Id = i + 1;
                }
            }

            // Setup the tips in the repository
            _tipDriver.SetupTips(tips);

            // Store all tips in the context
            _context.StoreModel(tips, "AllTips");
        }

        [Given(@"I have multiple tips for the ""(.*)"" type:")]
        public void GivenIHaveMultipleTipsForTheType(string tipTypeName, Table table)
        {
            var tipTypeModel = _tipTypesByName[tipTypeName];
            var tips = table.CreateSet<TipTestModel>().ToList();

            // Set the correct tip type ID and assign IDs if not provided
            for (int i = 0; i < tips.Count; i++)
            {
                tips[i].TipTypeId = tipTypeModel.Id.Value;

                if (!tips[i].Id.HasValue)
                {
                    tips[i].Id = i + 1;
                }
            }

            // Setup the tips in the repository
            _tipDriver.SetupTips(tips);

            // Store for later use
            _context.StoreModel(tips, $"TipsForType_{tipTypeName}");
        }

        [Given(@"I have a tip type with the following details:")]
        public void GivenIHaveATipTypeWithTheFollowingDetails(Table table)
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

        [When(@"I create a new tip with the following details:")]
        public async Task WhenICreateANewTipWithTheFollowingDetails(Table table)
        {
            var tipModel = table.CreateInstance<TipTestModel>();
            await _tipDriver.CreateTipAsync(tipModel);
        }

        [When(@"I update the tip with the following details:")]
        public async Task WhenIUpdateTheTipWithTheFollowingDetails(Table table)
        {
            var tipModel = _context.GetTipData();
            tipModel.Should().NotBeNull("Tip data should be available in context");

            // Update the tip with the new details
            var updatedData = table.CreateDynamicInstance();

            if (table.Header.Contains("Title"))
                tipModel.Title = updatedData.Title;

            if (table.Header.Contains("Content"))
                tipModel.Content = updatedData.Content;

            if (table.Header.Contains("Fstop"))
                tipModel.Fstop = updatedData.Fstop;

            if (table.Header.Contains("ShutterSpeed"))
                tipModel.ShutterSpeed = updatedData.ShutterSpeed;

            if (table.Header.Contains("Iso"))
                tipModel.Iso = updatedData.Iso;

            await _tipDriver.UpdateTipAsync(tipModel);
        }

        [When(@"I delete the tip")]
        public async Task WhenIDeleteTheTip()
        {
            var tipModel = _context.GetTipData();
            tipModel.Should().NotBeNull("Tip data should be available in context");

            await _tipDriver.DeleteTipAsync(tipModel.Id.Value);
        }

        [When(@"I retrieve the tip by its ID")]
        public async Task WhenIRetrieveTheTipByItsID()
        {
            var tipModel = _context.GetTipData();
            tipModel.Should().NotBeNull("Tip data should be available in context");

            await _tipDriver.GetTipByIdAsync(tipModel.Id.Value);
        }

        [When(@"I request tips for type ""(.*)""")]
        public async Task WhenIRequestTipsForType(string tipTypeName)
        {
            var tipTypeModel = _tipTypesByName[tipTypeName];
            await _tipDriver.GetTipsByTypeAsync(tipTypeModel.Id.Value);
        }

        [When(@"I request a random tip for type ""(.*)""")]
        public async Task WhenIRequestARandomTipForType(string tipTypeName)
        {
            var tipTypeModel = _tipTypesByName[tipTypeName];
            await _tipDriver.GetRandomTipByTypeAsync(tipTypeModel.Id.Value);
        }

        [When(@"I create a new tip type with the following details:")]
        public async Task WhenICreateANewTipTypeWithTheFollowingDetails(Table table)
        {
            var tipTypeModel = table.CreateInstance<TipTypeTestModel>();
            await _tipTypeDriver.CreateTipTypeAsync(tipTypeModel);

            // Store for later use
            _tipTypesByName[tipTypeModel.Name] = tipTypeModel;
        }

        [Then(@"the tip should be created successfully")]
        public void ThenTheTipShouldBeCreatedSuccessfully()
        {
            var tipModel = _context.GetTipData();
            tipModel.Should().NotBeNull("Tip data should be stored in context after creation");
            tipModel.Id.Should().NotBeNull("Tip should have an ID after creation");
            tipModel.Id.Should().BeGreaterThan(0, "Tip ID should be positive");
        }

        [Then(@"the tip should have the correct details:")]
        public void ThenTheTipShouldHaveTheCorrectDetails(Table table)
        {
            var expectedTip = table.CreateInstance<TipTestModel>();
            var actualTip = _context.GetTipData();

            actualTip.Should().NotBeNull("Tip data should be available in context");
            actualTip.TipTypeId.Should().Be(expectedTip.TipTypeId, "Tip type ID should match expected value");
            actualTip.Title.Should().Be(expectedTip.Title, "Tip title should match expected value");
            actualTip.Content.Should().Be(expectedTip.Content, "Tip content should match expected value");
            actualTip.Fstop.Should().Be(expectedTip.Fstop, "F-stop should match expected value");
            actualTip.ShutterSpeed.Should().Be(expectedTip.ShutterSpeed, "Shutter speed should match expected value");
            actualTip.Iso.Should().Be(expectedTip.Iso, "ISO should match expected value");
        }

        [Then(@"the tip should be updated successfully")]
        public void ThenTheTipShouldBeUpdatedSuccessfully()
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Update result should be available");
            lastResult.IsSuccess.Should().BeTrue("Tip update should be successful");
        }

        [Then(@"the tip should have the following details:")]
        public void ThenTheTipShouldHaveTheFollowingDetails(Table table)
        {
            ThenTheTipShouldHaveTheCorrectDetails(table);
        }

        [Then(@"the tip should be deleted successfully")]
        public void ThenTheTipShouldBeDeletedSuccessfully()
        {
            var lastResult = _context.GetLastResult<bool>();
            lastResult.Should().NotBeNull("Delete result should be available");
            lastResult.IsSuccess.Should().BeTrue("Tip deletion should be successful");
            lastResult.Data.Should().BeTrue("Tip deletion should return true");
        }

        [Then(@"the tip should not exist in the system")]
        public void ThenTheTipShouldNotExistInTheSystem()
        {
            var tipModel = _context.GetTipData();

            // Mock that the tip no longer exists
            var tipDriver = new TipDriver(_context);
            var task = tipDriver.GetTipByIdAsync(tipModel.Id.Value);
            task.Wait();

            var result = task.Result;
            result.IsSuccess.Should().BeFalse("Tip should not be found after deletion");
        }

        [Then(@"the retrieved tip should match the original tip details")]
        public void ThenTheRetrievedTipShouldMatchTheOriginalTipDetails()
        {
            var originalTip = _context.GetTipData();
            originalTip.Should().NotBeNull("Original tip data should be available in context");

            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip retrieval should be successful");

            // The response type depends on the query, so we need to handle different types
            if (lastResult.Data is GetTipByIdQueryResponse response)
            {
                response.Id.Should().Be(originalTip.Id.Value, "Retrieved tip ID should match original");
                response.TipTypeId.Should().Be(originalTip.TipTypeId, "Retrieved tip type ID should match original");
                response.Title.Should().Be(originalTip.Title, "Retrieved tip title should match original");
                response.Content.Should().Be(originalTip.Content, "Retrieved tip content should match original");
                response.Fstop.Should().Be(originalTip.Fstop, "Retrieved F-stop should match original");
                response.ShutterSpeed.Should().Be(originalTip.ShutterSpeed, "Retrieved shutter speed should match original");
                response.Iso.Should().Be(originalTip.Iso, "Retrieved ISO should match original");
                response.I8n.Should().Be(originalTip.I8n, "Retrieved localization should match original");
            }
        }

        [Then(@"the result should contain (.*) tips")]
        public void ThenTheResultShouldContainTips(int expectedCount)
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip retrieval should be successful");

            if (lastResult.Data is List<TipDto> responses)
            {
                responses.Count.Should().Be(expectedCount, $"Result should contain {expectedCount} tips");
            }
        }

        [Then(@"the result should include ""(.*)""")]
        public void ThenTheResultShouldInclude(string expectedTitle)
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip retrieval should be successful");

            if (lastResult.Data is List<TipDto> responses)
            {
                responses.Should().Contain(t => t.Title == expectedTitle, $"Result should include tip with title '{expectedTitle}'");
            }
        }

        [Then(@"the result should not include ""(.*)""")]
        public void ThenTheResultShouldNotInclude(string unexpectedTitle)
        {
            var lastResult = _context.GetLastResult<object>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip retrieval should be successful");

            if (lastResult.Data is List<TipDto> responses)
            {
                responses.Should().NotContain(t => t.Title == unexpectedTitle, $"Result should not include tip with title '{unexpectedTitle}'");
            }
        }

        [Then(@"the result should contain a single tip")]
        public void ThenTheResultShouldContainASingleTip()
        {
            var lastResult = _context.GetLastResult<TipDto>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip retrieval should be successful");
            lastResult.Data.Should().NotBeNull("Tip data should be available");
        }

        [Then(@"the tip should be of type ""(.*)""")]
        public void ThenTheTipShouldBeOfType(string expectedTypeName)
        {
            var tipTypeModel = _tipTypesByName[expectedTypeName];

            var lastResult = _context.GetLastResult<TipDto>();
            lastResult.Should().NotBeNull("Result should be available after query");
            lastResult.IsSuccess.Should().BeTrue("Tip retrieval should be successful");
            lastResult.Data.Should().NotBeNull("Tip data should be available");
            lastResult.Data.TipTypeId.Should().Be(tipTypeModel.Id.Value, $"Tip should be of type '{expectedTypeName}'");
        }

        [Then(@"the tip type should be created successfully")]
        public void ThenTheTipTypeShouldBeCreatedSuccessfully()
        {
            var lastResult = _context.GetLastResult<TipTypeDto>();
            lastResult.Should().NotBeNull("Result should be available after creation");
            lastResult.IsSuccess.Should().BeTrue("Tip type creation should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");
            lastResult.Data.Id.Should().BeGreaterThan(0, "Tip type ID should be positive");
        }

        [Then(@"the tip type should have the correct details:")]
        public void ThenTheTipTypeShouldHaveTheCorrectDetails(Table table)
        {
            var expectedTipType = table.CreateInstance<TipTypeTestModel>();
            var lastResult = _context.GetLastResult<TipTypeDto>();

            lastResult.Should().NotBeNull("Result should be available after creation");
            lastResult.IsSuccess.Should().BeTrue("Tip type creation should be successful");
            lastResult.Data.Should().NotBeNull("Tip type data should be available");

            lastResult.Data.Name.Should().Be(expectedTipType.Name, "Tip type name should match expected value");
            lastResult.Data.I8n.Should().Be(expectedTipType.I8n, "Tip type localization should match expected value");
        }
    }
}