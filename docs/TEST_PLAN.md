# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — Enemy01 at full height (2026-10-21)

Last build: the character worked and looked fine but was too short. The model is about 2.19 m tall; the setup had
shrunk it to 1.9 m. It now keeps its own size, and the hit capsule, head zone, chest aim point and health bar follow
his height. Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Look at him | About 2.2 m tall, clearly taller than you. Feet on the floor, health bar just above his head | — |
| T2 | Shoot his head | Headshot (orange "!" number) when hitting from the neck up | `HEADSHOT on the dummy` |
| T3 | Shoot his chest and legs | Normal hits; nothing passes through his visible body, and shots above his head miss | `HIT ...` |
| T4 | Aim assist | Shots that are nearly on target curve toward his chest, not his belly | — |
| T5 | Double-shield reflect | Reflected bolt flies back at his chest | `Reflected bolt HIT` |
| T6 | Is this the right height now? | If you want him taller still, tell Claude a number (e.g. 2.5 m) | Report |

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
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
