using Location.Photography.Application.Services;
using Location.Photography.BDD.Tests.Models;

namespace Location.Photography.BDD.Tests.Support
{
    public static class TestDataGenerator
    {
        private static readonly Random _random = new();

        /// <summary>
        /// Generates a random exposure test model
        /// </summary>
        public static ExposureTestModel GenerateExposure(int? id = null)
        {
            string[] shutterSpeeds = { "1/4000", "1/2000", "1/1000", "1/500", "1/250", "1/125", "1/60", "1/30", "1/15", "1/8", "1/4", "1/2", "1", "2", "4", "8", "15", "30" };
            string[] apertures = { "f/1.4", "f/2", "f/2.8", "f/4", "f/5.6", "f/8", "f/11", "f/16", "f/22", "f/32" };
            string[] isos = { "50", "100", "200", "400", "800", "1600", "3200", "6400", "12800", "25600" };

            var randomShutter = shutterSpeeds[_random.Next(shutterSpeeds.Length)];
            var randomAperture = apertures[_random.Next(apertures.Length)];
            var randomIso = isos[_random.Next(isos.Length)];

            return new ExposureTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                BaseShutterSpeed = randomShutter,
                BaseAperture = randomAperture,
                BaseIso = randomIso,
                TargetShutterSpeed = shutterSpeeds[_random.Next(shutterSpeeds.Length)],
                TargetAperture = apertures[_random.Next(apertures.Length)],
                TargetIso = isos[_random.Next(isos.Length)],
                Increments = (ExposureIncrements)_random.Next(0, 3),
                FixedValue = (FixedValue)_random.Next(0, 4),
                EvCompensation = Math.Round((_random.NextDouble() * 4) - 2, 1), // -2.0 to +2.0
                ResultShutterSpeed = randomShutter,
                ResultAperture = randomAperture,
                ResultIso = randomIso,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// Generates a list of random exposure test models
        /// </summary>
        public static List<ExposureTestModel> GenerateExposures(int count)
        {
            var result = new List<ExposureTestModel>();

            for (int i = 0; i < count; i++)
            {
                result.Add(GenerateExposure(i + 1));
            }

            return result;
        }

        /// <summary>
        /// Generates a random sun calculation test model
        /// </summary>
        public static SunCalculationTestModel GenerateSunCalculation(int? id = null)
        {
            var randomDate = DateTime.Today.AddDays(_random.Next(-30, 31));
            var randomTime = TimeSpan.FromHours(_random.Next(0, 24));
            var randomLatitude = Math.Round((_random.NextDouble() * 180) - 90, 6);
            var randomLongitude = Math.Round((_random.NextDouble() * 360) - 180, 6);

            return new SunCalculationTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                Date = randomDate,
                Time = randomTime,
                DateTime = randomDate.Add(randomTime),
                Latitude = randomLatitude,
                Longitude = randomLongitude,
                Sunrise = randomDate.AddHours(6).AddMinutes(_random.Next(-30, 30)),
                Sunset = randomDate.AddHours(18).AddMinutes(_random.Next(-30, 30)),
                SolarNoon = randomDate.AddHours(12).AddMinutes(_random.Next(-15, 15)),
                CivilDawn = randomDate.AddHours(5).AddMinutes(_random.Next(0, 60)),
                CivilDusk = randomDate.AddHours(19).AddMinutes(_random.Next(0, 60)),
                NauticalDawn = randomDate.AddHours(4).AddMinutes(_random.Next(30, 90)),
                NauticalDusk = randomDate.AddHours(19).AddMinutes(_random.Next(30, 90)),
                AstronomicalDawn = randomDate.AddHours(4).AddMinutes(_random.Next(0, 60)),
                AstronomicalDusk = randomDate.AddHours(20).AddMinutes(_random.Next(0, 60)),
                SolarAzimuth = Math.Round(_random.NextDouble() * 360, 1),
                SolarElevation = Math.Round((_random.NextDouble() * 90) - 45, 1),
                GoldenHourMorningStart = randomDate.AddHours(6),
                GoldenHourMorningEnd = randomDate.AddHours(7),
                GoldenHourEveningStart = randomDate.AddHours(17),
                GoldenHourEveningEnd = randomDate.AddHours(18)
            };
        }

        /// <summary>
        /// Generates a list of random sun calculation test models
        /// </summary>
        public static List<SunCalculationTestModel> GenerateSunCalculations(int count)
        {
            var result = new List<SunCalculationTestModel>();

            for (int i = 0; i < count; i++)
            {
                result.Add(GenerateSunCalculation(i + 1));
            }

            return result;
        }

