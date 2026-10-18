# Ring Fighter

A Quest 3 VR fighting game: you wear a Green Lantern-style power ring, and button presses plus arm motions
trigger different moves (attack, defense, utility). Built with Unity 6 (6000.6.6f1) and OpenXR.

**Docs:** [Game design and roadmap](docs/GAME_DESIGN.md) · [How the code works](docs/ARCHITECTURE.md) · [Setup and troubleshooting](docs/SETUP_AND_TROUBLESHOOTING.md)

## Open the project

1. Install **Unity 6000.6.6f1** (same version on every machine) with the **Android Build Support** module
   (including OpenJDK and Android SDK & NDK Tools).
2. Install Git and Git LFS (`git lfs install`), then clone this repo and add the folder in Unity Hub.
3. The first open takes several minutes while Unity rebuilds its cache.

## One-time VR setup

Steps 1-4 and 6 are saved in the project, so they only need doing once (then commit). Step 5's Link-app setting is per PC.

1. **File > Build Profiles**: select **Android** and click **Switch Platform**.
2. **Edit > Project Settings > XR Plug-in Management**: on the **Android** tab (robot icon), tick **OpenXR**.
3. Click **OpenXR** under XR Plug-in Management (Android tab):
   - Under **Enabled Interaction Profiles**, add **Oculus Touch Controller Profile**.
   - Under **OpenXR Feature Groups**, tick **Meta Quest Support**.
4. In the same XR Plug-in Management area open **Project Validation** and click **Fix All** if anything is listed.
5. For **Quest Link** testing on the PC: in XR Plug-in Management, also open the **Windows** tab (monitor icon),
   tick **OpenXR**, and add the **Oculus Touch Controller Profile** there too. In the Meta Quest Link app on the PC,
   go to **Settings > General** and set **Meta Quest Link as the active OpenXR runtime**.
6. Menu **Ring Fighter > Build Training Scene** (creates `Assets/Scenes/TrainingArena.unity`).

## Test it

- **Quest Link (fastest):** connect the headset, enable Link, press Play in Unity.
- **On the headset:** File > Build Profiles > Android > Build And Run with the Quest plugged in and Developer Mode on.

## Moves (v1)

| Move | Input |
| --- | --- |
| Energy bolt | Point the controller forward and pull the trigger |
| Charged blast | Hold both triggers with both hands together off to one side; release either trigger to fire where you're looking |
| Shield | Raise your forearm to block (controller pointing up or sideways) and hold the trigger |
| Whirlwind | Hold the trigger and swing your arm in circles |

Each move costs ring energy (the bar above your left hand), which recharges when idle.
A debug panel to your front-left shows what the gesture system sees; press **Y** to hide or show it.

## Adding a move

Create a class that extends `Move` (see `Assets/Scripts/Move.cs`), recognize your gesture in `TryStart`, and add the
component to the **Ring Controller** object. Gesture helpers live in `Assets/Scripts/Gestures.cs`.

## Assets

Third-party asset packs are not committed to this repo. Re-import them on each machine (list them here as they are added).

## Tuning

- **Aim feels off:** each hand has an **Aim** child object (shown by the thin green line). Rotate it in the scene
  until the line points where you expect, then rebuild or save the scene.
- **Gesture too strict or too loose:** select **Ring Controller** and adjust the numbers on each move component.
