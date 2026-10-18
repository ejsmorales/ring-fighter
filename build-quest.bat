@echo off
REM Double-click to pull, build and install Ring Fighter on the Quest (no Unity window).
REM Close the Unity editor first and plug in the headset.
REM Options can be added when run from a terminal, e.g.:  build-quest.bat -NoPull -Wireless
cd /d "%~dp0"
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tools\build-quest.ps1" %*
echo.
if errorlevel 1 (echo *** Something went wrong - see the messages above.) else (echo Done. Put on the headset.)
pause
