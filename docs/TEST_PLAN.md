# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — enemy AI fixes (2026-11-05)

Last build ("a little hectic"): he could teleport behind you (no quick way to turn); he sometimes sank half into the
floor; the teleport was too quick with no hint of where he'd appear; his erratic movement made the whirlwind hard to
use. Changes: reappears only 35-55 degrees to your side and never more than 60 degrees from where you face (if that
spot is outside the arena he tries closer/other side, else skips the teleport); warning is longer (1.1 s) and a
**yellow ring + rising column of light appears at the destination**, with the warning sound coming from there;
calmer pace (circles slower, changes direction less often, pauses more, teleports every 15-25 s); a floor safeguard
lifts his model if an animation pushes his feet into the floor, and logs which animation did it. Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Wait for a teleport (15-25 s) | He flickers for about a second; at the same time a yellow ring and column of light grow on the floor where he'll appear, and the rising sound comes from that side | `Dummy teleports (timer)` |
| T2 | Watch where he reappears, several times | Always in your side vision, left or right, never behind you; then a quick bolt | `Dummy quick-fires after teleporting` |
| T3 | Stand near the edge of the arena facing outward and wait | He still only appears to your sides or front, or skips the teleport | — |
| T4 | Play a few minutes and watch his feet | He never sinks into the floor. If a line `Enemy sank 0.xx m (StateName) - corrected` appears on the panel, tell Claude the state name (that's the animation causing it) | `Enemy sank ...` (report it) |
| T5 | Overall pace | Calmer than last build: still circles and dodges, but less erratic | `Enemy: circling` / `watching` |
| T6 | Try the whirlwind on him again | Report whether it's usable now (it is unchanged; see the options Claude gave) | Report |

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
| R19 | Enemy AI basics: keeps distance and circles, dodges some shots, punishes openings, escapes your blast by teleporting | Passed 2026-11-05 ("hectic") |
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
