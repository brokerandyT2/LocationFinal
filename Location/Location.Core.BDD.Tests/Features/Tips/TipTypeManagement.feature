Feature: Tip Type Management
    As a user
    I want to manage photography tip types
    So that I can categorize my photography tips

Background:
    Given the application is initialized for testing

@tipTypeCreation
Scenario: Create a new tip type
    When I create a new tip type with the following details:
        | Name        | I8n     |
        | Macro       | en-US   |
    Then I should receive a successful result
    And the tip type should be created successfully
    And the tip type should have the correct details:
        | Name        | I8n     |
        | Macro       | en-US   |

@tipTypeUpdate
Scenario: Update an existing tip type
    Given I have a tip type with the following details:
        | Name            | I8n     |
        | Original Name   | en-US   |
    When I update the tip type with the following details:
        | Name            | I8n     |
        | Updated Name    | en-GB   |
    Then I should receive a successful result
    And the tip type should be updated successfully
    And the tip type should have the following details:
        | Name            | I8n     |
        | Updated Name    | en-GB   |

@tipTypeDeletion
Scenario: Delete a tip type
    Given I have a tip type with the following details:
        | Name                | I8n     |
        | Type To Delete     | en-US   |
    And the tip type has no associated tips
    When I delete the tip type
    Then I should receive a successful result
    And the tip type should be deleted successfully
    And the tip type should not exist in the system

@tipTypeList
Scenario: List all tip types
    Given I have multiple tip types in the system:
        | Name          | I8n     |
        | Landscape     | en-US   |
        | Portrait      | en-US   |
        | Street        | en-US   |
        | Wildlife      | en-US   |
    When I request a list of all tip types
    Then I should receive a successful result
    And the result should contain 4 tip types
    And the tip type list should include "Landscape"
    And the tip type list should include "Portrait"
    And the tip type list should include "Street"
    And the tip type list should include "Wildlife"

@tipTypeWithTips
Scenario: Retrieve a tip type with its associated tips
    Given I have a tip type with the following details:
        | Name          | I8n     |
        | Architecture  | en-US   |
    And the tip type has the following associated tips:
        | Title             | Content                       |
        | Rule of Thirds    | Align key elements to thirds  |
        | Leading Lines     | Use lines to guide the viewer |
    When I retrieve the tip type with its associated tips
    Then I should receive a successful result
    And the tip type should have 2 associated tips
    And the tips should include "Rule of Thirds"
    And the tips should include "Leading Lines"

@tipTypeWithLocalization
Scenario: Create a tip type with different localization
    When I create tip types with different localizations:
        | Name      | I8n     |
        | Wildlife  | en-US   |
        | Vida Silvestre | es-ES   |
        | Faune     | fr-FR   |
    Then I should receive successful results
    And the tip types should be created with the correct localizations