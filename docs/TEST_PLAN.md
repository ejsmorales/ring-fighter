# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — snap turn + smoother enemy animation (2026-11-05, second build)

Last build was "better". New: **snap turn** on the RING-hand stick (flick left/right = turn 45 degrees around your
head). Smoother enemy animation: no keyframe compression, loop seams blended, longer blends between animations, a
flinch can no longer restart itself (flinch at most every 0.8 s), and the floor safeguard raises him gradually
instead of in one jump. Untested.

Also new: the walk/strafe clips are in (strafe_right is mirrored from strafe_left until a real one exists).

This build re-imports the enemy art once (import settings changed) and rebuilds his Animator controller, so the first
build takes a little longer and afterwards `git status` shows changed files under `Assets/Art/Enemy` (the `.meta`
files and `Enemy01.controller`). **Commit those** (see the bottom of this section).

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Flick the RING-hand stick right | You turn 45 degrees right instantly; your position doesn't jump | `Snap turn right` |
| T2 | Flick left, then hold the stick to the side | One 45-degree turn per flick; holding doesn't keep turning | `Snap turn left` |
| T3 | Off-hand stick | Still moves you (dash/smooth) as before; stick click still switches mode | `Move: ...` |
| T4 | Snap turn while dashing | Dash stops, turn happens, no jump | — |
| T5 | Watch his idle and aiming for a while | Smooth, no pop at the loop point | — |
| T6 | Hit him repeatedly / hold the laser on him | He flinches smoothly, not a rapid restart stutter | — |
| T7 | Watch for any remaining stutter | If you still see it, note **when** (idle? walking/sliding? turning to face you? after a teleport? when hit?) and tell Claude | Report |
| T8 | Floor | Still never sinks; any `Enemy sank ... (State)` line -> tell Claude the state | `Enemy sank ...` |
| T9 | Watch him move (the art conversation delivered strafe_left, walk_forward, walk_back; no strafe_right, so strafe_left is used mirrored) | He steps instead of sliding: strafing both left and right, walking forward/back when he closes in or backs off | — |
| T10 | Watch his feet while he moves | Steps roughly match his speed (no ice skating). Too fast or slow? Tell Claude ("Full Move Speed") | Report |
| T11 | Dodges and teleports | A dodge is a fast sidestep (may still look like a slide: it's quicker than any walk clip) | — |

After testing, commit the re-imported art settings:
```
git add Assets/Art
git commit -m "Enemy art: re-imported with smoother animation settings"
git push
```

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
| R20 | Enemy teleport: warning + destination marker, appears only to your sides | Passed 2026-11-05 ("better") |
| R19 | Enemy AI basics: keeps distance and circles, dodges some shots, punishes openings, escapes your blast by teleporting | Passed 2026-11-05 ("hectic") |
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
