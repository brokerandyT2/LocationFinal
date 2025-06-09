using FluentAssertions;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using Location.Core.Application.Tips.Commands.CreateTip;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.CreateTip
{
    [Category("Tips")]
    [Category("Create")]
    [TestFixture]
    public class CreateTipCommandHandlerTests
    {
        private Mock<IUnitOfWork> _unitOfWorkMock;
        private Mock<ITipRepository> _tipRepositoryMock;
        private CreateTipCommandHandler _handler;
        private Mock<IMediator> _mediatorMock;
        [SetUp]
        public void SetUp()
        {
            _mediatorMock = new Mock<IMediator>();
            _unitOfWorkMock = new Mock<IUnitOfWork>();
            _tipRepositoryMock = new Mock<ITipRepository>();

            _unitOfWorkMock.Setup(u => u.Tips).Returns(_tipRepositoryMock.Object);

            _handler = new CreateTipCommandHandler(_unitOfWorkMock.Object, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_ShouldReturnCreatedTipId()
        {
            // Arrange
            var command = new CreateTipCommand
            {
                TipTypeId = 1,
                Title = "Test Tip",
                Content = "Test content"
            };

            // Create a tip with ID 42 explicitly
            var createdTip = TestDataBuilder.CreateValidTip(1);
            // Fix: Set the ID property using reflection since it might be protected/private
            var idProperty = typeof(Domain.Entities.Tip).GetProperty("Id",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            idProperty.SetValue(createdTip, 42);

            _tipRepositoryMock
                .Setup(x => x.CreateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(createdTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data[0].Id.Should().Be(42);
        }
    }
}