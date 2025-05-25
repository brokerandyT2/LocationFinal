using Location.Core.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using SQLite;
using System;
using System.Net;
using System.Net.Http;

namespace Location.Core.Infrastructure.Services
{
    public interface IInfrastructureExceptionMappingService
    {
        LocationDomainException MapToLocationDomainException(Exception exception, string operation);
        WeatherDomainException MapToWeatherDomainException(Exception exception, string operation);
        SettingDomainException MapToSettingDomainException(Exception exception, string operation);
        TipDomainException MapToTipDomainException(Exception exception, string operation);
        TipTypeDomainException MapToTipTypeDomainException(Exception exception, string operation);
    }

    public class InfrastructureExceptionMappingService : IInfrastructureExceptionMappingService
    {
        private readonly ILogger<InfrastructureExceptionMappingService> _logger;

        public InfrastructureExceptionMappingService(ILogger<InfrastructureExceptionMappingService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public LocationDomainException MapToLocationDomainException(Exception exception, string operation)
        {
            _logger.LogError(exception, "Infrastructure exception in {Operation}", operation);

            return exception switch
            {
                SQLiteException sqlEx when sqlEx.Message.Contains("UNIQUE constraint failed") =>
                    new LocationDomainException("DUPLICATE_TITLE", "A location with this title already exists", sqlEx),

                SQLiteException sqlEx when sqlEx.Message.Contains("CHECK constraint failed") =>
                    new LocationDomainException("INVALID_COORDINATES", "Invalid coordinate values provided", sqlEx),

                SQLiteException sqlEx =>
                    new LocationDomainException("DATABASE_ERROR", $"Database operation failed: {sqlEx.Message}", sqlEx),

                HttpRequestException httpEx =>
                    new LocationDomainException("NETWORK_ERROR", $"Network operation failed: {httpEx.Message}", httpEx),

                TimeoutException timeoutEx =>
                    new LocationDomainException("NETWORK_ERROR", "Operation timed out", timeoutEx),

                UnauthorizedAccessException authEx =>
                    new LocationDomainException("AUTHORIZATION_ERROR", "Access denied", authEx),

                _ => new LocationDomainException("INFRASTRUCTURE_ERROR", $"Infrastructure error in {operation}: {exception.Message}", exception)
            };
        }

        public WeatherDomainException MapToWeatherDomainException(Exception exception, string operation)
        {
            _logger.LogError(exception, "Infrastructure exception in weather {Operation}", operation);

            return exception switch
            {
                HttpRequestException httpEx when httpEx.Message.Contains("401") =>
                    new WeatherDomainException("INVALID_API_KEY", "Weather API authentication failed", httpEx),

                HttpRequestException httpEx when httpEx.Message.Contains("429") =>
                    new WeatherDomainException("RATE_LIMIT_EXCEEDED", "Weather API rate limit exceeded", httpEx),

                HttpRequestException httpEx when httpEx.Message.Contains("404") =>
                    new WeatherDomainException("LOCATION_NOT_FOUND", "Weather data not available for location", httpEx),

                HttpRequestException httpEx =>
                    new WeatherDomainException("API_UNAVAILABLE", $"Weather API error: {httpEx.Message}", httpEx),

                TimeoutException timeoutEx =>
                    new WeatherDomainException("NETWORK_TIMEOUT", "Weather service request timed out", timeoutEx),

                SQLiteException sqlEx =>
                    new WeatherDomainException("DATABASE_ERROR", $"Database operation failed: {sqlEx.Message}", sqlEx),

                _ => new WeatherDomainException("INFRASTRUCTURE_ERROR", $"Infrastructure error in {operation}: {exception.Message}", exception)
            };
        }

        public SettingDomainException MapToSettingDomainException(Exception exception, string operation)
        {
            _logger.LogError(exception, "Infrastructure exception in settings {Operation}", operation);

            return exception switch
            {
                SQLiteException sqlEx when sqlEx.Message.Contains("UNIQUE constraint failed") =>
                    new SettingDomainException("DUPLICATE_KEY", "A setting with this key already exists", sqlEx),

                SQLiteException sqlEx =>
                    new SettingDomainException("DATABASE_ERROR", $"Database operation failed: {sqlEx.Message}", sqlEx),

                UnauthorizedAccessException authEx =>
                    new SettingDomainException("READ_ONLY_SETTING", "Setting is read-only", authEx),

                _ => new SettingDomainException("INFRASTRUCTURE_ERROR", $"Infrastructure error in {operation}: {exception.Message}", exception)
            };
        }

        public TipDomainException MapToTipDomainException(Exception exception, string operation)
        {
            _logger.LogError(exception, "Infrastructure exception in tips {Operation}", operation);

            return exception switch
            {
                SQLiteException sqlEx when sqlEx.Message.Contains("UNIQUE constraint failed") =>
                    new TipDomainException("DUPLICATE_TITLE", "A tip with this title already exists", sqlEx),

                SQLiteException sqlEx when sqlEx.Message.Contains("FOREIGN KEY constraint failed") =>
                    new TipDomainException("INVALID_TIP_TYPE", "Invalid tip type specified", sqlEx),

                SQLiteException sqlEx =>
                    new TipDomainException("DATABASE_ERROR", $"Database operation failed: {sqlEx.Message}", sqlEx),

                _ => new TipDomainException("INFRASTRUCTURE_ERROR", $"Infrastructure error in {operation}: {exception.Message}", exception)
            };
        }

        public TipTypeDomainException MapToTipTypeDomainException(Exception exception, string operation)
        {
            _logger.LogError(exception, "Infrastructure exception in tip types {Operation}", operation);

            return exception switch
            {
                SQLiteException sqlEx when sqlEx.Message.Contains("UNIQUE constraint failed") =>
                    new TipTypeDomainException("DUPLICATE_NAME", "A tip type with this name already exists", sqlEx),

                SQLiteException sqlEx when sqlEx.Message.Contains("FOREIGN KEY constraint failed") =>
                    new TipTypeDomainException("TIP_TYPE_IN_USE", "Cannot delete tip type that is in use", sqlEx),

                SQLiteException sqlEx =>
                    new TipTypeDomainException("DATABASE_ERROR", $"Database operation failed: {sqlEx.Message}", sqlEx),

                _ => new TipTypeDomainException("INFRASTRUCTURE_ERROR", $"Infrastructure error in {operation}: {exception.Message}", exception)
            };
        }
    }
}