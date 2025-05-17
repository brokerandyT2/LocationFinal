using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Queries.ExposureCalculator;
using Location.Photography.Application.Services;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Queries.ExposureCalculator
{
    [TestFixture]
    public class GetExposureValuesQueryHandlerTests
    {
        private GetExposureValuesQueryHandler _handler;
        private Mock<IExposureCalculatorService> _exposureCalculatorServiceMock;

        [SetUp]
        public void SetUp()
        {
            _exposureCalculatorServiceMock = new Mock<IExposureCalculatorService>();
            _handler = new GetExposureValuesQueryHandler(_exposureCalculatorServiceMock.Object);
        }

        [Test]
        public void Constructor_WithNullExposureCalculatorService_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new GetExposureValuesQueryHandler(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("exposureCalculatorService");
        }

        [Test]
        public async Task Handle_WithFullIncrements_ShouldReturnCorrectExposureValues()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Full
            };

            var shutterSpeeds = new[] { "30\"", "15\"", "8\"", "4\"", "2\"", "1\"", "1/2", "1/4", "1/8", "1/15", "1/30", "1/60", "1/125", "1/250", "1/500", "1/1000", "1/2000", "1/4000", "1/8000" };
            var apertures = new[] { "f/1", "f/1.4", "f/2", "f/2.8", "f/4", "f/5.6", "f/8", "f/11", "f/16", "f/22", "f/32", "f/45", "f/64" };
            var isos = new[] { "25600", "12800", "6400", "3200", "1600", "800", "400", "200", "100", "50" };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(shutterSpeeds));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(apertures));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(isos));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ShutterSpeeds.Should().BeEquivalentTo(shutterSpeeds);
            result.Data.Apertures.Should().BeEquivalentTo(apertures);
            result.Data.ISOs.Should().BeEquivalentTo(isos);

            _exposureCalculatorServiceMock.Verify(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetAperturesAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetIsosAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithHalfIncrements_ShouldReturnCorrectExposureValues()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Half
            };

            var shutterSpeeds = new[] { "30\"", "20\"", "15\"", "10\"", "8\"", "6\"", "4\"", "3\"", "2\"", "1.5\"", "1\"", "0.7", "0.5", "0.3", "1/4", "1/6", "1/8", "1/10", "1/15", "1/20", "1/30", "1/45", "1/60", "1/90", "1/125", "1/180", "1/250", "1/350", "1/500", "1/750", "1/1000", "1/1500", "1/2000", "1/3000", "1/4000", "1/6000", "1/8000" };
            var apertures = new[] { "f/1", "f/1.2", "f/1.4", "f/2", "f/2.4", "f/2.8", "f/3.4", "f/4", "f/4.8", "f/5.6", "f/6.7", "f/8", "f/9.5", "f/11", "f/13.5", "f/16", "f/19", "f/22", "f/26.9", "f/32", "f/38.1", "f/45", "f/53.8", "f/64" };
            var isos = new[] { "25600", "17600", "12800", "8800", "6400", "4400", "3600", "3200", "2200", "1600", "1100", "800", "560", "400", "280", "200", "140", "100", "70", "50" };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Half, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(shutterSpeeds));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Half, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(apertures));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Half, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(isos));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ShutterSpeeds.Should().BeEquivalentTo(shutterSpeeds);
            result.Data.Apertures.Should().BeEquivalentTo(apertures);
            result.Data.ISOs.Should().BeEquivalentTo(isos);

            _exposureCalculatorServiceMock.Verify(x => x.GetShutterSpeedsAsync(ExposureIncrements.Half, It.IsAny<CancellationToken>()), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetAperturesAsync(ExposureIncrements.Half, It.IsAny<CancellationToken>()), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetIsosAsync(ExposureIncrements.Half, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithThirdIncrements_ShouldReturnCorrectExposureValues()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Third
            };

            var shutterSpeeds = new[] { "30\"", "25\"", "20\"", "15\"", "13\"", "10\"", "8\"", "6\"", "5\"", "4\"", "3.2\"", "2.5\"", "2\"", "1.6\"", "1.3\"", "1\"", "0.8", "0.6", "0.5", "0.4", "0.3", "1/4", "1/5", "1/6", "1/8", "1/10", "1/13", "1/15", "1/20", "1/25", "1/30", "1/40", "1/50", "1/60", "1/80", "1/100", "1/125", "1/160", "1/200", "1/250", "1/320", "1/400", "1/500", "1/640", "1/800", "1/1000", "1/1250", "1/1600", "1/2000", "1/2500", "1/3200", "1/4000", "1/5000", "1/6400", "1/8000" };
            var apertures = new[] { "f/1", "f/1.1", "f/1.3", "f/1.4", "f/1.6", "f/1.8", "f/2", "f/2.2", "f/2.5", "f/2.8", "f/3.2", "f/3.6", "f/4", "f/4.5", "f/5", "f/5.6", "f/6.3", "f/7.1", "f/8", "f/9", "f/10.1", "f/11", "f/12.7", "f/14.3", "f/16", "f/18", "f/20.2", "f/22", "f/25.4", "f/28.5", "f/32", "f/36", "f/40.3", "f/45", "f/50.8", "f/57", "f/64" };
            var isos = new[] { "25600", "20000", "16000", "12800", "10000", "8000", "6400", "5000", "4000", "3200", "2500", "2000", "1600", "1250", "1000", "800", "640", "500", "400", "320", "250", "200", "160", "125", "100", "70", "50" };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Third, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(shutterSpeeds));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Third, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(apertures));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Third, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(isos));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.ShutterSpeeds.Should().BeEquivalentTo(shutterSpeeds);
            result.Data.Apertures.Should().BeEquivalentTo(apertures);
            result.Data.ISOs.Should().BeEquivalentTo(isos);

            _exposureCalculatorServiceMock.Verify(x => x.GetShutterSpeedsAsync(ExposureIncrements.Third, It.IsAny<CancellationToken>()), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetAperturesAsync(ExposureIncrements.Third, It.IsAny<CancellationToken>()), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetIsosAsync(ExposureIncrements.Third, It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WhenShutterSpeedsServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Full
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Failure("Shutter speeds error"));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "f/8", "f/11" }));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "100", "200" }));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Shutter speeds error");
        }

        [Test]
        public async Task Handle_WhenAperturesServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Full
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "1/125", "1/250" }));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Failure("Apertures error"));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "100", "200" }));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("Apertures error");
        }

        [Test]
        public async Task Handle_WhenIsosServiceFails_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Full
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "1/125", "1/250" }));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "f/8", "f/11" }));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Failure("ISOs error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Be("ISOs error");
        }

        [Test]
        public async Task Handle_WhenServiceThrowsException_ShouldReturnFailure()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Full
            };

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, It.IsAny<CancellationToken>()))
                .ThrowsAsync(new Exception("Unexpected error"));

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.ErrorMessage.Should().Contain("Error retrieving exposure values");
            result.ErrorMessage.Should().Contain("Unexpected error");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassItToServices()
        {
            // Arrange
            var query = new GetExposureValuesQuery
            {
                Increments = ExposureIncrements.Full
            };

            var cancellationToken = new CancellationToken();

            _exposureCalculatorServiceMock
                .Setup(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, cancellationToken))
                .ReturnsAsync(Result<string[]>.Success(new[] { "1/125", "1/250" }));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetAperturesAsync(ExposureIncrements.Full, cancellationToken))
                .ReturnsAsync(Result<string[]>.Success(new[] { "f/8", "f/11" }));

            _exposureCalculatorServiceMock
                .Setup(x => x.GetIsosAsync(ExposureIncrements.Full, cancellationToken))
                .ReturnsAsync(Result<string[]>.Success(new[] { "100", "200" }));

            // Act
            await _handler.Handle(query, cancellationToken);

            // Assert
            _exposureCalculatorServiceMock.Verify(x => x.GetShutterSpeedsAsync(ExposureIncrements.Full, cancellationToken), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetAperturesAsync(ExposureIncrements.Full, cancellationToken), Times.Once);
            _exposureCalculatorServiceMock.Verify(x => x.GetIsosAsync(ExposureIncrements.Full, cancellationToken), Times.Once);
        }
    }
}