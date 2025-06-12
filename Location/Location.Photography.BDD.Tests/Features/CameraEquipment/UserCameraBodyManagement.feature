Feature: User Camera Body Management
  As a photographer
  I want to save and manage my camera equipment
  So that I can track my gear and get personalized recommendations

  Background:
    Given the photography application is initialized for testing

  Scenario: Save camera body to user collection
    Given I am a user with ID "user123"
    And I have a camera body available with the following specifications:
      | Id | Make   | Model   | SensorWidth | SensorHeight | MountType |
      | 1  | Canon  | EOS R5  | 36.0        | 24.0         | RF        |
    When I save the camera body to my collection
    Then I should receive a successful photography result
    And the camera body should be saved to my collection
    And the saved camera should have the correct specifications

  Scenario: Get all saved camera bodies for user
    Given I am a user with ID "user123"
    And I have saved the following camera bodies:
      | Id | Make   | Model     | SensorWidth | SensorHeight | MountType | IsFavorite |
      | 1  | Canon  | EOS R5    | 36.0        | 24.0         | RF        | true       |
      | 2  | Nikon  | Z9        | 35.9        | 23.9         | Z         | false      |
    When I get all my saved camera bodies
    Then I should receive a successful photography result
    And I should receive 2 camera bodies
    And the results should include "Canon EOS R5" and "Nikon Z9"

  Scenario: Get specific user camera body by camera ID
    Given I am a user with ID "user123"
    And I have saved a camera body with the following details:
      | CameraBodyId | Make   | Model   | IsFavorite | Notes              |
      | 1            | Canon  | EOS R5  | true       | My primary camera  |
    When I get my saved camera body by camera ID 1
    Then I should receive a successful photography result
    And I should receive the camera body details
    And the camera should be marked as favorite
    And the notes should be "My primary camera"

  Scenario: Check if camera body exists in user collection
    Given I am a user with ID "user123"
    And I have saved a camera body with ID 1
    When I check if camera body 1 exists in my collection
    Then I should receive a successful photography result
    And the camera body should exist in my collection

  Scenario: Update saved camera body details
    Given I am a user with ID "user123"
    And I have saved a camera body with the following details:
      | CameraBodyId | Make   | Model   | IsFavorite | Notes              |
      | 1            | Canon  | EOS R5  | false      | Secondary camera   |
    When I update my saved camera body with new details:
      | IsFavorite | Notes              |
      | true       | My primary camera  |
    Then I should receive a successful photography result
    And the camera body should be updated successfully
    And the camera should be marked as favorite
    And the notes should be "My primary camera"

  Scenario: Remove camera body from user collection
    Given I am a user with ID "user123"
    And I have saved a camera body with ID 1
    When I remove camera body 1 from my collection
    Then I should receive a successful photography result
    And the camera body should be removed successfully
    And the camera body should not exist in my collection

  Scenario: Get user's favorite camera bodies
    Given I am a user with ID "user123"
    And I have saved the following camera bodies:
      | Id | Make   | Model     | IsFavorite | Notes              |
      | 1  | Canon  | EOS R5    | true       | My primary camera  |
      | 2  | Nikon  | Z9        | false      | Backup camera      |
      | 3  | Sony   | A7R V     | true       | Portrait camera    |
    When I get my favorite camera bodies
    Then I should receive a successful photography result
    And I should receive 2 camera bodies
    And all results should be marked as favorites
    And the results should include "Canon EOS R5" and "Sony A7R V"

  Scenario: Mark camera body as favorite
    Given I am a user with ID "user123"
    And I have saved a camera body with the following details:
      | CameraBodyId | Make   | Model   | IsFavorite |
      | 1            | Canon  | EOS R5  | false      |
    When I mark camera body 1 as favorite
    Then I should receive a successful photography result
    And the camera body should be marked as favorite

  Scenario: Unmark camera body as favorite
    Given I am a user with ID "user123"
    And I have saved a camera body with the following details:
      | CameraBodyId | Make   | Model   | IsFavorite |
      | 1            | Canon  | EOS R5  | true       |
    When I unmark camera body 1 as favorite
    Then I should receive a successful photography result
    And the camera body should not be marked as favorite

  Scenario: Add notes to saved camera body
    Given I am a user with ID "user123"
    And I have saved a camera body with ID 1
    When I add notes to my camera body:
      | Notes                                    |
      | Excellent for landscape photography      |
    Then I should receive a successful photography result
    And the camera body notes should be updated
    And the notes should contain "landscape photography"

  Scenario: Get all user camera bodies across all users
    Given there are multiple users with saved camera bodies:
      | UserId  | CameraBodyId | Make   | Model     | IsFavorite |
      | user123 | 1            | Canon  | EOS R5    | true       |
      | user456 | 2            | Nikon  | Z9        | false      |
      | user789 | 1            | Canon  | EOS R5    | true       |
    When I get all user camera bodies in the system
    Then I should receive a successful photography result
    And I should receive 3 user camera body records
    And the results should span multiple users

  Scenario: Prevent duplicate camera body saves
    Given I am a user with ID "user123"
    And I have already saved camera body with ID 1
    When I try to save the same camera body again
    Then I should receive a photography failure result
    And the error should indicate the camera body is already saved

  Scenario: Handle saving non-existent camera body
    Given I am a user with ID "user123"
    When I try to save a camera body with ID 999 that doesn't exist
    Then I should receive a photography failure result
    And the error should indicate the camera body was not found

  Scenario: Get camera compatibility recommendations
    Given I am a user with ID "user123"
    And I have saved camera bodies with the following mount types:
      | CameraBodyId | Make   | Model   | MountType |
      | 1            | Canon  | EOS R5  | RF        |
      | 2            | Canon  | EOS 5D  | EF        |
    When I get lens compatibility recommendations
    Then I should receive a successful photography result
    And the recommendations should include RF and EF mount lenses
    And the recommendations should prioritize native mounts

  Scenario: Track camera usage statistics
    Given I am a user with ID "user123"
    And I have saved a camera body with the following details:
      | CameraBodyId | Make   | Model   | DateSaved           | LastUsed            |
      | 1            | Canon  | EOS R5  | 2024-01-01T00:00:00 | 2024-06-01T00:00:00 |
    When I get my camera usage statistics
    Then I should receive a successful photography result
    And the statistics should show the last used date
    And the statistics should show how long I've owned the camera

  Scenario: Export user camera collection
    Given I am a user with ID "user123"
    And I have saved multiple camera bodies with detailed information:
      | CameraBodyId | Make   | Model     | IsFavorite | Notes              | DateSaved           |
      | 1            | Canon  | EOS R5    | true       | Primary camera     | 2024-01-01T00:00:00 |
      | 2            | Nikon  | Z9        | false      | Backup camera      | 2024-02-01T00:00:00 |
    When I export my camera collection
    Then I should receive a successful photography result
    And the export should include all camera details
    And the export should be in a structured format