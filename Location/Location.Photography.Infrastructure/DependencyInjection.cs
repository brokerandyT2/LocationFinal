
using Location.Photography.Infrastructure.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace Location.Photography.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyInfrastructure(this IServiceCollection services)
        {
            // Register database initializer
            services.AddDatabaseInitializer();

            // Add other photography-specific infrastructure services here

            return services;
        }
    }
}