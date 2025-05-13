using NUnit.Framework;
using FluentAssertions;
using Location.Core.Domain.Entities;
using System;

namespace Location.Core.Domain.Tests.Entities
{
    [TestFixture]
    public class TipTests
    {
        [Test]
        public void Constructor_WithValidValues_ShouldCreateInstance()
        {
            // Arrange & Act
            var tip = new Tip(1, "Golden Hour", "The best light for photography occurs during golden hour");

            // Assert
            tip.TipTypeId.Should().Be(1);
            tip.Title.Should().Be("Golden Hour");
            tip.Content.Should().Be("The best light for photography occurs during golden hour");
            tip.I8n.Should().Be("en-US");
            tip.Fstop.Should().BeEmpty();
            tip.ShutterSpeed.Should().BeEmpty();
            tip.Iso.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithEmptyTitle_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Tip(1, "", "Content");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value")
                .WithMessage("Title cannot be empty*");
        }

        [Test]
        public void Constructor_WithNullTitle_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new Tip(1, null, "Content");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value");
        }

        [Test]
        public void UpdatePhotographySettings_ShouldUpdateProperties()
        {
            // Arrange
            var tip = new Tip(1, "Title", "Content");

            // Act
            tip.UpdatePhotographySettings("f/2.8", "1/500", "ISO 100");

            // Assert
            tip.Fstop.Should().Be("f/2.8");
            tip.ShutterSpeed.Should().Be("1/500");
            tip.Iso.Should().Be("ISO 100");
        }

        [Test]
        public void UpdatePhotographySettings_WithNullValues_ShouldSetEmptyStrings()
        {
            // Arrange
            var tip = new Tip(1, "Title", "Content");
            tip.UpdatePhotographySettings("f/2.8", "1/500", "ISO 100");

            // Act
            tip.UpdatePhotographySettings(null, null, null);

            // Assert
            tip.Fstop.Should().BeEmpty();
            tip.ShutterSpeed.Should().BeEmpty();
            tip.Iso.Should().BeEmpty();
        }

        [Test]
        public void UpdateContent_WithValidValues_ShouldUpdateProperties()
        {
            // Arrange
            var tip = new Tip(1, "Original Title", "Original Content");

            // Act
            tip.UpdateContent("New Title", "New Content");

            // Assert
            tip.Title.Should().Be("New Title");
            tip.Content.Should().Be("New Content");
        }

        [Test]
        public void UpdateContent_WithEmptyTitle_ShouldThrowException()
        {
            // Arrange
            var tip = new Tip(1, "Original Title", "Original Content");

            // Act
            Action act = () => tip.UpdateContent("", "New Content");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value");
        }

        [Test]
        public void UpdateContent_WithNullContent_ShouldSetEmptyString()
        {
            // Arrange
            var tip = new Tip(1, "Original Title", "Original Content");

            // Act
            tip.UpdateContent("New Title", null);

            // Assert
            tip.Title.Should().Be("New Title");
            tip.Content.Should().BeEmpty();
        }

        [Test]
        public void SetLocalization_WithValidValue_ShouldUpdateProperty()
        {
            // Arrange
            var tip = new Tip(1, "Title", "Content");

            // Act
            tip.SetLocalization("es-ES");

            // Assert
            tip.I8n.Should().Be("es-ES");
        }

        [Test]
        public void SetLocalization_WithNull_ShouldSetDefault()
        {
            // Arrange
            var tip = new Tip(1, "Title", "Content");

            // Act
            tip.SetLocalization(null);

            // Assert
            tip.I8n.Should().Be("en-US");
        }
    }
}