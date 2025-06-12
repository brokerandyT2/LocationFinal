Feature: Lens Management
  As a photographer
  I want to manage lens specifications and compatibility
  So that I can make informed decisions about lens selection and compatibility

  Background:
    Given the photography application is initialized for testing

  Scenario: Create a new lens
    Given I want to create a new lens
    When I create a lens with the following specifications:
      | Make          | Model           | FocalLengthMin | FocalLengthMax | ApertureMin | ApertureMax | MountType | LensType  |
      | Canon         | RF 24-70mm f/2.8| 24             | 70             | 2.8         | 2.8         | RF        | Zoom      |
    Then I should receive a successful photography result
    And the lens should be created successfully
    And the lens should have a valid ID

  Scenario: Get lens by ID
    Given I have a lens with the following specifications:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | ApertureMin | MountType |
      | 1  | Canon  | RF 24-70mm f/2.8| 24             | 70             | 2.8         | RF        |
    When I get the lens by ID 1
    Then I should receive a successful photography result
    And I should receive the lens details
    And the lens make should be "Canon"
    And the lens model should be "RF 24-70mm f/2.8"

  Scenario: Get lenses with paging
    Given I have multiple lenses in the system:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | MountType |
      | 1  | Canon  | RF 24-70mm f/2.8| 24             | 70             | RF        |
      | 2  | Canon  | RF 70-200mm f/4 | 70             | 200            | RF        |
      | 3  | Nikon  | Z 24-70mm f/2.8 | 24             | 70             | Z         |
    When I get lenses with skip 0 and take 2
    Then I should receive a successful photography result
    And I should receive 2 lenses
    And the results should include "Canon RF 24-70mm f/2.8"

  Scenario: Search lenses by focal length
    Given I have multiple lenses in the system:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | MountType |
      | 1  | Canon  | RF 24-70mm f/2.8| 24             | 70             | RF        |
      | 2  | Canon  | RF 70-200mm f/4 | 70             | 200            | RF        |
      | 3  | Canon  | RF 85mm f/1.2   | 85             | 85             | RF        |
    When I search for lenses by focal length 85
    Then I should receive a successful photography result
    And I should receive at least 1 lens
    And the results should include lenses covering 85mm focal length

  Scenario: Get lenses compatible with camera
    Given I have a camera body with the following specifications:
      | Id | Make   | Model   | MountType |
      | 1  | Canon  | EOS R5  | RF        |
    And I have multiple lenses with different mount types:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | MountType |
      | 1  | Canon  | RF 24-70mm f/2.8| 24             | 70             | RF        |
      | 2  | Canon  | EF 24-70mm f/2.8| 24             | 70             | EF        |
      | 3  | Nikon  | Z 24-70mm f/2.8 | 24             | 70             | Z         |
    When I get lenses compatible with camera ID 1
    Then I should receive a successful photography result
    And I should receive lenses with RF mount type
    And the results should include "Canon RF 24-70mm f/2.8"

  Scenario: Update existing lens specifications
    Given I have a lens with the following specifications:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | ApertureMin |
      | 1  | Canon  | RF 24-70mm f/2.8| 24             | 70             | 2.8         |
    When I update the lens with new specifications:
      | Model           | ApertureMin |
      | RF 24-70mm f/2.8L| 2.8         |
    Then I should receive a successful photography result
    And the lens should be updated successfully
    And the lens model should be "RF 24-70mm f/2.8L"

  Scenario: Delete a lens
    Given I have a lens with ID 1
    When I delete the lens
    Then I should receive a successful photography result
    And the lens should be deleted successfully
    And the lens should not exist in the system

  Scenario: Get all user-created lenses
    Given I have both system and user-created lenses:
      | Id | Make   | Model           | IsUserCreated | FocalLengthMin | FocalLengthMax |
      | 1  | Canon  | RF 24-70mm f/2.8| false         | 24             | 70             |
      | 2  | Custom | My Custom Lens  | true          | 50             | 50             |
      | 3  | Custom | Another Custom  | true          | 85             | 85             |
    When I get all user-created lenses
    Then I should receive a successful photography result
    And I should receive 2 lenses
    And all results should be user-created lenses

  Scenario: Get total lens count
    Given I have 10 lenses in the system
    When I get the total lens count
    Then I should receive a successful photography result
    And the total count should be 10

  Scenario: Calculate field of view for lens and camera combination
    Given I have a lens with focal length 50mm
    And I have a camera with sensor dimensions 36mm x 24mm
    When I calculate the field of view
    Then I should receive a successful photography result
    And the horizontal field of view should be approximately 39.6 degrees
    And the vertical field of view should be approximately 27.0 degrees

  Scenario: Determine lens type based on focal length
    Given I have multiple lenses with different focal lengths:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | ExpectedType |
      | 1  | Canon  | RF 16-35mm f/2.8| 16             | 35             | Wide Angle   |
      | 2  | Canon  | RF 24-70mm f/2.8| 24             | 70             | Standard     |
      | 3  | Canon  | RF 70-200mm f/4 | 70             | 200            | Telephoto    |
      | 4  | Canon  | RF 800mm f/11   | 800            | 800            | Super Tele   |
    When I classify the lens types
    Then I should receive a successful photography result
    And the 16-35mm lens should be classified as "Wide Angle"
    And the 24-70mm lens should be classified as "Standard"
    And the 70-200mm lens should be classified as "Telephoto"

  Scenario: Search lenses by aperture range
    Given I have multiple lenses with different apertures:
      | Id | Make   | Model           | FocalLengthMin | ApertureMin | ApertureMax |
      | 1  | Canon  | RF 85mm f/1.2   | 85             | 1.2         | 16.0        |
      | 2  | Canon  | RF 24-70mm f/2.8| 24             | 2.8         | 22.0        |
      | 3  | Canon  | RF 70-200mm f/4 | 70             | 4.0         | 32.0        |
    When I search for lenses with maximum aperture wider than f/2.0
    Then I should receive a successful photography result
    And I should receive 1 lens
    And the result should be "Canon RF 85mm f/1.2"

  Scenario: Get lens recommendations for photography type
    Given I have lenses suitable for different photography types:
      | Id | Make   | Model           | FocalLengthMin | FocalLengthMax | ApertureMin | LensType  |
      | 1  | Canon  | RF 85mm f/1.2   | 85             | 85             | 1.2         | Prime     |
      | 2  | Canon  | RF 16-35mm f/2.8| 16             | 35             | 2.8         | Wide Zoom |
      | 3  | Canon  | RF 100mm f/2.8  | 100            | 100            | 2.8         | Macro     |
    When I get lens recommendations for "Portrait Photography"
    Then I should receive a successful photography result
    And the recommendations should prioritize lenses with wide apertures
    And "Canon RF 85mm f/1.2" should be recommended

  Scenario: Calculate lens crop factor equivalence
    Given I have a lens with focal length 50mm
    And I have cameras with different crop factors:
      | CameraId | Make   | Model     | CropFactor |
      | 1        | Canon  | EOS R5    | 1.0        |
      | 2        | Canon  | EOS M50   | 1.6        |
    When I calculate the equivalent focal length for each camera
    Then I should receive a successful photography result
    And the full frame equivalent should be 50mm
    And the crop sensor equivalent should be 80mm

  Scenario: Validate lens specifications
    Given I want to validate lens specifications
    When I validate a lens with the following specs:
      | Make   | Model           | FocalLengthMin | FocalLengthMax | ApertureMin | ApertureMax |
      | Canon  | RF 24-70mm f/2.8| 24             | 70             | 2.8         | 22.0        |
    Then I should receive a successful photography result
    And the lens specifications should be valid
    And the focal length range should be valid
    And the aperture range should be valid

  Scenario: Handle invalid lens specifications
    Given I want to validate lens specifications
    When I validate a lens with invalid specs:
      | Make   | Model           | FocalLengthMin | FocalLengthMax | ApertureMin | ApertureMax |
      | Canon  | RF 24-70mm f/2.8| 70             | 24             | 22.0        | 2.8         |
    Then I should receive a photography failure result
    And the error should indicate invalid focal length range
    And the error should indicate invalid aperture range

  Scenario: Import lens database from external source
    Given I have lens data in CSV format:
      """
      Make,Model,FocalLengthMin,FocalLengthMax,ApertureMin,ApertureMax,MountType
      Canon,RF 24-70mm f/2.8,24,70,2.8,22.0,RF
      Canon,RF 70-200mm f/4,70,200,4.0,32.0,RF
      """
    When I import the lens database
    Then I should receive a successful photography result
    And 2 lenses should be imported successfully
    And the imported lenses should have correct specifications