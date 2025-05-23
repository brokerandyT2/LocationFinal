Feature: Color Temperature Analysis
  As a photographer
  I want to analyze color temperature and white balance in images
  So that I can correct color casts and achieve accurate color reproduction

  Background:
    Given the photography application is initialized for testing

  Scenario: Analyze daylight color temperature
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 128     | 128       | 128      | 5500             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And I should receive color temperature analysis
    And the color temperature should be approximately 5500 Kelvin
    And the color temperature should indicate neutral lighting

  Scenario: Analyze tungsten lighting color temperature
    Given I want to analyze color temperature in an image
    And the lighting condition is tungsten
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 180     | 140       | 90       | 3200             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 3200 Kelvin
    And the color temperature should indicate warm lighting

  Scenario: Analyze overcast lighting color temperature
    Given I want to analyze color temperature in an image
    And the lighting condition is overcast
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 110     | 120       | 140      | 7000             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 7000 Kelvin
    And the color temperature should indicate cool lighting

  Scenario: Measure white balance accuracy
    Given I want to analyze color temperature in an image
    And I have a reference white point
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 140     | 120       | 110      | 4500             | 0.2       |
    When I measure the white balance
    Then I should receive a successful photography result
    And I should receive white balance measurements
    And the white balance should be too warm

  Scenario: Calculate color correction values
    Given I want to correct white balance
    And I have a reference white point
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 160     | 130       | 100      | 3800             | 0.3       |
    When I calculate color correction values
    Then I should receive a successful photography result
    And I should receive color correction values
    And the temperature correction should be approximately 2700 Kelvin
    And the tint correction should be approximately -0.3

  Scenario: Analyze fluorescent lighting tint
    Given I want to analyze color temperature in an image
    And the lighting condition is fluorescent
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 120     | 140       | 130      | 4000             | 0.4       |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 4000 Kelvin
    And the tint should be approximately 0.4

  Scenario: Compare color temperatures between multiple images
    Given I have multiple images with different color temperatures:
      | Id | ImagePath                  | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 1  | /test/images/daylight.jpg  | 128     | 128       | 128      | 5500             |
      | 2  | /test/images/tungsten.jpg  | 180     | 140       | 90       | 3200             |
      | 3  | /test/images/overcast.jpg  | 110     | 120       | 140      | 7000             |
    When I compare color temperatures between images
    Then I should receive a successful photography result
    And the color temperature comparison should show different temperatures

  Scenario: Detect red color cast
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 170     | 120       | 110      |
    When I detect the dominant color cast
    Then I should receive a successful photography result
    And I should receive color cast detection
    And the dominant color cast should be Red

  Scenario: Detect blue color cast
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 100     | 115       | 160      |
    When I detect the dominant color cast
    Then I should receive a successful photography result
    And the dominant color cast should be Blue

  Scenario: Detect green color cast
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 110     | 150       | 120      |
    When I detect the dominant color cast
    Then I should receive a successful photography result
    And the dominant color cast should be Green

  Scenario: Analyze neutral white balance
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 130     | 128       | 125      | 6000             | 0.1       |
    When I measure the white balance
    Then I should receive a successful photography result
    And the white balance should be accurate
    And the tint should be approximately 0.1

  Scenario: Analyze color temperature for portrait photography
    Given I want to analyze color temperature in an image
    And the image was taken under tungsten lighting
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 170     | 135       | 100      | 3400             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should indicate warm lighting
    And the color temperature should be approximately 3400 Kelvin

  Scenario: Analyze color temperature for landscape photography
    Given I want to analyze color temperature in an image
    And the image was taken under daylight lighting
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 125     | 130       | 135      | 5800             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should indicate neutral lighting
    And the color temperature should be approximately 5800 Kelvin

  Scenario Outline: Analyze different lighting conditions
    Given I want to analyze color temperature in an image
    And the lighting condition is <LightingCondition>
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately <ExpectedTemp> Kelvin
    And the color temperature should indicate <ExpectedDescription> lighting

    Examples:
      | LightingCondition | ExpectedTemp | ExpectedDescription |
      | daylight          | 5500         | neutral             |
      | tungsten          | 3200         | warm                |
      | fluorescent       | 4000         | neutral             |
      | overcast          | 7000         | cool                |
      | shade             | 8000         | cool                |

  Scenario: Batch color temperature analysis
    Given I have multiple images with different color temperatures:
      | Id | ImagePath                    | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 1  | /test/batch/morning.jpg      | 160     | 140       | 120      | 4200             |
      | 2  | /test/batch/noon.jpg         | 128     | 128       | 128      | 5500             |
      | 3  | /test/batch/evening.jpg      | 180     | 130       | 90       | 3500             |
      | 4  | /test/batch/night.jpg        | 170     | 135       | 100      | 3200             |
    When I analyze color temperature for all images
    Then I should receive a successful photography result
    And all images should have color temperature analysis

  Scenario: Compare similar color temperatures
    Given I have multiple images with different color temperatures:
      | Id | ImagePath                  | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 1  | /test/images/daylight1.jpg | 125     | 130       | 135      | 5600             |
      | 2  | /test/images/daylight2.jpg | 130     | 128       | 132      | 5400             |
    When I compare color temperatures between images
    Then I should receive a successful photography result
    And the color temperature comparison should show similar temperatures

  Scenario: White balance correction for mixed lighting
    Given I want to correct white balance
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 150     | 135       | 110      | 4200             | 0.25      |
    And I have a reference white point
    When I calculate color correction values
    Then I should receive a successful photography result
    And the temperature correction should be approximately 2300 Kelvin
    And the tint correction should be approximately -0.25

  Scenario: Detect magenta color cast
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 140     | 110       | 130      |
    When I detect the dominant color cast
    Then I should receive a successful photography result
    And the dominant color cast should be Magenta

  Scenario: Analyze extreme color temperature conditions
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 200     | 120       | 60       | 2800             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 2800 Kelvin
    And the color temperature should indicate very warm lighting

  Scenario: Comprehensive color analysis
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 145     | 130       | 115      | 4800             | 0.15      |
    When I analyze the color temperature
    And I measure the white balance
    And I detect the dominant color cast
    Then I should receive a successful photography result
    And I should receive color temperature analysis
    And I should receive white balance measurements
    And I should receive color cast detection
    And the color temperature should be approximately 4800 Kelvin
    And the tint should be approximately 0.15

  Scenario: Color temperature analysis for macro photography
    Given I want to analyze color temperature in an image
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 135     | 145       | 125      | 5200             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 5200 Kelvin
    And the color temperature should indicate neutral lighting

  Scenario: Analyze shade lighting color temperature
    Given I want to analyze color temperature in an image
    And the lighting condition is shade
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 100     | 115       | 150      | 8000             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 8000 Kelvin
    And the color temperature should indicate very cool lighting

  Scenario: White balance validation
    Given I want to correct white balance
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 128     | 128       | 128      | 6500             | 0.0       |
    When I measure the white balance
    Then I should receive a successful photography result
    And the white balance should be accurate
    And the color temperature should be approximately 6500 Kelvin
    And the tint should be approximately 0.0