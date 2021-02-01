Feature: Moving windows around on a screen

Scenario: Two windows horizontal to each other. Moving right window to left
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation horizontal.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    And window 'win2' is focus.
    Then window 'win1' is in position 0 on screen 'screen1'.
    And window 'win2' is in position 1 on screen 'screen1'.
    When moving focused window left.
    Then window 'win2' is in position 0 on screen 'screen1'.
    And window 'win1' is in position 1 on screen 'screen1'.