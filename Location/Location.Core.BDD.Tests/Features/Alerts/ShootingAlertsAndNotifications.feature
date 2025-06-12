Feature: Shooting Alerts and Notifications
  As a photographer
  I want to receive timely alerts about optimal shooting conditions
  So that I never miss opportunities for great photography

  Background:
    Given the photography application is initialized for testing

  Scenario: Create weather alert for photography
    Given I want to create weather alerts for photography
    And I am at location coordinates 40.7128, -74.0060
    When I create a weather alert with the following details:
      | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | Weather   | Warning  | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Thunderstorm approaching   |
    Then I should receive a successful photography result
    And the weather alert should be created successfully
    And the alert should be active for the specified time period

  Scenario: Create light quality alert
    Given I want to create light quality alerts
    And I am at location coordinates 40.7128, -74.0060
    When I create a light alert with the following details:
      | AlertType | Severity | ValidFrom           | ValidTo             | LightQuality | Message                    |
      | Light     | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Excellent    | Golden hour starting soon  |
    Then I should receive a successful photography result
    And the light alert should be created successfully
    And the alert should include optimal light timing

  Scenario: Create shooting window alert
    Given I want to create shooting window alerts
    And I am at location coordinates 40.7128, -74.0060
    When I create a shooting alert with the following details:
      | LocationId | AlertTime           | ShootingWindowStart | ShootingWindowEnd   | LightQuality | RecommendedSettings | Message                    |
      | 1          | 2024-06-15T06:30:00 | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Excellent    | f/8, 1/125s, ISO 200| Perfect golden hour window |
    Then I should receive a successful photography result
    And the shooting alert should be created successfully
    And the alert should include camera settings recommendations

  Scenario: Create calibration alert for equipment
    Given I want to create calibration alerts
    When I create a calibration alert with the following details:
      | AlertType    | Severity | Message                           | ValidFrom           | ValidTo             |
      | Calibration  | Warning  | Light meter requires recalibration| 2024-06-15T08:00:00 | 2024-06-15T18:00:00 |
    Then I should receive a successful photography result
    And the calibration alert should be created successfully
    And the alert should remind about equipment maintenance

  Scenario: Get active alerts for current time
    Given I have multiple alerts in the system:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Weather   | Warning  | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Thunderstorm approaching   |
      | 2  | Light     | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Golden hour active         |
      | 3  | Light     | Info     | 2024-06-16T07:00:00 | 2024-06-16T08:00:00 | Tomorrow's golden hour     |
    And the current time is "2024-06-15T15:00:00"
    When I get active alerts for the current time
    Then I should receive a successful photography result
    And I should receive 1 active alert
    And the active alert should be the weather warning

  Scenario: Get alerts by severity level
    Given I have alerts with different severity levels:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Weather   | Critical | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Severe weather warning     |
      | 2  | Light     | Warning  | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Suboptimal light conditions|
      | 3  | Light     | Info     | 2024-06-15T19:00:00 | 2024-06-15T20:00:00 | Good evening light         |
    When I get alerts with severity "Critical"
    Then I should receive a successful photography result
    And I should receive 1 alert
    And the alert should be the severe weather warning

  Scenario: Get alerts by type
    Given I have alerts of different types:
      | Id | AlertType    | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Weather      | Warning  | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Rain expected              |
      | 2  | Light        | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Golden hour active         |
      | 3  | Shooting     | Info     | 2024-06-15T06:30:00 | 2024-06-15T08:00:00 | Perfect conditions         |
      | 4  | Calibration  | Warning  | 2024-06-15T08:00:00 | 2024-06-15T18:00:00 | Equipment check needed     |
    When I get alerts by type "Light"
    Then I should receive a successful photography result
    And I should receive 1 alert
    And the alert should be the golden hour notification

  Scenario: Get upcoming alerts for planning
    Given I have alerts scheduled for different times:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Light     | Info     | 2024-06-15T19:00:00 | 2024-06-15T20:00:00 | Evening golden hour        |
      | 2  | Weather   | Warning  | 2024-06-16T08:00:00 | 2024-06-16T12:00:00 | Morning fog expected       |
      | 3  | Shooting  | Info     | 2024-06-17T06:00:00 | 2024-06-17T07:00:00 | Excellent sunrise conditions|
    And the current time is "2024-06-15T12:00:00"
    When I get upcoming alerts for the next 24 hours
    Then I should receive a successful photography result
    And I should receive 2 alerts
    And the alerts should be ordered by time

  Scenario: Update alert details
    Given I have an existing alert with the following details:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Light     | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Golden hour starting       |
    When I update the alert with new details:
      | Severity | Message                           |
      | Warning  | Golden hour with possible clouds  |
    Then I should receive a successful photography result
    And the alert should be updated successfully
    And the alert severity should be "Warning"

  Scenario: Delete expired alerts
    Given I have alerts with different expiration times:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Light     | Info     | 2024-06-14T07:00:00 | 2024-06-14T08:00:00 | Yesterday's golden hour    |
      | 2  | Weather   | Warning  | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Current weather alert      |
      | 3  | Light     | Info     | 2024-06-16T07:00:00 | 2024-06-16T08:00:00 | Tomorrow's golden hour     |
    And the current time is "2024-06-15T15:00:00"
    When I delete expired alerts
    Then I should receive a successful photography result
    And 1 alert should be deleted
    And only current and future alerts should remain

  Scenario: Set alert preferences for user
    Given I want to customize my alert preferences
    When I set my alert preferences with the following settings:
      | AlertType    | Enabled | MinimumSeverity | NotificationMethod |
      | Weather      | true    | Warning         | Push               |
      | Light        | true    | Info            | Email              |
      | Shooting     | false   | Info            | None               |
      | Calibration  | true    | Warning         | Push               |
    Then I should receive a successful photography result
    And my alert preferences should be saved
    And shooting alerts should be disabled

  Scenario: Receive alerts based on user preferences
    Given I have alert preferences set to receive only "Warning" level alerts
    And I have alerts with different severity levels:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Weather   | Critical | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Severe weather             |
      | 2  | Light     | Warning  | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Suboptimal conditions      |
      | 3  | Light     | Info     | 2024-06-15T19:00:00 | 2024-06-15T20:00:00 | Good evening light         |
    When I get alerts based on my preferences
    Then I should receive a successful photography result
    And I should receive 2 alerts
    And both alerts should be Warning level or higher

  Scenario: Create location-specific alerts
    Given I want to create location-specific alerts
    And I have multiple photography locations:
      | LocationId | Name         | Latitude  | Longitude   |
      | 1          | Central Park | 40.785091 | -73.968285  |
      | 2          | Brooklyn Br  | 40.706086 | -73.996864  |
    When I create alerts for specific location 1:
      | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | Light     | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Great light at Central Park|
    Then I should receive a successful photography result
    And the alert should be linked to Central Park location
    And the alert should include location-specific information

  Scenario: Aggregate daily alert summary
    Given I have multiple alerts for a single day:
      | Id | AlertType | Severity | ValidFrom           | ValidTo             | Message                    |
      | 1  | Light     | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Morning golden hour        |
      | 2  | Weather   | Warning  | 2024-06-15T14:00:00 | 2024-06-15T18:00:00 | Afternoon storms           |
      | 3  | Light     | Info     | 2024-06-15T19:00:00 | 2024-06-15T20:00:00 | Evening golden hour        |
    When I get daily alert summary for "2024-06-15"
    Then I should receive a successful photography result
    And the summary should include all 3 alerts for the day
    And the summary should be organized by time
    And shooting recommendations should be provided

  Scenario: Handle alert notification delivery failures
    Given I have an alert ready for notification:
      | Id | AlertType | Severity | ValidFrom           | Message                    | NotificationMethod |
      | 1  | Weather   | Critical | 2024-06-15T14:00:00 | Severe weather approaching | Push               |
    When the notification delivery fails
    Then I should receive a photography failure result
    And the failure should be logged
    And retry attempts should be scheduled
    And alternative notification methods should be attempted