using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Application.Weather.Queries.UpdateAllWeather;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Weather.Queries.UpdateAllWeather
{
    [Category("Weather")]
    [Category("Update")]
    [TestFixture]
    public class UpdateAllWeatherQueryHandlerTests
    {
        private Mock<IWeatherService> _weatherServiceMock;
        private Mock<IMediator> _mediatorMock;
        private UpdateAllWeatherQueryHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _weatherServiceMock = new Mock<IWeatherService>();
            _mediatorMock = new Mock<IMediator>();
            _handler = new UpdateAllWeatherQueryHandler(_weatherServiceMock.Object, _mediatorMock.Object);
        }

        [Test]
        public void Constructor_WithNullWeatherService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new UpdateAllWeatherQueryHandler(null, _mediatorMock.Object))
                .Should().Throw<ArgumentNullException>();
        }

        [Test]
        public async Task Handle_WhenSuccessful_ShouldReturnNumberOfUpdatedLocations()
        {
            // Arrange
            var query = new UpdateAllWeatherQuery();
            var expectedCount = 5;

            _weatherServiceMock
                .Setup(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Success(expectedCount));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(expectedCount);

            _weatherServiceMock.Verify(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenNoLocationsUpdated_ShouldReturnZero()
        {
            // Arrange
            var query = new UpdateAllWeatherQuery();

            _weatherServiceMock
                .Setup(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Success(0));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().Be(0);
        }

        [Test]
        public async Task Handle_WhenWeatherServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new UpdateAllWeatherQuery();

            _weatherServiceMock
                .Setup(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<int>.Failure("Weather service API error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Weather service API error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var query = new UpdateAllWeatherQuery();
            var cancellationToken = new CancellationToken();

            _weatherServiceMock
                .Setup(x => x.UpdateAllWeatherAsync(cancellationToken))
                .ReturnsAsync(Result<int>.Success(3));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _weatherServiceMock.Verify(x => x.UpdateAllWeatherAsync(cancellationToken), Times.Once);
        }

        [Test]
        public async Task Handle_WhenExceptionThrown_ShouldReturnFailure()
        {
            // Arrange
            var query = new UpdateAllWeatherQuery();
            var exception = new Exception("Unexpected error");

            _weatherServiceMock
                .Setup(x => x.UpdateAllWeatherAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Failed to update all weather data");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }
    }
}