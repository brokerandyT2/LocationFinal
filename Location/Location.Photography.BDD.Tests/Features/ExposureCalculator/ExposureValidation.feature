Feature: Exposure Validation
  As a photographer
  I want the exposure calculator to validate input settings
  So that I receive appropriate error messages for invalid configurations

  Background:
    Given the photography application is initialized for testing

  Scenario: Validate missing shutter speed parameter
    Given I have an invalid exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      |                  | f/5.6        | 100     |
    When I attempt to calculate with missing shutter speed
    Then I should receive a photography failure result
    And the error should indicate missing parameters

  Scenario: Validate missing aperture parameter
    Given I have an invalid exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            |              | 100     |
    When I attempt to calculate with missing aperture
    Then I should receive a photography failure result
    And the error should indicate missing parameters

  Scenario: Validate missing ISO parameter
    Given I have an invalid exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        |         |
    When I attempt to calculate with missing ISO
    Then I should receive a photography failure result
    And the error should indicate missing parameters

  Scenario: Handle overexposure conditions
    Given I have exposure settings that will cause overexposure:
      | BaseShutterSpeed | BaseAperture | BaseIso | TargetAperture | TargetIso |
      | 1/8000           | f/1.4        | 50      | f/1            | 25        |
    When I attempt to calculate the exposure
    Then I should receive a photography failure result
    And the error should indicate overexposure
    And the exposure error should indicate "overexposed by"

  Scenario: Handle underexposure conditions
    Given I have exposure settings that will cause underexposure:
      | BaseShutterSpeed | BaseAperture | BaseIso | TargetShutterSpeed | TargetAperture |
      | 30               | f/22         | 25600   | 60                 | f/32           |
    When I attempt to calculate the exposure
    Then I should receive a photography failure result
    And the error should indicate underexposure
    And the exposure error should indicate "underexposed by"

  Scenario: Handle parameter limit violations
    Given I have exposure settings that will cause parameter limits:
      | BaseShutterSpeed | BaseAperture | BaseIso | TargetShutterSpeed | ErrorMessage                                               |
      | 1/125            | f/5.6        | 100     | 1/16000            | The requested shutter speed exceeds available limits       |
    When I attempt to calculate the exposure
    Then I should receive a photography failure result
    And the error should indicate parameter limits

  Scenario: Validate extreme exposure values
    Given I have extreme exposure values:
      | BaseShutterSpeed | BaseAperture | BaseIso | ErrorMessage                                    |
      | 1/16000          | f/0.5        | 204800  | Extreme exposure values may result in calculation errors |
    When I validate the exposure settings
    Then the validation should fail
    And the error should indicate invalid values
    And the exposure settings should be marked as invalid

  Scenario: Reject multiple invalid exposure scenarios
    Given I have multiple invalid exposure scenarios:
      | Id | BaseShutterSpeed | BaseAperture | BaseIso | ErrorMessage                        |
      | 1  |                  | f/5.6        | 100     | Missing shutter speed               |
      | 2  | 1/125            |              | 100     | Missing aperture                    |
      | 3  | 1/125            | f/5.6        |         | Missing ISO                         |
      | 4  | invalid          | f/5.6        | 100     | Invalid shutter speed format        |
      | 5  | 1/125            | invalid      | 100     | Invalid aperture format             |
    When I validate the exposure settings
    Then the validation should fail
    And all invalid exposures should be rejected
    And the system should provide helpful error messages

  Scenario: Handle unsupported increment types
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        | 100     |
    When I try to use an unsupported increment type
    Then I should receive a photography failure result
    And the error should indicate unsupported increment

  Scenario: Validate acceptable exposure value ranges
    Given I have a base exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125            | f/5.6        | 100     |
    And I want to calculate the shutter speed with the following target settings:
      | TargetAperture | TargetIso |
      | f/8            | 200       |
    When I calculate the shutter speed
    Then I should receive a successful photography result
    And the exposure values should be within acceptable ranges

  Scenario Outline: Validate different types of invalid inputs
    Given I have an invalid exposure with the following settings:
      | BaseShutterSpeed   | BaseAperture   | BaseIso   |
      | <InvalidShutter>   | <InvalidAperture> | <InvalidIso> |
    When I validate the exposure settings
    Then the validation should fail
    And the error should indicate <ErrorType>

    Examples:
      | InvalidShutter | InvalidAperture | InvalidIso | ErrorType         |
      |                | f/5.6           | 100        | missing parameters |
      | 1/125          |                 | 100        | missing parameters |
      | 1/125          | f/5.6           |            | missing parameters |
      | abc            | f/5.6           | 100        | invalid values    |
      | 1/125          | invalid         | 100        | invalid values    |
      | 1/125          | f/5.6           | abc        | invalid values    |

  Scenario: Validate exposure calculation with boundary conditions
    Given I have exposure settings that will cause parameter limits:
      | BaseShutterSpeed | BaseAperture | BaseIso | TargetAperture | TargetIso | ErrorMessage                                    |
      | 1/8000           | f/64         | 50      | f/128          | 25        | The requested aperture exceeds available limits |
    When I attempt to calculate the exposure
    Then I should receive a photography failure result
    And the error should indicate parameter limits
    And no calculation should be performed

  Scenario: Handle calculation with impossible exposure combinations
    Given I have exposure settings that will cause overexposure:
      | BaseShutterSpeed | BaseAperture | BaseIso | TargetShutterSpeed | TargetAperture | TargetIso |
      | 1/8000           | f/1          | 25      | 1/16000            | f/0.5          | 12        |
    When I attempt to calculate the exposure
    Then I should receive a photography failure result
    And the error should indicate overexposure
    And the system should provide helpful error messages

  Scenario: Validate input parameter formats
    Given I have an invalid exposure with the following settings:
      | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1/125sec         | f-5.6        | ISO100  |
    When I validate the exposure settings
    Then the validation should fail
    And the error should indicate invalid values
    And the exposure settings should be marked as invalid

  Scenario: Ensure error messages are descriptive and helpful
    Given I have multiple invalid exposure scenarios:
      | Id | BaseShutterSpeed | BaseAperture | BaseIso | ErrorMessage                                           |
      | 1  |                  | f/5.6        | 100     | Base exposure requires a valid shutter speed value    |
      | 2  | 1/125            | f/999        | 100     | Aperture value f/999 is outside the valid range       |
      | 3  | 1/125            | f/5.6        | 999999  | ISO value 999999 exceeds maximum supported range      |
    When I validate the exposure settings
    Then the validation should fail
    And the system should provide helpful error messages
    And all invalid exposures should be rejected