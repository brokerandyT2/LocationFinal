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
    And the histogram should show bright tones

  Scenario: Analyze green-dominant image histogram
    Given I want to analyze the green histogram
    And the scene has green dominant colors
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue |
      | 90      | 180       | 100      |
    When I analyze the green histogram
    Then I should receive a successful photography result
    And the green histogram should be generated
    And the green channel should be dominant

  Scenario: Analyze blue-dominant image histogram
    Given I want to analyze the blue histogram
    And the scene has blue dominant colors
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue |
      | 90      | 100       | 180      |
    When I analyze the blue histogram
    Then I should receive a successful photography result
    And the blue histogram should be generated
    And the blue channel should be dominant

  Scenario: Generate histograms for high contrast scene
    Given I want to generate histograms for an image
    And the scene has high contrast
    And the image has histogram data:
      | StdDevRed | StdDevGreen | StdDevBlue | StdDevContrast |
      | 95        | 100         | 90         | 95             |
    When I generate the histograms
    Then I should receive a successful photography result
    And I should receive histogram images
    And the contrast histogram should show High Contrast
    And the histogram should show good contrast

  Scenario: Generate histograms for low contrast scene
    Given I want to generate histograms for an image
    And the scene has low contrast
    And the image has histogram data:
      | StdDevRed | StdDevGreen | StdDevBlue | StdDevContrast |
      | 20        | 18          | 22         | 20             |
    When I generate the histograms
    Then I should receive a successful photography result
    And I should receive histogram images
    And the contrast histogram should show Low Contrast
    And the histogram should show poor contrast

  Scenario: Analyze underexposed image histogram
    Given I want to generate histograms for an image
    And the scene has dark lighting
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 50      | 45        | 55       | 50           |
    When I generate the histograms
    Then I should receive a successful photography result
    And the histogram should indicate under exposure
    And the histogram should show dark tones

  Scenario: Analyze overexposed image histogram
    Given I want to generate histograms for an image
    And the scene has bright lighting
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 200     | 210       | 190      | 200          |
    When I generate the histograms
    Then I should receive a successful photography result
    And the histogram should indicate over exposure
    And the histogram should show bright tones

  Scenario: Generate histograms for balanced exposure
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast |
      | 128     | 135       | 125      | 130          |
    When I generate the histograms
    Then I should receive a successful photography result
    And the RGB histograms should be balanced
    And the histogram should indicate proper exposure
    And the histogram should show balanced exposure

  Scenario: Generate histograms for multiple images
    Given I have multiple images for histogram generation:
      | Id | ImagePath                  | MeanRed | MeanGreen | MeanBlue |
      | 1  | /test/images/sunset.jpg    | 180     | 120       | 80       |
      | 2  | /test/images/forest.jpg    | 90      | 160       | 100      |
      | 3  | /test/images/ocean.jpg     | 100     | 120       | 180      |
    When I generate histograms for all images
    Then I should receive a successful photography result
    And all histogram images should be generated

  Scenario: Compare histograms between similar images
    Given I have multiple images for histogram generation:
      | Id | ImagePath                  | MeanRed | MeanGreen | MeanBlue |
      | 1  | /test/images/portrait1.jpg | 150     | 140       | 130      |
      | 2  | /test/images/portrait2.jpg | 155     | 145       | 135      |
    When I compare histograms between images
    Then I should receive a successful photography result
    And the histogram comparison should show similar images

  Scenario: Compare histograms between different images
    Given I have multiple images for histogram generation:
      | Id | ImagePath                  | MeanRed | MeanGreen | MeanBlue |
      | 1  | /test/images/sunset.jpg    | 200     | 100       | 50       |
      | 2  | /test/images/nightsky.jpg  | 30      | 40        | 80       |
    When I compare histograms between images
    Then I should receive a successful photography result
    And the histogram comparison should show different images

  Scenario Outline: Generate histograms for different lighting conditions
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanRed   | MeanGreen   | MeanBlue   | MeanContrast   |
      | <MeanRed> | <MeanGreen> | <MeanBlue> | <MeanContrast> |
    When I generate the histograms
    Then I should receive a successful photography result
    And the histogram should indicate <ExposureType> exposure
    And the histogram should show <ToneType> tones

    Examples:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast | ExposureType | ToneType |
      | 40      | 35        | 45       | 40           | under        | dark     |
      | 128     | 135       | 125      | 130          | proper       | balanced |
      | 220     | 210       | 225      | 218          | over         | bright   |

  Scenario: Generate histograms with detailed statistics
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | StdDevRed | StdDevGreen | StdDevBlue | TotalPixels |
      | 145     | 135       | 125      | 65        | 70          | 60         | 3840000     |
    When I generate the histograms
    Then I should receive a successful photography result
    And I should receive histogram images
    And the histogram data should be accurate
    And the RGB histograms should be balanced

  Scenario: Analyze histogram for portrait photography
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | MeanContrast | StdDevContrast |
      | 170     | 155       | 140      | 155          | 45             |
    When I generate the histograms
    Then I should receive a successful photography result
    And the red channel should be dominant
    And the histogram should show balanced exposure
    And the contrast histogram should show Medium Contrast

  Scenario: Analyze histogram for landscape photography
    Given I want to generate histograms for an image
    And the image has histogram data:
      | MeanRed | MeanGreen | MeanBlue | StdDevRed | StdDevGreen | StdDevBlue |
      | 110     | 140       | 120      | 80        | 85          | 75         |
    When I generate the histograms
    Then I should receive a successful photography result
    And the green channel should be dominant
    And the contrast histogram should show High Contrast

  Scenario: Generate histograms for macro photography
    Given I want to generate histograms for an image
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