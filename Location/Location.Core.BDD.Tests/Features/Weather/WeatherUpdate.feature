Feature: Weather Update
    As a user of the application
    I want to update weather data for my locations
    So that I can see the current weather conditions

Background:
    Given the application is initialized for testing
    And I have multiple locations stored in the system for weather:
      | Title    | Description   | Latitude  | Longitude   | City          | State |
      | Home     | My home       | 40.712776 | -74.005974  | New York      | NY    |
      | Office   | Work location | 37.774929 | -122.419418 | San Francisco | CA    |
      | Vacation | Holiday home  | 25.761681 | -80.191788  | Miami         | FL    |

Scenario: Update Weather for a Location
    When I update the weather data for location "Home"
    Then I should receive a successful result
    And the weather data should include the current temperature
    And the weather data should include the current wind information
    And the weather data should include a description

# This scenario is on approximately line 21-23, matching the error message
Scenario: Force Weather Update Even If Data Is Recent
    Given the "Office" location has recent weather data
    When I force update the weather data for "Office"
    Then I should receive a successful result
    And the weather data should be updated
    And the last update timestamp should be recent

Scenario: Skip Weather Update If Recent Data Exists
    Given I have a location with existing weather data from 1 hours ago
    When I update the weather data for the location
    Then I should not receive updated weather data

Scenario: Update All Locations
    When I update weather data for all locations
    Then the update operation should report 3 updated locations

Scenario: Handle API Unavailability
    Given the weather API is unavailable
    When I update the weather data for location "Home"
    Then I should receive an error related to API unavailability

Scenario: Include Sunrise and Sunset Times
    When I update the weather data for location "Vacation"
    Then I should receive a successful result
    And the weather data should include sunrise and sunset times

Scenario: Get Weather with Timezone Information
    When I update the weather data for location "Office"
    Then I should receive a successful result
    And the weather data should indicate the timezone "America/New_York"