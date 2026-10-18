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
| Energy bolt / laser | Offense | Point forward and **tap** the trigger for a single bolt (6 energy). **Hold** it past ~0.22 s for a continuous laser. The laser stops if you stop pointing forward or swing the hand hard | Bolt 6; laser 20/sec |
| Charged blast | Offense | Both triggers held, hands together off to one side; release to fire where you're looking. Longer charge = bigger | Drains while charging |
| Shield | Defense | Raise your forearm to block (controller pointing up or sideways, not at the opponent) and hold the trigger | Drains while held |
| Whirlwind | Offense (area) | Hold trigger and circle your arm | Drains while active |

**Pose rule (2026-10-17):** the controller's direction decides between shooting and blocking. Pointing
forward = aiming (laser). Pointing up or sideways in front of you = blocking (shield). Aim assist gently
curves shots that are close to the target (bolt 9°, blast 14°).

### Ideas backlog

- **Construct punch**: trigger + A button + upward motion throws a giant glowing fist.
- More two-hand combos using both controllers together.
- **Ring Nova** (idea from the owner, 2026-10-18): hold both triggers AND both grips (all four), hands start
  up by the shoulders, sweep both down to the hips in one motion: a green explosion around the player that
  damages nearby enemies and blocks/knocks away incoming shots. Needs: enemy attacks to exist first (roadmap 3),
  a big energy cost or cooldown, and a deliberate-squeeze grip threshold so just holding the controllers doesn't count.
- **Controller input vocabulary** (what can be combined): index trigger, grip (middle finger), A/B on the right,
  X/Y on the left, thumbstick (tilt and click). The menu/Oculus buttons belong to the system. Y currently
  toggles the debug panel; A/B tune the aim tilt while the panel shows.
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

- **2026-10-18 — wireless build.** Much more responsive; gestures perform consistently and attacks fire.
  - Attacks passed through the dummy (no damage). Cause not confirmed. Suspected: the moving dummy's colliders
    weren't being tracked by physics. **Changes made (untested):** dummy gets a kinematic Rigidbody, projectiles
    sync transforms before each cast, and every shot now logs `HIT <part>`, `Shot hit <thing> (not a target)` or
    `Shot missed (expired)` in the debug readout. If it still passes through, read that log and report it.
  - The always-on green aim line was confusing and cluttered. **Decision:** no aim indicator at all; aiming is
    skill-based, helped by aim assist. Line removed from the scene builder.
  - **Decision (hit feedback):** red hit flash and knockback, floating damage numbers, impact burst, impact sound
    (generated in code for now), controller buzz when firing. Implemented in `Impact.cs` (untested).
  - **Decision (art):** style not chosen yet; Claude to present free-pack options in different styles.

- **2026-10-18 (later) — build with feedback changes.** Aim line gone, much more responsive.
  - The single bolt was awkward to aim: the controller had to be bent down for it to point forward. Cause:
    the controller's own forward axis points up along the handle. **Change (untested):** pointing direction is
    tilted down by `Aim Pitch Offset` (default 50 degrees) on each hand's Tracked Node. Tune it using the `fwd`
    number in the debug panel: it should read about 0.9+ when you point at the dummy with a relaxed wrist.
    Raise the offset if you still have to bend down; lower it if shots land low.
  - **Decision:** replace the single bolt with a held laser that burns energy. Implemented as `LaserMove`
    (the old `EnergyBoltMove` script is kept but no longer used). Open idea: beam hum/loop sound.

- **2026-10-18 (evening) — laser build.** Feels better. Findings and changes (all untested):
  - Bolt/laser still needed a bent controller to aim. **Change:** default aim tilt raised 50 to 65 degrees, and
    it can now be tuned in the headset: with the debug panel showing, **A** (right controller) tilts the aim
    5 degrees more, **B** 5 less. The value is saved on the headset (reinstalling the app resets it to the default).
    Once it feels right, tell Claude the number shown on the panel and it becomes the default.
  - The laser fired the instant the trigger was pulled, so there was no single shot. **Change:** the trigger now
    decides on release or after 0.22 s: release sooner = single bolt, keep holding = laser. `Hold Delay` on the
    Laser Move component tunes it.
