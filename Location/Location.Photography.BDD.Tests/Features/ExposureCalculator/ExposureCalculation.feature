Feature: Exposure Calculator
  As a photographer
  I want to calculate equivalent exposures
  So that I can maintain the same exposure while changing camera settings

  Background:
    Given the photography application is initialized for testing

  Scenario: Calculate equivalent shutter speed with different aperture
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        | 100     |
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso |
      | f/2.8          | 100       |
    When I calculate the shutter speed
    Then I should receive a successful photography result
    And the calculated shutter speed should be "1/500"

  Scenario: Calculate equivalent aperture with different shutter speed
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/60             | f/8          | 200     |
    And I want to calculate the aperture with the following target settings:
      | TargetShutterSpeed | TargetIso |
      | 1/250              | 200       |
    When I calculate the aperture
    Then I should receive a successful photography result
    And the calculated aperture should be "f/4"

  Scenario: Calculate equivalent ISO with different settings
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/30             | f/11         | 100     |
    And I want to calculate the ISO with the following target settings:
      | TargetShutterSpeed | TargetAperture |
      | 1/125              | f/8            |
    When I calculate the ISO
    Then I should receive a successful photography result
    And the calculated ISO should be "200"

  Scenario: Calculate exposure with EV compensation
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        | 100     |
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso | EvCompensation |
      | f/5.6          | 100       | 1.0            |
    When I calculate the shutter speed
    Then I should receive a successful photography result
    And the calculated shutter speed should be "1/60"

  Scenario: Calculate exposure with half-stop increments
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/4          | 100     |
    And I am using half stops increments
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso |
      | f/2.8          | 100       |
    When I calculate the shutter speed
    Then I should receive a successful photography result
    And the calculated shutter speed should be "1/250"

  Scenario: Calculate exposure with third-stop increments
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/60             | f/2.8        | 200     |
    And I am using third stops increments
    And I want to calculate the aperture with the following target settings:
      | TargetShutterSpeed | TargetIso |
      | 1/200              | 200       |
    When I calculate the aperture
    Then I should receive a successful photography result
    And the calculated aperture should be "f/5"

  Scenario: Handle overexposure calculation
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso | ErrorMessage                    |
      | 1/8000           | f/1.4        | 50      | Image will be overexposed by 2.0 stops |
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso |
      | f/1            | 25        |
    When I calculate the shutter speed
    Then I should receive a photography failure result
    And the exposure error should indicate "overexposed"

  Scenario: Handle underexposure calculation
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso | ErrorMessage                      |
      | 30               | f/22         | 25600   | Image will be underexposed by 1.5 stops |
    And I want to calculate the aperture with the following target settings:
      | TargetShutterSpeed | TargetIso |
      | 60                 | 51200     |
    When I calculate the aperture
    Then I should receive a photography failure result
    And the exposure error should indicate "underexposed"

  Scenario: Calculate multiple equivalent exposures
    Given I have multiple exposure scenarios:
      | Id | BaseShutterSpeed | BaseAperture | BaseIso | TargetAperture | TargetIso | ResultShutterSpeed | FixedValue    |
      | 1  | 1/125            | f/5.6        | 100     | f/2.8          | 100       | 1/500              | ShutterSpeeds |
      | 2  | 1/60             | f/8          | 200     | f/4            | 200       | 1/250              | ShutterSpeeds |
      | 3  | 1/30             | f/11         | 400     | f/5.6          | 400       | 1/125              | ShutterSpeeds |
    When I calculate the equivalent exposure
    Then I should receive a successful photography result

  Scenario Outline: Calculate exposures with different increment types
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | <BaseShutter>    | <BaseAperture> | <BaseIso> |
    And I am using <Increments> stops increments
    And I want to calculate the <CalculationType> with the following target settings:
      | TargetShutterSpeed | TargetAperture | TargetIso |
      | <TargetShutter>    | <TargetAperture> | <TargetIso> |
    When I calculate the equivalent exposure
    Then I should receive a successful photography result
    And the calculated <CalculationType> should be "<ExpectedResult>"

    Examples:
      | BaseShutter | BaseAperture | BaseIso | Increments | CalculationType | TargetShutter | TargetAperture | TargetIso | ExpectedResult |
      | 1/125       | f/5.6        | 100     | full       | shutter speed   |               | f/2.8          | 100       | 1/500          |
      | 1/60        | f/8          | 200     | half       | aperture        | 1/250         |                | 200       | f/4            |
      | 1/30        | f/11         | 400     | third      | ISO             | 1/125         | f/8            |           | 800            |

  Scenario: Calculate exposure with negative EV compensation
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        | 100     |
    And I apply an EV compensation of -1.0
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso |
      | f/5.6          | 100       |
    When I calculate the shutter speed
    Then I should receive a successful photography result
    And the calculated shutter speed should be "1/250"

  Scenario: Validate calculated exposure maintains equivalent exposure value
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/250            | f/4          | 200     |
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso |
      | f/8            | 800       |
    When I calculate the equivalent exposure
    Then I should receive a successful photography result
    And the calculated exposure should have the following settings:
      | ResultShutterSpeed | ResultAperture | ResultIso |
      | 1/250              | f/8            | 800       |