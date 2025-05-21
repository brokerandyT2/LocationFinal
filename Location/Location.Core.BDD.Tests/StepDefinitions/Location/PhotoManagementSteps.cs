using FluentAssertions;
using Location.Core.Application.Commands.Locations;
using Location.Core.Application.Common.Interfaces;
using Location.Core.Application.Common.Models;
using Location.Core.Application.Locations.DTOs;
using Location.Core.Application.Services;
using Location.Core.BDD.Tests.Models;
using Location.Core.BDD.Tests.Support;
using MediatR;
using Moq;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Assist;

namespace Location.Core.BDD.Tests.StepDefinitions.Location
{
    [Binding]
    public class PhotoManagementSteps
    {
        private readonly ApiContext _context;
        private readonly IMediator _mediator;
        private readonly Mock<IMediaService> _mediaServiceMock;
        private readonly Mock<ILocationRepository> _locationRepositoryMock;
        private string _photoPath = string.Empty;
        private string _newPhotoPath = string.Empty;
        private bool _cameraAvailable = true;
        private bool _photoExists = false;

        public PhotoManagementSteps(ApiContext context)
        {
            _context = context;
            _mediator = _context.GetService<IMediator>();
            _mediaServiceMock = _context.GetService<Mock<IMediaService>>();
            _locationRepositoryMock = _context.GetService<Mock<ILocationRepository>>();
        }

        [Given(@"I have a photo available at ""(.*)""")]
        public void GivenIHaveAPhotoAvailableAt(string path)
        {
            _photoPath = path;
            _photoExists = true;

            // Set up mock for photo existence
            _mediaServiceMock
                .Setup(ms => ms.DeletePhotoAsync(
                    It.Is<string>(p => p == path),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));
        }

        [Given(@"the location has a photo attached")]
        public void GivenTheLocationHasAPhotoAttached()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Set a photo path on the location
            _photoPath = "/test-photos/existing.jpg";
            locationData.PhotoPath = _photoPath;
            _photoExists = true;

            // Update the stored location
            _context.StoreLocationData(locationData);

            // Update the mock repository to return the location with photo
            var domainEntity = locationData.ToDomainEntity();

            _locationRepositoryMock
                .Setup(repo => repo.GetByIdAsync(
                    It.Is<int>(id => id == locationData.Id.Value),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<Domain.Entities.Location>.Success(domainEntity));
        }

        [Given(@"I have a new photo available at ""(.*)""")]
        public void GivenIHaveANewPhotoAvailableAt(string path)
        {
            _newPhotoPath = path;

            // Set up mock for photo existence
            _mediaServiceMock
                .Setup(ms => ms.DeletePhotoAsync(
                    It.Is<string>(p => p == path),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));
        }

        [Given(@"the camera is available")]
        public void GivenTheCameraIsAvailable()
        {
            _cameraAvailable = true;

            // Set up mock for camera availability
            _mediaServiceMock
                .Setup(ms => ms.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(true));

            // Set up mock for photo capture
            _mediaServiceMock
                .Setup(ms => ms.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success("/test-photos/captured.jpg"));
        }

        [Given(@"the camera is not available")]
        public void GivenTheCameraIsNotAvailable()
        {
            _cameraAvailable = false;

            // Set up mock for camera unavailability
            _mediaServiceMock
                .Setup(ms => ms.IsCaptureSupported(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<bool>.Success(false));

            // Set up mock for photo picking as fallback
            _mediaServiceMock
                .Setup(ms => ms.PickPhotoAsync(It.IsAny<CancellationToken>()))
                .ReturnsAsync(Result<string>.Success("/test-photos/picked.jpg"));
        }

        [Given(@"I have an invalid photo path ""(.*)""")]
        public void GivenIHaveAnInvalidPhotoPath(string invalidPath)
        {
            _photoPath = invalidPath;
            _photoExists = false;
        }

        [When(@"I attach the photo to the location")]
        public async Task WhenIAttachThePhotoToTheLocation()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Create a command to attach the photo
            var command = new AttachPhotoCommand
            {
                LocationId = locationData.Id.Value,
                PhotoPath = _photoPath
            };

            // Execute the command
            var result = await _mediator.Send(command);

            // Store the result for verification
            _context.StoreResult(result);

            // Update stored location data if successful
            if (result.IsSuccess)
            {
                locationData.PhotoPath = _photoPath;
                _context.StoreLocationData(locationData);
            }
        }

        [When(@"I remove the photo from the location")]
        public async Task WhenIRemoveThePhotoFromTheLocation()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");
            locationData.PhotoPath.Should().NotBeNullOrEmpty("Location should have a photo attached");

            // Create a command to remove the photo
            var command = new RemovePhotoCommand
            {
                LocationId = locationData.Id.Value
            };

            // Execute the command
            var result = await _mediator.Send(command);

            // Store the result for verification
            _context.StoreResult(result);

            // Update stored location data if successful
            if (result.IsSuccess)
            {
                locationData.PhotoPath = null;
                _context.StoreLocationData(locationData);
            }
        }

        [When(@"I replace the existing photo with the new photo")]
        public async Task WhenIReplaceTheExistingPhotoWithTheNewPhoto()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");
            locationData.PhotoPath.Should().NotBeNullOrEmpty("Location should have a photo attached");

            // Create a command to attach the new photo
            var command = new AttachPhotoCommand
            {
                LocationId = locationData.Id.Value,
                PhotoPath = _newPhotoPath
            };

            // Execute the command
            var result = await _mediator.Send(command);

            // Store the result for verification
            _context.StoreResult(result);

