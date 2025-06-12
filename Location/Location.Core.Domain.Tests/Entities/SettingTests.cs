using FluentAssertions;
using Location.Core.Domain.Entities;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Entities
{
    [TestFixture]
    public class SettingTests
    {
        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var setting = new Setting("theme", "dark", "User interface theme");

            // Assert
            setting.Key.Should().Be("theme");
            setting.Value.Should().Be("dark");
            setting.Description.Should().Be("User interface theme");
            setting.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        }

        [Test]
        public void Constructor_WithoutDescription_ShouldSetEmptyDescription()
        {
            // Arrange & Act
            var setting = new Setting("theme", "dark", "");

            // Assert
            setting.Description.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithEmptyKey_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Setting("", "value", "huh");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value")
                .WithMessage("Key cannot be empty*");
        }

        [Test]
        public void Constructor_WithNullKey_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Setting(null, "value", "huh");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value");
        }

        [Test]
        public void Constructor_WithNullValue_ShouldSetEmptyString()
        {
            // Arrange & Act
            var setting = new Setting("key", null, "huh");

            // Assert
            setting.Value.Should().BeEmpty();
        }

        [Test]
        public void UpdateValue_ShouldUpdateValueAndTimestamp()
        {
            // Arrange
            var setting = new Setting("theme", "light", "huh");
            var originalTimestamp = setting.Timestamp;
            System.Threading.Thread.Sleep(100); // Ensure some time passes

            // Act
            setting.UpdateValue("dark");

            // Assert
            setting.Value.Should().Be("dark");
            setting.Timestamp.Should().BeAfter(originalTimestamp);
        }

        [Test]
        public void UpdateValue_WithNull_ShouldSetEmptyString()
        {
            // Arrange
            var setting = new Setting("theme", "light", "huh");

            // Act
            setting.UpdateValue(null);

            // Assert
            setting.Value.Should().BeEmpty();
        }

        [TestCase("true", true)]
        [TestCase("True", true)]
        [TestCase("TRUE", true)]
        [TestCase("false", false)]
        [TestCase("False", false)]
        [TestCase("FALSE", false)]
        public void GetBooleanValue_WithValidBoolean_ShouldReturnCorrectValue(string value, bool expected)
        {
            // Arrange
            var setting = new Setting("enable_feature", value, "huh");

            // Act
            var result = setting.GetBooleanValue();

            // Assert
            result.Should().Be(expected);
        }

        [TestCase("")]
        [TestCase("yes")]
        [TestCase("1")]
        [TestCase("invalid")]
        public void GetBooleanValue_WithInvalidBoolean_ShouldReturnFalse(string value)
        {
            // Arrange
            var setting = new Setting("enable_feature", value, "huh");

            // Act
            var result = setting.GetBooleanValue();

            // Assert
            result.Should().BeFalse();
        }

        [TestCase("42", 42)]
        [TestCase("-10", -10)]
        [TestCase("0", 0)]
        public void GetIntValue_WithValidInteger_ShouldReturnCorrectValue(string value, int expected)
        {
            // Arrange
            var setting = new Setting("count", value, "huh");

            // Act
            var result = setting.GetIntValue();

            // Assert
            result.Should().Be(expected);
        }

        [TestCase("invalid", 0)]
        [TestCase("12.5", 0)]
        [TestCase("", 0)]
        public void GetIntValue_WithInvalidInteger_ShouldReturnDefaultValue(string value, int expected)
        {
            // Arrange
            var setting = new Setting("count", value, "huh");

            // Act
            var result = setting.GetIntValue();

            // Assert
            result.Should().Be(expected);
        }

        [Test]
        public void GetIntValue_WithCustomDefaultValue_ShouldReturnCustomDefault()
        {
            // Arrange
            var setting = new Setting("count", "invalid", "huh");

            // Act
            var result = setting.GetIntValue(99);

            // Assert
            result.Should().Be(99);
        }

        [Test]
        public void GetDateTimeValue_WithValidDateTime_ShouldReturnCorrectValue()
        {
            // Arrange
            var dateTimeString = "2024-01-15 14:30:00";
            var setting = new Setting("last_sync", dateTimeString, "huh");

            // Act
            var result = setting.GetDateTimeValue();

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be(DateTime.Parse(dateTimeString));
        }

        [TestCase("invalid")]
        [TestCase("")]
        [TestCase("2024-13-45")] // Invalid date
        public void GetDateTimeValue_WithInvalidDateTime_ShouldReturnNull(string value)
        {
            // Arrange
            var setting = new Setting("last_sync", value, "huh");

            // Act
            var result = setting.GetDateTimeValue();

            // Assert
            result.Should().BeNull();
        }
    }
}