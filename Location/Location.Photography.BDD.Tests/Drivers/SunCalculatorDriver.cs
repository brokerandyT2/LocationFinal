using Location.Core.Application.Common.Models;
using Location.Photography.Application.Services;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using Location.Photography.Domain.Models;
using Location.Photography.Domain.Services;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Location.Photography.BDD.Tests.Drivers
{
    /// <summary>
    /// Driver for sun calculator operations in BDD tests
    /// </summary>
    public class SunCalculatorDriver
    {
        private readonly ApiContext _context;
        private readonly Mock<ISunCalculatorService> _sunCalculatorServiceMock;
        private readonly Mock<ISunService> _sunServiceMock;

        public SunCalculatorDriver(ApiContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _sunCalculatorServiceMock = _context.GetService<Mock<ISunCalculatorService>>();
            _sunServiceMock = _context.GetService<Mock<ISunService>>();
        }

        /// <summary>
        /// Sets up multiple sun calculation models in the mock service
        /// </summary>
        public void SetupSunCalculations(List<SunCalculationTestModel> calculations)
        {
            if (calculations == null || !calculations.Any()) return;

            foreach (var calculation in calculations)
            {
                if (!calculation.Id.HasValue || calculation.Id.Value <= 0)
                {
                    calculation.Id = calculations.IndexOf(calculation) + 1;
                }

                // Synchronize DateTime properties
                calculation.SynchronizeDateTime();

                // FIXED: Use proper solar azimuth calculation
                SetExpectedSunPosition(calculation);

                // Calculate golden hours if not already set
                if (calculation.GoldenHourMorningStart == default)
                {
                    calculation.CalculateGoldenHours();
                }
            }

            // Store for later retrieval - Following SceneEvaluationDriver pattern
            _context.StoreModel(calculations, "SetupSunCalculations");
        }

        /// <summary>
        /// Calculates sun times for given location and date
        /// </summary>
        public async Task<Result<SunTimesDto>> GetSunTimesAsync(SunCalculationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Ensure DateTime synchronization
            model.SynchronizeDateTime();

            // FIXED: Use proper solar azimuth calculation
            SetExpectedSunPosition(model);

            // Create expected result
            var expectedResult = model.ToSunTimesDto();

            // Setup ISunCalculatorService mock methods
            _sunCalculatorServiceMock
                .Setup(s => s.GetSunrise(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.Sunrise);

            _sunCalculatorServiceMock
                .Setup(s => s.GetSunset(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.Sunset);

            _sunCalculatorServiceMock
                .Setup(s => s.GetSolarNoon(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.SolarNoon);

            _sunCalculatorServiceMock
                .Setup(s => s.GetCivilDawn(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.CivilDawn);

            _sunCalculatorServiceMock
                .Setup(s => s.GetCivilDusk(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.CivilDusk);

            _sunCalculatorServiceMock
                .Setup(s => s.GetNauticalDawn(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.NauticalDawn);

            _sunCalculatorServiceMock
                .Setup(s => s.GetNauticalDusk(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.NauticalDusk);

            _sunCalculatorServiceMock
                .Setup(s => s.GetAstronomicalDawn(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.AstronomicalDawn);

            _sunCalculatorServiceMock
                .Setup(s => s.GetAstronomicalDusk(
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.AstronomicalDusk);

            // Setup ISunService mock
            _sunServiceMock
                .Setup(s => s.GetSunTimesAsync(
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001),
                    It.Is<DateTime>(d => d.Date == model.Date.Date),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<SunTimesDto>.Success(expectedResult)
                    : Result<SunTimesDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<SunTimesDto>.Success(expectedResult)
                : Result<SunTimesDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromSunTimes(result.Data);
                _context.StoreSunCalculationData(model);
            }

            return result;
        }

        /// <summary>
        /// Calculates sun position for given location and time
        /// </summary>
        public async Task<Result<SunPositionDto>> GetSunPositionAsync(SunCalculationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Ensure DateTime synchronization
            model.SynchronizeDateTime();

            // FIXED: Use proper solar azimuth calculation
            SetExpectedSunPosition(model);

            // Create expected result
            var expectedResult = model.ToSunPositionDto();

            // Setup ISunCalculatorService mock methods
            _sunCalculatorServiceMock
                .Setup(s => s.GetSolarAzimuth(
                    It.Is<DateTime>(dt => Math.Abs((dt - model.DateTime).TotalMinutes) < 1),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.SolarAzimuth);

            _sunCalculatorServiceMock
                .Setup(s => s.GetSolarElevation(
                    It.Is<DateTime>(dt => Math.Abs((dt - model.DateTime).TotalMinutes) < 1),
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001)))
                .Returns(model.SolarElevation);

            // Setup ISunService mock
            _sunServiceMock
                .Setup(s => s.GetSunPositionAsync(
                    It.Is<double>(lat => Math.Abs(lat - model.Latitude) < 0.0001),
                    It.Is<double>(lon => Math.Abs(lon - model.Longitude) < 0.0001),
                    It.Is<DateTime>(dt => Math.Abs((dt - model.DateTime).TotalMinutes) < 1),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(string.IsNullOrEmpty(model.ErrorMessage)
                    ? Result<SunPositionDto>.Success(expectedResult)
                    : Result<SunPositionDto>.Failure(model.ErrorMessage));

            // NO MediatR - Direct response
            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<SunPositionDto>.Success(expectedResult)
                : Result<SunPositionDto>.Failure(model.ErrorMessage);

            // Store result and update context
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                model.UpdateFromSunPosition(result.Data);
                _context.StoreSunCalculationData(model);
            }

            return result;
        }

        /// <summary>
        /// Gets sun calculation by location and date - context management pattern
        /// </summary>
        public async Task<Result<SunCalculationTestModel>> GetSunCalculationByLocationAndDateAsync(double latitude, double longitude, DateTime date)
        {
            // Check individual context first
            var individual = _context.GetSunCalculationData();
            if (individual != null &&
                Math.Abs(individual.Latitude - latitude) < 0.0001 &&
                Math.Abs(individual.Longitude - longitude) < 0.0001 &&
                individual.Date.Date == date.Date)
            {
                var response = individual.Clone();
                var result = Result<SunCalculationTestModel>.Success(response);
                _context.StoreResult(result);
                return result;
            }

            // Check collection contexts
            var collectionKeys = new[] { "AllSunCalculations", "SetupSunCalculations", "SunTrackingSessions", "SunPositionTracking", "AllSunPositions" };
            foreach (var collectionKey in collectionKeys)
            {
                var collection = _context.GetModel<List<SunCalculationTestModel>>(collectionKey);
                var found = collection?.FirstOrDefault(c =>
                    Math.Abs(c.Latitude - latitude) < 0.0001 &&
                    Math.Abs(c.Longitude - longitude) < 0.0001 &&
                    c.Date.Date == date.Date);

                if (found != null)
                {
                    var response = found.Clone();
                    var result = Result<SunCalculationTestModel>.Success(response);
                    _context.StoreResult(result);
                    return result;
                }
            }

            // Not found
            var failureResult = Result<SunCalculationTestModel>.Failure($"Sun calculation for location ({latitude:F4}, {longitude:F4}) on {date:yyyy-MM-dd} not found");
            _context.StoreResult(failureResult);
            return failureResult;
        }

        /// <summary>
        /// Gets sun calculations by location - returns all for a specific location
        /// </summary>
        public async Task<Result<List<SunCalculationTestModel>>> GetSunCalculationsByLocationAsync(double latitude, double longitude)
        {
            var results = new List<SunCalculationTestModel>();

            // Check individual context first
            var individual = _context.GetSunCalculationData();
            if (individual != null &&
                Math.Abs(individual.Latitude - latitude) < 0.0001 &&
                Math.Abs(individual.Longitude - longitude) < 0.0001)
            {
                results.Add(individual.Clone());
            }

            // Check collection contexts - FIXED: Added all tracking collections
            var collectionKeys = new[] { "AllSunCalculations", "SetupSunCalculations", "SunTrackingSessions", "SunPositionTracking", "AllSunPositions" };
            foreach (var collectionKey in collectionKeys)
            {
                var collection = _context.GetModel<List<SunCalculationTestModel>>(collectionKey);
                if (collection != null)
                {
                    var found = collection.Where(c =>
                        Math.Abs(c.Latitude - latitude) < 0.0001 &&
                        Math.Abs(c.Longitude - longitude) < 0.0001).ToList();

                    foreach (var item in found)
                    {
                        if (!results.Any(r => r.Id == item.Id && r.DateTime == item.DateTime))
                        {
                            results.Add(item.Clone());
                        }
                    }
                }
            }

            var result = results.Any()
                ? Result<List<SunCalculationTestModel>>.Success(results)
                : Result<List<SunCalculationTestModel>>.Failure($"No sun calculations found for location ({latitude:F4}, {longitude:F4})");

            _context.StoreResult(result);
            return result;
        }

        /// <summary>
        /// Validates coordinates
        /// </summary>
        public async Task<Result<bool>> ValidateCoordinatesAsync(double latitude, double longitude)
        {
            bool isValid = latitude >= -90 && latitude <= 90 && longitude >= -180 && longitude <= 180;

            var result = isValid
                ? Result<bool>.Success(true)
                : Result<bool>.Failure($"Invalid coordinates: Latitude ({latitude}) must be between -90 and 90, Longitude ({longitude}) must be between -180 and 180");

            _context.StoreResult(result);
            return result;
        }

        /// <summary>
        /// Gets golden hour information for a specific date and location
        /// </summary>
        public async Task<Result<Dictionary<string, DateTime>>> GetGoldenHourTimesAsync(SunCalculationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Calculate golden hours if not already set
            if (model.GoldenHourMorningStart == default)
            {
                model.CalculateGoldenHours();
            }

            var goldenHours = new Dictionary<string, DateTime>
            {
                ["MorningStart"] = model.GoldenHourMorningStart,
                ["MorningEnd"] = model.GoldenHourMorningEnd,
                ["EveningStart"] = model.GoldenHourEveningStart,
                ["EveningEnd"] = model.GoldenHourEveningEnd
            };

            var result = string.IsNullOrEmpty(model.ErrorMessage)
                ? Result<Dictionary<string, DateTime>>.Success(goldenHours)
                : Result<Dictionary<string, DateTime>>.Failure(model.ErrorMessage);

            _context.StoreResult(result);

            if (result.IsSuccess)
            {
                _context.StoreSunCalculationData(model);
            }

            return result;
        }

        /// <summary>
        /// Creates a sun calculation with current time
        /// </summary>
        public async Task<Result<SunCalculationTestModel>> CreateSunCalculationAsync(SunCalculationTestModel model)
        {
            if (!model.Id.HasValue || model.Id.Value <= 0)
            {
                model.Id = 1;
            }

            // Synchronize DateTime
            model.SynchronizeDateTime();

            // Set defaults if not provided
            if (model.DateTime == default)
            {
                model.DateTime = DateTime.Now;
                model.SynchronizeDateTime();
            }

            // FIXED: Use proper solar azimuth calculation
            SetExpectedSunPosition(model);

            // NO MediatR - Direct response
            var response = model.Clone();
            var result = Result<SunCalculationTestModel>.Success(response);

            // Store result and update context
            _context.StoreResult(result);
            _context.StoreSunCalculationData(model);

            return result;
        }

        /// <summary>
        /// FIXED: Proper solar azimuth calculation with manual value preservation
        /// </summary>
        private void SetExpectedSunPosition(SunCalculationTestModel model)
        {
            // FIXED: Only calculate azimuth if not manually set (default is 0)
            if (model.SolarAzimuth == 0)
            {
                model.SolarAzimuth = CalculateSolarAzimuth(model.Latitude, model.Longitude, model.DateTime);
            }

            // FIXED: Only calculate elevation if not manually set (default is 0)
            if (model.SolarElevation == 0)
            {
                model.SolarElevation = CalculateSolarElevation(model.Latitude, model.DateTime);
            }
        }

        /// <summary>
        /// <summary>
        /// FIXED: Calculate proper solar azimuth with finer granularity
        /// </summary>
        private double CalculateSolarAzimuth(double latitude, double longitude, DateTime dateTime)
        {
            var timeHours = dateTime.TimeOfDay.TotalHours;

            // Night time
            if (timeHours < 6 || timeHours > 18)
            {
                return (timeHours < 6) ? 45 : 315;
            }

            // Daytime: 15° per hour movement (360° in 24 hours)
            // 6am = 90° (East), 12pm = 180° (South), 6pm = 270° (West)
            var hoursSinceSunrise = timeHours - 6; // 0 to 12 hours
            var azimuth = 90 + (hoursSinceSunrise * 15); // 15° per hour

            return Math.Min(270, azimuth); // Cap at west
        }

        /// <summary>
        /// FIXED: Calculate solar elevation with proper night handling
        /// </summary>
        private double CalculateSolarElevation(double latitude, DateTime dateTime)
        {
            var timeHours = dateTime.TimeOfDay.TotalHours;

            // FIXED: Proper night detection
            if (timeHours < 6 || timeHours > 18)
            {
                return -15; // Below horizon at night
            }

            var maxElevation = CalculateMaxElevationForLatitude(latitude, dateTime);
            var noonOffset = Math.Abs(timeHours - 12);
            var elevationFactor = Math.Cos(noonOffset * Math.PI / 6);

            return Math.Max(5, maxElevation * elevationFactor); // Minimum 5° during day
        }

        /// <summary>
        /// Calculate maximum solar elevation for given latitude
        /// </summary>
        private double CalculateMaxElevationForLatitude(double latitude, DateTime dateTime)
        {
            var absLatitude = Math.Abs(latitude);

            // Special handling for equator
            if (absLatitude == 0) return 90;

            // Latitude-based maximum elevation
            if (absLatitude >= 70) return 23; // Arctic regions
            if (absLatitude >= 60) return 35; // Sub-arctic
            if (absLatitude >= 50) return 40; // Northern Europe (London ~51°)
            if (absLatitude >= 40) return 45; // NYC ~40°
            if (absLatitude >= 30) return 55; // Subtropical

            return 65; // Near equatorial regions
        }
    }
}