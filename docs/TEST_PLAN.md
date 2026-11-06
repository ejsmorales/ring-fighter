# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — bigger play area, store variety (2026-11-06, second arena build)

Last build: worked as expected. Notes: the same store building repeated; the map looks big but feels small because
you couldn't get near the cars (you were limited to 4 m from the middle, the enemy to 10 m). Changes: parked rows
pushed out (north/south rows start at 20 m, side rows at 22 m, side rows longer) and **you can now move anywhere in
the open lot right up to the car bumpers** (about 42 x 38 m); the enemy roams the same area (1 m less). The wrecked
car now sits crashed into the south row. Store fronts are laid out side by side by their real widths and will use
every design that exists (`Env_StoreFront`, `Env_StoreFront_B` ... `_F`); until more designs arrive the single one
repeats with tints. Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Walk/glide toward any row of cars | You can get right up to the bumpers (stop about 1 m short), on all four sides | — |
| T2 | Does the lot feel bigger now? | Report | — |
| T3 | Fight near an edge | He follows you around the whole lot, turns back at the edges instead of walking into cars, dodges and teleports stay inside | `Enemy: circling` |
| T4 | Teleport near an edge | He still appears only to your sides/front, inside the lot | `Dummy teleports ...` |
| T5 | Find the wrecked car | Crashed into the south row (behind you, left), no parked car overlapping it | — |
| T6 | Store strip ahead | A continuous row of store fronts, evenly spaced (still the same design until new ones are added) | — |
| T7 | Get knocked out | Fight restarts with you in the middle and him at his start | `Fight restarted` |

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
| R23 | Recharge stance: fist + ring in front of your face | Passed 2026-11-06 |
| R24 | Arena with Meshy props: cars, palms, islands, poles, store fronts, tower, corral, wreck, hillside | Passed 2026-11-06 ("worked as expected") |
| R22 | Enemy EVADE (label, untouchable flicker, no counter-shot) and DODGE label | Passed 2026-11-06 ("a lot more manageable") |
| R21 | Snap turn (ring-hand stick), smoother enemy animation, walk/strafe clips | Passed 2026-11-05 ("much smoother") |
| R20 | Enemy teleport: warning + destination marker, appears only to your sides | Passed 2026-11-05 ("better") |
| R19 | Enemy AI basics: keeps distance and circles, dodges some shots, punishes openings, escapes your blast by teleporting | Passed 2026-11-05 ("hectic") |
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
