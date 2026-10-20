# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: shield didn't absorb, panel said 'Shield broke: no energy'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — stick movement + dodge tuning (2026-10-19, third build)

New: move with the **off-hand thumbstick** (ring on the LEFT hand = move with the RIGHT stick, and vice versa).
Directions are relative to where your head faces. Default is **Dash**; click the off-hand stick to cycle
Dash / Smooth / Off (remembered). Torso hit sphere shrunk a little (radius 0.22 to 0.18) so a dodge that clears your
chest counts. Untested (built in the cloud only).

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Before picking the ring hand, push either stick | Nothing moves | — |
| T2 | Pick the ring hand. Flick the OFF-hand stick forward | You burst ~1.4 m toward where you look in about 1/7 s, small buzz in both hands | `Dash`, `Move: DASH (dashing)` |
| T3 | Flick again right away | Nothing for ~0.45 s (cooldown), then it works. Holding the stick doesn't repeat; release and flick again | `Move: DASH (cooldown)` |
| T4 | Flick left, right, back | Burst in that direction relative to your gaze | `Dash` |
| T5 | Does a dash feel comfortable? (dizzy / jarring / fine?) | Report feel. Too far or too short? Too fast? | — |
| T6 | Dash sideways right after the dummy's glow starts | The bolt misses you | no `YOU WERE HIT` |
| T7 | Click the off-hand stick | Mode changes to SMOOTH, short buzz. Hold the stick: you glide at walking speed in that direction. Let go: you stop | `Move mode: Smooth` |
| T8 | Smooth: is it comfortable or nauseating? Speed OK? | Report feel | — |
| T9 | Click again | OFF: stick does nothing. Click once more: DASH again. Restart the app: the last mode is remembered | `Move mode: Off` / `Dash` |
| T10 | Dash or glide as far as you can in one direction | You stop at about 4 m from the middle (invisible wall) | — |
| T11 | Dash, then immediately fire / shield / whirlwind | Moves behave normally: a dash is not mistaken for a hand swing (laser doesn't cut out, shield isn't blocked) | — |
| T12 | Get knocked out | Restart puts you back on the platform in the middle | `Fight restarted` |
| T13 | Pause (left menu) while dashing | Freezes, resumes cleanly | `PAUSED`, `Resumed` |
| T14 | Overall: does dash + lean make the bolts dodgeable enough? Dummy fight fair? | Report feel | — |

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
