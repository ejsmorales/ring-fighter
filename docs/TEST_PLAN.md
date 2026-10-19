# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: shield didn't absorb, panel said 'Shield broke: no energy'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — tuning from the first fight + pause (2026-10-19, second build)

Changes since the last build: dummy starts 5.5 m away (was 4) and wanders less in depth; its bolt is slower
(7 m/s, was 9); blocking with a single shield now **gives back** 2 ring energy instead of costing 4; HUD bars moved
out toward the lower corners; **pause** with the left menu button. All untested (built in the cloud only).

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Start the game and pick the ring hand. Look straight ahead | Health bar (red) and energy bar (green) are out near the lower left and right edges of your view, not near the center. Tell Claude if still too close, too far, or too low | — |
| T2 | Watch the dummy for a few shots | It is farther away. From the start of the yellow glow to the bolt reaching you is roughly 1.6 s. Is that enough to lean or duck? Too easy now? | `Dummy fires` |
| T3 | Lean or duck sideways after the glow starts | Bolt misses | no `YOU WERE HIT` |
| T4 | Raise ONE forearm into a block, trigger held, take a bolt | Bolt bursts on the shield, arm buzzes, no health lost, the green bar goes **up** a little (+2) | `Shield absorbed bolt (+2 energy)` |
| T5 | Raise BOTH arms (double shield), take a bolt | Bolt flips green, flies back faster, damages the dummy for 25, no energy change | `Bolt REFLECTED (25 dmg)` |
| T6 | Press the **menu button** (three lines) on the LEFT controller | Everything freezes (dummy, bolts), a "PAUSED" sign floats in front of you, ring moves do nothing, the HUD still follows your head | `PAUSED` |
| T7 | Press the left menu button again | Game resumes where it was, sign disappears. Nothing was reset | `Resumed` |
| T8 | Pause in the middle of a bolt flying at you, then resume | Bolt continues from the same spot | — |
| T9 | Take hits until health is 0 | Fade to red, "KNOCKED OUT", auto-restart after ~3 s, health/energy/dummy reset | `Fight restarted` |
| T10 | Press menu while knocked out or while holding the shield | No crash or stuck state; resume works | — |
| T11 | Focus stance at the new 0.23 m limit | Still easy enough to get the laser? | `focus: YES (0.2x m)` |
| T12 | Overall: is the fight fair now at 10 damage / 3 s / 100 HP? | Report feel | — |

If the menu button does nothing (T6), tell Claude: some Quest setups route it differently and another pause
gesture is needed.

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
| R11 | Enemy bolt, player damage, knock-out and restart | Same as the last build (see git history of this file) |
