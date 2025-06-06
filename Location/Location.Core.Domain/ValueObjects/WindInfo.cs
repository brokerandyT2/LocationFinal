namespace Location.Core.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing wind information
    /// </summary>
    public class WindInfo : ValueObject
    {
        public double Speed { get; private set; }
        public double Direction { get; private set; }
        public double? Gust { get; private set; }

        public WindInfo(double speed, double direction, double? gust = null)
        {
            if (speed < 0)
                throw new ArgumentOutOfRangeException(nameof(speed), "Wind speed cannot be negative");

            if (direction < 0 || direction > 360)
                throw new ArgumentOutOfRangeException(nameof(direction), "Wind direction must be between 0 and 360 degrees");

            Speed = Math.Round(speed, 2);
            Direction = Math.Round(direction, 0);
            Gust = gust.HasValue ? Math.Round(gust.Value, 2) : null;
        }

        /// <summary>
        /// Gets cardinal direction from degrees
        /// </summary>
        public string GetCardinalDirection()
        {
            var directions = new[] { "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE", "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW" };
            var index = (int)Math.Round(Direction / 22.5) % 16;
            return directions[index];
        }

        protected override IEnumerable<object> GetEqualityComponents()
        {
            yield return Speed;
            yield return Direction;
            yield return Gust ?? 0;
        }

        public override string ToString()
        {
            var gustInfo = Gust.HasValue ? $", Gust: {Gust:F1}" : "";
            return $"{Speed:F1} mph from {GetCardinalDirection()} ({Direction}°){gustInfo}";
        }
    }
}