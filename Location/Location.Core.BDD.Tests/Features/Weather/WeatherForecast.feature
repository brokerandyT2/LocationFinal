Feature: Weather Forecast
    As a user
    I want to view weather forecasts for my locations
    So that I can plan my photography sessions accordingly

Background:
    Given the application is initialized for testing
    And I have a location with the following details:
        | Title       | Description       | Latitude  | Longitude  | City     | State |
        | Test Place  | Weather test site | 40.712776 | -74.005974 | New York | NY    |

@weatherForecastRetrieval
Scenario: Retrieve weather forecast for a location
    When I request the weather forecast for the location
    Then I should receive a successful result
    And the forecast should contain weather data for the current day
    And the forecast should include the following information:
        | Temperature | Description | Sunrise | Sunset | Wind Speed | Humidity |
    And the forecast should include at least 1 upcoming day

@weatherDetailedForecast
Scenario: View detailed forecast for a specific day
    When I request the detailed forecast for tomorrow
    Then I should receive a successful result
    And the forecast details should include:
        | Temperature | Min Temperature | Max Temperature | Description | Wind Speed | Wind Direction | Humidity | Pressure | UV Index | Precipitation |

@weatherMoonPhase
Scenario: View moon phase information
    When I request the moon phase information for the current day
    Then I should receive a successful result
    And the moon phase information should include:
        | Moon Phase | Moon Rise | Moon Set | Moon Phase Description |

@weatherUpdateForecast
Scenario: Update weather forecast data
    Given the location has existing weather data from yesterday
    When I update the weather forecast for the location
    Then I should receive a successful result
    And the forecast data should be