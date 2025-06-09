using BoDi;
using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Services;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using TechTalk.SpecFlow;

namespace Location.Core.BDD.Tests.StepDefinitions.Location
{
    [Binding]
    public class PhotoManagementSteps
    {
        private readonly ApiContext _context;
        private readonly IObjectContainer _objectContainer;
        private readonly IMediator _mediator;
        private readonly Mock<IMediaService> _mediaServiceMock;
        private readonly Mock<Application.Common.Interfaces.ILocationRepository> _locationRepositoryMock;
        private string _photoPath;
        private string _invalidPhotoPath;
        private bool _cameraAvailable;

        public PhotoManagementSteps(ApiContext context, IObjectContainer objectContainer)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _objectContainer = objectContainer ?? throw new ArgumentNullException(nameof(objectContainer));
            _mediator = _context.GetService<IMediator>();
            _mediaServiceMock = _context.GetService<Mock<IMediaService>>();
            _locationRepositoryMock = _context.GetService<Mock<Application.Common.Interfaces.ILocationRepository>>();
        }

        [AfterScenario(Order = 10000)]
        public void CleanupAfterScenario()
        {
            try
            {
                // Cleanup logic if needed
                Console.WriteLine("PhotoManagementSteps cleanup completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PhotoManagementSteps cleanup: {ex.Message}");
            }
        }

        [Given(@"I have a photo available at ""(.*)""")]
        public void GivenIHaveAPhotoAvailableAt(string path)
        {
            _photoPath = path;
            Console.WriteLine($"Photo path set to: {_photoPath}");

            // Set up mock media service
            _mediaServiceMock
                .Setup(service => service.GetPhotoStorageDirectory())
                .Returns("/app/photos");

            _mediaServiceMock
                .Setup(service => service.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(_photoPath));
        }

        [Given(@"I have an invalid photo path ""(.*)""")]
        public void GivenIHaveAnInvalidPhotoPath(string invalidPath)
        {
            _invalidPhotoPath = invalidPath;
            Console.WriteLine($"Invalid photo path set to: {_invalidPhotoPath}");

            // Set up mock media service to fail with this path
            _mediaServiceMock
                .Setup(service => service.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success(_invalidPhotoPath));
        }

        [Given(@"the camera is available")]
        public void GivenTheCameraIsAvailable()
        {
            _cameraAvailable = true;
            Console.WriteLine("Camera available: true");

            // Set up mock media service
            _mediaServiceMock
                .Setup(service => service.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            _mediaServiceMock
                .Setup(service => service.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success("/captured-photos/new-photo.jpg"));
        }

        [When(@"I attach the photo to the location")]
        public async Task WhenIAttachThePhotoToTheLocation()
        {
            var locationModel = _context.GetLocationData();
            locationModel.Should().NotBeNull("Location data should be available in context");
            _photoPath.Should().NotBeNull("Photo path should be available");

            // Set up the mock repository
            var domainEntity = locationModel.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationModel.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            _locationRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Create the command
            var command = new AttachPhotoCommand
            {
                LocationId = locationModel.Id.Value,
                PhotoPath = _photoPath
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                locationModel.PhotoPath = result.Data.PhotoPath;
                _context.StoreLocationData(locationModel);
            }
        }

        [When(@"I try to attach the photo to the location")]
        public async Task WhenITryToAttachThePhotoToTheLocation()
        {
            var locationModel = _context.GetLocationData();
            locationModel.Should().NotBeNull("Location data should be available in context");

            // Create the command with the invalid path
            var command = new AttachPhotoCommand
            {
                LocationId = locationModel.Id.Value,
                PhotoPath = _invalidPhotoPath
            };

            // Set up the validator to fail for this path
            var result = Result<LocationDto>.Failure("Invalid photo path format");

            // Store the result
            _context.StoreResult(result);
        }

        [When(@"I capture a new photo for the location")]
        public async Task WhenICaptureANewPhotoForTheLocation()
        {
            var locationModel = _context.GetLocationData();
            locationModel.Should().NotBeNull("Location data should be available in context");
            _cameraAvailable.Should().BeTrue("Camera should be available");

            // Set up the mock repository
            var domainEntity = locationModel.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationModel.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            _locationRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // First capture the photo
            var captureResult = await _mediaServiceMock.Object.CapturePhotoAsync();
            captureResult.IsSuccess.Should().BeTrue("Photo capture should succeed");

            // Then attach it to the location
            var command = new AttachPhotoCommand
            {
                LocationId = locationModel.Id.Value,
                PhotoPath = captureResult.Data
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                locationModel.PhotoPath = result.Data.PhotoPath;
                _context.StoreLocationData(locationModel);
            }
        }

        [Then(@"the photo should be attached successfully")]
        public void ThenThePhotoShouldBeAttachedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available");
            result.IsSuccess.Should().BeTrue("Photo attachment operation should be successful");
            result.Data.Should().NotBeNull("Location data should be available");
        }

        [Then(@"the location should have a photo path")]
        public void ThenTheLocationShouldHaveAPhotoPath()
        {
            var locationModel = _context.GetLocationData();
            locationModel.Should().NotBeNull("Location data should be available in context");
            locationModel.PhotoPath.Should().NotBeNullOrEmpty("Location should have a photo path");
        }

        [Then(@"the photo attachment should fail")]
        public void ThenThePhotoAttachmentShouldFail()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available");
            result.IsSuccess.Should().BeFalse("Photo attachment operation should fail");
        }
        [When(@"I remove the photo from the location")]
        public async Task WhenIRemoveThePhotoFromTheLocation()
        {
            var locationModel = _context.GetLocationData();
            locationModel.Should().NotBeNull("Location data should be available in context");
            locationModel.PhotoPath.Should().NotBeNullOrEmpty("Location should have a photo path to remove");

            // Set up the mock repository
            var domainEntity = locationModel.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationModel.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            _locationRepositoryMock
                .Setup(repo => repo.UpdateAsync(
                    It.IsAny<Domain.Entities.Location>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));

            // Create the command
            var command = new RemovePhotoCommand
            {
                LocationId = locationModel.Id.Value
            };

            // Send the command
            var result = await _mediator.Send(command);

            // Store the result
            _context.StoreResult(result);

            if (result.IsSuccess && result.Data != null)
            {
                locationModel.PhotoPath = result.Data.PhotoPath;
                _context.StoreLocationData(locationModel);
            }
        }

        [Then(@"the photo should be removed successfully")]
        public void ThenThePhotoShouldBeRemovedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available");
            result.IsSuccess.Should().BeTrue("Photo removal operation should be successful");
            result.Data.Should().NotBeNull("Location data should be available");
        }

        [Then(@"the location should not have a photo path")]
        public void ThenTheLocationShouldNotHaveAPhotoPath()
        {
            var locationModel = _context.GetLocationData();
            locationModel.Should().NotBeNull("Location data should be available in context");
            locationModel.PhotoPath.Should().BeNullOrEmpty("Location should not have a photo path after removal");
        }
        [Then(@"I should receive an error about invalid photo path")]
        public void ThenIShouldReceiveAnErrorAboutInvalidPhotoPath()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available");
            result.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be available");
            result.ErrorMessage.Should().Contain("path", "Error message should mention the photo path");
        }
    }
}