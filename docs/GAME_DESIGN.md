# Ring Fighter — Game Design (the bible)

This is the source of truth for what the game *is*. Update it whenever a design decision changes.

## Pitch

A one-on-one VR fighter for Meta Quest 3. You wear a Green Lantern-style power ring. Pressing buttons while
making motions with your arms creates glowing energy constructs: offensive, defensive, and neutral moves.

## Core decisions

| Topic | Decision | Notes |
| --- | --- | --- |
| Platform | Meta Quest 3, standalone (Android APK) | Built with Unity 6 (6000.6.6f1), OpenXR |
| Opponent | AI-controlled | v1 is a training dummy that moves, reacts, and dies but doesn't attack |
| Player movement | Stationary first | Physically dodge and lean. Add movement (likely teleport/dash) later |
| Input | Controllers: button + motion | No hand tracking for now |
| Resource | Ring energy meter | Moves cost energy; recharges after a short pause |
| Art | Placeholder shapes now, free asset packs later | Asset packs are NOT committed to git (licensing); list them in README |
| Expandability | Every move is its own small script | New moves and two-hand combos should be additions, not rewrites |

## Move list

### Implemented (v1)

| Move | Type | Input | Cost |
| --- | --- | --- | --- |
| Energy bolt | Offense | Point the controller forward and pull the trigger (fires on pull) | 6 per shot |
| Charged blast | Offense | Both triggers held, hands together off to one side; release to fire where you're looking. Longer charge = bigger | Drains while charging |
| Shield | Defense | Raise your forearm to block (controller pointing up or sideways, not at the opponent) and hold the trigger | Drains while held |
| Whirlwind | Offense (area) | Hold trigger and circle your arm | Drains while active |

**Pose rule (2026-10-17):** the controller's direction decides between shooting and blocking. Pointing
forward = aiming (bolt). Pointing up or sideways in front of you = blocking (shield). Aim assist gently
curves shots that are close to the target (bolt 9°, blast 14°).

### Ideas backlog

- **Construct punch**: trigger + A button + upward motion throws a giant glowing fist.
- More two-hand combos using both controllers together.
- Neutral moves (for example, recharge stance, grab/pull, construct wall).

## Roadmap

1. **Controls feel** (current): make gestures reliable and aiming easy. See "Feel targets" below.
   Tuning is done with the in-headset debug readout (Y on the left controller toggles it).
2. **Feedback**: sounds, hit flashes, impact effects, clearer energy display.
3. **Enemy fights back**: the opponent uses ring-style attacks and blocks; player health; shields actually stop shots.
4. **Movement**: short dash/teleport to dodge.
5. **Art pass**: character model for the opponent, arena, ring effects (free asset packs).
6. **More moves and combos** from the backlog.
7. Menus, rounds, win/lose flow.

## Feel targets

- A move should fire when the player clearly does its gesture, close to 100% of the time.
- Moves should almost never fire by accident.
- Aiming should feel like pointing a finger: shots go where the player expects.

## Playtest log

Record each headset session here: date, build, what worked, what didn't.

- **2026-10-17 — first on-device build.** VR, tracking, all four moves, and the dummy work.
  - Bolt only fired by accident (it required a very fast tap-and-release under 0.28 s).
  - Shield was hit or miss (its pose overlapped with simply pointing at the dummy).
  - Charged blast consistent but aiming was hard (it averaged both controllers' directions).
  - Whirlwind consistent, but its look isn't what's wanted yet (revisit in the art pass).
  - **Changes made:** bolt fires on trigger pull; pose rule above; blast aims with head gaze; aim assist;
    in-headset debug readout showing what the gesture system sees.
