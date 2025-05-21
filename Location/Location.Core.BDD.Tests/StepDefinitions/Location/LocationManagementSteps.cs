using BoDi;
using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Location
{
    [Binding]
    public class LocationManagementSteps
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly IObjectContainer _objectContainer;

        public LocationManagementSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context;
            _objectContainer = objectContainer;
        }

        // This is the TestCleanup method that will safely handle cleanup
        [BeforeScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {

            }
            catch (Exception ex)
            {
                // Log but don't throw to avoid masking test failures
                Console.WriteLine($"Error in LocationManagementSteps cleanup: {ex.Message}");
            }
        }
        public LocationManagementSteps(ApiContext context)
        {
            _context = context;
            _mediator = _context.GetService<IMediator>();
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>();
        }

       

        [Given(@"I want to create a new location with the following details:")]
        public void GivenIWantToCreateANewLocationWithTheFollowingDetails(Table table)
        {
            var locationData = table.CreateInstance<LocationTestModel>();
            _context.StoreLocationData(locationData);
        }

        [Given(@"I have a location with the following details:")]
        public void GivenIHaveALocationWithTheFollowingDetails(Table table)
        {
            var locationData = table.CreateInstance<LocationTestModel>();
            locationData.Id = 1; // Assign a valid ID

            // Set up the mock repository to return this location
            var domainEntity = locationData.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationData.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            _locationRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Store for later steps
            _context.StoreLocationData(locationData);
        }

        [Given(@"I have a deleted location with the following details:")]
        public void GivenIHaveADeletedLocationWithTheFollowingDetails(Table table)
        {
            var locationData = table.CreateInstance<LocationTestModel>();
            locationData.Id = 1; // Assign a valid ID
            locationData.IsDeleted = true; // Mark as deleted

            // Set up the mock repository to return this location
            var domainEntity = locationData.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationData.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            _locationRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location location, CancellationToken _) =>
                {
                    return Result<Domain.Entities.Location>.Success(location);
                });

            // Store for later steps
            _context.StoreLocationData(locationData);
        }

        [When(@"I save the location")]
        public async Task WhenISaveTheLocation()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be provided before saving");

            // Create the save command
            var command = new SaveLocationCommand
            {
                Id = locationData.Id,
                Title = locationData.Title,
                Description = locationData.Description,
                Latitude = locationData.Latitude,
                Longitude = locationData.Longitude,
                City = locationData.City,
                State = locationData.State,
                PhotoPath = locationData.PhotoPath
            };

            // Set up mock for creating a new location
            if (!locationData.Id.HasValue || locationData.Id.Value <= 0)
            {
                var createdLocation = locationData.ToDomainEntity();
                SetPrivateProperty(createdLocation, "Id", 1); // Assign an ID for the created location

                _locationRepositoryMock
                    .Setup(repo => repo.CreateAsync(
                        It.IsAny<Domain.Entities.Location>(),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(createdLocation));
            }

            // Execute the command via MediatR
            var result = await _mediator.Send(command);

            // Store the result for verification in the Then steps
            _context.StoreResult(result);
        }

        [When(@"I update the location with the following details:")]
        public async Task WhenIUpdateTheLocationWithTheFollowingDetails(Table table)
        {
            var updateData = table.CreateInstance<LocationTestModel>();
            var locationData = _context.GetLocationData();

            locationData.Should().NotBeNull("Location data should be provided before updating");
            locationData.Id.Should().NotBeNull().And.BeGreaterThan(0, "Location ID should be valid for updates");

            // Create the save command for updating
            var command = new SaveLocationCommand
            {
                Id = locationData.Id,
                Title = updateData.Title,
                Description = updateData.Description,
                Latitude = locationData.Latitude, // Keep original coordinates
                Longitude = locationData.Longitude,
                City = locationData.City, // Keep original address
                State = locationData.State,
                PhotoPath = locationData.PhotoPath
            };

            // Execute the command via MediatR
            var result = await _mediator.Send(command);

            // Store the result for verification in the Then steps
            _context.StoreResult(result);

            // Update the stored location data with the new values
            locationData.Title = updateData.Title;
            locationData.Description = updateData.Description;
            _context.StoreLocationData(locationData);
        }

        [When(@"I delete the location")]
        public async Task WhenIDeleteTheLocation()
        {
            var locationData = _context.GetLocationData();

            locationData.Should().NotBeNull("Location data should be provided before deleting");
            locationData.Id.Should().NotBeNull().And.BeGreaterThan(0, "Location ID should be valid for deletion");

            // Create the delete command
            var command = new DeleteLocationCommand
            {
                Id = locationData.Id.Value
            };

            // Execute the command via MediatR
            var result = await _mediator.Send(command);

            // Store the result for verification in the Then steps
            _context.StoreResult(result);

            // Update the stored location data
            locationData.IsDeleted = true;
            _context.StoreLocationData(locationData);
        }

        [When(@"I restore the location")]
        public async Task WhenIRestoreTheLocation()
        {
            var locationData = _context.GetLocationData();

            locationData.Should().NotBeNull("Location data should be provided before restoring");
            locationData.Id.Should().NotBeNull().And.BeGreaterThan(0, "Location ID should be valid for restoration");
            locationData.IsDeleted.Should().BeTrue("Location should be deleted before restoring");

            // Create the restore command
            var command = new RestoreLocationCommand
            {
                LocationId = locationData.Id.Value
            };

            // Execute the command via MediatR
            var result = await _mediator.Send(command);

            // Store the result for verification in the Then steps
            _context.StoreResult(result);

            // Update the stored location data
            locationData.IsDeleted = false;
            _context.StoreLocationData(locationData);
        }

        [Then(@"the location should be created successfully")]
        public void ThenTheLocationShouldBeCreatedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after creation");
            result.IsSuccess.Should().BeTrue("Location creation should be successful");
            result.Data.Should().NotBeNull("Created location data should be returned");
            result.Data.Id.Should().BeGreaterThan(0, "Created location should have a valid ID");
        }

        [Then(@"the location should have the correct details:")]
        public void ThenTheLocationShouldHaveTheCorrectDetails(Table table)
        {
            var expectedDetails = table.CreateInstance<LocationTestModel>();
            var result = _context.GetLastResult<LocationDto>();

            result.Should().NotBeNull("Result should be available");
            result.IsSuccess.Should().BeTrue("Operation should be successful");
            result.Data.Should().NotBeNull("Location data should be returned");

            // Verify location details
            result.Data.Title.Should().Be(expectedDetails.Title, "Title should match expected value");
            result.Data.Description.Should().Be(expectedDetails.Description, "Description should match expected value");
            result.Data.Latitude.Should().BeApproximately(expectedDetails.Latitude, 0.000001, "Latitude should match expected value");
            result.Data.Longitude.Should().BeApproximately(expectedDetails.Longitude, 0.000001, "Longitude should match expected value");
            result.Data.City.Should().Be(expectedDetails.City, "City should match expected value");
            result.Data.State.Should().Be(expectedDetails.State, "State should match expected value");
        }

        [Then(@"the location should be updated successfully")]
        public void ThenTheLocationShouldBeUpdatedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after update");
            result.IsSuccess.Should().BeTrue("Location update should be successful");
            result.Data.Should().NotBeNull("Updated location data should be returned");
        }

        [Then(@"the location should have the following details:")]
        public void ThenTheLocationShouldHaveTheFollowingDetails(Table table)
        {
            ThenTheLocationShouldHaveTheCorrectDetails(table);
        }

        [Then(@"the location should be deleted successfully")]
        public void ThenTheLocationShouldBeDeletedSuccessfully()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Result should be available after deletion");
            result.IsSuccess.Should().BeTrue("Location deletion should be successful");
            result.Data.Should().BeTrue("Deletion operation should return true");
        }

        [Then(@"the location should not exist in the system")]
        public void ThenTheLocationShouldNotExistInTheSystem()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Verify the location is marked as deleted in our model
            locationData.IsDeleted.Should().BeTrue("Location should be marked as deleted");

            // Set up the repository mock to verify the state
            _locationRepositoryMock.Verify(repo =>
                repo.UpdateAsync(
                    It.Is<Domain.Entities.Location>(l => l.IsDeleted),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce,
                "Repository should have been called to update the location as deleted");
        }

        [Then(@"the location should be restored successfully")]
        public void ThenTheLocationShouldBeRestoredSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after restoration");
            result.IsSuccess.Should().BeTrue("Location restoration should be successful");
            result.Data.Should().NotBeNull("Restored location data should be returned");
        }

        [Then(@"the location should exist in the system")]
        public void ThenTheLocationShouldExistInTheSystem()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Set up the repository mock to verify the state
            _locationRepositoryMock.Verify(repo =>
                repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce,
                "Repository should have been called to update the location");
        }

        [Then(@"the location should not be marked as deleted")]
        public void ThenTheLocationShouldNotBeMarkedAsDeleted()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Verify the location is not marked as deleted in our model
            locationData.IsDeleted.Should().BeFalse("Location should not be marked as deleted");

            // Verify with the repository
            _locationRepositoryMock.Verify(repo =>
                repo.UpdateAsync(
                    It.Is<Domain.Entities.Location>(l => !l.IsDeleted),
                    It.IsAny<CancellationToken>()),
                Times.AtLeastOnce,
                "Repository should have been called to update the location as not deleted");
        }

        // Helper method to set private property using reflection
        private static void SetPrivateProperty(object obj, string propertyName, object value)
        {
            var property = obj.GetType().GetProperty(propertyName);
            if (property != null)
            {
                property.SetValue(obj, value);
            }
            else
            {
                var field = obj.GetType().GetField(propertyName,
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(obj, value);
            }
        }
    }
}