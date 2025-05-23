Feature: Image Analysis
  As a photographer
  I want to analyze images for composition, exposure, and color characteristics
  So that I can improve my photography skills and optimize camera settings

  Background:
    Given the photography application is initialized for testing

  Scenario: Analyze basic image characteristics
    Given I have an image file at path "/test/images/landscape.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | TotalPixels |
      | 120     | 140       | 100      | 2073600     |
    When I analyze the image
    Then I should receive a successful photography result
    And the image analysis should be successful
    And I should receive image statistics
    And the image should have RGB values

  Scenario: Detect dominant colors in image
    Given I have an image file at path "/test/images/sunset.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 200     | 120       | 80       |
    When I analyze the image
    Then I should receive a successful photography result
    And the dominant color should be Red
    And the image analysis should detect warm colors

  Scenario: Analyze image brightness levels
    Given I have an image file at path "/test/images/dark_scene.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 40      | 45        | 35       |
    When I analyze the image
    Then I should receive a successful photography result
    And the brightness level should be Dark
    And the exposure should be underexposed

  Scenario: Analyze image contrast
    Given I have an image file at path "/test/images/high_contrast.jpg"
    And the image has the following characteristics:
      | StdDevRed | StdDevGreen | StdDevBlue |
      | 95        | 100         | 90         |
    And the image analysis should detect high contrast
    When I analyze the image
    Then I should receive a successful photography result
    And the contrast level should be High Contrast
    And the image analysis should detect high contrast

  Scenario: Analyze color balance and temperature
    Given I have an image file at path "/test/images/daylight.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 128     | 128       | 128      | 5500             |
    When I analyze the image for color balance
    Then I should receive a successful photography result
    And I should receive color analysis results
    And the color temperature should be approximately 5500 Kelvin

  Scenario: Analyze multiple images in batch
    Given I have multiple images for analysis:
      | Id | ImagePath                    | MeanRed | MeanGreen | MeanBlue |
      | 1  | /test/images/portrait1.jpg   | 150     | 140       | 130      |
      | 2  | /test/images/landscape1.jpg  | 100     | 120       | 140      |
      | 3  | /test/images/macro1.jpg      | 180     | 160       | 120      |
    When I analyze multiple images
    Then I should receive a successful photography result
    And all images should be analyzed successfully

  Scenario: Extract image metadata
    Given I have an image file at path "/test/images/sample.jpg"
    When I extract image metadata
    Then I should receive a successful photography result
    And I should receive image metadata
    And the image dimensions should be 1920 x 1080

  Scenario: Analyze image composition
    Given I have an image file at path "/test/images/rule_of_thirds.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 128     | 128       | 128      |
    When I analyze the image for composition
    Then I should receive a successful photography result
    And the composition analysis should show balanced composition

  Scenario: Analyze exposure quality
    Given I have an image file at path "/test/images/properly_exposed.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 160     | 170       | 150      |
    When I analyze the image for exposure quality
    Then I should receive a successful photography result
    And the exposure should be properly exposed
    And the brightness level should be Medium-Bright

  Scenario: Process batch of images
    Given I have a batch of images to process:
      | ImagePath                  |
      | /test/batch/image1.jpg     |
      | /test/batch/image2.jpg     |
      | /test/batch/image3.jpg     |
      | /test/batch/image4.jpg     |
    When I process all images in the batch
    Then I should receive a successful photography result
    And the batch processing should be complete

  Scenario: Validate image format
    Given I have an image file at path "/test/images/valid_format.jpg"
    When I validate the image format
    Then I should receive a successful photography result
    And the image format should be valid

  Scenario: Analyze low contrast image
    Given I have an image file at path "/test/images/foggy_scene.jpg"
    And the image has the following characteristics:
      | StdDevRed | StdDevGreen | StdDevBlue |
      | 15        | 18          | 12         |
    And the image analysis should detect low contrast
    When I analyze the image
    Then I should receive a successful photography result
    And the contrast level should be Low Contrast
    And the image analysis should detect low contrast

  Scenario: Analyze cool-toned image
    Given I have an image file at path "/test/images/winter_scene.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 90      | 120       | 180      | 7000             |
    When I analyze the image for color balance
    Then I should receive a successful photography result
    And the image analysis should detect cool colors
    And the color temperature should be approximately 7000 Kelvin

  Scenario: Analyze overexposed image
    Given I have an image file at path "/test/images/bright_scene.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue |
      | 220     | 230       | 210      |
    When I analyze the image for exposure quality
    Then I should receive a successful photography result
    And the brightness level should be Bright
    And the exposure should be overexposed

  Scenario Outline: Analyze different image types
    Given I have an image file at path "<ImagePath>"
    And the image has the following characteristics:
      | MeanRed   | MeanGreen   | MeanBlue   |
      | <MeanRed> | <MeanGreen> | <MeanBlue> |
    When I analyze the image
    Then I should receive a successful photography result
    And the dominant color should be <DominantColor>
    And the brightness level should be <BrightnessLevel>

    Examples:
      | ImagePath                | MeanRed | MeanGreen | MeanBlue | DominantColor | BrightnessLevel |
      | /test/red_dominant.jpg   | 200     | 100       | 100      | Red           | Medium-Bright   |
      | /test/green_dominant.jpg | 100     | 200       | 100      | Green         | Medium-Bright   |
      | /test/blue_dominant.jpg  | 100     | 100       | 200      | Blue          | Medium-Bright   |
      | /test/dark_image.jpg     | 50      | 50        | 50       | Red           | Dark            |

  Scenario: Analyze image for photography improvement suggestions
    Given I want to analyze an image for exposure settings
    And I have an image file at path "/test/images/needs_improvement.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | StdDevRed | StdDevGreen | StdDevBlue |
      | 80      | 85        | 75       | 25        | 30          | 20         |
    When I analyze the image for exposure quality
    Then I should receive a successful photography result
    And the exposure should be underexposed
    And the contrast level should be Low Contrast

  Scenario: Comprehensive image analysis
    Given I have an image file at path "/test/images/comprehensive_test.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast | StdDevRed | StdDevGreen | StdDevBlue | TotalPixels | ColorTemperature |
      | 145     | 135       | 125      | 135          | 65        | 70          | 60         | 3840000     | 4500             |
    When I analyze the image
    And I analyze the image for color balance
    And I extract image metadata
    Then I should receive a successful photography result
    And the image analysis should be successful
    And I should receive color analysis results
    And I should receive image metadata
    And the dominant color should be Red
    And the brightness level should be Medium-Bright
    And the contrast level should be Medium Contrast

  Scenario: Analyze portrait image characteristics
    Given I have an image file at path "/test/images/portrait.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | ColorTemperature |
      | 170     | 155       | 140      | 3200             |
    When I analyze the image for color balance
    And I analyze the image for composition
    Then I should receive a successful photography result
    And the image analysis should detect warm colors
    And the color temperature should be approximately 3200 Kelvin
    And the composition analysis should show balanced composition

  Scenario: Analyze macro photography image
    Given I have an image file at path "/test/images/macro_flower.jpg"
    And the image has the following characteristics:
      | MeanRed | MeanGreen | MeanBlue | StdDevRed | StdDevGreen | StdDevBlue |
      | 180     | 200       | 120      | 80        | 85          | 75         |
    When I analyze the image
    Then I should receive a successful photography result
    And the dominant color should be Green
    And the contrast level should be High Contrast
    And the brightness level should be Medium-Bright

  Scenario: Validate unsupported image format
    Given I have an image file at path "/test/images/document.txt"
    When I validate the image format
    Then I should receive a photography failure result
    And the photography error message should contain "Invalid image format"