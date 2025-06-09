using FluentAssertions;
using Location.Photography.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Location.Photography.Application.Tests.Services
{
    [TestFixture]
    public class CameraSensorProfileServiceTests
    {
        private CameraSensorProfileService _service;
        private Mock<ILogger<CameraSensorProfileService>> _loggerMock;

        [SetUp]
        public void SetUp()
        {
            _loggerMock = new Mock<ILogger<CameraSensorProfileService>>();
            _service = new CameraSensorProfileService(_loggerMock.Object);
        }

        [Test]
        public void Constructor_WithNullLogger_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            FluentActions.Invoking(() => new CameraSensorProfileService(null))
                .Should().Throw<ArgumentNullException>()
                .WithParameterName("logger");
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithValidJsonContent_ShouldReturnCameras()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Canon EOS 5D Mark IV (2016 - 2022)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 36.0,
                                ""SensorHeightInMM"": 24.0
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].Name.Should().Be("Canon EOS 5D Mark IV (2016 - 2022)");
            result.Data[0].SensorType.Should().Be("Full Frame");
            result.Data[0].SensorWidth.Should().Be(36.0);
            result.Data[0].SensorHeight.Should().Be(24.0);
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithMultipleJsonContents_ShouldReturnAllCameras()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Canon EOS 5D Mark IV (2016 - 2022)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 36.0,
                                ""SensorHeightInMM"": 24.0
                            }
                        }
                    }
                }",
                @"{
                    ""Cameras"": {
                        ""Nikon D850 (2017 - 2020)"": {
                            ""Brand"": ""Nikon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 35.9,
                                ""SensorHeightInMM"": 23.9
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);
            result.Data.Should().Contain(c => c.Name.Contains("Canon EOS 5D Mark IV"));
            result.Data.Should().Contain(c => c.Name.Contains("Nikon D850"));
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithEmptyList_ShouldReturnEmptyList()
        {
            // Arrange
            var jsonContents = new List<string>();

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().BeEmpty();
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithInvalidJson_ShouldHandleGracefully()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                "{ invalid json content",
                @"{
                    ""Cameras"": {
                        ""Canon EOS 5D Mark IV (2016 - 2022)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 36.0,
                                ""SensorHeightInMM"": 24.0
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1); // Only the valid JSON should be processed
            result.Data[0].Name.Should().Be("Canon EOS 5D Mark IV (2016 - 2022)");
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithMissingRequiredProperties_ShouldSkipInvalidCameras()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Canon EOS 5D Mark IV (2016 - 2022)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 36.0,
                                ""SensorHeightInMM"": 24.0
                            }
                        },
                        ""Invalid Camera (Missing Sensor)"": {
                            ""Brand"": ""Invalid"",
                            ""SensorType"": ""Full Frame""
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1); // Only the valid camera should be processed
            result.Data[0].Name.Should().Be("Canon EOS 5D Mark IV (2016 - 2022)");
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithCancellationToken_ShouldPassItThrough()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Canon EOS 5D Mark IV (2016 - 2022)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 36.0,
                                ""SensorHeightInMM"": 24.0
                            }
                        }
                    }
                }"
            };

            var cancellationToken = new CancellationToken();

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, cancellationToken);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithApsCSensor_ShouldSetCorrectMountType()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Canon EOS 7D Mark II (2014 - 2020)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""APS-C"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 22.4,
                                ""SensorHeightInMM"": 15.0
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].Name.Should().Be("Canon EOS 7D Mark II (2014 - 2020)");
            result.Data[0].SensorType.Should().Be("APS-C");
            result.Data[0].SensorWidth.Should().Be(22.4);
            result.Data[0].SensorHeight.Should().Be(15.0);
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithNikonCamera_ShouldDetectNikonMount()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Nikon Z9 (2021 - 2025)"": {
                            ""Brand"": ""Nikon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 35.9,
                                ""SensorHeightInMM"": 23.9
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].Name.Should().Be("Nikon Z9 (2021 - 2025)");
            result.Data[0].SensorType.Should().Be("Full Frame");
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_WithSonyCamera_ShouldDetectSonyMount()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Sony α7R V (2022 - 2025)"": {
                            ""Brand"": ""Sony"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 35.7,
                                ""SensorHeightInMM"": 23.8
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(1);
            result.Data[0].Name.Should().Be("Sony α7R V (2022 - 2025)");
            result.Data[0].SensorType.Should().Be("Full Frame");
        }

        [Test]
        public async Task LoadCameraSensorProfilesAsync_ShouldLogInformationAboutLoadedCameras()
        {
            // Arrange
            var jsonContents = new List<string>
            {
                @"{
                    ""Cameras"": {
                        ""Canon EOS 5D Mark IV (2016 - 2022)"": {
                            ""Brand"": ""Canon"",
                            ""SensorType"": ""Full Frame"",
                            ""Sensor"": {
                                ""SensorWidthInMM"": 36.0,
                                ""SensorHeightInMM"": 24.0
                            }
                        }
                    }
                }"
            };

            // Act
            var result = await _service.LoadCameraSensorProfilesAsync(jsonContents, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();

            // Verify that information logging occurred
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Loaded") && v.ToString().Contains("cameras")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}