using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
namespace Location.Photography.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyInfrastructure(this IServiceCollection services)
        {
            // Register database initializer
            //services.AddDatabaseInitializer();

            services.AddScoped<ISunCalculatorService, SunCalculatorService>();
            services.AddScoped<ISunService, SunService>();

            // Register exposure calculation services
            services.AddScoped<IExposureTriangleService, ExposureTriangleService>();
            services.AddScoped<IExposureCalculatorService, ExposureCalculatorService>();

            // Register other photography services
            services.AddScoped<ISceneEvaluationService, SceneEvaluationService>();
            return services;
        }
    }
}