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
    And the color temperature should be approximately 5500 Kelvin
    And the lighting should be identified as daylight
    And the white balance should be neutral

  Scenario: Detect tungsten lighting color cast
    Given I want to analyze color temperature in an image
    And the lighting condition is tungsten
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 180     | 140       | 100      | 2800             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 2800 Kelvin
    And the lighting should be identified as tungsten
    And the image should have a warm color cast
    And I should receive color correction recommendations

  Scenario: Detect blue hour color temperature
    Given I want to analyze color temperature in an image
    And the lighting condition is blue hour
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 100     | 120       | 160      | 8000             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 8000 Kelvin
    And the lighting should be identified as blue hour
    And the image should have a cool color cast

  Scenario: Calculate white balance correction values
    Given I want to analyze color temperature in an image
    And the target color temperature is 5500 Kelvin
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 200     | 140       | 110      | 2800             | -0.3      |
    When I calculate white balance correction
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
    And the lighting should be identified as fluorescent
    And the image should have a green tint
    And the tint value should be approximately 0.4

  Scenario: Detect mixed lighting conditions
    Given I want to analyze color temperature in an image
    And the image has mixed lighting sources
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 150     | 135       | 125      | 4200             | 0.1       |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the lighting should be identified as mixed
    And the color temperature should be approximately 4200 Kelvin
    And the analysis should detect multiple light sources
    And color correction should be more complex

  Scenario: Analyze sunset golden hour colors
    Given I want to analyze color temperature in an image
    And the lighting condition is golden hour
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 220     | 180       | 120      | 3200             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 3200 Kelvin
    And the lighting should be identified as golden hour
    And the image should have warm, pleasing colors
    And minimal correction should be recommended

  Scenario: Detect overcast daylight conditions
    Given I want to analyze color temperature in an image
    And the lighting condition is overcast
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 110     | 125       | 145      | 6500             |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 6500 Kelvin
    And the lighting should be identified as overcast daylight
    And the image should have a slight cool cast
    And warming correction should be suggested

  Scenario: Analyze artificial LED lighting
    Given I want to analyze color temperature in an image
    And the lighting condition is LED
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | TintValue |
      | 135     | 138       | 132      | 5000             | 0.2       |
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the color temperature should be approximately 5000 Kelvin
    And the lighting should be identified as LED
    And the white balance should be nearly neutral
    And the tint should be minimal

  Scenario: Calculate color temperature from RGB ratios
    Given I want to calculate color temperature from RGB values
    And the image has RGB ratios:
      | RedRatio | GreenRatio | BlueRatio |
      | 1.4      | 1.0        | 0.7       |
    When I calculate color temperature from ratios
    Then I should receive a successful photography result
    And the calculated color temperature should be approximately 2900 Kelvin
    And the calculation should be based on RGB relationships
    And the accuracy should be within acceptable range

  Scenario: Detect color temperature variations across image
    Given I want to analyze color temperature variations
    And the image has regional color temperatures:
      | Region     | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | TopLeft    | 180     | 140       | 100      | 2800             |
      | TopRight   | 140     | 135       | 130      | 4800             |
      | BottomLeft | 200     | 160       | 110      | 3000             |
      | BottomRight| 120     | 125       | 140      | 6200             |
    When I analyze regional color temperature variations
    Then I should receive a successful photography result
    And multiple color temperatures should be detected
    And the variation should be significant
    And selective correction recommendations should be provided

  Scenario: Generate color temperature histogram
    Given I want to generate a color temperature histogram
    And the image has distributed color temperatures:
      | TemperatureRange | PixelCount | Percentage |
      | 2500-3000K      | 150000     | 25%        |
      | 3000-4000K      | 200000     | 35%        |
      | 4000-5500K      | 180000     | 30%        |
      | 5500-7000K      | 70000      | 10%        |
    When I generate the color temperature histogram
    Then I should receive a successful photography result
    And the histogram should show temperature distribution
    And the dominant temperature range should be identified
    And the histogram should aid in correction decisions

  Scenario: Analyze skin tone color accuracy
    Given I want to analyze skin tone color accuracy
    And the image contains skin tones
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | SkinToneAccuracy |
      | 190     | 160       | 130      | 4200             | Good             |
    When I analyze skin tone color reproduction
    Then I should receive a successful photography result
    And the skin tone accuracy should be evaluated
    And the color temperature should be suitable for skin tones
    And any skin tone color cast should be identified
    And correction suggestions should preserve skin tone quality

  Scenario: Compare white balance presets effectiveness
    Given I want to compare white balance preset effectiveness
    And the image was shot with auto white balance
    And the image has color characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature | ActualLighting |
      | 160     | 145       | 120      | 3800             | Tungsten       |
    When I compare white balance preset effectiveness
    Then I should receive a successful photography result
    And the auto white balance accuracy should be evaluated
    And the optimal preset should be identified
    And the improvement potential should be calculated
    And manual correction values should be provided

  Scenario: Handle extreme color temperature conditions
    Given I want to analyze extreme color temperature
    And the image has extreme lighting conditions:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 250     | 150       | 80       | 2000             |
    When I analyze the extreme color temperature
    Then I should receive a successful photography result
    And the extreme color temperature should be detected
    And the analysis should handle the extreme values gracefully
    And appropriate correction recommendations should be provided
    And the limitations of correction should be noted

  Scenario: Batch analyze color temperature across multiple images
    Given I want to batch analyze color temperature
    And I have multiple images with different lighting:
      | ImagePath              | ColorTemperature | LightingType |
      | /test/daylight.jpg     | 5500             | Daylight     |
      | /test/tungsten.jpg     | 2800             | Tungsten     |
      | /test/fluorescent.jpg  | 4000             | Fluorescent  |
      | /test/led.jpg          | 5000             | LED          |
    When I batch analyze color temperatures
    Then I should receive a successful photography result
    And all images should be analyzed successfully
    And each image should have color temperature identified
    And lighting types should be correctly classified
    And batch correction recommendations should be provided

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
    And the comprehensive analysis should be complete

  Scenario Outline: Analyze different lighting conditions
    Given I want to analyze color temperature in an image
    And the lighting condition is <LightingType>
    And the image has color temperature <ColorTemperature> Kelvin
    When I analyze the color temperature
    Then I should receive a successful photography result
    And the lighting should be identified as <LightingType>
    And the color temperature should be approximately <ColorTemperature> Kelvin
    And the color cast should be <ExpectedCast>

    Examples:
      | LightingType | ColorTemperature | ExpectedCast |
      | Candle       | 1900             | Very Warm    |
      | Tungsten     | 2800             | Warm         |
      | Fluorescent  | 4000             | Cool         |
      | Daylight     | 5500             | Neutral      |
      | Overcast     | 6500             | Cool         |
      | Blue Hour    | 8000             | Very Cool    |