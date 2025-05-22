using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Photography.Application;
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Microsoft.Extensions.Logging;
using Moq;

namespace Location.Photography.BDD.Tests.Support
{
    /// <summary>
    /// Provides service configuration for Photography BDD tests
    /// </summary>
    public class TestServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new service provider configured for photography testing
        /// </summary>
        public TestServiceProvider()
        {
            var services = new ServiceCollection();

            // Add required services
            ConfigureServices(services);

            // Build the service provider
            _serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Gets a service of the specified type
        /// </summary>
        public T GetService<T>() where T : class
        {
            return _serviceProvider.GetRequiredService<T>();
        }

        /// <summary>
        /// Configures the services for testing
        /// </summary>
        private void ConfigureServices(IServiceCollection services)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            // Register mocked services
            RegisterMockedServices(services);

            // Register application layer (for AutoMapper, MediatR, etc.)
            services.AddPhotographyApplication();
        }

        /// <summary>
        /// Registers mocked services for testing
        /// </summary>
        private void RegisterMockedServices(IServiceCollection services)
        {
            // Create mocks for Photography services
            var mockSunCalculatorService = new Mock<ISunCalculatorService>();
            var mockSunService = new Mock<ISunService>();
            var mockExposureCalculatorService = new Mock<IExposureCalculatorService>();
            var mockSceneEvaluationService = new Mock<ISceneEvaluationService>();

            // Create mocks for Core services
            var mockMediaService = new Mock<IMediaService>();
            var mockAlertService = new Mock<IAlertService>();
            var mockEventBus = new Mock<IEventBus>();

            // Configure default sun calculator behavior
            mockSunCalculatorService
                .Setup(s => s.GetSunrise(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((DateTime date, double lat, double lon) => date.Date.AddHours(6));

            mockSunCalculatorService
                .Setup(s => s.GetSunset(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((DateTime date, double lat, double lon) => date.Date.AddHours(18));

            mockSunCalculatorService
                .Setup(s => s.GetSolarNoon(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns((DateTime date, double lat, double lon) => date.Date.AddHours(12));

            mockSunCalculatorService
                .Setup(s => s.GetSolarAzimuth(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(180.0);

            mockSunCalculatorService
                .Setup(s => s.GetSolarElevation(It.IsAny<DateTime>(), It.IsAny<double>(), It.IsAny<double>()))
                .Returns(45.0);

            // Configure default exposure calculator behavior
            mockExposureCalculatorService
                .Setup(s => s.GetShutterSpeedsAsync(It.IsAny<ExposureIncrements>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "1/60", "1/125", "1/250" }));

            mockExposureCalculatorService
                .Setup(s => s.GetAperturesAsync(It.IsAny<ExposureIncrements>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "f/2.8", "f/4", "f/5.6" }));

            mockExposureCalculatorService
                .Setup(s => s.GetIsosAsync(It.IsAny<ExposureIncrements>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string[]>.Success(new[] { "100", "200", "400" }));

            // Configure default scene evaluation behavior
            mockSceneEvaluationService
                .Setup(s => s.EvaluateSceneAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<SceneEvaluationResultDto>.Success(new SceneEvaluationResultDto
                {
                    RedHistogramPath = "/temp/red.png",
                    GreenHistogramPath = "/temp/green.png",
                    BlueHistogramPath = "/temp/blue.png",
                    ContrastHistogramPath = "/temp/contrast.png",
                    ImagePath = "/temp/image.jpg",
                    Stats = new SceneEvaluationStatsDto
                    {
                        MeanRed = 128.0,
                        MeanGreen = 128.0,
                        MeanBlue = 128.0,
                        MeanContrast = 128.0,
                        StdDevRed = 64.0,
                        StdDevGreen = 64.0,
                        StdDevBlue = 64.0,
                        StdDevContrast = 64.0,
                        TotalPixels = 1000000
                    }
                }));

            // Configure media service defaults
            mockMediaService
                .Setup(s => s.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success("/temp/captured.jpg"));

            mockMediaService
                .Setup(s => s.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success("/temp/picked.jpg"));

            // Register mocks first (for test classes that need the mocks)
            services.AddSingleton(mockSunCalculatorService);
            services.AddSingleton(mockSunService);
            services.AddSingleton(mockExposureCalculatorService);
            services.AddSingleton(mockSceneEvaluationService);
            services.AddSingleton(mockMediaService);
            services.AddSingleton(mockAlertService);
            services.AddSingleton(mockEventBus);

            // Register interfaces with mocked implementations
            services.AddSingleton<ISunCalculatorService>(mockSunCalculatorService.Object);
            services.AddSingleton<ISunService>(mockSunService.Object);
            services.AddSingleton<IExposureCalculatorService>(mockExposureCalculatorService.Object);
            services.AddSingleton<ISceneEvaluationService>(mockSceneEvaluationService.Object);
            services.AddSingleton<IMediaService>(mockMediaService.Object);
            services.AddSingleton<IAlertService>(mockAlertService.Object);
            services.AddSingleton<IEventBus>(mockEventBus.Object);
        }
    }
}