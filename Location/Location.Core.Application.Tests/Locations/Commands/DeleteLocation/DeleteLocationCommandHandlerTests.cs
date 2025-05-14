using NUnit.Framework;
using FluentAssertions;
using Moq;
using System.Threading;
using System.Threading.Tasks;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Helpers;
using MediatR;
using Location.Core.Application.Common.Interfaces.Persistence;

namespace Location.Core.Application.Tests.Locations.Commands.DeleteLocation
{
    [TestFixture]
    public class DeleteLocationCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<IEventBus> _eventBusMock;
        private DeleteLocationCommandHandler _handler;

        [SetUp]
        public void Setup()
        {
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _eventBusMock = new Mock<IEventBus>();

            // Setup mock repository
            var locationRepoMock = new Mock<ILocationRepository>();
            _unitOfWorkMock.Setup(x => x.Locations).Returns(locationRepoMock.Object);

            _handler = new DeleteLocationCommandHandler(
                _unitOfWorkMock.Object,
                _eventBusMock.Object
            );
        }

        [Test]
        public async Task Handle_WithExistingLocation_ShouldSoftDelete()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var existingLocation = TestDataBuilder.CreateValidLocation();

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLocation);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.Locations.Delete(existingLocation), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithNonExistentLocation_ShouldReturnNotFound()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 999 };

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(999, It.IsAny<CancellationToken>()))
                .ReturnsAsync((Domain.Entities.Location?)null);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "NOT_FOUND");
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        }

        [Test]
        public async Task Handle_WithDomainEvents_ShouldPublishEvents()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var existingLocation = TestDataBuilder.CreateValidLocation();
            existingLocation.Delete(); // This should add LocationDeletedEvent

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLocation);

            // Act
            await _handler.Handle(command, CancellationToken.None);

            // Assert
            _eventBusMock.Verify(x => x.PublishAllAsync(
                It.IsAny<Domain.Interfaces.IDomainEvent[]>(),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithAlreadyDeletedLocation_ShouldStillSucceed()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var existingLocation = TestDataBuilder.CreateValidLocation();
            existingLocation.Delete(); // Already deleted

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLocation);

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }

        [Test]
        public async Task Handle_WithDatabaseException_ShouldReturnDatabaseError()
        {
            // Arrange
            var command = new DeleteLocationCommand { Id = 1 };
            var existingLocation = TestDataBuilder.CreateValidLocation();

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, It.IsAny<CancellationToken>()))
                .ReturnsAsync(existingLocation);

            _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
                .ThrowsAsync(new System.Exception("Database error"));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeFalse();
            result.Errors.Should().ContainSingle();
            result.Errors.Should().Contain(x => x.Code == "DATABASE_ERROR");
        }

        [Test]
        public async Task Handle_WithCancellationToken_ShouldPassThrough()
        {
            // Arrange
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            var command = new DeleteLocationCommand { Id = 1 };
            var existingLocation = TestDataBuilder.CreateValidLocation();

            _unitOfWorkMock.Setup(x => x.Locations.GetByIdAsync(1, token))
                .ReturnsAsync(existingLocation);

            // Act
            await _handler.Handle(command, token);

            // Assert
            _unitOfWorkMock.Verify(x => x.Locations.GetByIdAsync(1, token), Times.Once);
            _unitOfWorkMock.Verify(x => x.SaveChangesAsync(token), Times.Once);
        }
    }

    // Placeholder for the actual implementation
    public class DeleteLocationCommandHandler : IRequestHandler<DeleteLocationCommand, Result>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IEventBus _eventBus;

        public DeleteLocationCommandHandler(IUnitOfWork unitOfWork, IEventBus eventBus)
        {
            _unitOfWork = unitOfWork;
            _eventBus = eventBus;
        }

        public async Task<Result> Handle(DeleteLocationCommand request, CancellationToken cancellationToken)
        {
            try
            {
                var location = await _unitOfWork.Locations.GetByIdAsync(request.Id, cancellationToken);

                if (location == null)
                {
                    return Result.Failure(Error.NotFound($"Location with ID {request.Id} not found"));
                }

                // Soft delete
                location.Delete();
                _unitOfWork.Locations.Delete(location);
                await _unitOfWork.SaveChangesAsync(cancellationToken);

                // Publish domain events
                if (location.DomainEvents.Count > 0)
                {
                    await _eventBus.PublishAllAsync(location.DomainEvents.ToArray(), cancellationToken);
                    location.ClearDomainEvents();
                }

                return Result.Success();
            }
            catch (System.Exception ex)
            {
                return Result.Failure(Error.Database($"Failed to delete location: {ex.Message}"));
            }
        }
    }
}