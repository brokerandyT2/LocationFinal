using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.DTOs;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Weather
{
    [Binding]
    public class WeatherUpdateSteps
    {
        private readonly ApiContext _apiContext;
        private readonly IMediator _mediator;

        public WeatherUpdateSteps(ApiContext apiContext, IMediator mediator)
        {
            _apiContext = apiContext;
            _mediator = mediator;
        }

        // In WeatherUpdateSteps.cs
        [Given(@"I have multiple locations stored in the system for weather:")]
        public void GivenIHaveMultipleLocationsStoredInTheSystemForWeather(Table table)
        {
            var locations = table.CreateSet<LocationTestModel>().ToList();

            // Setup mock repository once at the beginning
            var locationRepoMock = _apiContext.GetService<Mock<Application.Common.Interfaces.ILocationRepository>>();

            foreach (var location in locations)
            {
                // Ensure the location has an ID
                if (!location.Id.HasValue)
                {
                    location.Id = locations.IndexOf(location) + 1;
                }

                var domainEntity = location.ToDomainEntity();
                _apiContext.StoreModel(location, $"Location_{location.Title}");

                // Setup GetByTitleAsync using the already defined mock
                locationRepoMock.Setup(repo => repo.GetByTitleAsync(
                    It.Is<string>(title => title == location.Title),
                    It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));
            }

            // Setup mock for GetAllAsync using the already defined mock
            locationRepoMock.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(
                    locations.Select(l => l.ToDomainEntity()).ToList()));

            _apiContext.StoreModel(locations, "LocationsList");
        }

        // Other step definitions...
    }
}