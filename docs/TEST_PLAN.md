# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: shield didn't absorb, panel said 'Shield broke: no energy'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — whirlwind lift fix (2026-10-20)

Last build: everything passed except the whirlwind, which only pushed the dummy back. Cause: the whirlwind still
pushed the dummy out of its radius every frame, so it never stayed inside long enough (0.35 s) to be lifted.
**Fix:** the whirlwind no longer pushes at all, and it lifts after 0.15 s of contact. Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Start a whirlwind, keep the trigger held, steer it onto the dummy | The dummy is **not** pushed away. Within a moment it rises ~2.2 m, hangs, drops, orange ~40 damage number on landing | `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
| T2 | Keep the whirlwind on it | No second lift for ~2.5 s; vortex damage keeps ticking | `Whirlwind HIT` |
| T3 | After contact | Dummy turns light blue; its shots come ~20% slower for 6 s; no shots while airborne | — |
| T4 | Kill the dummy while it is in the air | It drops to the floor and falls over normally, respawns | `Dummy knocked out!` |
| T5 | Feel: is the lift satisfying? Fall damage 40 right? | Report | — |

If T1 still fails, read out the "Recent" lines on the debug panel while the whirlwind is on the dummy.

## Regression (every build)

| # | Do this | Expected |
| --- | --- | --- |
| R1 | Look around | Full VR, head tracking smooth |
| R2 | Bolt the dummy until it dies | Health bar drops, flash + knockback, falls over, respawns after ~3.5 s |
| R3 | Fire anything | Controller buzz on firing |
| R4 | Spam attacks | Energy drains and refills after a short pause; moves stop when empty |
| R5 | Debug panel | Y toggles it; top line says `physics test OK` (FAILED is OK too, hits use the backup) |
| R6 | Aim tilt | A / B (with panel showing) changes `Aim tilt`; a relaxed wrist points at the dummy |
| R7 | Ring hand pick | Prompt at start, pull a trigger to choose, ring only on that hand |
| R8 | Ring hand alone | Every trigger pull fires one bolt; off hand never fires |
| R9 | Focus stance | Tap = bolt, hold = laser (see T11) |
| R10 | Shield, double shield, whirlwind, charged blast | All still work as before |
| R11 | Enemy bolt, player damage, knock-out and restart | Bolt flies at your chest; hit = flash + buzz; 0 HP = fade and restart |
| R12 | Pause with the left menu button | Freezes and resumes (works as of 2026-10-19) |
| R13 | Off-hand stick movement | Dash and Smooth both work (pass as of 2026-10-19) |
| R14 | Bolt clashes, laser cuts bolts, headshots both ways, orange head-aimed shots | Passed 2026-10-20 |
| R15 | Whirlwind keeps going while the trigger is held (no circling needed after start) | — |
