Feature: Tip Management
    As a user
    I want to manage photography tips
    So that I can improve my photography skills and reference them when taking photos

Background:
    Given the application is initialized for testing
    And I have the following tip types in the system:
        | Name          | I8n     |
        | Landscape     | en-US   |
        | Portrait      | en-US   |
        | Night         | en-US   |

@tipCreation
Scenario: Create a new photography tip
    When I create a new tip with the following details:
        | TipTypeId | Title            | Content                                     | Fstop | ShutterSpeed | Iso   |
        | 1         | Golden Hour      | Take advantage of morning and evening light | f/8   | 1/125s      | 100   |
    Then I should receive a successful result
    And the tip should be created successfully
    And the tip should have the correct details:
        | TipTypeId | Title            | Content                                     | Fstop | ShutterSpeed | Iso   |
        | 1         | Golden Hour      | Take advantage of morning and evening light | f/8   | 1/125s      | 100   |

@tipUpdate
Scenario: Update an existing tip
    Given I have a tip with the following details:
        | TipTypeId | Title            | Content                                     | Fstop | ShutterSpeed | Iso   |
        | 1         | Original Title   | Initial content                             | f/5.6 | 1/60s       | 400   |
    When I update the tip with the following details:
        | Title           | Content                           | Fstop | ShutterSpeed | Iso   |
        | Updated Title   | Updated content for the tip       | f/8   | 1/125s      | 200   |
    Then I should receive a successful result
    And the tip should be updated successfully
    And the tip should have the following details:
        | TipTypeId | Title           | Content                           | Fstop | ShutterSpeed | Iso   |
        | 1         | Updated Title   | Updated content for the tip       | f/8   | 1/125s      | 200   |

@tipDeletion
Scenario: Delete a tip
    Given I have a tip with the following details:
        | TipTypeId | Title            | Content           |
        | 2         | Tip to Delete    | Will be removed   |
    When I delete the tip
    Then I should receive a successful result
    And the tip should be deleted successfully
    And the tip should not exist in the system

@tipRetrieval
Scenario: Retrieve a tip by ID
    Given I have a tip with the following details:
        | TipTypeId | Title            | Content           |
        | 3         | Night Sky        | Astrophotography  |
    When I retrieve the tip by its ID
    Then I should receive a successful result
    And the retrieved tip should match the original tip details

@tipByType
Scenario: Get tips by type
    Given I have multiple tips for each type:
        | TipTypeId | Title              | Content                   |
        | 1         | Landscape Tip 1    | First landscape tip       |
        | 1         | Landscape Tip 2    | Second landscape tip      |
        | 2         | Portrait Tip 1     | First portrait tip        |
        | 3         | Night Tip 1        | First night photography   |
    When I request tips for type "Landscape"
    Then I should receive a successful result
    And the result should contain 2 tips
    And the result should include "Landscape Tip 1"
    And the result should include "Landscape Tip 2"
    And the result should not include "Portrait Tip 1"
    And the result should not include "Night Tip 1"

@randomTip
Scenario: Get a random tip of a specific type
    Given I have multiple tips for the "Portrait" type:
        | Title              | Content               |
        | Portrait Tip 1     | First portrait tip    |
        | Portrait Tip 2     | Second portrait tip   |
        | Portrait Tip 3     | Third portrait tip    |
    When I request a random tip for type "Portrait"
    Then I should receive a successful result
    And the result should contain a single tip
    And the tip should be of type "Portrait"