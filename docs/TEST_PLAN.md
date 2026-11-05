# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — enemy AI (2026-11-04, branch `claude/enemy-ai`)

The opponent now thinks for himself (EnemyBrain): he keeps his distance and circles you, sometimes sidesteps your
shots, shoots sooner when you leave an opening, and teleports to your flank with a quick shot. He still slides
(no walk/strafe clips yet). Untested (built in the cloud only). The debug panel has a new `Enemy:` line showing
what he is doing.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Before picking the ring hand | He stands still | `Enemy: waiting` |
| T2 | Pick the ring hand and watch | He circles around you at about 5.5 m, switches direction every few seconds and sometimes stops to watch | `Enemy: circling` / `watching` |
| T3 | Walk or dash toward him | He backs off to keep his distance; if you get within 2.5 m he backs off faster | `Enemy: backing off` |
| T4 | Move away from him | He follows to close the gap | `Enemy: closing in` |
| T5 | Watch him while he charges a shot | He slows down while the glow is on, so the shot is readable | `Enemy: aiming` |
| T6 | Fire single bolts straight at him, several times | About 4 in 10 times he sidesteps (~1.3 m) just after you fire, and the bolt misses. Not more than once every 1.5 s | `Dummy dodges`, `Enemy: DODGING` |
| T7 | Fire a charged blast at him | Same chance to sidestep it | `Dummy dodges` |
| T8 | Wait 12-20 s | He flickers yellow for ~0.6 s with a rising sound, vanishes, and reappears 60-90 degrees to your LEFT or RIGHT (never behind) with a pop, then fires a quick, faster bolt almost at once | `Dummy teleports (timer)`, `Dummy quick-fires after teleporting` |
| T9 | Start charging a blast | He teleports away (if he hasn't teleported in the last 6 s) | `Dummy teleports (escaping your blast)` |
| T10 | Hit him hard quickly (35+ damage within 2 s) | He teleports away | `Dummy teleports (escaping damage)` |
| T11 | Drain your ring energy below 20% | His next shot comes noticeably sooner (at most once every 5 s) | `Dummy punishes an opening (your energy is low)` |
| T12 | Finish a laser, whirlwind or blast | Within ~1 s his next shot starts sooner | `Dummy punishes an opening (after your big attack)` |
| T13 | Whirlwind lift, knock-out, get up | Still work; he doesn't move or teleport while lifted, knocked out or getting up | `Enemy: in the air` / `getting up` |
| T14 | Get knocked out yourself | Fight restarts; he is back at his starting spot | `Fight restarted` |
| T15 | He never leaves the arena (about 10 m from the middle) or walks into you | — | — |
| T16 | Feel: too easy or too hard? Dodges too often? Teleport too frequent, too surprising? Is the teleport warning enough to react? | Report | — |

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
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
