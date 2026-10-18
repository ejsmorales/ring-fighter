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

**Ring hand and off hand (2026-10-18):** at every launch a prompt asks you to pull the trigger on the hand that
wears the ring. That hand is the main attacker; the other hand is support (defense and focus) and never shoots.
Only the ring hand shows the ring; the energy bar floats over the off hand.

| Move | Type | Input | Cost |
| --- | --- | --- | --- |
| Energy bolt | Offense | Ring hand alone: point forward, pull the trigger. Fires instantly | 6 per shot |
| Focused laser | Offense | **Focus stance:** off hand resting on the ring hand (within 0.2 m). Tap = bolt; hold past ~0.22 s = continuous laser. Stops if you let go, break focus, stop pointing forward or swing hard | 20/sec |
| Charged blast | Offense | Both triggers held, hands together off to one side; release to fire where you're looking. Longer charge = bigger | Drains while charging |
| Shield | Defense | Either hand: raise the forearm (controller pointing up or sideways) and hold the trigger. **Both arms = one big shield** (double size, strength 2) | 8/sec, 12/sec doubled |
| Whirlwind | Offense (area) | Ring hand: hold trigger and circle the arm | Drains while active |

**Pose rule (2026-10-17):** the controller's direction decides between shooting and blocking. Pointing
forward = aiming (laser). Pointing up or sideways in front of you = blocking (shield). Aim assist gently
curves shots that are close to the target (bolt 9°, blast 14°).

### Ideas backlog

- **Construct punch**: trigger + A button + upward motion throws a giant glowing fist.
- Off-hand support moves (off hand never attacks): grab/pull construct, recharge stance, focus-boosted blast.
- Change the ring hand without restarting (for now it's picked once per launch).
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
  - Dummy still took no damage (third build in a row). Root cause still unconfirmed (no debug-panel log yet).
    **Change (untested):** hits no longer depend on Unity physics. Each target registers an invisible capsule
    (`HitShapes`) and shots, the laser and the whirlwind test it with plain math. Also: the dummy's Rigidbody and a
    hit collider are now saved in the scene, `Assets/link.xml` stops the build stripping physics code, and the
    debug panel's top line shows a startup physics self-test ("physics test OK / FAILED").

- **2026-10-18 (night) — first build where damage works.** Dummy takes damage, dies and respawns; haptics on
  attacks; "felt great and actually fun". Physics-free `HitShapes` fixed hits.
  - **Decisions (grill):** ring hand picked at startup every launch; laser requires the focus stance (off hand on
    the ring hand); shield works with either hand and both arms make a bigger, stronger shield; the off hand is
    defense/support only. Claude's calls: whirlwind ring hand only, charged blast stays two-handed, ring hand alone
    fires instant bolts (no tap delay). Implemented (untested).
  - Damage numbers: TextMesh created from code had no font, so numbers may have been invisible. Now given the
    built-in font (also used for the ring-hand prompt).

