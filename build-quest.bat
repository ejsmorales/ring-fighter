@echo off
REM ===================================================================================
REM  build-quest.bat  -  pull, build and install Ring Fighter on the Quest (no Unity window)
REM
REM  Before running: close the Unity editor and plug in the headset (USB).
REM  Double-click to run with the defaults, or run from a terminal with options:
REM      build-quest.bat -NoPull -Wireless
REM
REM  OPTIONS (combine as many as you like):
REM    -NoPull      Skip "git pull" and build what is already on this computer.
REM    -NoInstall   Build the APK only (Builds\RingFighter.apk), do not install it.
REM    -SkipScene   Do not rebuild the training scene (faster, but new scene changes
REM                 from Claude will NOT be included).
REM    -Wireless    Install over Wi-Fi instead of the USB cable. Uses the headset
REM                 address remembered by tools\quest-wireless.ps1.
REM
REM  To always use some options when double-clicking, put them in OPTIONS below, e.g.:
REM      set OPTIONS=-NoPull -Wireless
REM
REM  OTHER HANDY COMMANDS (run in PowerShell from C:\Dev\RingFighter):
REM    Set up / reconnect wireless ADB (cable plugged in the first time):
REM      powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1
REM    Reconnect wirelessly to a specific address:
REM      powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1 -Ip 192.168.86.25
REM    Install an APK you already built, and start it:
REM      powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1 -Usb -Apk Builds\RingFighter.apk -Launch
REM
REM  If a build fails, the error lines are printed below. Full log: Builds\unity-build.log
REM ===================================================================================

set OPTIONS=
REM set OPTIONS=-NoPull
REM set OPTIONS=-NoInstall
REM set OPTIONS=-SkipScene
REM set OPTIONS=-Wireless
REM set OPTIONS=-NoPull -SkipScene

cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\build-quest.ps1" %OPTIONS% %*
echo.
if errorlevel 1 (echo *** Something went wrong - see the messages above.) else (echo Done. Put on the headset.)
pause
