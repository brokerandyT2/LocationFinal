Feature: Sun Location Tracking
  As a photographer
  I want to track the sun's real-time position using my device sensors
  So that I can align my camera with the sun for optimal lighting

  Background:
    Given the photography application is initialized for testing

  Scenario: Start basic sun location tracking
    Given I am at location coordinates 40.7128, -74.0060
    And I have a device with compass and orientation sensors
    And I want to track the sun location for photography
    When I start sun location tracking
    Then I should receive a successful photography result
    And I should receive the current sun direction
    And the tracking should be active
    And the sun location coordinates should match my current position

  Scenario: Align device with current sun position
    Given I am at location coordinates 51.5074, -0.1278
    And I have a device with compass and orientation sensors
    And the sun is currently at azimuth 180 and elevation 45
    When I point my device toward the sun
    And I check if my device is aligned with the sun
    Then I should receive a successful photography result
    And the device should be aligned with the sun

  Scenario: Check device misalignment with sun
    Given I am at location coordinates 40.7128, -74.0060
    And I have a device with compass and orientation sensors
    And the sun is currently at azimuth 180 and elevation 45
    And the device is pointing in direction 90 degrees
    And the device is tilted at 20 degrees elevation
    When I check if my device is aligned with the sun
    Then I should receive a successful photography result
    And the device should not be aligned with the sun

  Scenario: Update location and track sun position change
    Given I am at location coordinates 40.7128, -74.0060
    And I want to track the sun location for photography
    When I start sun location tracking
    And I update my location to coordinates 51.5074, -0.1278
    Then I should receive a successful photography result
    And the sun location should update based on my new position
    And I should receive the current sun direction

  Scenario: Track sun movement over time
    Given I am at location coordinates 37.7749, -122.4194
    And I want to track the sun location for photography
    When I start sun location tracking
    And I track the sun for 60 minutes
    Then I should receive a successful photography result
    And I should see the sun position change over time

  Scenario: Get current sun direction for photography setup
    Given I am at location coordinates 48.8566, 2.3522
    And I have a device with compass and orientation sensors
    When I get the current sun direction
    Then I should receive a successful photography result
    And I should receive the current sun direction
    And the sun direction should be approximately 180 degrees azimuth

  Scenario: Track sun location for multiple photography sessions
    Given I have multiple sun tracking sessions:
      | Id | Latitude | Longitude | DateTime            | SolarAzimuth | SolarElevation |
      | 1  | 40.7128  | -74.0060  | 2024-06-21 08:00:00 | 120          | 30             |
      | 2  | 40.7128  | -74.0060  | 2024-06-21 12:00:00 | 180          | 60             |
      | 3  | 40.7128  | -74.0060  | 2024-06-21 16:00:00 | 240          | 30             |
    When I calculate sun location for all tracking sessions
    Then I should receive a successful photography result
    And all sun tracking sessions should be successful

  Scenario: Track sun position during golden hour
    Given I am at location coordinates 35.6762, 139.6503
    And I want to track the sun location for photography
    When I start sun location tracking
    And I track the sun for 30 minutes
    Then I should receive a successful photography result
    And I should see the sun position change over time
    And the tracking should be active

  Scenario: Align camera with sun for backlighting
    Given I am at location coordinates -33.8688, 151.2093
    And I have a device with compass and orientation sensors
    And the sun is currently at azimuth 270 and elevation 15
    When I point my device toward the sun
    And I check if my device is aligned with the sun
    Then I should receive a successful photography result
    And the device should be aligned with the sun
    And the sun elevation should be approximately 15 degrees

  Scenario: Track sun location at different times of day
    Given I am at location coordinates 64.1466, -21.9426
    And I want to track the sun location for photography
    When I start sun location tracking
    And I track the sun for 120 minutes
    Then I should receive a successful photography result
    And I should see the sun position change over time
    And all sun tracking sessions should be successful

  Scenario: Real-time sun tracking for timelapse photography
    Given I am at location coordinates 0.0, 0.0
    And I have a device with compass and orientation sensors
    And I want to track the sun location for photography
    When I start sun location tracking
    And I track the sun for 180 minutes
    Then I should receive a successful photography result
    And I should see the sun position change over time
    And the tracking should be active

  Scenario: Verify sun position accuracy after location change
    Given I am at location coordinates 25.2048, 55.2708
    And I want to track the sun location for photography
    When I start sun location tracking
    And I update my location to coordinates 1.3521, 103.8198
    Then I should receive a successful photography result
    And the sun location should update based on my new position
    And the sun location coordinates should match my current position

  Scenario Outline: Track sun at different global locations
    Given I am at location coordinates <Latitude>, <Longitude>
    And I want to track the sun location for photography
    When I start sun location tracking
    Then I should receive a successful photography result
    And I should receive the current sun direction
    And the sun direction should be approximately <ExpectedAzimuth> degrees azimuth
    And the sun elevation should be approximately <ExpectedElevation> degrees

    Examples:
      | Latitude | Longitude | ExpectedAzimuth | ExpectedElevation |
      | 40.7128  | -74.0060  | 180             | 45                |
      | 51.5074  | -0.1278   | 180             | 40                |
      | -33.8688 | 151.2093  | 180             | 50                |
      | 35.6762  | 139.6503  | 180             | 55                |

  Scenario: Monitor sun alignment during photography session
    Given I am at location coordinates 47.6062, -122.3321
    And I have a device with compass and orientation sensors
    And the sun is currently at azimuth 225 and elevation 25
    And the device is pointing in direction 225 degrees
    And the device is tilted at 25 degrees elevation
    When I check if my device is aligned with the sun
    Then I should receive a successful photography result
    And the device should be aligned with the sun

  Scenario: Track sun location for architectural photography
    Given I am at location coordinates 41.9028, 12.4964
    And I want to track the sun location for photography
    When I start sun location tracking
    And I get the current sun direction
    Then I should receive a successful photography result
    And I should receive the current sun direction
    And the tracking should be active
    And the sun location coordinates should match my current position

  Scenario: Continuous sun tracking with sensor updates
    Given I am at location coordinates 55.7558, 37.6176
    And I have a device with compass and orientation sensors
    And I want to track the sun location for photography
    When I start sun location tracking
    And I track the sun for 90 minutes
    Then I should receive a successful photography result
    And I should see the sun position change over time
    And the tracking should be active