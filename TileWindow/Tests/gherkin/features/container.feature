Feature: Actions regarding an container. For example creating an container, changing direction on it, etc.

Scenario: When focus a window and requesting horizontal and screens orientation is vertical then create a new container.
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation vertical.
    And a window 'win1' on screen 'screen1'.
    And window 'win1' is focus.
    Then node '0' on screen 'screen1' desktop '0' should be of type 'WindowNode'.
    When requesting orientation horizontal.
    Then node '0' on screen 'screen1' desktop '0' should be of type 'ContainerNode'.

Scenario: When focus a window and requesting vertical and screens orientation is horizontal then create a new container.
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation horizontal.
    And a window 'win1' on screen 'screen1'.
    And window 'win1' is focus.
    Then node '0' on screen 'screen1' desktop '0' should be of type 'WindowNode'.
    When requesting orientation vertical.
    Then node '0' on screen 'screen1' desktop '0' should be of type 'ContainerNode'.
