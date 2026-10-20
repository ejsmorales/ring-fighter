# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: shield didn't absorb, panel said 'Shield broke: no energy'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — whirlwind rework, bolt clashes, headshots (2026-10-19, fourth build)

Changes: stick click now switches **Dash <-> Smooth** only; whirlwind rework (sustain, lift, fall damage, slow);
bolts clash; laser cuts enemy bolts; headshots 1.5x both ways; about 15% of the dummy's shots go for your head
(orange glow). All untested (built in the cloud only). Useful: the debug panel's "Recent" list names each event.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Click the off-hand stick several times | Alternates Dash, Smooth, Dash, Smooth. Never "off" | `Move mode: Smooth` / `Dash` |
| T2 | Start a whirlwind (hold trigger, circle the ring arm), then **stop circling** but keep holding the trigger | It keeps travelling and speeds up the longer you hold (up to about 5.5 m/s), until you release, run out of energy (~7 s) or it travels 14 m | `Whirlwind R started` |
| T3 | While it travels, turn your head slowly | It gently curves toward where you look | — |
| T4 | Steer the whirlwind over the dummy and stay there | After about 0.35 s the dummy is lifted ~2.2 m, hangs, then drops. On landing: orange damage number about 40 and a thump | `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
| T5 | Keep the whirlwind on it right after | Not lifted again for ~2.5 s. Still takes the vortex damage | — |
| T6 | After the whirlwind touches it, watch the dummy for 6 s | It turns light blue, and its shots come about 20% slower (3.75 s instead of 3 s). It does not shoot while in the air | `Dummy fires` timing |
| T7 | Fire a normal bolt (10) into an incoming dummy bolt (10) | Both vanish in a small dark purple-black burst | `Bolts cancelled (10 vs 10)` |
| T8 | Fire a **charged blast** into an incoming dummy bolt | Dummy bolt vanishes in the burst, your blast carries on with its damage reduced by 5 | `Your shot broke through (NN vs 10, now NN)` |
| T9 | Hold the **laser** (focus stance) across an incoming dummy bolt | The bolt is cut in a burst; the laser is not interrupted | `Laser cut an enemy bolt` |
| T10 | Hit the dummy in the **head** (aim high) with a bolt | Damage 1.5x (15 instead of 10), orange number ending in "!" | `HEADSHOT on the dummy` |
| T11 | Hit its body | Normal damage | — |
| T12 | Watch for a dummy glow that is **orange** instead of yellow (about 1 in 7 shots) | The bolt comes at your head. Duck/lean/dash to dodge it or block it | `Dummy fires (AT YOUR HEAD)` |
| T13 | Let an orange bolt hit your head | 15 damage instead of 10 | `HEADSHOT! YOU WERE HIT for 15` |
| T14 | Does the whirlwind feel worth using now (reach, lift, slow)? Is fall damage too much / too little? Are clashes satisfying? | Report feel | — |

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
| R13 | Off-hand stick movement | Dash and Smooth both work (pass as of 2026-10-19) |
