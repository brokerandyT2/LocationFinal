Feature: Application Integration
  As a photographer
  I want all photography features to work together seamlessly
  So that I can have a complete end-to-end photography planning experience

  Background:
    Given the photography application is initialized for testing

  Scenario: Complete photography planning workflow
    Given I am a photographer planning a shoot
    And I have selected a location with coordinates 40.7128, -74.0060
    And I want to shoot on "2024-06-15"
    When I plan a complete photography session
    Then I should receive a successful photography result
    And I should get sun position calculations for the date
    And I should get weather predictions for the location
    And I should get optimal shooting time recommendations
    And I should get recommended camera settings
    And I should receive alerts for optimal conditions

  Scenario: Camera equipment and exposure integration
    Given I have saved a camera body with the following specifications:
      | Id | Make   | Model   | SensorWidth | SensorHeight | IsoRange  | MountType |
      | 1  | Canon  | EOS R5  | 36.0        | 24.0         | 100-51200 | RF        |
    And I have a compatible lens:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | ApertureMin | MountType |
      | 1  | Canon  | RF 24-70mm f/2.8| 24             | 70             | 2.8         | RF        |
    And I have base exposure settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        | 400     |
    When I calculate exposure for my equipment setup
    Then I should receive a successful photography result
    And the exposure should account for camera ISO capabilities
    And the aperture should be within lens specifications
    And equipment compatibility should be verified

  Scenario: Location-based sun tracking integration
    Given I am at a photography location with coordinates 51.5074, -0.1278
    And I want to track the sun for optimal lighting
    And today's date is "2024-06-21" (summer solstice)
    When I start integrated sun tracking for the location
    Then I should receive a successful photography result
    And sun position should be calculated for the location
    And golden hour times should be identified
    And I should receive real-time sun tracking updates
    And location-specific recommendations should be provided

  Scenario: Weather-aware exposure recommendations
    Given I am planning a shoot at location 40.7128, -74.0060
    And the weather conditions are:
      | CloudCover | Precipitation | Humidity | Visibility | Description |
      | 60         | 0             | 70       | 8          | Overcast    |
    And I have camera equipment with ISO range 100-6400
    When I get weather-aware exposure recommendations
    Then I should receive a successful photography result
    And the exposure should compensate for reduced light
    And ISO recommendations should account for weather conditions
    And shooting tips for overcast conditions should be provided

  Scenario: Multi-location shooting day planning
    Given I want to plan a multi-location shooting day
    And I have the following locations saved:
      | Id | Name         | Latitude  | Longitude   | BestTimes        | TravelTime |
      | 1  | Central Park | 40.785091 | -73.968285  | 07:00-08:00      | 30         |
      | 2  | Brooklyn Br  | 40.706086 | -73.996864  | 19:00-20:00      | 45         |
      | 3  | Times Square | 40.758896 | -73.985130  | 20:30-21:30      | 20         |
    And today's date is "2024-06-15"
    When I create an integrated shooting schedule
    Then I should receive a successful photography result
    And the schedule should optimize travel time between locations
    And each location should have optimal lighting windows
    And buffer time for setup should be included
    And weather considerations should be factored in

  Scenario: Meteor shower photography planning integration
    Given I want to plan meteor shower photography
    And today's date is "2024-08-12" (Perseids peak)
    And I am at a dark sky location with coordinates 44.3106, -71.9926
    When I plan integrated meteor shower photography
    Then I should receive a successful photography result
    And active meteor showers should be identified
    And optimal viewing times should be calculated
    And moon phase impact should be assessed
    And recommended camera settings for astrophotography should be provided
    And alerts should be set for peak activity times

  Scenario: Image analysis feedback loop integration
    Given I have captured images during a photography session
    And I have image files with the following characteristics:
      | ImagePath              | MeanRed | MeanGreen | MeanBlue | ColorTemperature | ExposureSettings    |
      | /session/image001.jpg  | 140     | 135       | 130      | 5200             | 1/125, f/8, ISO 400 |
      | /session/image002.jpg  | 160     | 155       | 145      | 4800             | 1/250, f/5.6, ISO 400|
    When I analyze session images for feedback
    Then I should receive a successful photography result
    And image analysis should provide exposure quality feedback
    And color temperature analysis should suggest white balance adjustments
    And composition analysis should be performed
    And recommendations for future shoots should be generated

  Scenario: Alert-driven shooting workflow
    Given I have alerts enabled for optimal conditions
    And I receive a light quality alert:
      | AlertType | Severity | ValidFrom           | ValidTo             | LightQuality | LocationId |
      | Light     | Info     | 2024-06-15T07:00:00 | 2024-06-15T08:00:00 | Excellent    | 1          |
    When I respond to the shooting alert
    Then I should receive a successful photography result
    And location details should be retrieved automatically
    And current sun position should be calculated
    And optimal camera settings should be recommended
    And travel time to location should be estimated
    And session planning should be initiated

  Scenario: Equipment recommendation based on conditions
    Given I am planning to shoot in the following conditions:
      | LightLevel | PhotoType        | WeatherConditions | LocationCharacteristics |
      | Low        | Wildlife         | Foggy             | Forest                  |
    And I have access to multiple camera bodies and lenses
    When I get equipment recommendations for the conditions
    Then I should receive a successful photography result
    And high ISO capable cameras should be recommended
    And telephoto lenses should be suggested for wildlife
    And weather sealing considerations should be noted
    And specific camera settings should be provided

  Scenario: Real-time condition monitoring integration
    Given I am actively shooting at location 40.7128, -74.0060
    And I have started a photography session at "2024-06-15T07:30:00"
    When I monitor real-time conditions during the shoot
    Then I should receive a successful photography result
    And sun position should be continuously updated
    And light quality changes should be tracked
    And weather updates should be monitored
    And exposure adjustments should be recommended as conditions change

  Scenario: Cross-feature data consistency
    Given I have data across multiple photography features:
      | Feature           | DataType        | Value                    |
      | Location          | Coordinates     | 40.7128, -74.0060       |
      | Sun Calculator    | Sunrise Time    | 2024-06-15T05:45:00     |
      | Weather           | Conditions      | Clear skies              |
      | Exposure Calc     | Settings        | 1/125, f/8, ISO 200     |
      | Equipment         | Camera Body     | Canon EOS R5             |
    When I verify data consistency across features
    Then I should receive a successful photography result
    And all location coordinates should match
    And sun calculations should align with exposure recommendations
    And equipment capabilities should match exposure settings
    And weather data should be consistent across features

  Scenario: Performance under load integration
    Given I have a complex photography planning scenario with:
      | Locations | CameraBodies | Lenses | ExposureCalculations | SunCalculations | WeatherQueries |
      | 50        | 20           | 100    | 500                  | 200             | 50             |
    When I execute the complete planning workflow
    Then I should receive a successful photography result
    And all calculations should complete within reasonable time
    And data consistency should be maintained
    And no performance degradation should occur
    And memory usage should remain stable

  Scenario: Error handling across integrated features
    Given I have a photography planning workflow with potential failures:
      | Feature           | PotentialError              | FailureRate |
      | Weather Service   | API timeout                 | 10%         |
      | Sun Calculator    | Invalid coordinates         | 5%          |
      | Exposure Calc     | Invalid camera settings     | 3%          |
      | Image Analysis    | Corrupted image file        | 8%          |
    When I execute the workflow with error simulation
    Then the system should handle errors gracefully
    And partial results should be returned when possible
    And error messages should be descriptive and helpful
    And fallback options should be provided
    And the workflow should continue with available data

  Scenario: User preference integration across features
    Given I have user preferences set across multiple features:
      | Feature           | Preference              | Value                    |
      | Exposure          | Default ISO Range       | 100-1600                 |
      | Sun Calculator    | Preferred Golden Hour   | Evening                  |
      | Weather           | Alert Threshold         | Warning and above        |
      | Equipment         | Favorite Camera         | Canon EOS R5             |
      | Location          | Preferred Region        | Northeast US             |
    When I plan photography with integrated user preferences
    Then I should receive a successful photography result
    And all recommendations should respect user preferences
    And preferred equipment should be prioritized
    And alerts should follow user-defined thresholds
    And location suggestions should match regional preferences