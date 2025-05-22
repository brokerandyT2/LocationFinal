Feature: WeatherForecast
  As a user of the location app
  I want to view weather forecasts for my saved locations
  So that I can plan my activities according to the weather

Background:
  Given the application is initialized for testing

Scenario: Retrieve weather forecast for a location
  Given I have a location with the following details:
    | Title      | Description       | Latitude  | Longitude  | City     | State |
    | Test Place | Weather test site | 40.712776 | -74.005974 | New York | NY    |
  When I request the weather forecast for the location
  Then I should receive a successful weather forecast result
  And the forecast should contain weather data for the current day
  And the forecast should include at least 1 upcoming day

Scenario: View detailed forecast for a specific day
  Given I have a location with the following details:
    | Title      | Description       | Latitude  | Longitude  | City     | State |
    | Test Place | Weather test site | 40.712776 | -74.005974 | New York | NY    |
  When I request the detailed forecast for tomorrow
  Then I should receive a successful weather forecast result
  And the forecast details should include:
    | Temperature | Min Temperature | Max Temperature | Description | Wind Speed | Wind Direction | Humidity | Pressure | UV Index | Precipitation |

Scenario: View moon phase information
  Given I have a location with the following details:
    | Title      | Description       | Latitude  | Longitude  | City     | State |
    | Test Place | Weather test site | 40.712776 | -74.005974 | New York | NY    |
  When I request the moon phase information for the current day
  Then I should receive a successful weather forecast result
  And the moon phase information should include:
    | Moon Phase | Moon Rise | Moon Set | Moon Phase Description |

Scenario: Update weather forecast data
    Given the application is initialized for testing
    And I have a location with the following details:
      | Title      | Description       | Latitude  | Longitude  | City     | State |
      | Test Place | Weather test site | 40.712776 | -74.005974 | New York | NY    |
    And the location has existing weather data from yesterday
    When I update the weather forecast for the location
    Then I should receive a successful weather forecast result
    And the forecast data should be current

Scenario: Handle invalid coordinates when requesting weather
  Given I have a location with invalid coordinates:
    | Title         | Description           | Latitude | Longitude | City   | State |
    | Invalid Place | Has invalid location  | 100.0    | 200.0     | Nowhere| XX    |
  When I request the weather forecast for the invalid location
  Then I should receive a failure result
  And the error message should contain information about invalid coordinates

Scenario: Update weather with API unavailability
  Given I have multiple locations stored in the system for weather:
    | Title    | Description   | Latitude  | Longitude   | City          | State |
    | Home     | My home       | 40.712776 | -74.005974  | New York      | NY    |
    | Office   | Work location | 37.774929 | -122.419418 | San Francisco | CA    |
    | Vacation | Holiday home  | 25.761681 | -80.191788  | Miami         | FL    |
  And the weather API is unavailable
  When I update the weather data for location "Home"
  Then I should receive an error related to API unavailability

Scenario: Get weather forecast for a new location
  Given I have a new location with the following details:
    | Title       | Description        | Latitude  | Longitude  | City     | State |
    | New Place   | Just added location| 40.712776 | -74.005974 | New York | NY    |
  When I request the weather forecast for the new location
  Then I should receive a successful weather forecast result
  And the forecast should include the following information:
    | Temperature | Description | Wind Speed | Humidity | Pressure | Sunrise | Sunset |

Scenario: Update all location weather data
  Given I have multiple locations stored in the system for weather:
    | Title    | Description   | Latitude  | Longitude   | City          | State |
    | Home     | My home       | 40.712776 | -74.005974  | New York      | NY    |
    | Office   | Work location | 37.774929 | -122.419418 | San Francisco | CA    |
    | Vacation | Holiday home  | 25.761681 | -80.191788  | Miami         | FL    |
  When I update weather data for all locations
  Then the update operation should report 3 updated locations

Scenario: Display timezone information in weather data
    Given the application is initialized for testing
    And I have a location with the following details:
      | Title      | Description       | Latitude  | Longitude  | City     | State |
      | Test Place | Weather test site | 40.712776 | -74.005974 | New York | NY    |
    When I request the weather forecast for the location
    Then I should receive a successful weather forecast result
    And the forecast data should show timezone "America/New_York"