using Location.Core.Application;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Interfaces.Persistence;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Services;
using Location.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using System;

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

            // Register mocked repositories and services
            RegisterMockedServices(services);

            // Register application layer (for AutoMapper, MediatR, etc.)
            services.AddApplication();
        }

        /// <summary>
        /// Registers mocked services for testing
        /// </summary>
        private void RegisterMockedServices(IServiceCollection services)
        {
            // Create mocks
            var mockUnitOfWork = new Mock<IUnitOfWork>();
            var mockLocationRepo = new Mock<Application.Common.Interfaces.ILocationRepository>();
            var mockWeatherRepo = new Mock<Application.Common.Interfaces.IWeatherRepository>();
            var mockTipRepo = new Mock<Application.Common.Interfaces.ITipRepository>();
            var mockTipTypeRepo = new Mock<Application.Common.Interfaces.ITipTypeRepository>();
            var mockSettingRepo = new Mock<Application.Common.Interfaces.ISettingRepository>();

            // Repository mocks for persistence interfaces
            var mockLocationPersistenceRepo = new Mock<Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository>();
            var mockWeatherPersistenceRepo = new Mock<Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository>();
            var mockTipPersistenceRepo = new Mock<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>();
            var mockTipTypePersistenceRepo = new Mock<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>();
            var mockSettingPersistenceRepo = new Mock<Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository>();

            // Service mocks
            var mockMediaService = new Mock<IMediaService>();
            var mockWeatherService = new Mock<IWeatherService>();
            var mockGeolocationService = new Mock<IGeolocationService>();
            var mockAlertService = new Mock<IAlertService>();
            var mockEventBus = new Mock<IEventBus>();

            // Configure UnitOfWork to return mocked repositories
            mockUnitOfWork.Setup(uow => uow.Locations).Returns(mockLocationRepo.Object);
            mockUnitOfWork.Setup(uow => uow.Weather).Returns(mockWeatherRepo.Object);
            mockUnitOfWork.Setup(uow => uow.Tips).Returns(mockTipRepo.Object);
            mockUnitOfWork.Setup(uow => uow.TipTypes).Returns(mockTipTypeRepo.Object);
            mockUnitOfWork.Setup(uow => uow.Settings).Returns(mockSettingRepo.Object);

            // Register mocks first (for test classes that need the mocks)
            services.AddSingleton(mockLocationRepo);
            services.AddSingleton(mockWeatherRepo);
            services.AddSingleton(mockTipRepo);
            services.AddSingleton(mockTipTypeRepo);
            services.AddSingleton(mockSettingRepo);
            services.AddSingleton(mockLocationPersistenceRepo);
            services.AddSingleton(mockWeatherPersistenceRepo);
            services.AddSingleton(mockTipPersistenceRepo);
            services.AddSingleton(mockTipTypePersistenceRepo);
            services.AddSingleton(mockSettingPersistenceRepo);
            services.AddSingleton(mockUnitOfWork);
            services.AddSingleton(mockMediaService);
            services.AddSingleton(mockWeatherService);
            services.AddSingleton(mockGeolocationService);
            services.AddSingleton(mockAlertService);
            services.AddSingleton(mockEventBus);

            // Register interfaces with mocked implementations
            services.AddSingleton<IUnitOfWork>(mockUnitOfWork.Object);
            services.AddSingleton<Application.Common.Interfaces.ILocationRepository>(mockLocationRepo.Object);
            services.AddSingleton<Application.Common.Interfaces.IWeatherRepository>(mockWeatherRepo.Object);
            services.AddSingleton<Application.Common.Interfaces.ITipRepository>(mockTipRepo.Object);
            services.AddSingleton<Application.Common.Interfaces.ITipTypeRepository>(mockTipTypeRepo.Object);
            services.AddSingleton<Application.Common.Interfaces.ISettingRepository>(mockSettingRepo.Object);
            services.AddSingleton<IMediaService>(mockMediaService.Object);
            services.AddSingleton<IWeatherService>(mockWeatherService.Object);
            services.AddSingleton<IGeolocationService>(mockGeolocationService.Object);
            services.AddSingleton<IAlertService>(mockAlertService.Object);
            services.AddSingleton<IEventBus>(mockEventBus.Object);

            // Register persistence interfaces
            services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.ILocationRepository>(mockLocationPersistenceRepo.Object);
            services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.IWeatherRepository>(mockWeatherPersistenceRepo.Object);
            services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.ITipRepository>(mockTipPersistenceRepo.Object);
            services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.ITipTypeRepository>(mockTipTypePersistenceRepo.Object);
            services.AddSingleton<Location.Core.Application.Common.Interfaces.Persistence.ISettingRepository>(mockSettingPersistenceRepo.Object);

            // Setup default return values for common operations if needed
            // For example:
            mockLocationRepo.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Location>>.Success(new List<Domain.Entities.Location>()));

            mockTipRepo.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Tip>>.Success(new List<Domain.Entities.Tip>()));

            mockSettingRepo.Setup(repo => repo.GetAllAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<List<Domain.Entities.Setting>>.Success(new List<Domain.Entities.Setting>()));
        }
    }
}