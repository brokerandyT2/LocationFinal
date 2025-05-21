Feature: Photo Management
    As a user
    I want to manage photos for my locations
    So that I can visually identify and remember places

Background:
    Given the application is initialized for testing
    And I have a location with the following details:
        | Title          | Description         | Latitude  | Longitude | City     | State   |
        | Photo Location | For photo testing   | 40.712776 | -74.005974 | New York | NY      |

@photoAttachment
Scenario: Attach a photo to a location
    Given I have a photo available at "/test-photos/sample.jpg"
    When I attach the photo to the location
    Then the photo should be attached successfully
    And the location should have a photo path

@photoRemoval
Scenario: Remove a photo from a location
    Given the location has a photo attached
    When I remove the photo from the location
    Then the photo should be removed successfully
    And the location should not have a photo path

@photoReplacement
Scenario: Replace a photo on a location
    Given the location has a photo attached
    And I have a new photo available at "/test-photos/replacement.jpg"
    When I replace the existing photo with the new photo
    Then the photo should be replaced successfully
    And the location should have the new photo path

@photoCapture
Scenario: Capture a new photo for a location
    Given the camera is available
    When I capture a new photo for the location
    Then the photo should be attached successfully
    And the location should have a photo path

@photoUnavailableCamera
Scenario: Handle unavailable camera when capturing photo
    Given the camera is not available
    When I try to capture a new photo for the location
    Then the photo capture should fail gracefully
    And I should be offered the option to pick a photo instead

@photoInvalidPath
Scenario: Handle invalid photo path
    Given I have an invalid photo path "invalid://photo.path"
    When I try to attach the photo to the location
    Then the photo attachment should fail
    And I should receive an error about invalid photo path