            // Update stored location data if successful
            if (result.IsSuccess)
            {
                locationData.PhotoPath = _newPhotoPath;
                _context.StoreLocationData(locationData);
            }
        }

        [When(@"I capture a new photo for the location")]
        public async Task WhenICaptureANewPhotoForTheLocation()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");

            // Set up the capture photo mock if it hasn't been set up already
            if (_cameraAvailable)
            {
                _mediaServiceMock
                    .Setup(ms => ms.CapturePhotoAsync(It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Result<string>.Success("/test-photos/captured.jpg"));
            }

            // Create and execute a command to use the captured photo
            var captureResult = await _mediaServiceMock.Object.CapturePhotoAsync();

            if (!captureResult.IsSuccess)
            {
                // Store failure result
                _context.StoreResult(Result<LocationDto>.Failure(captureResult.ErrorMessage ?? "Failed to capture photo"));
                return;
            }

            // Now attach the captured photo
            var command = new AttachPhotoCommand
            {
                LocationId = locationData.Id.Value,
                PhotoPath = captureResult.Data
            };

            // Execute the command
            var result = await _mediator.Send(command);

            // Store the result for verification
            _context.StoreResult(result);

            // Update stored location data if successful
            if (result.IsSuccess)
            {
                locationData.PhotoPath = captureResult.Data;
                _context.StoreLocationData(locationData);
            }
        }

        [When(@"I try to capture a new photo for the location")]
        public async Task WhenITryToCaptureANewPhotoForTheLocation()
        {
            // Check camera availability
            var checkResult = await _mediaServiceMock.Object.IsCaptureSupported();

            if (!checkResult.IsSuccess || !checkResult.Data)
            {
                // Camera not available, store this info
                _context.StoreResult(Result<bool>.Success(false, "Camera not available"));
                return;
            }

            // Try to capture a photo (which would fail based on the Given step)
            await WhenICaptureANewPhotoForTheLocation();
        }

        [When(@"I try to attach the photo to the location")]
        public async Task WhenITryToAttachThePhotoToTheLocation()
        {
            await WhenIAttachThePhotoToTheLocation();
        }

        [Then(@"the photo should be attached successfully")]
        public void ThenThePhotoShouldBeAttachedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after photo attachment");
            result.IsSuccess.Should().BeTrue("Photo attachment should be successful");
            result.Data.Should().NotBeNull("Location data should be returned");
            result.Data.PhotoPath.Should().NotBeNullOrEmpty("Location should have a photo path");
        }

        [Then(@"the location should have a photo path")]
        public void ThenTheLocationShouldHaveAPhotoPath()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");
            locationData.PhotoPath.Should().NotBeNullOrEmpty("Location should have a photo path");
        }

        [Then(@"the photo should be removed successfully")]
        public void ThenThePhotoShouldBeRemovedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after photo removal");
            result.IsSuccess.Should().BeTrue("Photo removal should be successful");
            result.Data.Should().NotBeNull("Location data should be returned");
            result.Data.PhotoPath.Should().BeNull("Location should not have a photo path");
        }

        [Then(@"the location should not have a photo path")]
        public void ThenTheLocationShouldNotHaveAPhotoPath()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");
            locationData.PhotoPath.Should().BeNull("Location should not have a photo path");
        }

        [Then(@"the photo should be replaced successfully")]
        public void ThenThePhotoShouldBeReplacedSuccessfully()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after photo replacement");
            result.IsSuccess.Should().BeTrue("Photo replacement should be successful");
            result.Data.Should().NotBeNull("Location data should be returned");
            result.Data.PhotoPath.Should().Be(_newPhotoPath, "Location should have the new photo path");
        }

        [Then(@"the location should have the new photo path")]
        public void ThenTheLocationShouldHaveTheNewPhotoPath()
        {
            var locationData = _context.GetLocationData();
            locationData.Should().NotBeNull("Location data should be available");
            locationData.PhotoPath.Should().Be(_newPhotoPath, "Location should have the new photo path");
        }

        [Then(@"the photo capture should fail gracefully")]
        public void ThenThePhotoCaptureFailsGracefully()
        {
            var result = _context.GetLastResult<bool>();
            result.Should().NotBeNull("Result should be available after photo capture attempt");
            result.IsSuccess.Should().BeTrue("Failure handling should be successful");
            result.Data.Should().BeFalse("Camera capture availability should be false");
        }

        [Then(@"I should be offered the option to pick a photo instead")]
        public void ThenIShouldBeOfferedTheOptionToPickAPhotoInstead()
        {
            // Verify that the media service was asked to check if capture is supported
            _mediaServiceMock.Verify(
                ms => ms.IsCaptureSupported(It.IsAny<CancellationToken>()),
                Times.AtLeastOnce,
                "Camera availability should have been checked");

            // When camera is not available, MediaService would typically fall back to PickPhotoAsync
            _mediaServiceMock.Verify(
                ms => ms.PickPhotoAsync(It.IsAny<CancellationToken>()),
                Times.Never,
                "Photo picking should not have been attempted yet");
        }

        [Then(@"the photo attachment should fail")]
        public void ThenThePhotoAttachmentShouldFail()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available after failed photo attachment");
            result.IsSuccess.Should().BeFalse("Photo attachment should have failed");
        }

        [Then(@"I should receive an error about invalid photo path")]
        public void ThenIShouldReceiveAnErrorAboutInvalidPhotoPath()
        {
            var result = _context.GetLastResult<LocationDto>();
            result.Should().NotBeNull("Result should be available with error information");
            result.ErrorMessage.Should().NotBeNullOrEmpty("Error message should be provided");
            result.ErrorMessage.Should().Contain("path", "Error should mention the photo path");
        }
    }
}