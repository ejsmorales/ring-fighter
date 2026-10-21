# Ring Fighter — Game Design (the bible)

This is the source of truth for what the game *is*. Update it whenever a design decision changes.

## Pitch

A one-on-one VR fighter for Meta Quest 3. You wear a Green Lantern-style power ring. Pressing buttons while
making motions with your arms creates glowing energy constructs: offensive, defensive, and neutral moves.

## Core decisions

| Topic | Decision | Notes |
| --- | --- | --- |
| Platform | Meta Quest 3, standalone (Android APK) | Built with Unity 6 (6000.6.6f1), OpenXR |
| Opponent | AI-controlled | v1 is a training dummy that moves, reacts, dies, and (since 2026-10-19) fires a telegraphed yellow bolt back |
| Player movement | Stationary first | Physically dodge and lean. Add movement (likely teleport/dash) later |
| Input | Controllers: button + motion | No hand tracking for now |
| Resource | Ring energy meter | Moves cost energy; recharges after a short pause |
| Art | Placeholder shapes now, free asset packs later | Asset packs are NOT committed to git (licensing); list them in README |
| Expandability | Every move is its own small script | New moves and two-hand combos should be additions, not rewrites |

## Move list

### Implemented (v1)

**Ring hand and off hand (2026-10-18):** at every launch a prompt asks you to pull the trigger on the hand that
wears the ring. That hand is the main attacker; the other hand is support (defense and focus) and never shoots.
Only the ring hand shows the ring. Health (left) and ring energy (right) are on a curved HUD in your view.

| Move | Type | Input | Cost |
| --- | --- | --- | --- |
| Energy bolt | Offense | Ring hand alone: point forward, pull the trigger. Fires instantly | 6 per shot |
| Focused laser | Offense | **Focus stance:** off hand resting on the ring hand (within 0.23 m; breaks at 0.35 m). Tap = bolt; hold past ~0.22 s = continuous laser. Stops if you let go, break focus, stop pointing forward or swing hard | 20/sec |
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
- **Whirlwind, two gameplay variants** (owner ideas, 2026-10-20; the VFX pass only changed how it *looks*):
  1. *Ground circle, erupts on release:* the whirlwind travels along the floor as an energy circle toward the
     enemy; when the trigger is released it erupts upward, launching and damaging the enemy as green wind circles
     around it. (Today the lift happens after 0.15 s of contact while the trigger is still held.)
  2. *Horizontal whirl from the hand:* the whirl emanates from the ring hand pointing at the enemy, parallel to the
     floor (wind perpendicular to the floor); on contact it curves upward as it launches the enemy.
  Both change the move's logic (timing, path, hit detection), so they need their own branch and test plan.

## Roadmap

1. **Controls feel** (current): make gestures reliable and aiming easy. See "Feel targets" below.
   Tuning is done with the in-headset debug readout (Y on the left controller toggles it).
2. **Feedback**: sounds, hit flashes, impact effects, clearer energy display. (Mostly done: haptics, flash, burst, numbers, HUD.)
3. **Enemy fights back** (current, first pass built 2026-10-19): the dummy fires a yellow bolt; player health and HUD; shield absorbs, double shield reflects. Next: blocking enemy, more attack types, faster/random rhythm for difficulty.
4. **Movement**: short dash/teleport to dodge.
5. **Art pass**: character model for the opponent, arena (free asset packs). **Ring effects: first pass built
   2026-10-20** (branch `claude/vfx`, see the playtest log); still to do: sound for the effects, a real arena look.
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

- **2026-10-19 — ring-hand build passed all of T1-T17.** Haptic attacks "feel really good". Focus stance failed
  often because the off hand drifted too high on the arm: focus distance raised 0.20 to **0.23 m** (break distance
  0.32 to 0.35 m).
  - **Decisions (grill) for "enemy fights back":** the dummy has 200 HP (doubled) and fires one yellow bolt every 3 s
    after a 0.8 s yellow charge-up glow (steady rhythm now; speeding up as it's hurt and random gaps are for
    later difficulty). The bolt flies 9 m/s at where your chest was when fired, 10 damage, so a lean or duck dodges
    it. Player has 100 HP; at 0 the screen fades red and the fight auto-restarts after 3 s (health, energy, dummy).
    **Shield:** single shield absorbs the bolt for 4 ring energy (SUPERSEDED the same evening: now gives back 2; if you couldn't pay, the shield broke and it hit
    you); double shield reflects it at 2x speed for 25 damage, free. **HUD:** soft-follow curved bars, health
    vertical on the left, ring energy vertical on the right (the floating bar above the off hand is gone);
    red edge flash and haptic buzz in both hands when hit. All numbers are public fields to tweak.
  - Built and syntax-checked in the cloud container only; **untested** until built and run on the headset.

