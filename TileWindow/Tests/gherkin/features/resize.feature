Feature: Contains tests for resizing of nodes

Scenario: One node should cover the whole screen.
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And a window 'win1' on screen 'screen1'.
    Then node '0' on screen 'screen1' desktop '0' should have position 0x0 and size 1920x1080.

Scenario: Two horizontal nodes should cover half the screen.
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation horizontal.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    Then node '0' on screen 'screen1' desktop '0' should have position 0x0 and size 960x1080.
    Then node '1' on screen 'screen1' desktop '0' should have position 960x0 and size 960x1080.

Scenario: Two vertical nodes should cover half the screen.
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation vertical.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    Then node '0' on screen 'screen1' desktop '0' should have position 0x0 and size 1920x540.
    Then node '1' on screen 'screen1' desktop '0' should have position 0x540 and size 1920x540.
