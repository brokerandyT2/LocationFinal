using Location.Photography.Application.Services;
using Location.Photography.Domain.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Location.Photography.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyApplication(this IServiceCollection services)
        {
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));

            // Register application services
            services.AddScoped<ISunService, SunService>();

            return services;
        }
    }
}