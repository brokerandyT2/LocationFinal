using FluentAssertions;
using Location.Core.Domain.Entities;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Entities
{
    [TestFixture]
    public class TipTypeTests
    {
        [Test]
        public void Constructor_WithValidName_ShouldCreateInstance()
        {
            // Arrange & Act
            var tipType = new TipType("Landscape Photography");

            // Assert
            tipType.Name.Should().Be("Landscape Photography");
            tipType.I8n.Should().Be("en-US");
            tipType.Tips.Should().BeEmpty();
        }

        [Test]
        public void Constructor_WithEmptyName_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new TipType("");

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value")
                .WithMessage("Name cannot be empty*");
        }

        [Test]
        public void Constructor_WithNullName_ShouldThrowException()
        {
            // Arrange & Act
            Action act = () => new TipType(null);

            // Assert
            act.Should().Throw<ArgumentException>()
                .WithParameterName("value");
        }

        [Test]
        public void SetLocalization_WithValidValue_ShouldUpdateProperty()
        {
            // Arrange
            var tipType = new TipType("Landscape Photography");

            // Act
            tipType.SetLocalization("fr-FR");

            // Assert
            tipType.I8n.Should().Be("fr-FR");
        }

        [Test]
        public void SetLocalization_WithNull_ShouldSetDefault()
        {
            // Arrange
            var tipType = new TipType("Landscape Photography");

            // Act
            tipType.SetLocalization(null);

            // Assert
            tipType.I8n.Should().Be("en-US");
        }

        [Test]
        public void AddTip_WithValidTip_ShouldAddToCollection()
        {
            // Arrange
            var tipType = CreateTipTypeWithId("Landscape Photography", 1);
            var tip = new Tip(1, "Golden Hour", "Use golden hour light");

            // Act
            tipType.AddTip(tip);

            // Assert
            tipType.Tips.Should().ContainSingle();
            tipType.Tips.Should().Contain(tip);
        }

        [Test]
        public void AddTip_WithNull_ShouldThrowException()
        {
            // Arrange
            var tipType = new TipType("Landscape Photography");

            // Act
            Action act = () => tipType.AddTip(null);

            // Assert
            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("tip");
        }

        [Test]
        public void AddTip_WithMismatchedTipTypeId_ShouldThrowException()
        {
            // Arrange
            var tipType = CreateTipTypeWithId("Landscape Photography", 1);
            var tip = new Tip(2, "Golden Hour", "Use golden hour light");

            // Act
            Action act = () => tipType.AddTip(tip);

            // Assert
            act.Should().Throw<InvalidOperationException>()
                .WithMessage("Tip type ID mismatch");
        }

        [Test]
        public void AddTip_WhenTipTypeIdIsZero_ShouldNotThrow()
        {
            // Arrange
            var tipType = new TipType("Landscape Photography");
            var tip = new Tip(1, "Golden Hour", "Use golden hour light");

            // Act
            Action act = () => tipType.AddTip(tip);

            // Assert
            act.Should().NotThrow();
        }

        [Test]
        public void RemoveTip_WithExistingTip_ShouldRemoveFromCollection()
        {
            // Arrange
            var tipType = CreateTipTypeWithId("Landscape Photography", 1);
            var tip = new Tip(1, "Golden Hour", "Use golden hour light");
            tipType.AddTip(tip);

            // Act
            tipType.RemoveTip(tip);

            // Assert
            tipType.Tips.Should().BeEmpty();
        }

        [Test]
        public void RemoveTip_WithNonExistingTip_ShouldNotAffectCollection()
        {
            // Arrange
            var tipType = CreateTipTypeWithId("Landscape Photography", 1);
            var tip1 = new Tip(1, "Golden Hour", "Use golden hour light");
            var tip2 = new Tip(1, "Blue Hour", "Use blue hour light");
            tipType.AddTip(tip1);

            // Act
            tipType.RemoveTip(tip2);

            // Assert
            tipType.Tips.Should().ContainSingle();
            tipType.Tips.Should().Contain(tip1);
        }

        [Test]
        public void Tips_ShouldReturnReadOnlyCollection()
        {
            // Arrange
            var tipType = new TipType("Landscape Photography");

            // Act
            var tips = tipType.Tips;

            // Assert
            tips.Should().BeAssignableTo<IReadOnlyCollection<Tip>>();
        }

        // Helper method to create TipType with specific Id using reflection
        private TipType CreateTipTypeWithId(string name, int id)
        {
            var tipType = new TipType(name);
            var idProperty = tipType.GetType().GetProperty("Id");
            idProperty.SetValue(tipType, id);
            return tipType;
        }
    }
}