Feature: Exposure Settings Management
  As a photographer
  I want to manage camera exposure settings
  So that I can configure my camera with appropriate shutter speeds, apertures, and ISO values

  Background:
    Given the photography application is initialized for testing

  Scenario: Retrieve available shutter speeds for full stops
    Given I have camera settings configured for full increments
    When I request the available shutter speeds for full stops
    Then I should receive a successful photography result
    And the shutter speeds list should contain 19 values
    And the available shutter speeds should include:
      | ShutterSpeed |
      | 1/8000       |
      | 1/4000       |
      | 1/2000       |
      | 1/1000       |
      | 1/500        |
      | 1/250        |
      | 1/125        |
      | 1/60         |
      | 1/30         |
      | 1/15         |
      | 1/8          |
      | 1/4          |
      | 1/2          |
      | 1            |
      | 2            |
      | 4            |
      | 8            |
      | 15           |
      | 30           |

  Scenario: Retrieve available apertures for full stops
    Given I have camera settings configured for full increments
    When I request the available apertures for full stops
    Then I should receive a successful photography result
    And the apertures list should contain 13 values
    And the available apertures should include:
      | Aperture |
      | f/1      |
      | f/1.4    |
      | f/2      |
      | f/2.8    |
      | f/4      |
      | f/5.6    |
      | f/8      |
      | f/11     |
      | f/16     |
      | f/22     |
      | f/32     |
      | f/45     |
      | f/64     |

  Scenario: Retrieve available ISOs for full stops
    Given I have camera settings configured for full increments
    When I request the available ISOs for full stops
    Then I should receive a successful photography result
    And the ISOs list should contain 10 values
    And the available ISOs should include:
      | ISO   |
      | 25600 |
      | 12800 |
      | 6400  |
      | 3200  |
      | 1600  |
      | 800   |
      | 400   |
      | 200   |
      | 100   |
      | 50    |

  Scenario: Retrieve available settings for half stops
    Given I have camera settings configured for half increments
    When I request the available shutter speeds for half stops
    Then I should receive a successful photography result
    And the shutter speeds list should contain 37 values
    And the available shutter speeds should include:
      | ShutterSpeed |
      | 1/8000       |
      | 1/6000       |
      | 1/4000       |
      | 1/3000       |
      | 1/2000       |
      | 1/1500       |
      | 1/1000       |
      | 1/750        |
      | 1/500        |
      | 1/350        |
      | 1/250        |

  Scenario: Retrieve available settings for third stops
    Given I have camera settings configured for third increments
    When I request the available apertures for third stops
    Then I should receive a successful photography result
    And the apertures list should contain 37 values
    And the available apertures should include:
      | Aperture |
      | f/1      |
      | f/1.1    |
      | f/1.3    |
      | f/1.4    |
      | f/1.6    |
      | f/1.8    |
      | f/2      |
      | f/2.2    |
      | f/2.5    |
      | f/2.8    |

  Scenario: Change increment setting and verify available options
    Given I have camera settings configured for full increments
    When I retrieve the available camera settings
    And I change the increment setting to third
    Then I should receive a successful photography result
    And the increment setting should be third
    And the number of available shutter speeds should be greater for third stops than full

  Scenario: Select specific exposure settings
    Given I want to use the exposure with settings "1/125", "f/5.6", "100"
    When I select exposure settings "1/125", "f/5.6", "100"
    Then I should receive a successful photography result
    And the exposure settings should be successfully selected
    And the camera should be configured with the selected settings

  Scenario: Configure camera with preset exposure combinations
    Given I have preset exposure combinations:
      | Id | BaseShutterSpeed | BaseAperture | BaseIso | Increments |
      | 1  | 1/60             | f/8          | 200     | Full       |
      | 2  | 1/125            | f/5.6        | 100     | Half       |
      | 3  | 1/250            | f/4          | 400     | Third      |
    When I select exposure settings "1/125", "f/5.6", "100"
    Then I should receive a successful photography result
    And the preset exposure "Portrait" should have settings "1/125", "f/5.6", "100"

  Scenario: Validate exposure settings compatibility
    Given I want to use the exposure with settings "1/250", "f/2.8", "800"
    When I select exposure settings "1/250", "f/2.8", "800"
    Then I should receive a successful photography result
    And all exposure settings should be valid
    And I should be able to calculate equivalent exposures

  Scenario Outline: Retrieve settings for different increment types
    Given I have camera settings configured for <IncrementType> increments
    When I request the available <SettingType> for <IncrementType> stops
    Then I should receive a successful photography result
    And the <SettingType> list should contain <ExpectedCount> values

    Examples:
      | IncrementType | SettingType     | ExpectedCount |
      | full          | shutter speeds  | 19            |
      | full          | apertures       | 13            |
      | full          | ISOs            | 10            |
      | half          | shutter speeds  | 37            |
      | half          | apertures       | 24            |
      | half          | ISOs            | 20            |
      | third         | shutter speeds  | 55            |
      | third         | apertures       | 37            |
      | third         | ISOs            | 27            |

  Scenario: Compare increment precision levels
    Given I have camera settings configured for full increments
    When I retrieve the available camera settings
    And I change the increment setting to half
    And I change the increment setting to third
    Then I should receive a successful photography result
    And the number of available shutter speeds should be greater for third stops than half

  Scenario: Select settings and verify camera configuration
    Given I have preset exposure combinations:
      | Id | BaseShutterSpeed | BaseAperture | BaseIso |
      | 1  | 1/500            | f/2.8        | 200     |
    When I select exposure settings "1/500", "f/2.8", "200"
    Then I should receive a successful photography result
    And the camera should be configured with the selected settings
    And all exposure settings should be valid

  Scenario: Retrieve settings with different precision and verify options
    Given I have camera settings configured for third increments
    When I retrieve the available camera settings
    Then I should receive a successful photography result
    And the shutter speed options should include "1/640"
    And the aperture options should include "f/1.8"
    And the ISO options should include "1250"