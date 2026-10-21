# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — Enemy01 character model (2026-10-21, branch `claude/enemy-model`)

The capsule dummy is replaced by the Enemy01 character (Meshy art in `Assets/Art/Enemy`), with animations driven by
the game: idle, charge/shoot, flinch, lifted, knocked out, get up. Bolts now come from his right hand (the pen).
Import settings, material and Animator are set up automatically by the build. Gameplay is unchanged except: the
dummy holds still and holds fire while getting up after a knock-out, and it no longer fires instantly on respawn.
Untested (built in the cloud only).

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Launch, look at the opponent | The character stands where the dummy was, about your height-ish (1.9 m), textured (not pink, not white), **facing you**, playing a breathing idle. Tell Claude if he faces away or sideways | — |
| T2 | Watch him attack | During the yellow/orange glow he plays the aim/shoot animation; the glow and the bolt come from his **right hand**, not his chest | `Dummy fires` |
| T3 | Hit him with a bolt | Short flinch animation, then back to idle. Damage numbers as before | `HIT ...` |
| T4 | Hold the laser on him | He flinches every ~0.4 s at most, not every frame (no jitter) | `Laser HIT ...` |
| T5 | Whirlwind him | He is lifted with the lifted/falling animation and drops; fall damage as before | `Dummy lifted by whirlwind` |
| T6 | Knock him out | He plays the knock-out animation (no stiff tipping over). After ~3.5 s he gets up (about 4 s), standing still and **not shooting** until he is up | `Dummy knocked out!` |
| T7 | Headshots | Aim at his actual head: headshots still register (orange "!" number) | `HEADSHOT on the dummy` |
| T8 | Body shots | Aim at his chest/legs: normal damage; shots don't pass through his visible body | — |
| T9 | Damage flash / slowed tint | He flashes red when hit; light blue tint while slowed by the whirlwind; textures still visible | — |
| T10 | Performance | No stutter compared with the capsule dummy (try the effects quality on High) | — |
| T11 | Overall look | Does he read as a villain at 5.5 m? Anything wrong (scale, feet in the floor, floating, stretched)? | Report |

If something looks broken, the build log (`Builds\unity-build.log`) has lines starting `RingFighter enemy:` that say
whether the rig mapped as humanoid and how many clips loaded.

## Regression (every build)

| # | Do this | Expected |
| --- | --- | --- |
| R1 | Look around | Full VR, head tracking smooth |
| R2 | Bolt the dummy until it dies | Health bar drops, flash + knockback, falls over, respawns after ~3.5 s |
| R3 | Fire anything | Controller buzz on firing |
| R4 | Spam attacks | Energy drains and refills after a short pause; moves stop when empty |
| R5 | Debug panel | Y toggles it; top line says `physics test OK` (FAILED is OK too, hits use the backup) |
| R6 | Aim tilt | A / B (with panel showing, not paused) changes `Aim tilt`; a relaxed wrist points at the dummy |
| R7 | Ring hand pick | Prompt at start, pull a trigger to choose, ring only on that hand |
| R8 | Ring hand alone | Every trigger pull fires one bolt; off hand never fires |
| R9 | Focus stance | Tap = bolt, hold = laser (see T4) |
| R10 | Shield, double shield, whirlwind, charged blast | All still work as before |
| R11 | Enemy bolt, player damage, knock-out and restart | Bolt flies at your chest; hit = flash + buzz; 0 HP = fade and restart |
| R12 | Pause with the left menu button | Freezes and resumes (works as of 2026-10-19) |
| R13 | Off-hand stick movement | Dash and Smooth both work (pass as of 2026-10-19) |
| R14 | Bolt clashes, laser cuts bolts, headshots both ways, orange head-aimed shots | Passed 2026-10-20 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
