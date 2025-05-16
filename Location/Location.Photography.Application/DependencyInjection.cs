// Location.Photography.Application/DependencyInjection.cs
using Location.Core.Application.Common.Behaviors;
using Location.Photography.Application.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Location.Photography.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddPhotographyApplication(this IServiceCollection services)
        {
            // Register MediatR and add validation behavior
            services.AddMediatR(cfg => {
                cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
                cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            });

            // Register application services
            services.AddScoped<ISunService, SunService>();

            return services;
        }
    }
}