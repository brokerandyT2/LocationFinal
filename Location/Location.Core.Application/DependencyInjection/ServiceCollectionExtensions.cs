using Location.Core.Application.Common.Behaviors;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;
using FluentValidation;
using AutoMapper;

namespace Location.Core.Application
{
 
/// <summary>
/// Provides extension methods for configuring application services in an <see cref="IServiceCollection"/>.
/// </summary>
/// <remarks>This class contains methods to register application-specific dependencies, such as AutoMapper,
/// MediatR,  and custom pipeline behaviors, into the dependency injection container.</remarks>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Configures and registers application-level services, including AutoMapper, MediatR, and validation
        /// behaviors, into the specified <see cref="IServiceCollection"/>.
        /// </summary>
        /// <remarks>This method performs the following configurations: <list type="bullet">
        /// <item><description>Registers AutoMapper with mappings from the executing assembly.</description></item>
        /// <item><description>Registers MediatR with handlers and services from the executing
        /// assembly.</description></item> <item><description>Registers all validators from the executing assembly using
        /// FluentValidation.</description></item> <item><description>Adds pipeline behaviors for validation and logging
        /// to MediatR.</description></item> </list></remarks>
        /// <param name="services">The <see cref="IServiceCollection"/> to which the application services will be added.</param>
        /// <returns>The same <see cref="IServiceCollection"/> instance, allowing for method chaining.</returns>
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddMaps(assembly);
            });
            services.AddSingleton(mapperConfig.CreateMapper());

            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly);
            });

            services.AddValidatorsFromAssembly(assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));

            return services;
        }
    }
}