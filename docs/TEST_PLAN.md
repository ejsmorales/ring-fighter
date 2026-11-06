# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — new recharge motion (2026-11-06, second build)

Last build: "a lot more manageable" (evade, dodge label). Owner: change the recharge motion from raising the hand to
"cocking" the ring hand so you can see it shine. **New motion:** make a fist with the RING hand (squeeze its grip)
and bring the ring up in front of your face, about a forearm's length away (8-50 cm ahead, chin to forehead
height). The off hand no longer needs to grip. Still: no attacks while charging, slow walk, the enemy sees it as an
opening. The glow is now concentrated on the ring itself, with only a small wisp above it so it doesn't block your
view. Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Make a fist with the ring hand (grip), bring the ring in front of your face | Ring shines (pulsing green glow), soft pulses, energy fills fast | `Recharging`, `Recharge: ... CHARGING` |
| T2 | Move the ring a little while charging | Keeps charging within a small margin | — |
| T3 | Lower the hand or open the fist | Stops | `Recharge ended: ring lowered` / `fist released` |
| T4 | Fist held but hand down at your side | No charge; hint appears | `Recharge: fist OK, bring the ring up in front of your face` |
| T5 | Normal fighting (focus laser, shield, bolts) | Doesn't start recharging by accident (the focus stance holds triggers, not the grip) | — |
| T6 | Does the glow block your view of him? Comfortable? | Report | — |

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
| R22 | Enemy EVADE (label, untouchable flicker, no counter-shot) and DODGE label | Passed 2026-11-06 ("a lot more manageable") |
| R21 | Snap turn (ring-hand stick), smoother enemy animation, walk/strafe clips | Passed 2026-11-05 ("much smoother") |
| R20 | Enemy teleport: warning + destination marker, appears only to your sides | Passed 2026-11-05 ("better") |
| R19 | Enemy AI basics: keeps distance and circles, dodges some shots, punishes openings, escapes your blast by teleporting | Passed 2026-11-05 ("hectic") |
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
