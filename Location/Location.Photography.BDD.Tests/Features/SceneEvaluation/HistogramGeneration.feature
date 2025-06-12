Feature: Histogram Generation
  As a photographer
  I want to generate RGB and contrast histograms from images
  So that I can analyze exposure, color balance, and tonal distribution

  Background:
    Given the photography application is initialized for testing

  Scenario: Generate basic RGB histograms
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast | TotalPixels |
      | 128     | 128       | 128      | 128          | 1920000     |
    When I generate RGB histograms
    Then I should receive a successful photography result
    And I should receive histogram images
    And the red histogram should be generated
    And the green histogram should be generated
    And the blue histogram should be generated

  Scenario: Generate contrast histogram
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanContrast | StdDevContrast | TotalPixels |
      | 135          | 65             | 2073600     |
    When I generate the contrast histogram
    Then I should receive a successful photography result
    And the contrast histogram should be generated
    And the histogram data should be accurate

  Scenario: Evaluate scene and generate all histograms
    Given I have captured a scene for histogram analysis
    When I evaluate the scene
    Then I should receive a successful photography result
    And the scene evaluation should be complete
    And I should receive histogram images
    And the histogram data should be accurate

  Scenario: Analyze red-dominant image histogram
    Given I want to analyze the red histogram
    And the scene has red dominant colors
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue |
      | 180     | 100       | 90       |
    When I analyze the red histogram
    Then I should receive a successful photography result
    And the red histogram should be generated
    And the red channel should be dominant

  Scenario: Analyze blue-dominant image histogram
    Given I want to analyze the blue histogram
    And the scene has blue dominant colors
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue |
      | 80      | 90        | 170      |
    When I analyze the blue histogram
    Then I should receive a successful photography result
    And the blue histogram should be generated
    And the blue channel should be dominant

  Scenario: Analyze high contrast histogram
    Given I want to generate histograms for an image
    And the image has high contrast characteristics:
      | MeanContrast | StdDevContrast | Range |
      | 150          | 95             | 255   |
    When I generate the contrast histogram
    Then I should receive a successful photography result
    And the contrast histogram should be generated
    And the histogram should show high contrast distribution
    And the dynamic range should be wide

  Scenario: Analyze low contrast histogram
    Given I want to generate histograms for an image
    And the image has low contrast characteristics:
      | MeanContrast | StdDevContrast | Range |
      | 120          | 25             | 100   |
    When I generate the contrast histogram
    Then I should receive a successful photography result
    And the contrast histogram should be generated
    And the histogram should show low contrast distribution
    And the dynamic range should be narrow

  Scenario: Generate histograms for underexposed image
    Given I want to generate histograms for an image
    And the image is underexposed:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 45      | 50        | 40       | 45           |
    When I generate RGB histograms
    Then I should receive a successful photography result
    And the histograms should show left-skewed distribution
    And the exposure should be identified as underexposed
    And highlight recovery potential should be assessed

  Scenario: Generate histograms for overexposed image
    Given I want to generate histograms for an image
    And the image is overexposed:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 220     | 225       | 215      | 220          |
    When I generate RGB histograms
    Then I should receive a successful photography result
    And the histograms should show right-skewed distribution
    And the exposure should be identified as overexposed
    And clipping should be detected

  Scenario: Generate histograms for properly exposed image
    Given I want to generate histograms for an image
    And the image is properly exposed:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 128     | 135       | 125      | 130          |
    When I generate RGB histograms
    Then I should receive a successful photography result
    And the histograms should show balanced distribution
    And the exposure should be identified as proper
    And the tonal range should be well distributed

  Scenario: Analyze color balance through histograms
    Given I want to analyze color balance through histograms
    And the image has color balance data:
      | RedBalance | GreenBalance | BlueBalance | ColorCast |
      | High       | Medium       | Low         | Warm      |
    When I generate RGB histograms for color analysis
    Then I should receive a successful photography result
    And the red channel should show higher values
    And the blue channel should show lower values
    And the color cast should be detected as warm
    And color correction suggestions should be provided

  Scenario: Generate luminance histogram
    Given I want to generate a luminance histogram
    And the image has luminance characteristics:
      | MeanLuminance | StdDevLuminance | Range |
      | 118           | 60              | 200   |
    When I generate the luminance histogram
    Then I should receive a successful photography result
    And the luminance histogram should be generated
    And the brightness distribution should be analyzed
    And the tonal curve should be evaluated

  Scenario: Compare histograms across multiple exposures
    Given I have multiple exposures for histogram comparison:
      | ExposureValue | MeanRed | MeanGreen | MeanBlue |
      | -2 EV         | 60      | 65        | 55       |
      | 0 EV          | 128     | 135       | 125      |
      | +2 EV         | 200     | 210       | 195      |
    When I generate histograms for all exposures
    Then I should receive a successful photography result
    And each exposure should have distinct histogram shapes
    And the tonal distribution should shift with exposure
    And optimal exposure should be identified

  Scenario: Detect histogram clipping
    Given I want to detect histogram clipping
    And the image has clipping characteristics:
      | ShadowClipping | HighlightClipping | ClippedPixels |
      | 5%             | 8%                | 130000        |
    When I analyze histogram clipping
    Then I should receive a successful photography result
    And shadow clipping should be detected
    And highlight clipping should be detected
    And the percentage of clipped pixels should be calculated
    And recovery recommendations should be provided

  Scenario: Generate channel separation histograms
    Given I want to generate channel separation histograms
    And the image has distinct channel characteristics:
      | RedPeak | GreenPeak | BluePeak | Separation |
      | 180     | 140       | 100      | High       |
    When I generate channel separation analysis
    Then I should receive a successful photography result
    And each channel should have distinct peaks
    And the channel separation should be high
    And color purity should be assessed

  Scenario Outline: Generate histograms for different lighting conditions
    Given I want to generate histograms for an image
    And the image has lighting characteristics:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | <Red>   | <Green>   | <Blue>   | <Contrast>   |
    When I generate RGB histograms
    Then I should receive a successful photography result
    And the exposure should be <ExposureType>
    And the tonal distribution should be <ToneType>

    Examples:
      | Red | Green | Blue | Contrast | ExposureType | ToneType  |
      | 45  | 50    | 40   | 45       | under        | dark      |
      | 128 | 135   | 125  | 130      | proper       | balanced  |
      | 220 | 210   | 225  | 218      | over         | bright    |

  Scenario: Generate histograms with detailed statistics
    Given I want to generate histograms with detailed statistics
    And the image has comprehensive data:
      | Metric          | Red | Green | Blue | Contrast |
      | Mean            | 142 | 138   | 132  | 137      |
      | StandardDev     | 68  | 72    | 65   | 68       |
      | Minimum         | 12  | 15    | 8    | 10       |
      | Maximum         | 248 | 251   | 245  | 250      |
      | Median          | 145 | 140   | 135  | 140      |
    When I generate detailed histogram statistics
    Then I should receive a successful photography result
    And comprehensive statistics should be calculated
    And distribution metrics should be provided
    And histogram shape analysis should be complete

  Scenario: Generate saturation histogram
    Given I want to generate a saturation histogram
    And the image has saturation characteristics:
      | MeanSaturation | MaxSaturation | Colorfulness |
      | 65             | 95            | High         |
    When I generate the saturation histogram
    Then I should receive a successful photography result
    And the saturation histogram should be generated
    And color vibrancy should be analyzed
    And saturation distribution should be evaluated

  Scenario: Analyze green-dominant landscape histogram
    Given I want to analyze the green histogram
    And the scene has green dominant colors
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | StdDevRed | StdDevGreen | StdDevBlue |
      | 160     | 180       | 120      | 70        | 75          | 65         |
    When I analyze the green histogram
    Then I should receive a successful photography result
    And the green channel should be dominant
    And the green histogram should be generated

  Scenario: Comprehensive histogram analysis
    Given I have captured a scene for histogram analysis
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast | StdDevRed | StdDevGreen | StdDevBlue | StdDevContrast | TotalPixels |
      | 142     | 138       | 132      | 137          | 68        | 72          | 65         | 68             | 2073600     |
    When I evaluate the scene
    And I generate RGB histograms
    And I generate the contrast histogram
    Then I should receive a successful photography result
    And the scene evaluation should be complete
    And I should receive histogram images
    And the histogram data should be accurate
    And the RGB histograms should be balanced
    And the histogram should indicate proper exposure

  Scenario: Batch histogram generation
    Given I have multiple images for histogram generation:
      | Id | ImagePath                    | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 1  | /test/batch/morning.jpg      | 180     | 160       | 120      | 153          |
      | 2  | /test/batch/noon.jpg         | 200     | 190       | 180      | 190          |
      | 3  | /test/batch/evening.jpg      | 160     | 120       | 80       | 120          |
      | 4  | /test/batch/night.jpg        | 60      | 50        | 70       | 60           |
    When I generate histograms for all images
    Then I should receive a successful photography result
    And all histogram images should be generated
    And the histogram data should be accurate

  Scenario: Real-time histogram generation
    Given I have captured a scene for histogram analysis
    When I evaluate the scene
    And I generate the histograms
    Then I should receive a successful photography result
    And the scene evaluation should be complete
    And I should receive histogram images
    And the red histogram should be generated
    And the green histogram should be generated
    And the blue histogram should be generated
    And the contrast histogram should be generated