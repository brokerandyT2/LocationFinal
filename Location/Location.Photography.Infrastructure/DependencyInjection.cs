// Location.Photography.Infrastructure/DependencyInjection.cs
using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Extensions;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Location.Photography.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyInfrastructure(this IServiceCollection services)
        {
            // Register database initializer
            services.AddDatabaseInitializer();

            // Register photography services
            services.AddScoped<ISunCalculatorService, SunCalculatorService>();
            services.AddScoped<IExposureCalculatorService, ExposureCalculatorService>();

            return services;
        }
    }
}