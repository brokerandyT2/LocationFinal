using System;
using System.Collections.Generic;
using System.Linq;
using Location.Core.Domain.Common;
using Location.Core.Domain.Events;
using Location.Core.Domain.ValueObjects;

namespace Location.Core.Domain.Entities
{
    /// <summary>
    /// Weather aggregate root containing weather data for a location
    /// </summary>
    public class Weather : AggregateRoot
    {
        private readonly List<WeatherForecast> _forecasts = new();
        private Coordinate _coordinate = null!;
        private DateTime _lastUpdate;
        private string _timezone = string.Empty;
        private int _timezoneOffset;

        public int LocationId { get; private set; }
        public Coordinate Coordinate
        {
            get => _coordinate;
            private set => _coordinate = value ?? throw new ArgumentNullException(nameof(value));
        }

        public DateTime LastUpdate
        {
            get => _lastUpdate;
            private set => _lastUpdate = value;
        }

        public string Timezone
        {
            get => _timezone;
            set => _timezone = value ?? string.Empty;
        }

        public int TimezoneOffset
        {
            get => _timezoneOffset;
            private set => _timezoneOffset = value;
        }

        public IReadOnlyCollection<WeatherForecast> Forecasts => _forecasts.AsReadOnly();

        protected Weather() { } // For ORM

        public Weather(int locationId, Coordinate coordinate, string timezone, int timezoneOffset)
        {
            LocationId = locationId;
            Coordinate = coordinate;
            Timezone = timezone;
            TimezoneOffset = timezoneOffset;
            LastUpdate = DateTime.UtcNow;
        }

        public void UpdateForecasts(IEnumerable<WeatherForecast> forecasts)
        {
            if (forecasts == null)
                throw new ArgumentNullException(nameof(forecasts));

            _forecasts.Clear();
            _forecasts.AddRange(forecasts.Take(7)); // Limit to 7-day forecast
            LastUpdate = DateTime.UtcNow;

            AddDomainEvent(new WeatherUpdatedEvent(LocationId, LastUpdate));
        }

        public WeatherForecast? GetForecastForDate(DateTime date)
        {
            return _forecasts.FirstOrDefault(f => f.Date.Date == date.Date);
        }

        public WeatherForecast? GetCurrentForecast()
        {
            return GetForecastForDate(DateTime.Today);
        }
    }
}