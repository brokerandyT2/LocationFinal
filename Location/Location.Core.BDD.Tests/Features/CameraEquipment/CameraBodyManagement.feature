Feature: Camera Body Management
  As a photographer
  I want to manage camera bodies and their specifications
  So that I can track equipment capabilities and make informed photography decisions

  Background:
    Given the photography application is initialized for testing

  Scenario: Load camera sensor profiles from JSON
    Given I have camera sensor profile JSON data:
      """
      [
        {
          "Id": 1,
          "Make": "Canon",
          "Model": "EOS R5",
          "SensorWidth": 36.0,
          "SensorHeight": 24.0,
          "ResolutionWidth": 8192,
          "ResolutionHeight": 5464,
          "PixelPitch": 4.39,
          "IsoRange": "100-51200",
          "MountType": "RF"
        }
      ]
      """
    When I load the camera sensor profiles
    Then I should receive a successful photography result
    And the camera profiles should be loaded successfully
    And I should have 1 camera profile loaded

  Scenario: Get camera body by ID
    Given I have a camera body with the following specifications:
      | Id | Make   | Model   | SensorWidth | SensorHeight | MountType |
      | 1  | Canon  | EOS R5  | 36.0        | 24.0         | RF        |
    When I get the camera body by ID 1
    Then I should receive a successful photography result
    And I should receive the camera body details
    And the camera make should be "Canon"
    And the camera model should be "EOS R5"

  Scenario: Get camera bodies with paging
    Given I have multiple camera bodies in the system:
      | Id | Make   | Model     | SensorWidth | SensorHeight | MountType |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         | RF        |
      | 2  | Nikon  | Z9        | 35.9        | 23.9         | Z         |
      | 3  | Sony   | A7R V     | 35.7        | 23.8         | E         |
    When I get camera bodies with skip 0 and take 2
    Then I should receive a successful photography result
    And I should receive 2 camera bodies
    And the results should include "Canon EOS R5"

  Scenario: Search camera bodies by make
    Given I have multiple camera bodies in the system:
      | Id | Make   | Model     | SensorWidth | SensorHeight | MountType |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         | RF        |
      | 2  | Canon  | EOS R6    | 35.9        | 23.9         | RF        |
      | 3  | Nikon  | Z9        | 35.9        | 23.9         | Z         |
    When I search for camera bodies by make "Canon"
    Then I should receive a successful photography result
    And I should receive 2 camera bodies
    And all results should have make "Canon"

  Scenario: Search camera bodies by mount type
    Given I have multiple camera bodies in the system:
      | Id | Make   | Model     | SensorWidth | SensorHeight | MountType |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         | RF        |
      | 2  | Canon  | EOS R6    | 35.9        | 23.9         | RF        |
      | 3  | Nikon  | Z9        | 35.9        | 23.9         | Z         |
    When I search for camera bodies by mount type "RF"
    Then I should receive a successful photography result
    And I should receive 2 camera bodies
    And all results should have mount type "RF"

  Scenario: Get total camera body count
    Given I have 5 camera bodies in the system
    When I get the total camera body count
    Then I should receive a successful photography result
    And the total count should be 5

  Scenario: Validate camera body specifications
    Given I want to validate camera body specifications
    When I validate a camera body with the following specs:
      | Make   | Model   | SensorWidth | SensorHeight | ResolutionWidth | ResolutionHeight |
      | Canon  | EOS R5  | 36.0        | 24.0         | 8192            | 5464             |
    Then I should receive a successful photography result
    And the camera specifications should be valid
    And the aspect ratio should be approximately 1.5

  Scenario: Calculate crop factor for camera sensor
    Given I have a camera body with the following specifications:
      | Make   | Model     | SensorWidth | SensorHeight |
      | Canon  | EOS M50   | 22.3        | 14.9         |
    When I calculate the crop factor for the camera
    Then I should receive a successful photography result
    And the crop factor should be approximately 1.6

  Scenario: Compare camera sensor sizes
    Given I have two camera bodies to compare:
      | Id | Make   | Model     | SensorWidth | SensorHeight |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         |
      | 2  | Canon  | EOS M50   | 22.3        | 14.9         |
    When I compare the sensor sizes
    Then I should receive a successful photography result
    And the full frame sensor should be larger
    And the size difference should be calculated

  Scenario: Get camera bodies by sensor format
    Given I have camera bodies with different sensor formats:
      | Id | Make   | Model     | SensorWidth | SensorHeight | Format        |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         | Full Frame    |
      | 2  | Canon  | EOS M50   | 22.3        | 14.9         | APS-C         |
      | 3  | Olympus| OM-1      | 17.4        | 13.0         | Micro 4/3     |
    When I filter camera bodies by sensor format "Full Frame"
    Then I should receive a successful photography result
    And I should receive 1 camera body
    And the result should be "Canon EOS R5"

  Scenario: Calculate pixel density for camera sensor
    Given I have a camera body with the following specifications:
      | Make   | Model   | SensorWidth | SensorHeight | ResolutionWidth | ResolutionHeight |
      | Canon  | EOS R5  | 36.0        | 24.0         | 8192            | 5464             |
    When I calculate the pixel density
    Then I should receive a successful photography result
    And the pixel density should be calculated correctly
    And the megapixel count should be approximately 45

  Scenario: Validate ISO performance characteristics
    Given I have a camera body with ISO specifications:
      | Make   | Model   | IsoRange    | MaxUsableIso |
      | Canon  | EOS R5  | 100-51200   | 6400         |
    When I validate the ISO performance
    Then I should receive a successful photography result
    And the ISO range should be valid
    And the maximum usable ISO should be within range

  Scenario: Get recommended camera bodies for photography type
    Given I have camera bodies with different capabilities:
      | Id | Make   | Model     | SensorWidth | SensorHeight | IsoRange    | VideoCapability |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         | 100-51200   | 8K              |
      | 2  | Canon  | EOS M50   | 22.3        | 14.9         | 100-25600   | 4K              |
      | 3  | Sony   | A7S III   | 35.6        | 23.8         | 80-409600   | 4K              |
    When I get recommendations for "Low Light Photography"
    Then I should receive a successful photography result
    And the recommendations should prioritize high ISO performance
    And "Sony A7S III" should be recommended

  Scenario: Handle invalid camera body specifications
    Given I want to validate camera body specifications
    When I validate a camera body with invalid specs:
      | Make   | Model   | SensorWidth | SensorHeight | ResolutionWidth | ResolutionHeight |
      | Canon  | EOS R5  | 0           | 0            | -1              | -1               |
    Then I should receive a photography failure result
    And the error should indicate invalid sensor dimensions

  Scenario: Load camera sensor profiles with validation errors
    Given I have invalid camera sensor profile JSON data:
      """
      [
        {
          "Id": 1,
          "Make": "",
          "Model": "",
          "SensorWidth": 0,
          "SensorHeight": 0
        }
      ]
      """
    When I load the camera sensor profiles
    Then I should receive a photography failure result
    And the error should indicate invalid camera profile data