Feature: LocationSearch
  As a user
  I want to search for locations based on different criteria
  So that I can find specific locations quickly

Scenario: Search locations by title
  Given the application is initialized for testing
  And I have multiple locations stored in the system for search:
    | Title             | Description                    | Latitude | Longitude | City          | State |
    | Golden Gate       | Famous bridge in San Francisco | 37.8199  | -122.4783 | San Francisco | CA    |
    | Empire State      | Iconic skyscraper              | 40.7484  | -73.9857  | New York      | NY    |
    | Grand Canyon      | Natural wonder                 | 36.1069  | -112.1129 | Grand Canyon  | AZ    |
    | Space Needle      | Seattle landmark               | 47.6205  | -122.3493 | Seattle       | WA    |
    | Statue of Liberty | Monument on Liberty Island     | 40.6892  | -74.0445  | New York      | NY    |
  When I search for locations with title containing "State"
  Then I should receive a successful location search result
  And the location search result should contain 1 location
  And the location search result should include "Empire State"

Scenario: Search locations by city
  Given the application is initialized for testing
  And I have multiple locations stored in the system for search:
    | Title             | Description                    | Latitude | Longitude | City          | State |
    | Golden Gate       | Famous bridge in San Francisco | 37.8199  | -122.4783 | San Francisco | CA    |
    | Empire State      | Iconic skyscraper              | 40.7484  | -73.9857  | New York      | NY    |
    | Grand Canyon      | Natural wonder                 | 36.1069  | -112.1129 | Grand Canyon  | AZ    |
    | Space Needle      | Seattle landmark               | 47.6205  | -122.3493 | Seattle       | WA    |
    | Statue of Liberty | Monument on Liberty Island     | 40.6892  | -74.0445  | New York      | NY    |
  When I search for locations in city "New York"
  Then I should receive a successful location search result
  And the location search result should contain 2 locations
  And the location search result should include "Empire State"
  And the location search result should include "Statue of Liberty"

Scenario: Find locations near a specific coordinate
  Given the application is initialized for testing
  And I have multiple locations stored in the system for search:
    | Title             | Description                    | Latitude | Longitude | City          | State |
    | Golden Gate       | Famous bridge in San Francisco | 37.8199  | -122.4783 | San Francisco | CA    |
    | Empire State      | Iconic skyscraper              | 40.7484  | -73.9857  | New York      | NY    |
    | Grand Canyon      | Natural wonder                 | 36.1069  | -112.1129 | Grand Canyon  | AZ    |
    | Space Needle      | Seattle landmark               | 47.6205  | -122.3493 | Seattle       | WA    |
    | Statue of Liberty | Monument on Liberty Island     | 40.6892  | -74.0445  | New York      | NY    |
  When I search for locations within 50 km of coordinates:
    | Latitude | Longitude |
    | 40.7128  | -74.0060  |
  Then I should receive a successful location search result
  And the location search result should contain 2 locations
  And the location search result should include "Empire State"
  And the location search result should include "Statue of Liberty"
  And the location search result should not include "Golden Gate"

Scenario: Search locations with multiple filters
  Given the application is initialized for testing
  And I have multiple locations stored in the system for search:
    | Title             | Description                    | Latitude | Longitude | City          | State |
    | Golden Gate       | Famous bridge in San Francisco | 37.8199  | -122.4783 | San Francisco | CA    |
    | Empire State      | Iconic skyscraper              | 40.7484  | -73.9857  | New York      | NY    |
    | Grand Canyon      | Natural wonder                 | 36.1069  | -112.1129 | Grand Canyon  | AZ    |
    | Space Needle      | Seattle landmark               | 47.6205  | -122.3493 | Seattle       | WA    |
    | Statue of Liberty | Monument on Liberty Island     | 40.6892  | -74.0445  | New York      | NY    |
  When I search for locations with the following criteria:
    | Title    | City     | State |
    | Needle   | Seattle  | WA    |
  Then I should receive a successful location search result
  And the location search result should contain 1 location
  And the location search result should include "Space Needle"

Scenario: Search locations with no matches
  Given the application is initialized for testing
  And I have multiple locations stored in the system for search:
    | Title             | Description                    | Latitude | Longitude | City          | State |
    | Golden Gate       | Famous bridge in San Francisco | 37.8199  | -122.4783 | San Francisco | CA    |
    | Empire State      | Iconic skyscraper              | 40.7484  | -73.9857  | New York      | NY    |
    | Grand Canyon      | Natural wonder                 | 36.1069  | -112.1129 | Grand Canyon  | AZ    |
    | Space Needle      | Seattle landmark               | 47.6205  | -122.3493 | Seattle       | WA    |
    | Statue of Liberty | Monument on Liberty Island     | 40.6892  | -74.0445  | New York      | NY    |
  When I search for locations in city "Chicago"
  Then I should receive a successful location search result
  And the location search result should contain 0 locations