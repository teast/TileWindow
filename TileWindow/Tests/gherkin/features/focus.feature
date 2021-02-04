Feature: Moving focus or switching virtual desktops

Scenario: When start virtual desktop 0 should be ActiveDesktop
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    Then ActiveDesktop should be index '0'.

Scenario: When switching ActiveDesktop
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    When switching ActiveDesktop to index '1'.
    Then ActiveDesktop should be index '1'.

Scenario: Two windows and one screen. Moving focus between them
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation <orientation>.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    And window '<focus>' is focus.
    Then window '<expect1>' has focus.
    When moving focus <direction>.
    Then window '<expect2>' has focus.

Examples:
 | orientation | focus | direction | expect1 | expect2 | comment                                       |
 | vertical    | win1  | down      | win1    | win2    | move focus from top window down.              |
 | vertical    | win1  | up        | win1    | win1    | move focus from top window up.                |
 | vertical    | win1  | left      | win1    | win1    | move focus from top window left.              |
 | vertical    | win1  | right     | win1    | win1    | move focus from top window right              |

 | vertical    | win2  | down      | win2    | win2    | move focus from bottom window down.           |
 | vertical    | win2  | up        | win2    | win1    | move focus from bottom window up.             |
 | vertical    | win2  | left      | win2    | win2    | move focus from bottom window left.           |
 | vertical    | win2  | right     | win2    | win2    | move focus from bottom window right           |

 | horizontal  | win1  | down      | win1    | win1    | move focus from left window down.             |
 | horizontal  | win1  | up        | win1    | win1    | move focus from left window up.               |
 | horizontal  | win1  | left      | win1    | win1    | move focus from left window left.             |
 | horizontal  | win1  | right     | win1    | win2    | move focus from left window right             |

 | horizontal  | win2  | down      | win2    | win2    | move focus from right window down.            |
 | horizontal  | win2  | up        | win2    | win2    | move focus from right window up.              |
 | horizontal  | win2  | left      | win2    | win1    | move focus from right window left.            |
 | horizontal  | win2  | right     | win2    | win2    | move focus from right window right            |

Scenario: Three windows and one screen. Moving focus between them
    Given a primary screen 'screen1' position 0x0 with resolution 1920x1080.
    And screen 'screen1' has orientation <orientation>.
    And a window 'win1' on screen 'screen1'.
    And a window 'win2' on screen 'screen1'.
    And a window 'win3' on screen 'screen1'.
    And window '<focus>' is focus.
    Then window '<focus>' has focus.
    When moving focus <direction>.
    Then window '<expect1>' has focus.
    When moving focus <direction>.
    Then window '<expect2>' has focus.

Examples:
 | orientation | focus | direction | expect1 | expect2 | comment                                       |
 | vertical    | win1  | down      | win2    | win3    | move focus from top window down.              |
 | vertical    | win3  | up        | win2    | win1    | move focus from bottom window up.             |

 | horizontal  | win1  | right     | win2    | win3    | move focus from left window left.             |
 | horizontal  | win3  | left      | win2    | win1    | move focus from right window right.           |

