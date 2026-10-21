# Ring Fighter — Setup and troubleshooting

## Machines

| Machine | Use |
| --- | --- |
| Desktop (i5-13600, 32 GB, RTX 5070 Ti) | Main dev machine: building and installing to the headset |
| Surface Pro 9 | Secondary: code and editor work, builds over USB |

Both need **Unity 6000.6.6f1** with **Android Build Support** (OpenJDK, Android SDK & NDK), Git for Windows
(`git lfs install` once), and the repo cloned to `C:\Dev\RingFighter`.

## Daily git workflow

1. Before starting: close Unity, `git pull`.
2. Before stopping or switching machines: **File > Save Project**, close Unity, then
   `git add .`, `git commit -m "..."`, `git push`.
3. Don't edit the same scene on both machines without pulling in between.

## Testing on the headset

**Build and Run (works today):** headset plugged in over USB-C, **File > Build Profiles > Android > Build And Run**.
Save APKs to `C:\Dev\RingFighter\Builds` (ignored by git). Relaunch later from the headset's App Library under
**Unknown Sources**. The headset can be unplugged after install.

**Live preview in the editor:** needs a PC VR runtime. The Meta Quest Link installer failed on the desktop
(possibly the RTX 50-series not being on Meta's supported list yet). Alternative: Steam + SteamVR on the PC,
**Steam Link** app on the Quest, and set SteamVR as the OpenXR runtime (SteamVR > Settings > OpenXR).

## One-time headset setup (done)

1. Verified Meta developer account and an organization at developers.meta.com.
2. Meta Horizon phone app: headset > Headset Settings > Developer Mode on. Restart the headset.
3. Headset: Settings > Developer > MTP Notification on. Plug in, accept **Allow USB debugging** (always allow).
4. Check with `adb devices` (adb is in Unity's Android SDK `platform-tools` folder).

## Problems we've hit, and fixes

| Symptom | Cause | Fix |
| --- | --- | --- |
| `AssetImportWorker ... has crashed` in `OpenXRPackageSettings`, plus empty `XR 1`…`XR 5` folders | OpenXR creating its settings folder inside an import worker | Delete the stray folders, create `Assets\XR\Settings`, set import worker counts low in Project Settings > Editor |
| Game runs as a flat window, no hands or moves | OpenXR not ticked under XR Plug-in Management (Android) | Tick OpenXR on the Android and Windows tabs; it's now saved in `Assets/XR/XRGeneralSettingsPerBuildTarget.asset`. `RingRig` logs an error if VR isn't running |
| No "Allow USB debugging" prompt | Developer Mode not active, or charge-only cable | Verified dev account, restart headset, data cable, enable MTP Notification |
| `git checkout` refuses due to local `ProjectSettings` changes | Unity rewrote settings on first open | Close Unity, `git restore` the file, then checkout |
| Project Validation warns about Screen Space Ambient Occlusion | Only the PC renderer has it; Quest uses the Mobile renderer | Safe to ignore |

## Wireless testing (either computer)

Run this from the repo folder with the Quest plugged in by USB and on the same Wi-Fi as the PC:

```powershell
powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1
```

It finds `adb` (on PATH, in the Unity Hub editor folders, or via `ANDROID_HOME`), reads the headset's Wi-Fi
address, switches it to wireless mode and connects. Then unplug the cable. Add `-Apk C:\path\to\file.apk` to
install a build as well (installs over the old copy, so saved settings survive).

Wireless mode lives on the headset until it restarts, but **each computer must connect to it itself**: running
the script on a second PC with no cable reconnects to the address remembered from the last run (or pass
`-Ip 192.168.x.x`). Run it with the cable again whenever the headset restarts or its address changes. In Unity, open File > Build Profiles >
Android and choose the wireless entry (the IP address) under Run Device, then Build And Run. The Run Device
choice is per computer and has to be picked on each machine. The script is untested on a real machine so far:
if it errors, paste the message to Claude.

## Attacks pass through the dummy

Open the debug panel (Y). The top line shows the startup physics self-test. Fire at the dummy and read "Recent":
`HIT ...` means damage landed; `Shot hit Floor (not a target)` means the shot went low (aim tilt, A/B);
`Shot missed (expired)` means it flew past. Since 2026-10-18, hits use `HitShapes` math as well as physics.

## One-command build and install (no Unity window)

Close the Unity editor, plug in the Quest, then from the repo folder:

```powershell
powershell -ExecutionPolicy Bypass -File tools\build-quest.ps1
```

Or just double-click `build-quest.bat` in the repo folder (same thing; the window stays open at the end).

It runs `git pull`, then Unity in batch mode (`Assets/Editor/CommandLineBuild.cs`): rebuilds the training scene,
builds `Builds\RingFighter.apk`, then installs it over USB and starts the game. Options: `-NoPull`, `-NoInstall`,
`-SkipScene` (keep the current scene), `-Wireless` (use wireless ADB instead of the cable). If the build fails it
prints the error lines from `Builds\unity-build.log`. The first run on a machine is slow (Unity imports the
project); later runs are faster. Untested on a real machine so far.


### build-quest.bat: build OK, then 'Cannot convert value "@{Serial=...}" to type SwitchParameter'
PowerShell variable names are not case sensitive, so a local `$usb` overwrote the `-Usb` switch in `tools\quest-wireless.ps1`.
Fixed 2026-10-18 by renaming the locals (`$usbDev`, `$wifiDev`, `$headsetIp`). If it recurs, `git pull` and run
`powershell -ExecutionPolicy Bypass -File tools\quest-wireless.ps1 -Apk Builds\RingFighter.apk -Usb -Launch`
(the APK is already built, so no rebuild is needed).

### Build failed: CS0841 / CS0136 'head' in PlayerHealth.cs (2026-10-19)
A new `out bool head` parameter clashed with an existing local variable of the same name in `SegmentHits`. Fixed by
renaming the local to `headPos`. (The cloud syntax check can't catch name clashes like this; only Unity's compiler does.)

### Effects look like plain shapes, or the panel says `VFX unavailable (rebuild scene)` (2026-10-20)
The "VFX System" object is added by **Ring Fighter > Build Training Scene** and is saved into
`Assets/Scenes/TrainingArena.unity`. A scene built before the effects pass (or a build with `-SkipScene`) doesn't
have it, so the old placeholder shapes are drawn. Rebuild the scene (the default `tools\build-quest.ps1` does).

### An effect is pink, black or invisible on the headset (2026-10-20)
Pink means a shader failed to compile or was stripped. Check `Builds\unity-build.log` for lines containing `RF_`.
The effect shaders (`Assets/VFX/Shaders`) are referenced by the materials in `Assets/VFX/Materials`, which the scene's
VFX System object holds; if someone deletes a material or the object, rebuild the scene to recreate them. Don't
load these shaders by name only (`Shader.Find` / `Resources`): the Quest build strips shaders nothing references.

### Effects are slow on the headset
Pause (left menu button) and press **A** to drop effects quality (High, Low, Minimal), and make sure Bloom (**B**) is
off. Tell Claude which setting first felt smooth so the defaults can be tuned.
