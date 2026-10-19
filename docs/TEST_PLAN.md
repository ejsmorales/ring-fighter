# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: shield didn't absorb, panel said 'Shield broke: no energy'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — enemy attacks, player health, HUD (2026-10-19)

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Launch, pick the ring hand, then look around | Curved **red** bar on the left, curved **green** bar on the right, like parentheses at the edges of your view. They trail slightly behind fast head turns and settle | — |
| T2 | Spend energy (fire bolts, shield) | Right (green) bar drops, refills after a pause. Nothing floats above your off hand any more | `Energy` |
| T3 | Stand still and watch the dummy | Before the pick: it doesn't attack. After the pick: ~2 s later it glows **yellow** and the orb grows for 0.8 s, then a yellow ball flies at your chest. Repeats about every 3 s | `Dummy fires` |
| T4 | Let a bolt hit you | Red flash around the screen edge, both controllers buzz, left bar drops by 10% | `YOU WERE HIT for 10 (health 90)` |
| T5 | After the glow, lean sideways (or duck) about 40 cm | Bolt passes by, no damage | no `YOU WERE HIT` |
| T6 | Raise ONE forearm into a block with trigger held (shield up) and take a bolt | Bolt bursts on the shield, arm buzzes, no health lost, a little energy lost (4) | `Shield absorbed bolt (-4 energy)` |
| T7 | Drain energy to near zero, then take a bolt on a single shield | Shield breaks and the bolt hurts you | `Shield broke: no energy to absorb`, then `YOU WERE HIT` |
| T8 | Raise BOTH arms (double shield) and take a bolt | Bolt flips green, flies back faster, hits the dummy for big damage, both arms buzz, no energy lost | `Bolt REFLECTED (25 dmg)`, `Reflected bolt HIT for 25`, `HIT` number on dummy |
| T9 | Take hits until health is 0 | Screen fades dark red, "KNOCKED OUT" shows, nothing you do fires. After ~3 s everything resets: health and energy full, dummy full health | `You were knocked out. Restarting...`, `Fight restarted` |
| T10 | Hit the dummy | It now needs about twice as many hits to die (200 HP) | health bar over its head |
| T11 | Focus stance: rest the off hand on the ring hand naturally, tap and hold | Laser now starts at the new 0.23 m limit. Tell Claude whether you still fail it | `focus: YES (0.2x m)` |
| T12 | Keep playing a minute | Is the bolt easy to read and dodge? Is 10 damage / 3 s too easy or too hard? Is the HUD comfortable (no eye strain, not in the way)? | — (report feel) |

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
