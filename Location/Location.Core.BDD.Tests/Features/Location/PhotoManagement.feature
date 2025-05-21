Feature: PhotoManagement
  As a user
  I want to manage photos for my locations
  So that I can have visual representations of my locations

Scenario: Attach a photo to a location
  Given the application is initialized for testing
  And I have a location with the following details:
    | Title          | Description       | Latitude  | Longitude  | City     | State |
    | Photo Location | For photo testing | 40.712776 | -74.005974 | New York | NY    |
  Given I have a photo available at "/test-photos/sample.jpg"
  When I attach the photo to the location
  Then the photo should be attached successfully
  And the location should have a photo path

Scenario: Remove a photo from a location
  Given the application is initialized for testing
  And I have a location with the following details:
    | Title          | Description       | Latitude  | Longitude  | City     | State | PhotoPath               |
    | Photo Location | For photo testing | 40.712776 | -74.005974 | New York | NY    | /test-photos/exists.jpg |
  When I remove the photo from the location
  Then the photo should be removed successfully
  And the location should not have a photo path

Scenario: Capture a new photo for a location
  Given the application is initialized for testing
  And I have a location with the following details:
    | Title          | Description       | Latitude  | Longitude  | City     | State |
    | Photo Location | For photo testing | 40.712776 | -74.005974 | New York | NY    |
  Given the camera is available
  When I capture a new photo for the location
  Then the photo should be attached successfully
  And the location should have a photo path

Scenario: Handle invalid photo path
  Given the application is initialized for testing
  And I have a location with the following details:
    | Title          | Description       | Latitude  | Longitude  | City     | State |
    | Photo Location | For photo testing | 40.712776 | -74.005974 | New York | NY    |
  Given I have an invalid photo path "invalid://photo.path"
  When I try to attach the photo to the location
  Then the photo attachment should fail
  And I should receive an error about invalid photo path