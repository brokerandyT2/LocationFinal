Feature: Location Search
    As a user
    I want to search for locations
    So that I can find places based on different criteria

Background:
    Given the application is initialized for testing
    And I have multiple locations stored in the system:
        | Title           | Description       | Latitude    | Longitude   | City         | State   |
        | Home            | My home address   | 40.712776   | -74.005974  | New York     | NY      |
        | Office          | Work location     | 40.758896   | -73.985130  | New York     | NY      |
        | Beach House     | Vacation home     | 26.461700   | -80.058310  | Boca Raton   | FL      |
        | Mountain Cabin  | Hiking spot       | 39.191097   | -106.817535 | Aspen        | CO      |

@locationListing
Scenario: List all locations
    When I request a list of all locations
    Then I should receive a successful result
    And the result should contain 4 locations
    And the location list should include "Home"
    And the location list should include "Office"
    And the location list should include "Beach House"
    And the location list should include "Mountain Cabin"

@locationSearchByTitle
Scenario: Find a location by title
    When I search for a location with title "Home"
    Then I should receive a successful result
    And the result should contain a location with title "Home"
    And the location details should be:
        | Title  | Description     | City      | State |
        | Home   | My home address | New York  | NY    |

@locationSearchNearby
Scenario: Find locations near a specific coordinate
    When I search for locations within 10 km of coordinates:
        | Latitude    | Longitude   |
        | 40.730610   | -73.935242  |
    Then I should receive a successful result
    And the result should contain 2 locations
    And the location list should include "Home"
    And the location list should include "Office"
    And the location list should not include "Beach House"
    And the location list should not include "Mountain Cabin"

@locationSearchWithFilter
Scenario: Search locations with text filter
    When I search for locations with filter "home"
    Then I should receive a successful result
    And the result should contain 2 locations
    And the location list should include "Home"
    And the location list should include "Beach House"
    And the location list should not include "Office"
    And the location list should not include "Mountain Cabin"

@locationEmptySearch
Scenario: Search for non-existent locations
    When I search for a location with title "Non-existent place"
    Then I should receive a failure result
    And the error message should contain "not found"