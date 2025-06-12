Feature: Meteor Shower Tracking
  As an astrophotographer
  I want to track meteor shower events and their visibility
  So that I can plan photography sessions for optimal meteor capture

  Background:
    Given the photography application is initialized for testing

  Scenario: Get active meteor showers for specific date
    Given I want to track meteor showers for photography
    And today's date is "2024-08-12"
    When I get active meteor showers for the date
    Then I should receive a successful photography result
    And I should receive meteor shower data
    And the results should include active showers for that date
    And the showers should be ordered by ZHR descending

  Scenario: Get active meteor showers with minimum ZHR threshold
    Given I want to track meteor showers for photography
    And today's date is "2024-08-12"
    And I want showers with minimum ZHR of 50
    When I get active meteor showers with ZHR threshold
    Then I should receive a successful photography result
    And I should receive meteor shower data
    And all showers should have ZHR of 50 or higher
    And the results should prioritize high ZHR showers

  Scenario: Get specific meteor shower by code
    Given I want to track meteor showers for photography
    When I get meteor shower details by code "PER"
    Then I should receive a successful photography result
    And I should receive the Perseids meteor shower details
    And the shower name should be "Perseids"
    And the shower should have valid date ranges

  Scenario: Get all available meteor showers
    Given I want to track meteor showers for photography
    When I get all meteor showers in the database
    Then I should receive a successful photography result
    And I should receive a complete list of meteor showers
    And the results should be ordered by designation
    And the list should include major showers like Perseids and Geminids

  Scenario: Get meteor showers within date range
    Given I want to track meteor showers for photography
    And I have a date range from "2024-08-01" to "2024-08-31"
    When I get meteor showers within the date range
    Then I should receive a successful photography result
    And I should receive meteor showers active in August
    And the results should include the Perseids shower
    And all showers should overlap with the specified date range

  Scenario: Track Perseids meteor shower peak
    Given I want to track meteor showers for photography
    And today's date is "2024-08-12"
    When I get meteor shower details by code "PER"
    Then I should receive a successful photography result
    And the Perseids should be at or near peak activity
    And the ZHR should be at maximum expected rate
    And the shower should be active

  Scenario: Track Geminids meteor shower peak
    Given I want to track meteor showers for photography
    And today's date is "2024-12-14"
    When I get meteor shower details by code "GEM"
    Then I should receive a successful photography result
    And the Geminids should be at or near peak activity
    And the ZHR should be at maximum expected rate
    And the shower should be active

  Scenario: Get meteor shower radiant position
    Given I want to track meteor showers for photography
    When I get meteor shower details by code "PER"
    Then I should receive a successful photography result
    And the shower should have radiant coordinates
    And the radiant should be in the Perseus constellation
    And the radiant position should be valid for tracking

  Scenario: Calculate meteor shower visibility for location
    Given I want to track meteor showers for photography
    And I am at location coordinates 40.7128, -74.0060
    And today's date is "2024-08-12"
    When I calculate meteor shower visibility for my location
    Then I should receive a successful photography result
    And I should receive visibility information
    And the best viewing times should be calculated
    And the moon phase impact should be considered

  Scenario: Get photography-worthy meteor showers
    Given I want to track meteor showers for photography
    And today's date is "2024-08-12"
    And I want showers suitable for photography with minimum ZHR of 20
    When I get photography-worthy meteor showers
    Then I should receive a successful photography result
    And all showers should have ZHR of 20 or higher
    And the results should exclude minor showers
    And the showers should be ranked by photography potential

  Scenario: Track meteor shower duration and peak timing
    Given I want to track meteor showers for photography
    When I get meteor shower details by code "PER"
    Then I should receive a successful photography result
    And the shower should have a defined active period
    And the peak date should be within the active period
    And the shower duration should be reasonable (multiple weeks)

  Scenario: Get upcoming meteor showers for planning
    Given I want to track meteor showers for photography
    And today's date is "2024-07-01"
    And I want to plan for the next 6 months
    When I get upcoming meteor showers for planning
    Then I should receive a successful photography result
    And the results should include future showers
    And the Perseids should be included (August peak)
    And the Geminids should be included (December peak)

  Scenario: Compare meteor shower intensities
    Given I want to track meteor showers for photography
    And I have multiple meteor showers:
      | Code | Name      | PeakDate   | MaxZHR |
      | PER  | Perseids  | 2024-08-12 | 100    |
      | GEM  | Geminids  | 2024-12-14 | 120    |
      | QUA  | Quadrantids| 2024-01-04 | 80     |
    When I compare meteor shower intensities
    Then I should receive a successful photography result
    And the Geminids should have the highest ZHR
    And the showers should be ranked by maximum ZHR
    And all ZHR values should be positive

  Scenario: Handle invalid meteor shower code
    Given I want to track meteor showers for photography
    When I get meteor shower details by code "XYZ"
    Then I should receive a successful photography result
    And the result should be null or empty
    And no meteor shower should be returned for invalid code

  Scenario: Track meteor shower calendar for year
    Given I want to track meteor showers for photography
    And I want to plan for the year 2024
    When I get the meteor shower calendar for 2024
    Then I should receive a successful photography result
    And the calendar should include all major showers
    And each month should have shower information
    And peak dates should be accurate for 2024

  Scenario: Get meteor shower observing conditions
    Given I want to track meteor showers for photography
    And today's date is "2024-08-12"
    When I get meteor shower observing conditions
    Then I should receive a successful photography result
    And the conditions should include moon phase information
    And the best observing hours should be specified
    And weather impact should be considered
    And light pollution effects should be noted

  Scenario: Calculate meteor shower photography settings
    Given I want to track meteor showers for photography
    And I have a meteor shower with ZHR of 100
    And I am using a camera with the following settings:
      | SensorSize | FocalLength | Aperture | ISO  |
      | Full Frame | 24mm        | f/2.8    | 3200 |
    When I calculate recommended photography settings
    Then I should receive a successful photography result
    And the exposure time should be recommended
    And the interval between shots should be calculated
    And the total session duration should be suggested