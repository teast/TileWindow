Feature: Moving windows around on a screen

Scenario: Two windows and one screen. Moving one of them one step
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation <orientation>.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    And window '<focus>' is focus.
    Then window '<expect1_1>' is in position 0 on screen 'screen1'.
    And window '<expect1_2>' is in position 1 on screen 'screen1'.
    When moving focused window <direction>.
    Then window '<expect2_1>' is in position 0 on screen 'screen1'.
    And window '<expect2_2>' is in position 1 on screen 'screen1'.

Examples:
 | orientation | focus | direction | expect1_1 | expect1_2 | expect2_1   |expect2_2   | comment                            |
 | vertical    | win1  | down      | win1      | win2      | win2        | win1       | move top window down.              |
 | vertical    | win1  | up        | win1      | win2      | win1        | win2       | move top window up.                |
 | vertical    | win1  | left      | win1      | win2      | win1        | win2       | move top window left.              |
 | vertical    | win1  | right     | win1      | win2      | win1        | win2       | move top window right.             |
 | vertical    | win2  | down      | win1      | win2      | win1        | win2       | move bottom window down.           |
 | vertical    | win2  | up        | win1      | win2      | win2        | win1       | move bottom window up.             |
 | vertical    | win2  | left      | win1      | win2      | win1        | win2       | move bottom window left.           |
 | vertical    | win2  | right     | win1      | win2      | win1        | win2       | move bottom window right.          |
 | horizontal  | win1  | right     | win1      | win2      | win2        | win1       | move left window right.            |
 | horizontal  | win1  | left      | win1      | win2      | win1        | win2       | move left window left.             |
 | horizontal  | win1  | up        | win1      | win2      | win1        | win2       | move left window up.               |
 | horizontal  | win1  | down      | win1      | win2      | win1        | win2       | move left window down.             |


Scenario: Three windows and one screen. Moving one of them two steps
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation <orientation>.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    And a window 'win3' on screen 'screen1'.
    And window '<focus>' is focus.
    Then window 'win1' is in position 0 on screen 'screen1'.
    And window 'win2' is in position 1 on screen 'screen1'.
    And window 'win3' is in position 2 on screen 'screen1'.
    When moving focused window <direction>.
    Then window '<expect1_1>' is in position 0 on screen 'screen1'.
    And window '<expect1_2>' is in position 1 on screen 'screen1'.
    And window '<expect1_3>' is in position 2 on screen 'screen1'.
    When moving focused window <direction>.
    Then window '<expect2_1>' is in position 0 on screen 'screen1'.
    And window '<expect2_2>' is in position 1 on screen 'screen1'.
    And window '<expect2_3>' is in position 2 on screen 'screen1'.

Examples:
 | orientation | focus | direction | expect1_1 |expect1_2 | expect1_3 | expect2_1 | expect2_2 | expect2_3 | comment                       |
 | vertical    | win3  | up        | win1      | win3     | win2      | win3      | win1      | win2      | move bottom window up         |
 | vertical    | win3  | down      | win1      | win2     | win3      | win1      | win2      | win3      | move bottom window down       |
 | vertical    | win3  | left      | win1      | win2     | win3      | win1      | win2      | win3      | move bottom window left       |
 | vertical    | win3  | right     | win1      | win2     | win3      | win1      | win2      | win3      | move bottom window right      |

 | vertical    | win1  | down      | win2      | win1     | win3      | win2      | win3      | win1      | move top window down          |
 | vertical    | win1  | up        | win1      | win2     | win3      | win1      | win2      | win3      | move top window up            |

 | vertical    | win2  | down      | win1      | win3     | win2      | win1      | win3      | win2      | move middle window down       |
 | vertical    | win2  | up        | win2      | win1     | win3      | win2      | win1      | win3      | move middle window up         |

Scenario: Moving window to another desktop
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    And window 'win1' is focus.
    Then window 'win1' is in position 0 on screen 'screen1' desktop '0'.
    And window 'win2' is in position 1 on screen 'screen1' desktop '0'.
    When moving focused window to desktop '1'.
    Then window 'win1' is in position 0 on screen 'screen1' desktop '1'.
    And window 'win2' is in position 0 on screen 'screen1' desktop '0'.
