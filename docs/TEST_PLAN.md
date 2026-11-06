# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — recharge stance + whirlwind pull (2026-11-05, third build)

Last build: "much smoother and manageable". But with him moving you miss more and run low on energy, and the
whirlwind is hard to use. New: a **recharge stance** (squeeze BOTH grips and raise the ring hand above your head:
+35 energy/s, but no attacks, slow walk, and he sees it as an opening); the **whirlwind gently pulls** him toward it
when it passes within 3 m (0.6 m/s at 3 m up to 1.2 m/s close in; he can still walk out). Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Spend energy, then squeeze both grips and raise the ring hand above your head | Green glow and a column of light at your ring, soft pulses in the ring hand, energy bar fills fast (empty to full in about 3 s) | `Recharging` |
| T2 | Lower the ring hand (or let go of a grip) | Stance ends, glow fades | `Recharge ended: ring hand lowered (energy NN)` |
| T3 | Hold the stance at full energy | Pulses stop; nothing else happens | — |
| T4 | Try to fire, shield or whirlwind while in the stance | Nothing fires (both hands are busy) | — |
| T5 | Move with the off-hand stick while in the stance | Slow walk (about a third of normal speed), no dash | — |
| T6 | Recharge while he's attacking | His next shot comes sooner (he punishes it) | `Dummy punishes an opening (you're recharging)` |
| T7 | Grips held but ring hand at chest height | No recharge (the ring must be above your head) | — |
| T8 | Send a whirlwind past him, about 2-3 m away | He's dragged toward the vortex; if it reaches him he's lifted as before | `Whirlwind caught the dummy` |
| T9 | Whirlwind near him while he walks away | He drifts toward it but can still escape if moving directly away | — |
| T10 | Feel: is the stance comfortable and worth the risk? Is the pull enough now? | Report | — |

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
| R21 | Snap turn (ring-hand stick), smoother enemy animation, walk/strafe clips | Passed 2026-11-05 ("much smoother") |
| R20 | Enemy teleport: warning + destination marker, appears only to your sides | Passed 2026-11-05 ("better") |
| R19 | Enemy AI basics: keeps distance and circles, dodges some shots, punishes openings, escapes your blast by teleporting | Passed 2026-11-05 ("hectic") |
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
