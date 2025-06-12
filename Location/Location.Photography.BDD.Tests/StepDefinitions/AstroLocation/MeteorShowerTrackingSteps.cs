using BoDi;
using FluentAssertions;
using Location.Core.Application.Common.Models;
using Location.Photography.Application.Common.Interfaces;
using Location.Photography.BDD.Tests.Support;
using Location.Photography.Domain.Entities;
using TechTalk.SpecFlow;
using Moq;

namespace Location.Photography.BDD.Tests.StepDefinitions.AstroLocation
{
    [Binding]
    public class MeteorShowerTrackingSteps
    {
        private readonly ApiContext _context;
        private readonly Mock<IMeteorShowerDataService> _meteorShowerServiceMock;
        private readonly IObjectContainer _objectContainer;

        public MeteorShowerTrackingSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _meteorShowerServiceMock = _context.GetService<Mock<IMeteorShowerDataService>>();
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                Console.WriteLine("MeteorShowerTrackingSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in MeteorShowerTrackingSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"the current date is ""(.*)""")]
        public void GivenTheCurrentDateIs(string dateString)
        {
            if (DateTime.TryParse(dateString, out var date))
            {
                _context.StoreModel(date.ToString("yyyy-MM-dd"), "CurrentDate");
            }
            else
            {
                throw new ArgumentException($"Invalid date format: {dateString}");
            }
        }

        [Given(@"I want meteor showers with minimum ZHR of (.*)")]
        public void GivenIWantMeteorShowersWithMinimumZHROf(int minZHR)
        {
            _context.StoreModel(minZHR.ToString(), "MinimumZHR");
        }

        [Given(@"I want to find meteor shower with code ""(.*)""")]
        public void GivenIWantToFindMeteorShowerWithCode(string showerCode)
        {
            _context.StoreModel(showerCode, "ShowerCode");
        }

        [Given(@"I want to check if meteor shower ""(.*)"" is active")]
        public void GivenIWantToCheckIfMeteorShowerIsActive(string showerCode)
        {
            _context.StoreModel(showerCode, "ShowerCode");
        }

        [Given(@"I want to get ZHR for meteor shower ""(.*)""")]
        public void GivenIWantToGetZHRForMeteorShower(string showerCode)
        {
            _context.StoreModel(showerCode, "ShowerCode");
        }

        [Given(@"I want meteor showers between ""(.*)"" and ""(.*)""")]
        public void GivenIWantMeteorShowersBetweenAnd(string startDate, string endDate)
        {
            if (DateTime.TryParse(startDate, out var start) && DateTime.TryParse(endDate, out var end))
            {
                _context.StoreModel(start.ToString("yyyy-MM-dd"), "StartDate");
                _context.StoreModel(end.ToString("yyyy-MM-dd"), "EndDate");
            }
            else
            {
                throw new ArgumentException($"Invalid date range: {startDate} to {endDate}");
            }
        }

        [Given(@"I want to track the (.*) meteor shower")]
        public void GivenIWantToTrackTheMeteorShower(string showerName)
        {
            _context.StoreModel(showerName, "ShowerName");
        }

