Feature: Sun Position Calculation
  As a photographer
  I want to calculate the sun's position at any given time and location
  So that I can plan my photography sessions for optimal lighting

  Background:
    Given the photography application is initialized for testing

  Scenario: Calculate sun position for New York at noon
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-06-21 | 12:00 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be visible (elevation > 0)
    And the sun should be roughly in the south direction

  Scenario: Calculate sun azimuth at sunrise
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-06-21 | 06:00 |
    When I request the sun azimuth
    Then I should receive a successful photography result
    And I should receive the sun azimuth
    And the sun should be roughly in the east direction

  Scenario: Calculate sun elevation at sunset
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-06-21 | 18:00 |
    When I request the sun elevation
    Then I should receive a successful photography result
    And I should receive the sun elevation
    And the sun should be roughly in the west direction

  Scenario: Calculate sun position at midnight (below horizon)
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-06-21 | 00:00 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be below the horizon (elevation < 0)

  Scenario: Calculate sun position for multiple locations
    Given I have multiple locations for sun position calculation:
      | Id | Latitude | Longitude | Date       | Time  | SolarAzimuth | SolarElevation |
      | 1  | 40.7128  | -74.0060  | 2024-06-21 | 12:00 | 180          | 45             |
      | 2  | 51.5074  | -0.1278   | 2024-06-21 | 12:00 | 180          | 40             |
      | 3  | 35.6762  | 139.6503  | 2024-06-21 | 12:00 | 180          | 50             |
    When I calculate sun positions for all locations
    Then I should receive a successful photography result
    And all sun positions should be calculated successfully

  Scenario: Track sun position over time
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       |
      | 2024-06-21 |
    When I track the sun position over time
    Then I should receive a successful photography result
    And the sun position should change over time

  Scenario: Calculate sun position with specific coordinates validation
    Given I have a location with coordinates 48.8566, 2.3522
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-12-21 | 14:00 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun coordinates should match the location
    And the sun position should be calculated successfully

  Scenario Outline: Calculate sun position for different times of day
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       |
      | 2024-06-21 |
    When I calculate the sun position at <Time>
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be roughly in the <Direction> direction

    Examples:
      | Time  | Direction |
      | 06:00 | east      |
      | 09:00 | southeast |
      | 12:00 | south     |
      | 15:00 | southwest |
      | 18:00 | west      |

  Scenario: Calculate sun position for different seasons
    Given I have a location with coordinates 40.7128, -74.0060
    And the current time is 12:00
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be visible (elevation > 0)

  Scenario: Calculate precise sun azimuth values
    Given I have a location with coordinates 0, 0
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-03-20 | 12:00 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun azimuth should be approximately 180 degrees
    And the sun elevation should be approximately 90 degrees

  Scenario: Calculate sun position for Arctic location in summer
    Given I have a location with coordinates 70, 20
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-06-21 | 12:00 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be visible (elevation > 0)

  Scenario: Calculate sun position for Southern Hemisphere
    Given I have a location with coordinates -33.8688, 151.2093
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-12-21 | 12:00 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be visible (elevation > 0)
    And the sun should be roughly in the north direction

  Scenario: Validate sun position calculation accuracy
    Given I want to track the sun position for New York
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-06-21 | 15:30 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun coordinates should match the location
    And the sun azimuth should be approximately 240 degrees
    And the sun elevation should be approximately 35 degrees

  Scenario: Calculate sun position throughout a full day
    Given I have a location with coordinates 40.7128, -74.0060
    And I have a specific date and time:
      | Date       |
      | 2024-06-21 |
    When I track the sun position over time
    Then I should receive a successful photography result
    And the sun position should change over time
    And all sun positions should be calculated successfully

  Scenario: Calculate sun position for photography golden hour
    Given I have a location with coordinates 37.7749, -122.4194
    And I have a specific date and time:
      | Date       | Time  |
      | 2024-09-22 | 07:30 |
    When I calculate the sun position
    Then I should receive a successful photography result
    And the sun position should be calculated successfully
    And the sun should be visible (elevation > 0)
    And the sun should be roughly in the southeast direction