        /// <summary>
        /// Generates a random scene evaluation test model
        /// </summary>
        public static SceneEvaluationTestModel GenerateSceneEvaluation(int? id = null)
        {
            return new SceneEvaluationTestModel
            {
                Id = id ?? _random.Next(1, 1000),
                ImagePath = $"/test/images/test_image_{_random.Next(1, 100)}.jpg",
                RedHistogramPath = $"/test/histograms/red_{_random.Next(1, 100)}.png",
                GreenHistogramPath = $"/test/histograms/green_{_random.Next(1, 100)}.png",
                BlueHistogramPath = $"/test/histograms/blue_{_random.Next(1, 100)}.png",
                ContrastHistogramPath = $"/test/histograms/contrast_{_random.Next(1, 100)}.png",
                MeanRed = Math.Round(_random.NextDouble() * 255, 1),
                MeanGreen = Math.Round(_random.NextDouble() * 255, 1),
                MeanBlue = Math.Round(_random.NextDouble() * 255, 1),
                MeanContrast = Math.Round(_random.NextDouble() * 255, 1),
                StdDevRed = Math.Round(_random.NextDouble() * 128, 1),
                StdDevGreen = Math.Round(_random.NextDouble() * 128, 1),
                StdDevBlue = Math.Round(_random.NextDouble() * 128, 1),
                StdDevContrast = Math.Round(_random.NextDouble() * 128, 1),
                TotalPixels = _random.Next(100000, 10000000),
                ColorTemperature = Math.Round((_random.NextDouble() * 6500) + 2700, 0), // 2700K to 9200K
                TintValue = Math.Round((_random.NextDouble() * 2) - 1, 2), // -1.0 to 1.0
                IsProcessing = false,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// Generates a list of random scene evaluation test models
        /// </summary>
        public static List<SceneEvaluationTestModel> GenerateSceneEvaluations(int count)
        {
            var result = new List<SceneEvaluationTestModel>();

            for (int i = 0; i < count; i++)
            {
                result.Add(GenerateSceneEvaluation(i + 1));
            }

            return result;
        }

        /// <summary>
        /// Generates specific exposure settings for testing
        /// </summary>
        public static ExposureTestModel GenerateSpecificExposure(
            string baseShutter = "1/125",
            string baseAperture = "f/5.6",
            string baseIso = "100",
            ExposureIncrements increments = ExposureIncrements.Full,
            FixedValue fixedValue = FixedValue.ShutterSpeeds,
            double evCompensation = 0.0)
        {
            return new ExposureTestModel
            {
                Id = _random.Next(1, 1000),
                BaseShutterSpeed = baseShutter,
                BaseAperture = baseAperture,
                BaseIso = baseIso,
                Increments = increments,
                FixedValue = fixedValue,
                EvCompensation = evCompensation,
                TargetShutterSpeed = baseShutter,
                TargetAperture = baseAperture,
                TargetIso = baseIso,
                ResultShutterSpeed = baseShutter,
                ResultAperture = baseAperture,
                ResultIso = baseIso,
                ErrorMessage = string.Empty
            };
        }

        /// <summary>
        /// Generates specific sun calculation for testing
        /// </summary>
        public static SunCalculationTestModel GenerateSpecificSunCalculation(
            DateTime? date = null,
            double latitude = 40.7128,
            double longitude = -74.0060)
        {
            var testDate = date ?? DateTime.Today;

            return new SunCalculationTestModel
            {
                Id = _random.Next(1, 1000),
                Date = testDate,
                Time = TimeSpan.FromHours(12),
                DateTime = testDate.AddHours(12),
                Latitude = latitude,
                Longitude = longitude,
                Sunrise = testDate.AddHours(6),
                Sunset = testDate.AddHours(18),
                SolarNoon = testDate.AddHours(12),
                CivilDawn = testDate.AddHours(5).AddMinutes(30),
                CivilDusk = testDate.AddHours(18).AddMinutes(30),
                NauticalDawn = testDate.AddHours(5),
                NauticalDusk = testDate.AddHours(19),
                AstronomicalDawn = testDate.AddHours(4).AddMinutes(30),
                AstronomicalDusk = testDate.AddHours(19).AddMinutes(30),
                SolarAzimuth = 180.0,
                SolarElevation = 45.0,
                GoldenHourMorningStart = testDate.AddHours(6),
                GoldenHourMorningEnd = testDate.AddHours(7),
                GoldenHourEveningStart = testDate.AddHours(17),
                GoldenHourEveningEnd = testDate.AddHours(18)
            };
        }
    }
}