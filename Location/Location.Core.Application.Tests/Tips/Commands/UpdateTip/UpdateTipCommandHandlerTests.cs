using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Location.Core.Application.Tips.Commands.UpdateTip;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Tests.Utilities;
using MediatR;
using Moq;
using NUnit.Framework;

namespace Location.Core.Application.Tests.Tips.Commands.UpdateTip
{
    [Category("Tips")]
    [Category("Update")]
    [TestFixture]
    public class UpdateTipCommandHandlerTests
    {
        private Mock<ITipRepository> _tipRepositoryMock;
        private Mock<IMediator> _mediatorMock;
        private UpdateTipCommandHandler _handler;

        [SetUp]
        public void SetUp()
        {
            _tipRepositoryMock = new Mock<ITipRepository>();
            _mediatorMock = new Mock<IMediator>();
            _handler = new UpdateTipCommandHandler(_tipRepositoryMock.Object, _mediatorMock.Object);
        }

        [Test]
        public async Task Handle_WithValidUpdate_ShouldUpdateTip()
        {
            // Arrange
            var command = new UpdateTipCommand
            {
                Id = 1,
                TipTypeId = 2,
                Title = "Updated Rule of Thirds",
                Content = "Updated content about composition",
                Fstop = "f/11",
                ShutterSpeed = "1/250",
                Iso = "ISO 200",
                I8n = "es-ES"
            };

            var existingTip = TestDataBuilder.CreateValidTip(1);
            // Fix: Set the ID property explicitly using reflection
            var idProperty = typeof(Domain.Entities.Tip).GetProperty("Id",
                System.Reflection.BindingFlags.Public |
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            idProperty.SetValue(existingTip, 1);

            _tipRepositoryMock
                .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            _tipRepositoryMock
                .Setup(x => x.UpdateAsync(It.IsAny<Domain.Entities.Tip>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Tip>.Success(existingTip));

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            result.IsSuccess.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Id.Should().Be(command.Id);
            result.Data.Title.Should().Be(command.Title);
            result.Data.Content.Should().Be(command.Content);
            result.Data.Fstop.Should().Be(command.Fstop);
            result.Data.ShutterSpeed.Should().Be(command.ShutterSpeed);
            result.Data.Iso.Should().Be(command.Iso);
            result.Data.I8n.Should().Be(command.I8n);
        }
    }
}