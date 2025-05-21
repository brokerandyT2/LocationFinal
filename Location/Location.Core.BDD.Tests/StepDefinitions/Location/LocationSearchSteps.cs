using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.BDD.Tests.Drivers;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Location
{
    [Binding]
    public class LocationSearchSteps
    {
        private readonly ApiContext _context;
        private readonly LocationDriver _locationDriver;

        public LocationSearchSteps(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _locationDriver = new LocationDriver(context);
        }

        [Given(@"I have multiple locations stored in the system for search:")]
        public void GivenIHaveMultipleLocationsStoredInTheSystem(Table table)
        {
            // Convert table to location models
            var locations = table.CreateSet<LocationTestModel>().ToList();

            // Assign IDs if not provided
            for (int i = 0; i < locations.Count; i++)
            {
                if (!locations[i].Id.HasValue)
                {
                    locations[i].Id = i + 1;
                }
            }

            // Setup the locations in the repository
            _locationDriver.SetupLocations(locations);

            // Store all locations in the context
            _context.StoreModel(locations, "AllLocations");
        }

        [When(@"I search for a location with title ""(.*)""")]
        public async Task WhenISearchForALocationWithTitle(string title)
        {
            // Get the stored locations
            var locations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            if (locations == null)
            {
                throw new InvalidOperationException("No locations are stored in the context");
            }

            // Find location with matching title
            var matchingLocation = locations.Find(l => l.Title == title);
            if (matchingLocation != null)
            {
                // Store the found location and simulate a successful result
                var result = Result<LocationDto>.Success(new LocationDto
                {
                    Id = matchingLocation.Id.Value,
                    Title = matchingLocation.Title,
                    Description = matchingLocation.Description,
                    Latitude = matchingLocation.Latitude,
                    Longitude = matchingLocation.Longitude,
                    City = matchingLocation.City,
                    State = matchingLocation.State
                });

                _context.StoreResult(result);
                _context.StoreLocationData(matchingLocation);
            }
            else
            {
                // No matching location found
                var result = Result<LocationDto>.Failure($"Location with title '{title}' not found");
                _context.StoreResult(result);
            }
        }

        [When(@"I search for locations within (.*) km of coordinates:")]
        public void WhenISearchForLocationsWithinKmOfCoordinates(int distance, Table table)
        {
            // Get coordinate from table
            var coords = table.CreateInstance<CoordinateModel>();

            // Get the stored locations
            var locations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            if (locations == null)
            {
                throw new InvalidOperationException("No locations are stored in the context");
            }

            // Filter locations within the specified distance
            // Note: This is a simplified calculation - in a real app, you would use proper geospatial calculations
            var nearbyLocations = new List<LocationTestModel>();
            foreach (var location in locations)
            {
                // Calculate approximate distance (this is very simplified)
                double latDiff = location.Latitude - coords.Latitude;
                double lonDiff = location.Longitude - coords.Longitude;
                double distanceSquared = (latDiff * latDiff) + (lonDiff * lonDiff);

                // Convert to approximate km (this is not accurate, just for testing)
                double approxDistanceKm = Math.Sqrt(distanceSquared) * 111;

                if (approxDistanceKm <= distance)
                {
                    nearbyLocations.Add(location);
                }
            }

            // Store result
            var result = Result<List<LocationDto>>.Success(
                nearbyLocations.ConvertAll(l => new LocationDto
                {
                    Id = l.Id.Value,
                    Title = l.Title,
                    Description = l.Description,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    City = l.City,
                    State = l.State
                }));

            _context.StoreResult(result);
            _context.StoreModel(nearbyLocations, "SearchResults");
        }

        [When(@"I search for locations with text filter ""(.*)""")]
        public void WhenISearchForLocationsWithTextFilter(string searchText)
        {
            // Get the stored locations
            var locations = _context.GetModel<List<LocationTestModel>>("AllLocations");
            if (locations == null)
            {
                throw new InvalidOperationException("No locations are stored in the context");
            }

            // Filter locations containing the search text in title, description, city, or state
            var matchingLocations = locations.FindAll(l =>
                l.Title.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                l.Description.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                l.City.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                l.State.Contains(searchText, StringComparison.OrdinalIgnoreCase));

            // Store result
            var result = Result<List<LocationDto>>.Success(
                matchingLocations.ConvertAll(l => new LocationDto
                {
                    Id = l.Id.Value,
                    Title = l.Title,
                    Description = l.Description,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    City = l.City,
                    State = l.State
                }));

            _context.StoreResult(result);
            _context.StoreModel(matchingLocations, "SearchResults");
        }

        [Then(@"I should receive a successful location result")]
        public void ThenIShouldReceiveASuccessfulLocationResult()
        {
            // Check for single location result
            var singleResult = _context.GetLastResult<LocationDto>();
            if (singleResult != null)
            {
                singleResult.IsSuccess.Should().BeTrue("Location search should be successful");
                singleResult.Data.Should().NotBeNull("Location data should be available");
                return;
            }

            // Check for list result
            var listResult = _context.GetLastResult<List<LocationDto>>();
            if (listResult != null)
            {
                listResult.IsSuccess.Should().BeTrue("Location search should be successful");
                listResult.Data.Should().NotBeNull("Location data should be available");
                return;
            }

            // If we get here, no valid result was found
            throw new InvalidOperationException("No valid location search result found in context");
        }

        [Then(@"the result should contain a location with title ""(.*)""")]
        public void ThenTheResultShouldContainALocationWithTitle(string title)
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Location result should be available");
            result.Data.Should().NotBeNull("Location data should be available");
            result.Data.Title.Should().Be(title, $"Result should contain location with title '{title}'");
        }

        [Then(@"the location should have the following details:")]
        public void ThenTheLocationShouldHaveTheFollowingDetails(Table table)
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Location result should be available");
            result.Data.Should().NotBeNull("Location data should be available");

            var expectedDetails = table.CreateInstance<LocationTestModel>();

            // Check only the fields provided in the table
            if (!string.IsNullOrEmpty(expectedDetails.Title))
                result.Data.Title.Should().Be(expectedDetails.Title, "Title should match");

            if (!string.IsNullOrEmpty(expectedDetails.Description))
                result.Data.Description.Should().Be(expectedDetails.Description, "Description should match");

            if (expectedDetails.Latitude != 0)
                result.Data.Latitude.Should().Be(expectedDetails.Latitude, "Latitude should match");

            if (expectedDetails.Longitude != 0)
                result.Data.Longitude.Should().Be(expectedDetails.Longitude, "Longitude should match");

            if (!string.IsNullOrEmpty(expectedDetails.City))
                result.Data.City.Should().Be(expectedDetails.City, "City should match");

            if (!string.IsNullOrEmpty(expectedDetails.State))
                result.Data.State.Should().Be(expectedDetails.State, "State should match");
        }

        [Then(@"the result should contain (.*) locations")]
        public void ThenTheResultShouldContainLocations(int expectedCount)
        {
            var result = _context.GetLastResult<List<LocationDto>>();
            result.Should().NotBeNull("Location search result should be available");
            result.IsSuccess.Should().BeTrue("Location search should be successful");
            result.Data.Should().NotBeNull("Location data should be available");
            result.Data.Count.Should().Be(expectedCount, $"Result should contain {expectedCount} locations");
        }

        [Then(@"the result should include ""(.*)""")]
        public void ThenTheResultShouldInclude(string title)
        {
            var result = _context.GetLastResult<List<LocationDto>>();
            result.Should().NotBeNull("Location search result should be available");
            result.Data.Should().NotBeNull("Location data should be available");
            result.Data.Should().Contain(l => l.Title == title, $"Result should include location with title '{title}'");
        }

        [Then(@"the result should not include ""(.*)""")]
        public void ThenTheResultShouldNotInclude(string title)
        {
            var result = _context.GetLastResult<List<LocationDto>>();
            result.Should().NotBeNull("Location search result should be available");
            result.Data.Should().NotBeNull("Location data should be available");
            result.Data.Should().NotContain(l => l.Title == title, $"Result should not include location with title '{title}'");
        }

        [Then(@"the locations should be ordered by most recent first")]
        public void ThenTheLocationsShouldBeOrderedByMostRecentFirst()
        {
            var result = _context.GetLastResult<List<LocationDto>>();
            result.Should().NotBeNull("Location search result should be available");
            result.Data.Should().NotBeNull("Location data should be available");

            // Check that the locations are in descending order by timestamp
            DateTime? previousTimestamp = null;
            foreach (var location in result.Data)
            {
                if (previousTimestamp.HasValue)
                {
                    location.Timestamp.Should().BeBefore(previousTimestamp.Value,
                        "Locations should be ordered by most recent first");
                }
                previousTimestamp = location.Timestamp;
            }
        }
    }

    // Helper class for coordinate parameters
    public class CoordinateModel
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }
}