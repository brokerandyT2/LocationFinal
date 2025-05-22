using BoDi;
using FluentAssertions;
using Location.Photography.BDD.Tests.Drivers;
using Location.Photography.BDD.Tests.Models;
using Location.Photography.BDD.Tests.Support;
using Location.Photography.Domain.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Photography.BDD.Tests.StepDefinitions.SunCalculator
{
    [Binding]
    public class SunTimesSteps
    {
        private readonly ApiContext _context;
        private readonly SunCalculatorDriver _sunCalculatorDriver;
        private readonly IObjectContainer _objectContainer;

        public SunTimesSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _sunCalculatorDriver = new SunCalculatorDriver(context);
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                Console.WriteLine("SunTimesSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SunTimesSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have a location at coordinates (.*), (.*) for sun times")]
        public void GivenIHaveALocationAtCoordinatesForSunTimes(double latitude, double longitude)
        {
            var sunCalculationModel = new SunCalculationTestModel
            {
                Id = 1,
                Latitude = latitude,
                Longitude = longitude,
                Date = DateTime.Today,
                DateTime = DateTime.Today.AddHours(12) // Default to noon
            };

            sunCalculationModel.SynchronizeDateTime();
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [Given(@"I want to calculate sun times for date (.*)")]
        public void GivenIWantToCalculateSunTimesForDate(DateTime date)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            if (sunCalculationModel == null)
            {
                sunCalculationModel = new SunCalculationTestModel
                {
                    Id = 1,
                    Latitude = 40.7128, // Default to New York
                    Longitude = -74.0060
                };
            }

            sunCalculationModel.Date = date.Date;
            sunCalculationModel.DateTime = date.Date.AddHours(12);
            sunCalculationModel.SynchronizeDateTime();
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [Given(@"I have multiple locations for sun times calculation:")]
        public void GivenIHaveMultipleLocationsForSunTimesCalculation(Table table)
        {
            var sunCalculations = table.CreateSet<SunCalculationTestModel>().ToList();

            // Assign IDs and set expected sun times
            for (int i = 0; i < sunCalculations.Count; i++)
            {
                if (!sunCalculations[i].Id.HasValue)
                {
                    sunCalculations[i].Id = i + 1;
                }

                sunCalculations[i].SynchronizeDateTime();
                SetExpectedSunTimes(sunCalculations[i]);
            }

            // Setup the sun calculations in the driver
            _sunCalculatorDriver.SetupSunCalculations(sunCalculations);

            // Store all calculations in the context
            _context.StoreModel(sunCalculations, "AllSunTimes");
        }

        [Given(@"I want to plan photography for (.*)")]
        public void GivenIWantToPlanPhotographyFor(string locationName)
        {
            var coordinates = GetCoordinatesForLocation(locationName);
            var sunCalculationModel = new SunCalculationTestModel
            {
                Id = 1,
                Latitude = coordinates.latitude,
                Longitude = coordinates.longitude,
                Date = DateTime.Today,
                DateTime = DateTime.Today.AddHours(12)
            };

            sunCalculationModel.SynchronizeDateTime();
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [Given(@"today's date is (.*)")]
        public void GivenTodaysDateIs(DateTime date)
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            if (sunCalculationModel == null)
            {
                sunCalculationModel = new SunCalculationTestModel
                {
                    Id = 1,
                    Latitude = 40.7128,
                    Longitude = -74.0060
                };
            }

            sunCalculationModel.Date = date.Date;
            sunCalculationModel.DateTime = date.Date.AddHours(12);
            sunCalculationModel.SynchronizeDateTime();
            _context.StoreSunCalculationData(sunCalculationModel);
        }

        [When(@"I calculate the sun times")]
        public async Task WhenICalculateTheSunTimes()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            // Set expected sun times for testing
            SetExpectedSunTimes(sunCalculationModel);

            await _sunCalculatorDriver.GetSunTimesAsync(sunCalculationModel);
        }

        [When(@"I request the sunrise time")]
        public async Task WhenIRequestTheSunriseTime()
        {
            await WhenICalculateTheSunTimes();
        }

        [When(@"I request the sunset time")]
        public async Task WhenIRequestTheSunsetTime()
        {
            await WhenICalculateTheSunTimes();
        }

        [When(@"I request all twilight times")]
        public async Task WhenIRequestAllTwilightTimes()
        {
            await WhenICalculateTheSunTimes();
        }

        [When(@"I calculate sun times for all locations")]
        public async Task WhenICalculateSunTimesForAllLocations()
        {
            var allSunTimes = _context.GetModel<List<SunCalculationTestModel>>("AllSunTimes");
            allSunTimes.Should().NotBeNull("Sun times data should be available in context");

            foreach (var sunTime in allSunTimes)
            {
                SetExpectedSunTimes(sunTime);
                await _sunCalculatorDriver.GetSunTimesAsync(sunTime);
            }
        }

        [When(@"I request the golden hour times")]
        public async Task WhenIRequestTheGoldenHourTimes()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            SetExpectedSunTimes(sunCalculationModel);
            await _sunCalculatorDriver.GetGoldenHourTimesAsync(sunCalculationModel);
        }

        [When(@"I calculate sun times for the summer solstice")]
        public async Task WhenICalculateSunTimesForTheSummerSolstice()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            // Set to summer solstice (June 21)
            var summerSolstice = new DateTime(DateTime.Today.Year, 6, 21);
            sunCalculationModel.Date = summerSolstice;
            sunCalculationModel.DateTime = summerSolstice.AddHours(12);
            sunCalculationModel.SynchronizeDateTime();

            SetExpectedSunTimes(sunCalculationModel);
            await _sunCalculatorDriver.GetSunTimesAsync(sunCalculationModel);
        }

        [When(@"I calculate sun times for the winter solstice")]
        public async Task WhenICalculateSunTimesForTheWinterSolstice()
        {
            var sunCalculationModel = _context.GetSunCalculationData();
            sunCalculationModel.Should().NotBeNull("Sun calculation data should be available in context");

            // Set to winter solstice (December 21)
            var winterSolstice = new DateTime(DateTime.Today.Year, 12, 21);
            sunCalculationModel.Date = winterSolstice;
            sunCalculationModel.DateTime = winterSolstice.AddHours(12);
            sunCalculationModel.SynchronizeDateTime();

            SetExpectedSunTimes(sunCalculationModel);
            await _sunCalculatorDriver.GetSunTimesAsync(sunCalculationModel);
        }

        [Then(@"I should receive the sunrise time")]
        public void ThenIShouldReceiveTheSunriseTime()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.IsSuccess.Should().BeTrue("Sun times calculation should be successful");
            result.Data.Should().NotBeNull("Sun times data should be available");
            result.Data.Sunrise.Should().NotBe(default(DateTime), "Sunrise time should be set");
        }

        [Then(@"I should receive the sunset time")]
        public void ThenIShouldReceiveTheSunsetTime()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.IsSuccess.Should().BeTrue("Sun times calculation should be successful");
            result.Data.Should().NotBeNull("Sun times data should be available");
            result.Data.Sunset.Should().NotBe(default(DateTime), "Sunset time should be set");
        }

        [Then(@"the sunrise should be before sunset")]
        public void ThenTheSunriseShouldBeBeforeSunset()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");
            result.Data.Sunrise.Should().BeBefore(result.Data.Sunset, "Sunrise should occur before sunset");
        }

        [Then(@"the sun times should be calculated successfully")]
        public void ThenTheSunTimesShouldBeCalculatedSuccessfully()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.IsSuccess.Should().BeTrue("Sun times calculation should be successful");
            result.Data.Should().NotBeNull("Sun times data should be available");

            // Validate all major sun times are set
            result.Data.Sunrise.Should().NotBe(default(DateTime), "Sunrise should be set");
            result.Data.Sunset.Should().NotBe(default(DateTime), "Sunset should be set");
            result.Data.SolarNoon.Should().NotBe(default(DateTime), "Solar noon should be set");
            result.Data.CivilDawn.Should().NotBe(default(DateTime), "Civil dawn should be set");
            result.Data.CivilDusk.Should().NotBe(default(DateTime), "Civil dusk should be set");
        }

        [Then(@"I should receive all twilight times")]
        public void ThenIShouldReceiveAllTwilightTimes()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.IsSuccess.Should().BeTrue("Sun times calculation should be successful");
            result.Data.Should().NotBeNull("Sun times data should be available");

            // Validate all twilight times are set
            result.Data.AstronomicalDawn.Should().NotBe(default(DateTime), "Astronomical dawn should be set");
            result.Data.NauticalDawn.Should().NotBe(default(DateTime), "Nautical dawn should be set");
            result.Data.CivilDawn.Should().NotBe(default(DateTime), "Civil dawn should be set");
            result.Data.CivilDusk.Should().NotBe(default(DateTime), "Civil dusk should be set");
            result.Data.NauticalDusk.Should().NotBe(default(DateTime), "Nautical dusk should be set");
            result.Data.AstronomicalDusk.Should().NotBe(default(DateTime), "Astronomical dusk should be set");
        }

        [Then(@"the twilight times should be in correct order")]
        public void ThenTheTwilightTimesShouldBeInCorrectOrder()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");

            // Morning twilight progression
            result.Data.AstronomicalDawn.Should().BeBefore(result.Data.NauticalDawn, "Astronomical dawn should be before nautical dawn");
            result.Data.NauticalDawn.Should().BeBefore(result.Data.CivilDawn, "Nautical dawn should be before civil dawn");
            result.Data.CivilDawn.Should().BeBefore(result.Data.Sunrise, "Civil dawn should be before sunrise");

            // Evening twilight progression
            result.Data.Sunset.Should().BeBefore(result.Data.CivilDusk, "Sunset should be before civil dusk");
            result.Data.CivilDusk.Should().BeBefore(result.Data.NauticalDusk, "Civil dusk should be before nautical dusk");
            result.Data.NauticalDusk.Should().BeBefore(result.Data.AstronomicalDusk, "Nautical dusk should be before astronomical dusk");
        }

        [Then(@"the solar noon should be between sunrise and sunset")]
        public void ThenTheSolarNoonShouldBeBetweenSunriseAndSunset()
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");

            result.Data.SolarNoon.Should().BeAfter(result.Data.Sunrise, "Solar noon should be after sunrise");
            result.Data.SolarNoon.Should().BeBefore(result.Data.Sunset, "Solar noon should be before sunset");
        }

        [Then(@"the sunrise should be approximately at (.*)")]
        public void ThenTheSunriseShouldBeApproximatelyAt(string expectedTime)
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");

            if (TimeSpan.TryParse(expectedTime, out var expectedTimeSpan))
            {
                var actualTime = result.Data.Sunrise.TimeOfDay;
                var timeDifference = Math.Abs((actualTime - expectedTimeSpan).TotalMinutes);
                timeDifference.Should().BeLessThan(30, $"Sunrise should be approximately at {expectedTime}");
            }
        }

        [Then(@"the sunset should be approximately at (.*)")]
        public void ThenTheSunsetShouldBeApproximatelyAt(string expectedTime)
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");

            if (TimeSpan.TryParse(expectedTime, out var expectedTimeSpan))
            {
                var actualTime = result.Data.Sunset.TimeOfDay;
                var timeDifference = Math.Abs((actualTime - expectedTimeSpan).TotalMinutes);
                timeDifference.Should().BeLessThan(30, $"Sunset should be approximately at {expectedTime}");
            }
        }

        [Then(@"I should receive the golden hour times")]
        public void ThenIShouldReceiveTheGoldenHourTimes()
        {
            var result = _context.GetLastResult<Dictionary<string, DateTime>>();
            result.Should().NotBeNull("Golden hour times result should be available");
            result.IsSuccess.Should().BeTrue("Golden hour calculation should be successful");
            result.Data.Should().NotBeNull("Golden hour data should be available");

            result.Data.Should().ContainKey("MorningStart", "Should have morning golden hour start");
            result.Data.Should().ContainKey("MorningEnd", "Should have morning golden hour end");
            result.Data.Should().ContainKey("EveningStart", "Should have evening golden hour start");
            result.Data.Should().ContainKey("EveningEnd", "Should have evening golden hour end");
        }

        [Then(@"the golden hour times should be relative to sunrise and sunset")]
        public void ThenTheGoldenHourTimesShouldBeRelativeToSunriseAndSunset()
        {
            var goldenHourResult = _context.GetLastResult<Dictionary<string, DateTime>>();
            goldenHourResult.Should().NotBeNull("Golden hour times should be available");

            var sunCalculation = _context.GetSunCalculationData();
            sunCalculation.Should().NotBeNull("Sun calculation data should be available");

            if (goldenHourResult.IsSuccess && goldenHourResult.Data != null)
            {
                var morningStart = goldenHourResult.Data["MorningStart"];
                var morningEnd = goldenHourResult.Data["MorningEnd"];
                var eveningStart = goldenHourResult.Data["EveningStart"];
                var eveningEnd = goldenHourResult.Data["EveningEnd"];

                // Morning golden hour should end around sunrise
                Math.Abs((morningEnd - sunCalculation.Sunrise).TotalMinutes).Should().BeLessThan(30,
                    "Morning golden hour should end around sunrise");

                // Evening golden hour should start around sunset
                Math.Abs((eveningStart - sunCalculation.Sunset).TotalMinutes).Should().BeLessThan(30,
                    "Evening golden hour should start around sunset");
            }
        }

        [Then(@"all sun times should be calculated successfully")]
        public void ThenAllSunTimesShouldBeCalculatedSuccessfully()
        {
            var allSunTimes = _context.GetModel<List<SunCalculationTestModel>>("AllSunTimes");
            allSunTimes.Should().NotBeNull("Sun times data should be available in context");

            foreach (var sunTime in allSunTimes)
            {
                sunTime.Sunrise.Should().NotBe(default(DateTime), $"Sunrise for location {sunTime.Id} should be set");
                sunTime.Sunset.Should().NotBe(default(DateTime), $"Sunset for location {sunTime.Id} should be set");
                sunTime.Sunrise.Should().BeBefore(sunTime.Sunset, $"Sunrise should be before sunset for location {sunTime.Id}");
            }
        }

        [Then(@"the day length should be longer than (.*)")]
        public void ThenTheDayLengthShouldBeLongerThan(string minimumDuration)
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");

            var dayLength = result.Data.Sunset - result.Data.Sunrise;

            if (TimeSpan.TryParse(minimumDuration, out var minimumTimeSpan))
            {
                dayLength.Should().BeGreaterThan(minimumTimeSpan, $"Day length should be longer than {minimumDuration}");
            }
        }

        [Then(@"the day length should be shorter than (.*)")]
        public void ThenTheDayLengthShouldShorterThan(string maximumDuration)
        {
            var result = _context.GetLastResult<SunTimesDto>();
            result.Should().NotBeNull("Sun times result should be available");
            result.Data.Should().NotBeNull("Sun times data should be available");

            var dayLength = result.Data.Sunset - result.Data.Sunrise;

            if (TimeSpan.TryParse(maximumDuration, out var maximumTimeSpan))
            {
                dayLength.Should().BeLessThan(maximumTimeSpan, $"Day length should be shorter than {maximumDuration}");
            }
        }

        // Helper methods
        private (double latitude, double longitude) GetCoordinatesForLocation(string locationName)
        {
            return locationName.ToLowerInvariant() switch
            {
                "new york" => (40.7128, -74.0060),
                "london" => (51.5074, -0.1278),
                "tokyo" => (35.6762, 139.6503),
                "sydney" => (-33.8688, 151.2093),
                "paris" => (48.8566, 2.3522),
                "reykjavik" => (64.1466, -21.9426),
                "cape town" => (-33.9249, 18.4241),
                _ => (40.7128, -74.0060) // Default to New York
            };
        }

        private void SetExpectedSunTimes(SunCalculationTestModel model)
        {
            // Set reasonable sun times based on location and date
            // These are simplified calculations for testing purposes
            var baseDate = model.Date.Date;

            // Adjust times based on latitude (higher latitudes have more extreme variations)
            var latitudeFactor = Math.Abs(model.Latitude) / 90.0;
            var seasonFactor = GetSeasonFactor(model.Date);

            // Base times (approximate for mid-latitudes)
            var baseSunrise = TimeSpan.FromHours(6);
            var baseSunset = TimeSpan.FromHours(18);

            // Adjust for season and latitude
            var sunriseAdjustment = seasonFactor * latitudeFactor * 2; // Up to 2 hours adjustment
            var sunsetAdjustment = -seasonFactor * latitudeFactor * 2;

            model.Sunrise = baseDate.Add(baseSunrise).AddHours(sunriseAdjustment);
            model.Sunset = baseDate.Add(baseSunset).AddHours(sunsetAdjustment);
            model.SolarNoon = baseDate.Add(TimeSpan.FromHours(12));

            // Set twilight times
            model.CivilDawn = model.Sunrise.AddMinutes(-30);
            model.CivilDusk = model.Sunset.AddMinutes(30);
            model.NauticalDawn = model.Sunrise.AddMinutes(-60);
            model.NauticalDusk = model.Sunset.AddMinutes(60);
            model.AstronomicalDawn = model.Sunrise.AddMinutes(-90);
            model.AstronomicalDusk = model.Sunset.AddMinutes(90);

            // Calculate golden hours
            model.CalculateGoldenHours();
        }

        private double GetSeasonFactor(DateTime date)
        {
            // Calculate how far into the year we are (0-1)
            var dayOfYear = date.DayOfYear;
            var totalDays = DateTime.IsLeapYear(date.Year) ? 366 : 365;
            var yearProgress = (double)dayOfYear / totalDays;

            // Create a sine wave with peak at summer solstice (around day 172)
            var solsticeDay = 172.0 / totalDays;
            var radians = (yearProgress - solsticeDay) * 2 * Math.PI;

            // Return value between -1 (winter) and 1 (summer)
            return Math.Sin(radians);
        }
    }
}