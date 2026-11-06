# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — recharge made easier to trigger and see; DODGE label (2026-11-06)

Last build: the recharge stance never triggered; the enemy seemed to become "immune" after several hits. There is
no immunity in the code: hits only stop counting while he is knocked out. Likely causes of "immune": he sidesteps
(40% of shots aimed at him), he escape-teleports after 35+ damage in 2 s, or your bolt collides with his incoming
bolt and both cancel (dark burst). Changes: recharge starts with the ring hand at forehead height or higher (was
5 cm above the head) and stays on down to 25 cm below head height; a `Recharge:` line on the debug panel shows
both grip pressures and the ring hand's height, and a hint appears if the grips are held but the hand is too low;
a floating **DODGE** appears over him when he sidesteps. Untested.

**The recharge motion:** squeeze and hold the **grip** buttons (the side buttons under your middle fingers) on
**both** controllers, then raise the ring hand straight up over your head, like holding the ring to the sky. The
off hand can stay anywhere.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | With the panel open, squeeze both grips (hands down) | `Recharge:` line shows both grips near 1.0 and the ring hand well below the head; a hint says to raise the ring hand | `Recharge: grips OK, raise the ring hand above your head` |
| T2 | Keep squeezing, raise the ring hand overhead | Charging starts: green glow and column at the ring, energy fills fast | `Recharging`, `CHARGING` on the Recharge line |
| T3 | If T2 doesn't start | Read the Recharge line: which grip is under 0.7, or what height is shown, and tell Claude | Report the line |
| T4 | Fire bolts at him until he sidesteps | Yellow DODGE word floats over him each time he dodges | `Dummy dodges` |
| T5 | When he seems "immune" again, read the Recent list | Look for `Dummy dodges`, `Dummy teleports (escaping damage)`, or `Bolts cancelled`. Tell Claude which (or none) | Report |

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