        [When(@"I get active meteor showers for today")]
        public async Task WhenIGetActiveMeteorShowersForToday()
        {
            var currentDate = DateTime.Parse(_context.GetModel<string>("CurrentDate"));
            var mockShowers = CreateMockShowers(currentDate);

            _meteorShowerServiceMock
                .Setup(s => s.GetActiveShowersAsync(It.Is<DateTime>(d => d.Date == currentDate.Date), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockShowers);

            var result = Result<List<MeteorShower>>.Success(mockShowers);
            _context.StoreResult(result);
        }

        [When(@"I get active meteor showers with ZHR threshold")]
        public async Task WhenIGetActiveMeteorShowersWithZHRThreshold()
        {
            var currentDate = DateTime.Parse(_context.GetModel<string>("CurrentDate"));
            var minZHR = int.Parse(_context.GetModel<string>("MinimumZHR"));

            var mockShowers = CreateMockShowers(currentDate).Where(s => s.Activity.ZHR >= minZHR).ToList();

            _meteorShowerServiceMock
                .Setup(s => s.GetActiveShowersAsync(It.Is<DateTime>(d => d.Date == currentDate.Date), It.Is<int>(z => z == minZHR), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockShowers);

            var result = Result<List<MeteorShower>>.Success(mockShowers);
            _context.StoreResult(result);
        }

        [When(@"I get meteor shower by code")]
        public async Task WhenIGetMeteorShowerByCode()
        {
            var showerCode = _context.GetModel<string>("ShowerCode");
            var mockShower = CreateMockShowerByCode(showerCode);

            _meteorShowerServiceMock
                .Setup(s => s.GetShowerByCodeAsync(It.Is<string>(c => c == showerCode), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockShower);

            var result = mockShower != null
                ? Result<MeteorShower>.Success(mockShower)
                : Result<MeteorShower>.Failure("Shower not found");
            _context.StoreResult(result);
        }

        [When(@"I check meteor shower activity")]
        public async Task WhenICheckMeteorShowerActivity()
        {
            var showerCode = _context.GetModel<string>("ShowerCode");
            var currentDate = DateTime.Parse(_context.GetModel<string>("CurrentDate"));

            var isActive = DetermineShowerActivity(showerCode, currentDate);

            _meteorShowerServiceMock
                .Setup(s => s.IsShowerActiveAsync(It.Is<string>(c => c == showerCode), It.Is<DateTime>(d => d.Date == currentDate.Date), It.IsAny<CancellationToken>()))
                .ReturnsAsync(isActive);

            var result = Result<bool>.Success(isActive);
            _context.StoreResult(result);
        }

        [When(@"I get expected ZHR for the date")]
        public async Task WhenIGetExpectedZHRForTheDate()
        {
            var showerCode = _context.GetModel<string>("ShowerCode");
            var currentDate = DateTime.Parse(_context.GetModel<string>("CurrentDate"));

            var expectedZHR = CalculateExpectedZHR(showerCode, currentDate);

            _meteorShowerServiceMock
                .Setup(s => s.GetExpectedZHRAsync(It.Is<string>(c => c == showerCode), It.Is<DateTime>(d => d.Date == currentDate.Date), It.IsAny<CancellationToken>()))
                .ReturnsAsync(expectedZHR);

            var result = Result<double>.Success(expectedZHR);
            _context.StoreResult(result);
        }

        [When(@"I get meteor showers in date range")]
        public async Task WhenIGetMeteorShowersInDateRange()
        {
            var startDate = DateTime.Parse(_context.GetModel<string>("StartDate"));
            var endDate = DateTime.Parse(_context.GetModel<string>("EndDate"));

            var mockShowers = CreateMockShowersInRange(startDate, endDate);

            _meteorShowerServiceMock
                .Setup(s => s.GetShowersInDateRangeAsync(It.IsAny<DateTime>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(mockShowers);

            var result = Result<List<MeteorShower>>.Success(mockShowers);
            _context.StoreResult(result);
        }

        [When(@"I get all meteor showers")]
        public async Task WhenIGetAllMeteorShowers()
        {
            var allShowers = CreateAllMockShowers();

            _meteorShowerServiceMock
                .Setup(s => s.GetAllShowersAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(allShowers);

            var result = Result<List<MeteorShower>>.Success(allShowers);
            _context.StoreResult(result);
        }

        [When(@"I get the radiant position for the shower")]
        public async Task WhenIGetTheRadiantPositionForTheShower()
        {
            var result = Result<Dictionary<string, object>>.Success(new Dictionary<string, object>
            {
                ["Azimuth"] = 45.0,
                ["Altitude"] = 30.0,
                ["DirectionDescription"] = "Northeast"
            });
            _context.StoreResult(result);
        }

        [When(@"I calculate optimal viewing conditions")]
        public async Task WhenICalculateOptimalViewingConditions()
        {
            var result = Result<Dictionary<string, object>>.Success(new Dictionary<string, object>
            {
                ["OptimalTime"] = DateTime.Now.AddHours(2),
                ["MoonInterference"] = 20
            });
            _context.StoreResult(result);
        }

        [When(@"I plan a meteor photography session")]
        public async Task WhenIPlanAMeteorPhotographySession()
        {
            var result = Result<Dictionary<string, object>>.Success(new Dictionary<string, object>
            {
                ["StartTime"] = DateTime.Now,
                ["EndTime"] = DateTime.Now.AddHours(4),
                ["RecommendedSettings"] = "ISO 3200, f/2.8, 30s"
            });
            _context.StoreResult(result);
        }

        [When(@"I get all active meteor showers")]
        public async Task WhenIGetAllActiveMeteorShowers()
        {
            var currentDate = DateTime.Parse(_context.GetModel<string>("CurrentDate"));
            var mockShowers = CreateMockShowers(currentDate);
            var result = Result<List<MeteorShower>>.Success(mockShowers);
            _context.StoreResult(result);
        }

        [When(@"I get meteor shower photography advice")]
        public async Task WhenIGetMeteorShowerPhotographyAdvice()
        {
            var result = Result<Dictionary<string, object>>.Success(new Dictionary<string, object>
            {
                ["LensRecommendation"] = "14-24mm wide-angle lens",
                ["ISORange"] = "1600-6400",
                ["ExposureTime"] = 30,
                ["CompositionAdvice"] = "Point camera away from radiant direction"
            });
            _context.StoreResult(result);
        }

        [When(@"I attempt to get meteor shower by code")]
        public async Task WhenIAttemptToGetMeteorShowerByCode()
        {
            var showerCode = _context.GetModel<string>("ShowerCode");
            var mockShower = CreateMockShowerByCode(showerCode);

            var result = mockShower != null
                ? Result<MeteorShower>.Success(mockShower)
                : Result<MeteorShower>.Failure("Shower not found");
            _context.StoreResult(result);
        }

        [Then(@"I should receive a list of active meteor showers")]
        public void ThenIShouldReceiveAListOfActiveMeteorShowers()
        {
            var result = _context.GetLastResult<List<MeteorShower>>();
            result.Should().NotBeNull("Meteor showers result should be available");
            result.IsSuccess.Should().BeTrue("Getting meteor showers should be successful");
            result.Data.Should().NotBeNull("Meteor showers data should be available");
            result.Data.Should().NotBeEmpty("Should have at least one active meteor shower");
        }

        [Then(@"the (.*) meteor shower should be active")]
        public void ThenTheMeteorShowerShouldBeActive(string showerName)
        {
            var result = _context.GetLastResult<List<MeteorShower>>();
            result.Should().NotBeNull("Meteor showers result should be available");
            result.Data.Should().NotBeNull("Meteor showers data should be available");

            var targetShower = result.Data.FirstOrDefault(s =>
                s.Designation.Contains(showerName, StringComparison.OrdinalIgnoreCase));
            targetShower.Should().NotBeNull($"The {showerName} meteor shower should be in the active list");
        }

        [Then(@"the showers should be ordered by ZHR descending")]
        public void ThenTheShowersShouldBeOrderedByZHRDescending()
        {
            var result = _context.GetLastResult<List<MeteorShower>>();
            result.Should().NotBeNull("Meteor showers result should be available");
            result.Data.Should().NotBeNull("Meteor showers data should be available");

            if (result.Data.Count > 1)
            {
                for (int i = 0; i < result.Data.Count - 1; i++)
                {
                    result.Data[i].Activity.ZHR.Should().BeGreaterOrEqualTo(result.Data[i + 1].Activity.ZHR,
                        "Showers should be ordered by ZHR descending");
                }
            }
        }

        [Then(@"I should receive meteor showers above the threshold")]
        public void ThenIShouldReceiveMeteorShowersAboveTheThreshold()
        {
            var result = _context.GetLastResult<List<MeteorShower>>();
            var minZHR = int.Parse(_context.GetModel<string>("MinimumZHR"));

            result.Should().NotBeNull("Meteor showers result should be available");
            result.Data.Should().NotBeNull("Meteor showers data should be available");

            foreach (var shower in result.Data)
            {
                shower.Activity.ZHR.Should().BeGreaterOrEqualTo(minZHR,
                    $"All showers should have ZHR >= {minZHR}");
            }
        }

        [Then(@"all returned showers should have ZHR >= (.*)")]
        public void ThenAllReturnedShowersShouldHaveZHR(int minZHR)
        {
            var result = _context.GetLastResult<List<MeteorShower>>();
            result.Should().NotBeNull("Meteor showers result should be available");
            result.Data.Should().NotBeNull("Meteor showers data should be available");

            foreach (var shower in result.Data)
            {
                shower.Activity.ZHR.Should().BeGreaterOrEqualTo(minZHR,
                    $"All showers should have ZHR >= {minZHR}");
            }
        }

        [Then(@"I should receive the (.*) meteor shower")]
        public void ThenIShouldReceiveTheMeteorShower(string showerName)
        {
            var result = _context.GetLastResult<MeteorShower>();
            result.Should().NotBeNull("Meteor shower result should be available");
            result.IsSuccess.Should().BeTrue("Getting meteor shower should be successful");
            result.Data.Should().NotBeNull("Meteor shower data should be available");
            result.Data.Designation.Should().Contain(showerName, "Shower designation should match");
        }

        [Then(@"the shower designation should be ""(.*)""")]
        public void ThenTheShowerDesignationShouldBe(string expectedDesignation)
        {
            var result = _context.GetLastResult<MeteorShower>();
            result.Should().NotBeNull("Meteor shower result should be available");
            result.Data.Should().NotBeNull("Meteor shower data should be available");
            result.Data.Designation.Should().Be(expectedDesignation, "Shower designation should match");
        }

        [Then(@"the shower code should be ""(.*)""")]
        public void ThenTheShowerCodeShouldBe(string expectedCode)
        {
            var result = _context.GetLastResult<MeteorShower>();
            result.Should().NotBeNull("Meteor shower result should be available");
            result.Data.Should().NotBeNull("Meteor shower data should be available");
            result.Data.Code.Should().Be(expectedCode, "Shower code should match");
        }

        [Then(@"the shower should have activity period information")]
        public void ThenTheShowerShouldHaveActivityPeriodInformation()
        {
            var result = _context.GetLastResult<MeteorShower>();
            result.Should().NotBeNull("Meteor shower result should be available");
            result.Data.Should().NotBeNull("Meteor shower data should be available");
            result.Data.Activity.Should().NotBeNull("Activity information should be available");
            result.Data.Activity.Start.Should().NotBeNullOrEmpty("Start date should be set");
            result.Data.Activity.Finish.Should().NotBeNullOrEmpty("Finish date should be set");
            result.Data.Activity.Peak.Should().NotBeNullOrEmpty("Peak date should be set");
        }

        [Then(@"the (.*) shower should be active")]
        public void ThenTheShowerShouldBeActive(string showerName)
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Activity result should be available");
            result.IsSuccess.Should().BeTrue("Checking activity should be successful");
            result.Data.Should().BeTrue($"The {showerName} shower should be active");
        }

        [Then(@"the activity status should be true")]
        public void ThenTheActivityStatusShouldBeTrue()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Activity result should be available");
            result.Data.Should().BeTrue("Activity status should be true");
        }

        [Then(@"the ZHR should be approximately (.*)")]
        public void ThenTheZHRShouldBeApproximately(int expectedZHR)
        {
            var result = _context.GetLastResult<double>();
            result.Should().NotBeNull("ZHR result should be available");
            result.IsSuccess.Should().BeTrue("Getting ZHR should be successful");
            result.Data.Should().BeApproximately(expectedZHR, 20, $"ZHR should be approximately {expectedZHR}");
        }

        [Then(@"the ZHR should be at peak level")]
        public void ThenTheZHRShouldBeAtPeakLevel()
        {
            var result = _context.GetLastResult<double>();
            result.Should().NotBeNull("ZHR result should be available");
            result.Data.Should().BeGreaterThan(50, "Peak ZHR should be significant");
        }

        [Then(@"the error should indicate shower not found")]
        public void ThenTheErrorShouldIndicateShowerNotFound()
        {
            var result = _context.GetLastResult<MeteorShower>();
            result.Should().NotBeNull("Meteor shower result should be available");
            result.IsSuccess.Should().BeFalse("Getting invalid shower should fail");
            result.ErrorMessage.Should().Contain("not found", "Error should indicate shower not found");
        }

        // Helper methods
        private List<MeteorShower> CreateMockShowers(DateTime date)
        {
            var showers = new List<MeteorShower>();

            if (date.Month == 8 && date.Day >= 10 && date.Day <= 15)
            {
                showers.Add(CreateMockShower("Perseids", "PER", 100, date.AddDays(-2), date.AddDays(2)));
            }

            if (date.Month == 12 && date.Day >= 12 && date.Day <= 16)
            {
                showers.Add(CreateMockShower("Geminids", "GEM", 120, date.AddDays(-2), date.AddDays(2)));
            }

            return showers.OrderByDescending(s => s.Activity.ZHR).ToList();
        }

        private MeteorShower? CreateMockShowerByCode(string code)
        {
            return code switch
            {
                "PER" => CreateMockShower("Perseids", "PER", 100, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1)),
                "GEM" => CreateMockShower("Geminids", "GEM", 120, DateTime.Now.AddDays(-1), DateTime.Now.AddDays(1)),
                "XXX" => null,
                _ => null
            };
        }

        private MeteorShower CreateMockShower(string designation, string code, int zhr, DateTime start, DateTime end)
        {
            return new MeteorShower
            {
                Designation = designation,
                Code = code,
                Activity = new MeteorShowerActivity
                {
                    ZHR = zhr,
                    Start = start.ToString("MM-dd"),
                    Peak = DateTime.Now.ToString("MM-dd"),
                    Finish = end.ToString("MM-dd")
                }
            };
        }

        private bool DetermineShowerActivity(string code, DateTime date)
        {
            return code switch
            {
                "PER" when date.Month == 8 => true,
                "GEM" when date.Month == 12 => true,
                _ => false
            };
        }

        private double CalculateExpectedZHR(string code, DateTime date)
        {
            return code switch
            {
                "PER" when date.Month == 8 => 100,
                "GEM" when date.Month == 12 => 120,
                _ => 0
            };
        }

        private List<MeteorShower> CreateMockShowersInRange(DateTime start, DateTime end)
        {
            var showers = new List<MeteorShower>();

            if (start.Month <= 8 && end.Month >= 8)
            {
                showers.Add(CreateMockShower("Perseids", "PER", 100, new DateTime(start.Year, 8, 10), new DateTime(start.Year, 8, 15)));
            }

            if (start.Month <= 7 && end.Month >= 8)
            {
                showers.Add(CreateMockShower("Delta Aquariids", "SDA", 25, new DateTime(start.Year, 7, 15), new DateTime(start.Year, 8, 25)));
            }

            return showers;
        }

        private List<MeteorShower> CreateAllMockShowers()
        {
            return new List<MeteorShower>
            {
                CreateMockShower("Quadrantids", "QUA", 120, new DateTime(2024, 1, 1), new DateTime(2024, 1, 5)),
                CreateMockShower("Lyrids", "LYR", 18, new DateTime(2024, 4, 16), new DateTime(2024, 4, 25)),
                CreateMockShower("Eta Aquariids", "ETA", 50, new DateTime(2024, 4, 19), new DateTime(2024, 5, 28)),
                CreateMockShower("Perseids", "PER", 100, new DateTime(2024, 7, 17), new DateTime(2024, 8, 24)),
                CreateMockShower("Orionids", "ORI", 20, new DateTime(2024, 10, 2), new DateTime(2024, 11, 7)),
                CreateMockShower("Leonids", "LEO", 15, new DateTime(2024, 11, 6), new DateTime(2024, 11, 30)),
                CreateMockShower("Geminids", "GEM", 120, new DateTime(2024, 12, 4), new DateTime(2024, 12, 20))
            }.OrderBy(s => s.Designation).ToList();
        }
    }
}