Feature: Location Search
    As a user
    I want to search for locations
    So that I can find places to take photographs

Background:
    Given the application is initialized for testing
    And I have multiple locations stored in the system for search:
        | Title           | Description                   | Latitude   | Longitude   | City         | State |
        | Golden Gate     | Famous bridge in San Francisco| 37.8199    | -122.4783   | San Francisco| CA    |
        | Empire State    | Iconic skyscraper             | 40.7484    | -73.9857    | New York     | NY    |
        | Grand Canyon    | Natural wonder                | 36.1069    | -112.1129   | Grand Canyon | AZ    |
        | Space Needle    | Seattle landmark              | 47.6205    | -122.3493   | Seattle      | WA    |
        | Statue of Liberty| Monument on Liberty Island    | 40.6892    | -74.0445    | New York     | NY    |

@locationListing
Scenario: List all locations
    When I request a list of all locations
    Then I should receive a successful location result
    And the result should contain 5 locations
    And the locations should be ordered by most recent first

@locationSearchByTitle
Scenario: Find a location by title
    When I search for a location with title "Empire State"
    Then I should receive a successful location result
    And the result should contain a location with title "Empire State"
    And the location search result should have the following details:
        | City     | State |
        | New York | NY    |

@locationSearchNearby
Scenario: Find locations near a specific coordinate
    When I search for locations within 50 km of coordinates:
        | Latitude | Longitude |
        | 40.7128  | -74.0060  |
    Then I should receive a successful result
    And the result should contain 2 locations
    And the result should include "Empire State"
    And the result should include "Statue of Liberty"
    And the result should not include "Golden Gate"

@locationSearchWithFilter
Scenario: Search locations with text filter
    When I search for locations with text filter "New"
    Then I should receive a successful location result
    And the result should contain 2 locations
    And the result should include "Empire State"
    And the result should include "Statue of Liberty"

@locationEmptySearch
Scenario: Search for non-existent locations
    When I search for a location with title "Non-existent Place"
    Then I should receive a successful location result
    And the result should contain 0 locations