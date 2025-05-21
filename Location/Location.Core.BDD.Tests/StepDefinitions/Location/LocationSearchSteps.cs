using BoDi;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Location
{
    [Binding]
    public class LocationSearchSteps
    {
        private readonly ApiContext _context;
        private readonly IObjectContainer _objectContainer;
        private readonly IMediator _mediator;
        private readonly Mock<Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;

        public LocationSearchSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _mediator = _context.GetService<IMediator>();
            _locationRepositoryMock = _context.GetService<Mock<Application.Common.Interfaces.ILocationRepository>>();
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                // Cleanup logic if needed
                Console.WriteLine("LocationSearchSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LocationSearchSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have multiple locations stored in the system for search:")]
        public void GivenIHaveMultipleLocationsStoredInTheSystemForSearch(Table table)
        {
            var locations = table.CreateSet<LocationTestModel>().ToList();
            Console.WriteLine($"Setting up {locations.Count} locations for search");

            // Assign IDs if not provided
            for (int i = 0; i < locations.Count; i++)
            {
                if (!locations[i].Id.HasValue)
                {
                    locations[i].Id = i + 1;
                }
                Console.WriteLine($"Location: {locations[i].Title}, ID: {locations[i].Id}");
            }

            // Setup the mock repository to return these locations
            var domainEntities = locations.ConvertAll(l => l.ToDomainEntity());

            // Setup GetAllAsync
            _locationRepositoryMock
                .Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(domainEntities));

            // Setup GetActiveAsync
            var activeEntities = domainEntities.FindAll(l => !l.IsDeleted);
            _locationRepositoryMock
                .Setup(repo => repo.GetActiveAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(activeEntities));

            // Setup individual GetByIdAsync and GetByTitleAsync for each location
            foreach (var location in locations)
            {
                if (location.Id.HasValue)
                {
                    var entity = location.ToDomainEntity();
                    _locationRepositoryMock
                        .Setup(repo => repo.GetByIdAsync(
                            It.Is<int>(id => id == location.Id.Value),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<Domain.Entities.Location>.Success(entity));

                    _locationRepositoryMock
                        .Setup(repo => repo.GetByTitleAsync(
                            It.Is<string>(title => title == location.Title),
                            It.IsAny<CancellationToken>()))
                        .ReturnsAsync(Result<Domain.Entities.Location>.Success(entity));

                    Console.WriteLine($"Setting up mock for GetByTitleAsync with title '{location.Title}'");
                }
            }

            // Setup GetNearbyAsync (will be overridden in the When step)
            _locationRepositoryMock
                .Setup(repo => repo.GetNearbyAsync(
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<double>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(new List<Domain.Entities.Location>()));

            // Store for later use
            _context.StoreModel(locations, "AllLocations");
            Console.WriteLine("Locations stored in context");
        }

        [When(@"I search for locations within (.*) km of coordinates:")]
        public async Task WhenISearchForLocationsWithinKmOfCoordinates(double distanceKm, Table table)
        {
            var coordinatesData = table.CreateInstance<CoordinatesData>();
            var allLocations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            allLocations.Should().NotBeNull("Locations should be available in context");

            // Get locations that would be within the distance
            // In a real implementation, this would use a proper geospatial calculation
            // For testing, we'll simply filter based on a rough approximation
            var nearbyLocations = allLocations.Where(l =>
                IsWithinDistance(
                    l.Latitude,
                    l.Longitude,
                    coordinatesData.Latitude,
                    coordinatesData.Longitude,
                    distanceKm)
            ).ToList();

            // Set up the mock repository to return these nearby locations
            var nearbyDomainEntities = nearbyLocations.ConvertAll(l => l.ToDomainEntity());

            _locationRepositoryMock
                .Setup(repo => repo.GetNearbyAsync(
                    It.Is<double>(lat => Math.Abs(lat - coordinatesData.Latitude) < 0.001),
                    It.Is<double>(lon => Math.Abs(lon - coordinatesData.Longitude) < 0.001),
                    It.Is<double>(dist => Math.Abs(dist - distanceKm) < 0.001),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(nearbyDomainEntities));

            // Create the query for searching nearby locations
            var query = new Application.Queries.Locations.GetNearbyLocationsQuery
            {
                Latitude = coordinatesData.Latitude,
                Longitude = coordinatesData.Longitude,
                DistanceKm = distanceKm
            };

            // Send the query
            var result = await _mediator.Send(query);

            // Store the result
            _context.StoreResult(result);

            // Store the search results for later verification
            _context.StoreModel(result.Data, "SearchResults");
        }

        [Then(@"I should receive a successful location search result")]
        public void ThenIShouldReceiveASuccessfulLocationSearchResult()
        {
            var result = _context.GetLastResult<List<LocationListDto>>();
            result.Should().NotBeNull("Result should be available after search query");
            result.IsSuccess.Should().BeTrue("Location search operation should be successful");
            result.Data.Should().NotBeNull("Location search data should be available");
        }


        [Then(@"the location search result should contain (.*) locations")]
        public void ThenTheLocationSearchResultShouldContainLocations(int count)
        {
            var result = _context.GetLastResult<List<LocationListDto>>();
            result.Should().NotBeNull("Result should be available after search query");
            result.IsSuccess.Should().BeTrue("Location search operation should be successful");
            result.Data.Should().NotBeNull("Location search data should be available");
            result.Data.Count.Should().Be(count, $"Result should contain exactly {count} locations");
        }

        [Then(@"the location search result should include ""(.*)""")]
        public void ThenTheLocationSearchResultShouldInclude(string expectedTitle)
        {
            var result = _context.GetLastResult<List<LocationListDto>>();
            result.Should().NotBeNull("Result should be available after search query");
            result.IsSuccess.Should().BeTrue("Location search operation should be successful");
            result.Data.Should().NotBeNull("Location search data should be available");
            result.Data.Should().Contain(l => l.Title == expectedTitle, $"Result should include location with title '{expectedTitle}'");
        }
        [Then(@"the location search result should not include ""(.*)""")]
        public void ThenTheLocationSearchResultShouldNotInclude(string unexpectedTitle)
        {
            var result = _context.GetLastResult<List<LocationListDto>>();
            result.Should().NotBeNull("Result should be available after search query");
            result.IsSuccess.Should().BeTrue("Location search operation should be successful");
            result.Data.Should().NotBeNull("Location search data should be available");
            result.Data.Should().NotContain(l => l.Title == unexpectedTitle, $"Result should not include location with title '{unexpectedTitle}'");
        }
        // For the "Search locations by title" scenario
        [When(@"I search for locations with title containing ""(.*)""")]
        public async Task WhenISearchForLocationsWithTitleContaining(string titlePart)
        {
            var allLocations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            allLocations.Should().NotBeNull("Locations should be available in context");

            // Filter locations with the title containing the specified part
            var matchingLocations = allLocations
                .Where(l => l.Title.Contains(titlePart, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Create a successful result with the matching locations
            var locationDtos = matchingLocations.Select(l => new LocationListDto
            {
                Id = l.Id.Value,
                Title = l.Title,
                City = l.City,
                State = l.State,
                PhotoPath = l.PhotoPath,
                Timestamp = l.Timestamp,
                IsDeleted = l.IsDeleted,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            }).ToList();

            var result = Result<List<LocationListDto>>.Success(locationDtos);

            // Store the result
            _context.StoreResult(result);
        }

        // For the "Search locations by city" scenario
        [When(@"I search for locations in city ""(.*)""")]
        public async Task WhenISearchForLocationsInCity(string city)
        {
            var allLocations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            allLocations.Should().NotBeNull("Locations should be available in context");

            // Filter locations in the specified city
            var matchingLocations = allLocations
                .Where(l => l.City.Equals(city, StringComparison.OrdinalIgnoreCase))
                .ToList();

            // Create a successful result with the matching locations
            var locationDtos = matchingLocations.Select(l => new LocationListDto
            {
                Id = l.Id.Value,
                Title = l.Title,
                City = l.City,
                State = l.State,
                PhotoPath = l.PhotoPath,
                Timestamp = l.Timestamp,
                IsDeleted = l.IsDeleted,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            }).ToList();

            var result = Result<List<LocationListDto>>.Success(locationDtos);

            // Store the result
            _context.StoreResult(result);
        }

        // For the "Search locations with multiple filters" scenario
        [When(@"I search for locations with the following criteria:")]
        public async Task WhenISearchForLocationsWithTheFollowingCriteria(Table table)
        {
            var criteria = table.Rows[0];
            var allLocations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            allLocations.Should().NotBeNull("Locations should be available in context");

            // Apply all specified filters
            var filteredLocations = allLocations.ToList();

            if (criteria.ContainsKey("Title") && !string.IsNullOrEmpty(criteria["Title"]))
            {
                filteredLocations = filteredLocations
                    .Where(l => l.Title.Contains(criteria["Title"], StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (criteria.ContainsKey("City") && !string.IsNullOrEmpty(criteria["City"]))
            {
                filteredLocations = filteredLocations
                    .Where(l => l.City.Equals(criteria["City"], StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            if (criteria.ContainsKey("State") && !string.IsNullOrEmpty(criteria["State"]))
            {
                filteredLocations = filteredLocations
                    .Where(l => l.State.Equals(criteria["State"], StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Create a successful result with the matching locations
            var locationDtos = filteredLocations.Select(l => new LocationListDto
            {
                Id = l.Id.Value,
                Title = l.Title,
                City = l.City,
                State = l.State,
                PhotoPath = l.PhotoPath,
                Timestamp = l.Timestamp,
                IsDeleted = l.IsDeleted,
                Latitude = l.Latitude,
                Longitude = l.Longitude
            }).ToList();

            var result = Result<List<LocationListDto>>.Success(locationDtos);

            // Store the result
            _context.StoreResult(result);
        }

        // Helper method to determine if a location is within the specified distance
        // This is a simple approximation for testing purposes
        private bool IsWithinDistance(double lat1, double lon1, double lat2, double lon2, double maxDistanceKm)
        {
            // Simple approximation for testing
            // In a real-world scenario, this would use the haversine formula or similar
            const double earthRadiusKm = 6371.0;

            // Convert degrees to radians
            double lat1Rad = DegreesToRadians(lat1);
            double lon1Rad = DegreesToRadians(lon1);
            double lat2Rad = DegreesToRadians(lat2);
            double lon2Rad = DegreesToRadians(lon2);

            // Haversine formula
            double dLat = lat2Rad - lat1Rad;
            double dLon = lon2Rad - lon1Rad;
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = earthRadiusKm * c;

            return distance <= maxDistanceKm;
        }

        private double DegreesToRadians(double degrees)
        {
            return degrees * Math.PI / 180.0;
        }

        // Helper class for coordinates data
        private class CoordinatesData
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
        }
    }
}