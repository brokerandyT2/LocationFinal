Feature: Sun Times Calculation
  As a photographer
  I want to calculate sunrise, sunset, and twilight times for any location and date
  So that I can plan my photography sessions around optimal lighting conditions

  Background:
    Given the photography application is initialized for testing

  Scenario: Calculate basic sun times for New York
    Given I have a location at coordinates 40.7128, -74.0060 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And I should receive the sunrise time
    And I should receive the sunset time
    And the sunrise should be before sunset
    And the solar noon should be between sunrise and sunset

  Scenario: Calculate twilight times for photography planning
    Given I have a location at coordinates 51.5074, -0.1278 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I request all twilight times
    Then I should receive a successful photography result
    And I should receive all twilight times
    And the twilight times should be in correct order

  Scenario: Calculate sun times for multiple locations
    Given I have multiple locations for sun times calculation:
      | Id | Latitude | Longitude | Date       | Sunrise | Sunset | SolarNoon |
      | 1  | 40.7128  | -74.0060  | 2024-06-21 | 05:25   | 20:31  | 12:58     |
      | 2  | 51.5074  | -0.1278   | 2024-06-21 | 04:43   | 21:21  | 13:02     |
      | 3  | 35.6762  | 139.6503  | 2024-06-21 | 04:25   | 19:00  | 11:42     |
    When I calculate sun times for all locations
    Then I should receive a successful photography result
    And all sun times should be calculated successfully

  Scenario: Calculate golden hour times for photography
    Given I have a location at coordinates 37.7749, -122.4194 for sun times
    And I want to calculate sun times for date 2024-09-22
    When I request the golden hour times
    Then I should receive a successful photography result
    And I should receive the golden hour times
    And the golden hour times should be relative to sunrise and sunset

  Scenario: Plan photography session for specific location
    Given I want to plan photography for London
    And today's date is 2024-06-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the sunrise should be approximately at 04:43
    And the sunset should be approximately at 21:21

  Scenario: Calculate sun times for summer solstice
    Given I have a location at coordinates 64.1466, -21.9426 for sun times
    When I calculate sun times for the summer solstice
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the day length should be longer than 18:00

  Scenario: Calculate sun times for winter solstice
    Given I have a location at coordinates 40.7128, -74.0060 for sun times
    When I calculate sun times for the winter solstice
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the day length should be shorter than 10:00

  Scenario: Calculate sun times for Arctic location in summer
    Given I have a location at coordinates 70.0, 20.0 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the day length should be longer than 20:00

  Scenario: Calculate sun times for Southern Hemisphere
    Given I have a location at coordinates -33.8688, 151.2093 for sun times
    And I want to calculate sun times for date 2024-12-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the sunrise should be before sunset

  Scenario Outline: Calculate sun times for different seasons
    Given I have a location at coordinates 40.7128, -74.0060 for sun times
    And I want to calculate sun times for date <Date>
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the sunrise should be approximately at <ExpectedSunrise>
    And the sunset should be approximately at <ExpectedSunset>

    Examples:
      | Date       | ExpectedSunrise | ExpectedSunset |
      | 2024-03-20 | 06:59           | 19:08          |
      | 2024-06-21 | 05:25           | 20:31          |
      | 2024-09-22 | 06:54           | 19:00          |
      | 2024-12-21 | 07:20           | 16:38          |

  Scenario: Validate sun times chronological order
    Given I have a location at coordinates 48.8566, 2.3522 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I request all twilight times
    Then I should receive a successful photography result
    And the twilight times should be in correct order
    And the solar noon should be between sunrise and sunset

  Scenario: Calculate sun times for photography location scouting
    Given I want to plan photography for Paris
    And today's date is 2024-08-15
    When I calculate the sun times
    And I request the golden hour times
    Then I should receive a successful photography result
    And I should receive the golden hour times
    And the sun times should be calculated successfully

  Scenario: Calculate sun times near equator
    Given I have a location at coordinates 0.0, 0.0 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the day length should be longer than 11:00
    And the day length should be shorter than 13:00

  Scenario: Calculate sun times for extreme latitude
    Given I want to plan photography for Reykjavik
    And today's date is 2024-12-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the day length should be shorter than 06:00

  Scenario: Verify sun times coordinate matching
    Given I have a location at coordinates 35.6762, 139.6503 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I calculate the sun times
    Then I should receive a successful photography result
    And the sun times should be calculated successfully
    And the sunrise should be before sunset
    And the solar noon should be between sunrise and sunset

  Scenario: Calculate sun times for landscape photography planning
    Given I have a location at coordinates -33.9249, 18.4241 for sun times
    And I want to calculate sun times for date 2024-06-21
    When I calculate the sun times
    And I request the golden hour times
    Then I should receive a successful photography result
    And I should receive the golden hour times
    And the golden hour times should be relative to sunrise and sunset
    And the sun times should be calculated successfully