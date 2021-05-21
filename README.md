# WyzeSense
Hobby Project to interface with Wyze V1 and V2 sensors/Keypad using the V1 Bridge. It may not be pretty but that is why know one is paying me for it :)
Note: tested on ubuntu 20.04

------
#### WyzeSenseCore

The heart of the project. 
Based on work from [HclX](https://github.com/HclX). Additional functionality found using [Ghidra](https://github.com/NationalSecurityAgency/ghidra).
Certaintly needs some refactoring but that isn't nearly as fun as uncovering new features.
1. [HclX/WyzeSensePy](https://github.com/HclX/WyzeSensePy)

------
#### WyzeSenseUpgrade

Console application to flash the hms cc1310 firmware to the v1 bridge. Use this at own risk - not all checks added.
1. [deeplyembeddedWP/cc2640r2f-sbl-linux](https://github.com/deeplyembeddedWP/cc2640r2f-sbl-linux)
2. [CC1310 Technical Reference Manual](https://www.ti.com/lit/ug/swcu117i/swcu117i.pdf?ts=1619413277495)

------
#### WyzeSenseApp

Console application to test WyzeSenseCore functionality

------
#### WyzesenseBlazor

Test project to bridge Wyze Sense to MQTT in a Blazor server app. Dipping my toes into some web dev. Still a work in progress

