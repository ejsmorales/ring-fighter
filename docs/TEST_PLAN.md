# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser didn't start, panel said 'focus lost (0.24 m)'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.

## This build — `9ad8751` ring hand / off hand (2026-10-18)

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Launch the game, don't touch anything | Green text in front: "Pull the trigger on the hand that wears the ring". Nothing fires | `Ring hand: not picked yet` |
| T2 | Pull the RIGHT trigger | Prompt disappears, strong buzz in the right hand. Ring only visible on the right controller. Energy bar moves above the LEFT hand | `Ring hand: RIGHT` |
| T3 | Ring hand alone: point at the dummy, pull the trigger (tap or hold) | One bolt per pull, instantly. Never a laser | `Bolt fired` |
| T4 | Off hand: point at the dummy, pull its trigger | Nothing is fired | nothing new (or a shield if arm raised) |
| T5 | Focus stance: rest the off hand on top of the ring hand, aim, **tap** the trigger | One bolt | `focus: YES (0.xx m)`, `Bolt fired` |
| T6 | Focus stance, **hold** the trigger | Laser beam after a brief moment, short buzz in the off hand; dummy takes damage while beam is on it | `Laser on (focused)`, `Laser HIT ...` |
| T7 | While lasering, pull the off hand away | Laser stops | `Laser ended: focus lost` |
| T8 | Note the `focus` distance when your hands feel "together" naturally | — | Tell Claude the number (focus needs < 0.20 m) |
| T9 | Raise ONE forearm (either hand) in a block, hold its trigger | Normal-size shield in front of that hand | `Shield L/R up` |
| T10 | While shielding, raise the OTHER forearm too, hold its trigger | Shield grows into one big disc between the hands, both hands buzz | `Shield DOUBLED (both arms)` |
| T11 | Lower one arm | Shield shrinks back to normal on the remaining arm | `Shield back to one arm` |
| T12 | Raise BOTH arms at once into block, both triggers | Big shield straight away | `Shield up (BOTH arms)` |
| T13 | Ring hand: hold trigger and circle the arm | Whirlwind | `Whirlwind R started` |
| T14 | Off hand: hold trigger and circle the arm | No whirlwind | — |
| T15 | Charged blast: both triggers, hands together off to one side, release | Blast still works | `Blast fired NN%` |
| T16 | Hit the dummy with anything | Damage numbers are now **visible** (they may have been invisible before) | `HIT ...` |
| T17 | Restart the app, pick the LEFT hand this time, repeat T3 and T6 quickly | Same behavior mirrored | `Ring hand: LEFT` |

## Regression (every build)

| # | Do this | Expected |
| --- | --- | --- |
| R1 | Look around | Full VR, head tracking smooth |
| R2 | Bolt the dummy until it dies | Health bar drops, flash + knockback, falls over, respawns after ~3.5 s |
| R3 | Fire anything | Controller buzz on firing |
| R4 | Spam attacks | Energy bar drains and refills after a short pause; moves stop when empty |
| R5 | Debug panel | Y toggles it; top line says `physics test OK` (FAILED is OK too, hits use the backup) |
| R6 | Aim tilt | A / B (with panel showing) changes `Aim tilt`; a relaxed wrist points at the dummy |
