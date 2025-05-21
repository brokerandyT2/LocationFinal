using BoDi;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Queries.Locations;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Collections.Generic;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Location
{
    [Binding]
    public class LocationSearchSteps
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private readonly List<LocationTestModel> _storedLocations = new();
        // Add this to the LocationSearchSteps.cs class

        private readonly IObjectContainer _objectContainer;

        public LocationSearchSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context;
            _objectContainer = objectContainer;
        }

        // This is the TestCleanup method that will safely handle cleanup
        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
            }
            catch (Exception ex)
            {
                // Log but don't throw to avoid masking test failures
                Console.WriteLine($"Error in LocationSearchSteps cleanup: {ex.Message}");
            }
        }
        public LocationSearchSteps(ApiContext context)
        {
            _context = context;
            _mediator = _context.GetService<IMediator>();
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>();
        }

        [Given(@"I have multiple locations stored in the system for search:")]
        public void GivenIHaveMultipleLocationsStoredInTheSystem(Table table)
        {
            var locations = table.CreateSet<LocationTestModel>();
            int idCounter = 1;

            // Assign IDs to the locations
            foreach (var location in locations)
            {
                location.Id = idCounter++;
                _storedLocations.Add(location);
            }

            // Convert to domain entities
            var domainEntities = _storedLocations.ConvertAll(l => l.ToDomainEntity());

            // Set up the mock repository to return these locations
            _locationRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(domainEntities));

            _locationRepositoryMock
                .Setup(repo => repo.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(domainEntities.FindAll(l => !l.IsDeleted)));
        }

        [When(@"I request a list of all locations")]
        public async Task WhenIRequestAListOfAllLocations()
        {
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 100
            };

            // Execute the query via MediatR
            var result = await _mediator.Send(query);

            // Store the result for verification
            _context.StoreResult(result);
        }

        [When(@"I search for a location with title ""(.*)""")]
        public async Task WhenISearchForALocationWithTitle(string title)
        {
            // Find the location in our stored list
            var matchingLocation = _storedLocations.Find(l => l.Title == title);

            if (matchingLocation != null)
            {
                // Set up the mock repository to return this specific location
                _locationRepositoryMock
                    .Setup(repo => repo.GetByTitleAsync(
                        It.Is<string>(t => t == title),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(matchingLocation.ToDomainEntity()));
            }
            else
            {
                // Set up the mock to return a failure result for non-existent location
                _locationRepositoryMock
                    .Setup(repo => repo.GetByTitleAsync(
                        It.Is<string>(t => t == title),
                        It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Failure($"Location with title '{title}' not found"));
            }

            // Create and execute the query
            var query = new GetLocationByTitleQuery { Title = title };
            var result = await _mediator.Send(query);

            // Store the result for verification
            _context.StoreResult(result);
        }

        [When(@"I search for locations within (.*) km of coordinates:")]
        public async Task WhenISearchForLocationsWithinKmOfCoordinates(double distance, Table table)
        {
            var coordinateData = table.CreateInstance<LocationTestModel>();

            // Filter locations by distance in our test data
            var matchingLocations = new List<Domain.Entities.Location>();

            foreach (var location in _storedLocations)
            {
                var domainEntity = location.ToDomainEntity();
                var locationCoordinate = domainEntity.Coordinate;
                var searchCoordinate = new Domain.ValueObjects.Coordinate(coordinateData.Latitude, coordinateData.Longitude);

                var distanceToLocation = locationCoordinate.DistanceTo(searchCoordinate);
                if (distanceToLocation <= distance)
                {
                    matchingLocations.Add(domainEntity);
                }
            }

            // Set up the mock repository to return these filtered locations
            _locationRepositoryMock
                .Setup(repo => repo.GetNearbyAsync(
                    It.Is<double>(lat => Math.Abs(lat - coordinateData.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - coordinateData.Longitude) < 0.0001),
                    It.Is<double>(dist => Math.Abs(dist - distance) < 0.0001),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(matchingLocations));

            // Create and execute the query
            var query = new GetNearbyLocationsQuery
            {
                Latitude = coordinateData.Latitude,
                Longitude = coordinateData.Longitude,
                DistanceKm = distance
            };

            var result = await _mediator.Send(query);

            // Store the result for verification
            _context.StoreResult(result);
        }

        [When(@"I search for locations with filter ""(.*)""")]
        public async Task WhenISearchForLocationsWithFilter(string filter)
        {
            // Create a query with the search term
            var query = new GetLocationsQuery
            {
                PageNumber = 1,
                PageSize = 100,
                SearchTerm = filter
            };

            // Execute the query
            var result = await _mediator.Send(query);

            // Store the result for verification
            _context.StoreResult(result);
        }

        [Then(@"I should receive a successful location search result")]
        public void ThenIShouldReceiveASuccessfulResult()
        {
            var result = _context.GetLastResult<object>();
            result.Should().NotBeNull("Result should be available");
            result.IsSuccess.Should().BeTrue("Location search operation should be successful");
        }

        [Then(@"I should receive a failure result")]
        public void ThenIShouldReceiveAFailureResult()
        {
            // Get the last result, which could be of different types
            var locationListResult = _context.GetLastResult<PagedList<LocationListDto>>();
            var locationResult = _context.GetLastResult<LocationDto>();
            var locationListResults = _context.GetLastResult<List<LocationListDto>>();

            // Check any of the results that are not null
            if (locationListResult != null)
            {
                locationListResult.IsSuccess.Should().BeFalse("Operation should have failed");
            }
            else if (locationResult != null)
            {
                locationResult.IsSuccess.Should().BeFalse("Operation should have failed");
            }
            else if (locationListResults != null)
            {
                locationListResults.IsSuccess.Should().BeFalse("Operation should have failed");
            }
            else
            {
                Assert.Fail("No result was found to verify");
            }
        }

        [Then(@"the result should contain (.*) locations?")]
        public void ThenTheResultShouldContainLocations(int count)
        {
            var locationListResult = _context.GetLastResult<PagedList<LocationListDto>>();
            var locationListResults = _context.GetLastResult<List<LocationListDto>>();

            if (locationListResult != null)
            {
                locationListResult.Data.Items.Count.Should().Be(count, $"Result should contain {count} locations");
            }
            else if (locationListResults != null)
            {
                locationListResults.Data.Count.Should().Be(count, $"Result should contain {count} locations");
            }
            else
            {
                Assert.Fail("No location list result was found to verify the count");
            }
        }

        [Then(@"the location list should include ""(.*)""")]
        public void ThenTheLocationListShouldInclude(string title)
        {
            var locationListResult = _context.GetLastResult<PagedList<LocationListDto>>();
            var locationListResults = _context.GetLastResult<List<LocationListDto>>();

            if (locationListResult != null)
            {
                locationListResult.Data.Items.Should().Contain(l => l.Title == title,
                    $"Result should include location with title '{title}'");
            }
            else if (locationListResults != null)
            {
                locationListResults.Data.Should().Contain(l => l.Title == title,
                    $"Result should include location with title '{title}'");
            }
            else
            {
                Assert.Fail("No location list result was found to verify inclusion");
            }
        }

        [Then(@"the location list should not include ""(.*)""")]
        public void ThenTheLocationListShouldNotInclude(string title)
        {
            var locationListResult = _context.GetLastResult<PagedList<LocationListDto>>();
            var locationListResults = _context.GetLastResult<List<LocationListDto>>();

            if (locationListResult != null)
            {
                locationListResult.Data.Items.Should().NotContain(l => l.Title == title,
                    $"Result should not include location with title '{title}'");
            }
            else if (locationListResults != null)
            {
                locationListResults.Data.Should().NotContain(l => l.Title == title,
                    $"Result should not include location with title '{title}'");
            }
            else
            {
                Assert.Fail("No location list result was found to verify exclusion");
            }
        }

        [Then(@"the result should contain a location with title ""(.*)""")]
        public void ThenTheResultShouldContainALocationWithTitle(string title)
        {
            var locationResult = _context.GetLastResult<LocationDto>();
            locationResult.Should().NotBeNull("Result should be available for verification");
            locationResult.Data.Should().NotBeNull("Location data should be available");
            locationResult.Data.Title.Should().Be(title, $"Location title should be '{title}'");
        }

        [Then(@"the location details should be:")]
        public void ThenTheLocationDetailsShouldBe(Table table)
        {
            var expectedDetails = table.CreateInstance<LocationTestModel>();
            var locationResult = _context.GetLastResult<LocationDto>();

            locationResult.Should().NotBeNull("Result should be available for verification");
            locationResult.Data.Should().NotBeNull("Location data should be available");

            // Verify the details specified in the table
            if (!string.IsNullOrEmpty(expectedDetails.Title))
                locationResult.Data.Title.Should().Be(expectedDetails.Title, "Title should match expected value");

            if (!string.IsNullOrEmpty(expectedDetails.Description))
                locationResult.Data.Description.Should().Be(expectedDetails.Description, "Description should match expected value");

            if (!string.IsNullOrEmpty(expectedDetails.City))
                locationResult.Data.City.Should().Be(expectedDetails.City, "City should match expected value");

            if (!string.IsNullOrEmpty(expectedDetails.State))
                locationResult.Data.State.Should().Be(expectedDetails.State, "State should match expected value");
        }

        [Then(@"the error message should contain ""(.*)""")]
        public void ThenTheErrorMessageShouldContain(string errorText)
        {
            // Get the last result, which could be of different types
            var locationListResult = _context.GetLastResult<PagedList<LocationListDto>>();
            var locationResult = _context.GetLastResult<LocationDto>();
            var locationListResults = _context.GetLastResult<List<LocationListDto>>();

            // Check any of the results that are not null
            if (locationListResult != null)
            {
                locationListResult.ErrorMessage.Should().Contain(errorText,
                    $"Error message should contain '{errorText}'");
            }
            else if (locationResult != null)
            {
                locationResult.ErrorMessage.Should().Contain(errorText,
                    $"Error message should contain '{errorText}'");
            }
            else if (locationListResults != null)
            {
                locationListResults.ErrorMessage.Should().Contain(errorText,
                    $"Error message should contain '{errorText}'");
            }
            else
            {
                Assert.Fail("No result was found to verify the error message");
            }
        }
    }
}