using Location.Core.Infrastructure.Services;
using Microsoft.Extensions.Logging;

namespace Location.Core.Infrastructure.Data
{
    public static class RepositoryExceptionWrapper
    {
        public static async Task<T> ExecuteWithExceptionMappingAsync<T>(
            Func<Task<T>> operation,
            IInfrastructureExceptionMappingService exceptionMapper,
            string operationName,
            string entityType,
            ILogger logger)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Repository operation {Operation} failed for {EntityType}", operationName, entityType);

                Exception domainException = entityType.ToLowerInvariant() switch
                {
                    "location" => exceptionMapper.MapToLocationDomainException(ex, operationName),
                    "weather" => exceptionMapper.MapToWeatherDomainException(ex, operationName),
                    "setting" => exceptionMapper.MapToSettingDomainException(ex, operationName),
                    "tip" => exceptionMapper.MapToTipDomainException(ex, operationName),
                    "tiptype" => exceptionMapper.MapToTipTypeDomainException(ex, operationName),
                    _ => ex
                };

                throw domainException;
            }
        }

        public static async Task ExecuteWithExceptionMappingAsync(
            Func<Task> operation,
            IInfrastructureExceptionMappingService exceptionMapper,
            string operationName,
            string entityType,
            ILogger logger)
        {
            try
            {
                await operation();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Repository operation {Operation} failed for {EntityType}", operationName, entityType);

                Exception domainException = entityType.ToLowerInvariant() switch
                {
                    "location" => exceptionMapper.MapToLocationDomainException(ex, operationName),
                    "weather" => exceptionMapper.MapToWeatherDomainException(ex, operationName),
                    "setting" => exceptionMapper.MapToSettingDomainException(ex, operationName),
                    "tip" => exceptionMapper.MapToTipDomainException(ex, operationName),
                    "tiptype" => exceptionMapper.MapToTipTypeDomainException(ex, operationName),
                    _ => ex
                };

                throw domainException;
            }
        }

        public static T ExecuteWithExceptionMapping<T>(
            Func<T> operation,
            IInfrastructureExceptionMappingService exceptionMapper,
            string operationName,
            string entityType,
            ILogger logger)
        {
            try
            {
                return operation();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Repository operation {Operation} failed for {EntityType}", operationName, entityType);

                Exception domainException = entityType.ToLowerInvariant() switch
                {
                    "location" => exceptionMapper.MapToLocationDomainException(ex, operationName),
                    "weather" => exceptionMapper.MapToWeatherDomainException(ex, operationName),
                    "setting" => exceptionMapper.MapToSettingDomainException(ex, operationName),
                    "tip" => exceptionMapper.MapToTipDomainException(ex, operationName),
                    "tiptype" => exceptionMapper.MapToTipTypeDomainException(ex, operationName),
                    _ => ex
                };

                throw domainException;
            }
        }
    }
}