- **2026-10-19 (evening) — first fight vs the fighting-back dummy.** Relatively challenging; dodging was tough because
  the dummy was close. HUD bars fine but too near the center. Requests and changes (untested):
  - **Dodging:** dummy placed 5.5 m away (was 4) with less depth wander (0.8, was 1.2), bolt speed 9 to 7 m/s.
    Reaction time from glow to impact is now about 1.6 s.
  - **HUD:** bars pushed outward (`sideOffset` 0.55 to 0.78) and 12 cm lower, toward the lower corners.
  - **Pause:** left controller menu button toggles pause (time stops, sign in front of you).
  - **Blocking rewards you:** a single shield absorbing a bolt now *restores* 2 ring energy (half of the old 4 cost).
    The "shield breaks when you can't pay" rule is gone. Double shield still reflects the bolt.

- **2026-10-19 (night) — pause works, enemy attacks.** Dodging with the head alone is hard (one dodge still got hit
  in the chest, because the torso hit sphere follows the head). **Decision:** add movement on the *off-hand*
  thumbstick (left ring = right stick, and vice versa), relative to where you look. Start with **quick dash steps**;
  keep **smooth strafing** as a switchable mode (click the stick to cycle Dash / Smooth / Off); movement is its own
  component so modes can be swapped or added. Torso hit sphere 0.22 to 0.18 m.
  - **Open idea: real-movement play with passthrough** (like Thrill of the Fight): real steps already move you (head
    tracking drives the hit tests); passthrough would show the room instead of the arena. It needs Meta's OpenXR
    passthrough feature and a manifest change, so it is a separate experiment, not built yet.

- **2026-10-19 (late night) — dash + smooth both pass; "smooth is a lot better than the dash".** Both stay in. The
  stick click switched Dash, Smooth, Off as built; **changed** to just Dash <-> Smooth (Off is an inspector tick).
  - **Decisions (grill) for the next attack changes:**
    - *Whirlwind:* hold-to-sustain (circle to start, then rest your arm; it keeps going while the trigger is held,
      speeds up over ~3 s, drains 14/s, follows your gaze, caps at 14 m). Contact lifts the dummy ~2.2 m and it
      takes fall damage on landing (capped at 40, deliberately below the vortex's own damage, per the owner's wish
      that it not out-damage the plain moves). Touching it also slows its shooting by 20% for 6 s.
    - *Bolt clashes:* equal damage cancels both in a small dark burst; the stronger carries on minus **half** of the
      weaker one's damage. Applies to your bolts/blasts vs the dummy's bolt. The laser cuts any enemy bolt it crosses.
      (Whirlwind does not touch bolts for now.)
    - *Headshots:* 1.5x on direct hits, both ways. The dummy aims at your head about 15% of the time (orange glow
      warns you).

- **2026-10-20 — fourth build.** First attempt failed to compile (`head` name clash in PlayerHealth, fixed). Then all
  passed except the whirlwind, which only pushed the dummy back. Cause: its radial knockback ejected the dummy from
  the vortex before the 0.35 s needed to lift it. Fix: whirlwind no longer pushes; lift after 0.15 s (untested).

- **2026-10-20 (night) — ring effects pass (branch `claude/vfx`, built in the cloud, untested).** Gameplay logic is
  unchanged; every effect is drawn by separate components that read the moves' state.
  - **Decisions (grill):**
    - *Overall look:* a **lightsaber glow in green**: a near-white hot core inside a saturated green halo. Done with
      layered additive shaders ("fake glow", nearly free on Quest 3). Real bloom is an optional toggle, off by default.
    - *Enemy:* **yellow** glow, **orange** when it is aimed at your head (the orange is the 1.5x head-shot warning
      from the earlier design session, not decoration). Menacing flicker, ember trail, same streak shape as yours;
      a bolt reflected by the double shield turns your green.
    - *Your bolt:* elongated streak with a fading tail. *Laser:* blade-style beam (white core, shimmering green
      body, halo), muzzle flare, sparking hot spot where it lands.
    - *Shield:* hex-grid barrier with a bright rim and a ripple from each bolt hit; the double shield is brighter.
    - *Charged blast:* pulsing sun-orb with inward sparks while charging, comet tail in flight, and an **explosion
      on every ending** (target, floor, or timing out). *Impacts:* white-green flash, shockwave ring facing you, sparks.
    - *Whirlwind (visual only):* travels as a flat glowing energy circle on the floor and rises into a green funnel
      when it catches the enemy (on contact, not on release). Owner's two gameplay variants are in the ideas backlog.
    - *Controls:* the pause menu has an effects panel: **A** cycles quality (High / Low / Minimal = no particles),
      **B** toggles bloom. Saved on the headset. This is the safety net because Quest performance can't be
      measured from the cloud.
  - Dummy hit colours stay yellow / orange (orange = head shot); each hit also gets the white-green flash.
