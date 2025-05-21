Feature: Location Management
    As a user
    I want to manage my locations
    So that I can save, update, and delete important places

Background:
    Given the application is initialized for testing

@locationCreation
Scenario: Create a new location
    Given I want to create a new location with the following details:
        | Title       | Description         | Latitude  | Longitude | City     | State   |
        | Home Office | My home work space  | 40.712776 | -74.005974 | New York | NY      |
    When I save the location
    Then the location should be created successfully
    And the location should have the correct details:
        | Title       | Description         | Latitude  | Longitude | City     | State   |
        | Home Office | My home work space  | 40.712776 | -74.005974 | New York | NY      |

@locationUpdate
Scenario: Update an existing location
    Given I have a location with the following details:
        | Title          | Description         | Latitude  | Longitude | City     | State   |
        | Original Place | Initial description | 40.712776 | -74.005974 | New York | NY      |
    When I update the location with the following details:
        | Title          | Description           |
        | Updated Place  | Updated description   |
    Then the location should be updated successfully
    And the location should have the following details:
        | Title          | Description           | Latitude  | Longitude | City     | State   |
        | Updated Place  | Updated description   | 40.712776 | -74.005974 | New York | NY      |

@locationDeletion
Scenario: Delete a location
    Given I have a location with the following details:
        | Title          | Description         | Latitude  | Longitude | City     | State   |
        | Temp Location  | To be deleted       | 40.712776 | -74.005974 | New York | NY      |
    When I delete the location
    Then the location should be deleted successfully
    And the location should not exist in the system

@locationRestoration
Scenario: Restore a deleted location
    Given I have a deleted location with the following details:
        | Title            | Description         | Latitude  | Longitude | City     | State   |
        | Deleted Location | Previously deleted  | 40.712776 | -74.005974 | New York | NY      |
    When I restore the location
    Then the location should be restored successfully
    And the location should exist in the system
    And the location should not be marked as deleted