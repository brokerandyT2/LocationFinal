using FluentAssertions;
using Location.Core.Domain.Common;
using NUnit.Framework;

namespace Location.Core.Domain.Tests.Common
{
    [TestFixture]
    public class EntityTests
    {
        private class TestEntity : Entity
        {
            public TestEntity(int id)
            {
                Id = id;
            }
        }

        [Test]
        public void Entity_WithDefaultId_ShouldBeTransient()
        {
            // Arrange
            var entity = new TestEntity(0);

            // Act
            var isTransient = entity.IsTransient();

            // Assert
            isTransient.Should().BeTrue();
        }

        [Test]
        public void Entity_WithNonDefaultId_ShouldNotBeTransient()
        {
            // Arrange
            var entity = new TestEntity(1);

            // Act
            var isTransient = entity.IsTransient();

            // Assert
            isTransient.Should().BeFalse();
        }

        [Test]
        public void Equals_WithSameId_ShouldReturnTrue()
        {
            // Arrange
            var entity1 = new TestEntity(1);
            var entity2 = new TestEntity(1);

            // Act
            var result = entity1.Equals(entity2);

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void Equals_WithDifferentId_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new TestEntity(1);
            var entity2 = new TestEntity(2);

            // Act
            var result = entity1.Equals(entity2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void Equals_WithNull_ShouldReturnFalse()
        {
            // Arrange
            var entity = new TestEntity(1);

            // Act
            var result = entity.Equals(null);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void Equals_WithDifferentType_ShouldReturnFalse()
        {
            // Arrange
            var entity = new TestEntity(1);
            var other = new object();

            // Act
            var result = entity.Equals(other);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void Equals_WithTransientEntities_ShouldReturnFalse()
        {
            // Arrange
            var entity1 = new TestEntity(0);
            var entity2 = new TestEntity(0);

            // Act
            var result = entity1.Equals(entity2);

            // Assert
            result.Should().BeFalse();
        }

        [Test]
        public void GetHashCode_WithSameId_ShouldReturnSameValue()
        {
            // Arrange
            var entity1 = new TestEntity(1);
            var entity2 = new TestEntity(1);

            // Act
            var hash1 = entity1.GetHashCode();
            var hash2 = entity2.GetHashCode();

            // Assert
            hash1.Should().Be(hash2);
        }

        [Test]
        public void EqualsOperator_WithSameId_ShouldReturnTrue()
        {
            // Arrange
            var entity1 = new TestEntity(1);
            var entity2 = new TestEntity(1);

            // Act
            var result = entity1 == entity2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void NotEqualsOperator_WithDifferentId_ShouldReturnTrue()
        {
            // Arrange
            var entity1 = new TestEntity(1);
            var entity2 = new TestEntity(2);

            // Act
            var result = entity1 != entity2;

            // Assert
            result.Should().BeTrue();
        }

        [Test]
        public void EqualsOperator_WithNull_ShouldHandleCorrectly()
        {
            // Arrange
            TestEntity? entity1 = null;
            TestEntity? entity2 = null;
            var entity3 = new TestEntity(1);

            // Act & Assert
            (entity1 == entity2).Should().BeTrue();
            (entity1 == entity3).Should().BeFalse();
            (entity3 == entity1).Should().BeFalse();
        }
    }
}