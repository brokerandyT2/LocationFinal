using Location.Core.Application;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Domain.Entities;
using Location.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace Location.Core.BDD.Tests.Support
{
    /// <summary>
    /// Provides service configuration for BDD tests
    /// </summary>
    public class TestServiceProvider
    {
        private readonly ServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new service provider configured for testing
        /// </summary>
        public TestServiceProvider(bool useMocks = true)
        {
            var services = new ServiceCollection();

            // Add required services
            ConfigureServices(services, useMocks);

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
        private void ConfigureServices(IServiceCollection services, bool useMocks)
        {
            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Debug);
            });

            if (useMocks)
            {
                // Register mocked repositories and services
                RegisterMockedServices(services);
            }
            else
            {
                // Register actual implementations (in-memory)
                services.AddApplication();
                services.AddInfrastructure();

                // Override certain services with test-specific implementations if needed
            }
        }

        /// <summary>
        /// Registers mocked services for testing
        /// </summary>
        private void RegisterMockedServices(IServiceCollection services)
        {
            // Mock UnitOfWork
            var mockUnitOfWork = new Mock<IUnitOfWork>();

            // Mock repositories
            var mockLocationRepo = new Mock<ILocationRepository>();
            var mockWeatherRepo = new Mock<IWeatherRepository>();
            var mockTipRepo = new Mock<ITipRepository>();
            var mockTipTypeRepo = new Mock<ITipTypeRepository>();
            var mockSettingRepo = new Mock<ISettingRepository>();

            // Configure UnitOfWork to return mocked repositories
            mockUnitOfWork.Setup(uow => uow.Locations).Returns(mockLocationRepo.Object);
            mockUnitOfWork.Setup(uow => uow.Weather).Returns(mockWeatherRepo.Object);
            mockUnitOfWork.Setup(uow => uow.Tips).Returns(mockTipRepo.Object);
            mockUnitOfWork.Setup(uow => uow.TipTypes).Returns(mockTipTypeRepo.Object);
            mockUnitOfWork.Setup(uow => uow.Settings).Returns(mockSettingRepo.Object);

            // Register mocked UnitOfWork
            services.AddSingleton(mockUnitOfWork.Object);

            // Register mocked repositories
            services.AddSingleton(mockLocationRepo.Object);
            services.AddSingleton(mockWeatherRepo.Object);
            services.AddSingleton(mockTipRepo.Object);
            services.AddSingleton(mockTipTypeRepo.Object);
            services.AddSingleton(mockSettingRepo.Object);

            // Register individual mocks for direct access in tests
            services.AddSingleton(mockLocationRepo);
            services.AddSingleton(mockWeatherRepo);
            services.AddSingleton(mockTipRepo);
            services.AddSingleton(mockTipTypeRepo);
            services.AddSingleton(mockSettingRepo);

            // Register other required services with mocks
            var mockMediaService = new Mock<Location.Core.Application.Services.IMediaService>();
            var mockWeatherService = new Mock<Location.Core.Application.Services.IWeatherService>();
            var mockGeolocationService = new Mock<Location.Core.Application.Services.IGeolocationService>();
            var mockAlertService = new Mock<Location.Core.Application.Services.IAlertService>();

            services.AddSingleton(mockMediaService.Object);
            services.AddSingleton(mockWeatherService.Object);
            services.AddSingleton(mockGeolocationService.Object);
            services.AddSingleton(mockAlertService.Object);

            // Register MediatR and AutoMapper from the application layer
            services.AddApplication();
        }
    }
}