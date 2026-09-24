kPlug Piston Transition Fix v1.0.0

CONTENTS
--------

This package contains two BepInEx plugins.

1. KPlugPistonWaitFix.dll v1.0.0
   Fixes an issue where animation navigation can stop responding after changing
   an insertion-state animation with Numpad 7 / Backspace.

2. KPlugPistonAutoResume.dll v0.2.0
   Automatically resumes piston motion through the game's normal Go route after
   changing an insertion/piston animation while piston motion is active.

TARGET
------

Koikatu 5.1
kPlug 3.6.0
BepInEx 5.4.23.5

Validated kPlug.dll SHA-256:
880b34324bf1563a1e09a02a6c0960918270fae7463132d47e5fc813bcf4607f

INSTALLATION
------------

Extract the contents of the ZIP into your Koikatu game folder.

Expected layout:

BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonWaitFix.dll
BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonAutoResume.dll

AUTO RESUME CONFIGURATION
-------------------------

After the first launch:

BepInEx\config\local.kplug.pistonautoresume.cfg

Main settings:

Enabled = true
ResumeDelaySeconds = 0.8

0.0 = fastest
0.8 = default
Up to 3.0 seconds

To disable automatic resume:
Enabled = false

STARTUP CHECK
-------------

BepInEx log:

[PistonWaitFix] v1.0.0 active.
[PistonAutoResume] v0.2.0 active.

UNINSTALLATION
--------------

Remove:

BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonWaitFix.dll
BepInEx\plugins\KPlugPistonTransitionFix\KPlugPistonAutoResume.dll

Optionally remove the Auto Resume config:

BepInEx\config\local.kplug.pistonautoresume.cfg
