Feature: Settings Management
    As a user
    I want to manage application settings
    So that I can configure and personalize the application

Background:
    Given the application is initialized for testing

@settingCreation
Scenario: Create a new setting
    When I create a new setting with the following details:
        | Key                 | Value    | Description                   |
        | DarkModeEnabled     | true     | Enable dark mode for the app  |
    Then I should receive a successful result
    And the setting should be created successfully
    And the setting should have the correct details:
        | Key                 | Value    | Description                   |
        | DarkModeEnabled     | true     | Enable dark mode for the app  |

@settingUpdate
Scenario: Update an existing setting
    Given I have a setting with the following details:
        | Key                 | Value    | Description                   |
        | MapZoomLevel        | 10       | Default zoom level for maps   |
    When I update the setting with the following value:
        | Value   |
        | 12      |
    Then I should receive a successful result
    And the setting should be updated successfully
    And the setting should have the new value "12"
    And the setting timestamp should be updated

@settingDeletion
Scenario: Delete a setting
    Given I have a setting with the following details:
        | Key                    | Value    | Description                |
        | TemporaryNotification  | 5000     | Notification display time  |
    When I delete the setting
    Then I should receive a successful result
    And the setting should be deleted successfully
    And the setting should not exist in the system

@settingRetrieval
Scenario: Retrieve a setting by key
    Given I have a setting with the following details:
        | Key                 | Value    | Description                |
        | CacheTimeoutMinutes | 30       | Time to keep cached data   |
    When I retrieve the setting by its key
    Then I should receive a successful result
    And the retrieved setting should match the original setting details

@settingsList
Scenario: List all settings
    Given I have multiple settings in the system:
        | Key                 | Value    | Description                |
        | DarkModeEnabled     | true     | Enable dark mode           |
        | MapZoomLevel        | 10       | Default zoom level         |
        | CacheTimeoutMinutes | 30       | Cache timeout in minutes   |
        | AutoSync            | false    | Enable automatic sync      |
    When I request a list of all settings
    Then I should receive a successful result
    And the result should contain 4 settings
    And the settings list should include "DarkModeEnabled"
    And the settings list should include "MapZoomLevel"
    And the settings list should include "CacheTimeoutMinutes"
    And the settings list should include "AutoSync"

@settingUpsert
Scenario: Create or update a setting (upsert)
    When I upsert a setting with the following details:
        | Key                 | Value    | Description                |
        | NewSetting          | 42       | New setting value          |
    Then I should receive a successful result
    And the setting should be created successfully
    When I upsert a setting with the following details:
        | Key                 | Value    | Description                |
        | NewSetting          | 99       | Updated description        |
    Then I should receive a successful result
    And the setting should have the new value "99"
    And the setting description should be "Updated description"

@settingValueConversion
Scenario: Convert setting values to different types
    Given I have settings with different value types:
        | Key                 | Value    |
        | BooleanSetting      | true     |
        | IntegerSetting      | 42       |
        | DateTimeSetting     | 2023-01-01T12:00:00Z |
    When I retrieve the settings and convert their values
    Then the "BooleanSetting" should be converted to boolean true
    And the "IntegerSetting" should be converted to integer 42
    And the "DateTimeSetting" should be converted to a valid date time

@settingDictionary
Scenario: Get all settings as dictionary
    Given I have multiple settings in the system:
        | Key                 | Value    | Description                |
        | Setting1            | value1   | First setting              |
        | Setting2            | value2   | Second setting             |
        | Setting3            | value3   | Third setting              |
    When I request all settings as a dictionary
    Then I should receive a successful result
    And the dictionary should contain 3 key-value pairs
    And the dictionary should have key "Setting1" with value "value1"
    And the dictionary should have key "Setting2" with value "value2"
    And the dictionary should have key "Setting3" with value "value3"