Feature: Weather Update
    As a user
    I want to update weather data for my locations
    So that I can have the most current information

Background:
    Given the application is initialized for testing
    And I have multiple locations stored in the system:
        | Title        | Description      | Latitude   | Longitude   | City       | State |
        | Home         | My home          | 40.712776  | -74.005974  | New York   | NY    |
        | Office       | Work location    | 37.774929  | -122.419418 | San Francisco | CA |
        | Vacation     | Holiday home     | 25.761681  | -80.191788  | Miami      | FL    |

@weatherUpdateSingle
Scenario: Update weather for a single location
    Given the "Home" location has outdated weather data
    When I update the weather data for "Home"
    Then I should receive a successful result
    And the weather data should be current
    And the last update timestamp should be recent

@weatherForceUpdate
Scenario: Force weather update even if data is recent
    Given the "Office" location has recent weather data
    When I force update the weather data for "Office"
    Then I should receive a successful result
    And the weather data should be updated
    And the last update timestamp should be recent

@weatherUpdateAll
Scenario: Update weather for all locations
    Given all locations have outdated weather data
    When I update weather data for all locations
    Then I should receive a successful result
    And the result should indicate 3 locations were updated
    And all locations should have current weather data

@weatherUpdatePartialSuccess
Scenario: Handle partial success when updating all locations
    Given some locations have connectivity issues:
        | Title    |
        | Vacation |
    When I update weather data for all locations
    Then I should receive a successful result
    And the result should indicate 2 locations were updated
    And locations "Home" and "Office" should have current weather data
    And location "Vacation" should not have updated weather data

@weatherUpdateAPIFailure
Scenario: Handle weather API failure gracefully
    Given the weather API is temporarily unavailable
    When I update the weather data for "Home"
    Then I should receive a failure result
    And the error message should contain information about API unavailability
    And the existing weather data should remain unchanged

@weatherCachedData
Scenario: Use cached weather data when available
    Given the "Home" location has weather data less than 1 hour old
    When I request weather data for "Home" without forcing an update
    Then I should receive a successful result
    And the cached weather data should be returned
    And no API call should be made