# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — mall parking lot arena (2026-11-06, branch `claude/arena`)

New arena modeled on the owner's photo of a tropical strip-mall lot on an overcast day: grey cloudy sky and soft
light with haze, asphalt with yellow stall lines and parking stops, yellow-curbed grass islands with palms, trees and
shrubs, light poles, rows of parked cars, a sidewalk and store fronts ahead, a watch tower, stop sign, cart corral,
a wrecked car near the fight, and green hills with small houses all around. The middle (about 11 m around you)
stays open; nothing blocks shots or movement. Until the Meshy props arrive in `Assets/Art/Environment/<Name>/`,
each prop is a simple placeholder shape; each real prop replaces its placeholder automatically on the next build.
Includes everything from `claude/enemy-ai`. Untested.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Start the game and look around | Grey overcast sky (not the old dark blue), soft light, asphalt with yellow lines under you, open space around you | — |
| T2 | Look ahead (past the enemy) | Store fronts behind rows of parked cars, sidewalk with a yellow curb, crosswalk stripes, a watch tower on a pole | — |
| T3 | Look all around | Parked car rows on all four sides, islands with palms/trees/shrubs and light poles, green hills with little houses in the haze far away | — |
| T4 | Find the wrecked car | Dark crushed car about 13 m away, behind-left | — |
| T5 | Fight normally (bolts, laser, blast, whirlwind, his AI, teleport) | Everything works as before; shots fly through props (only the floor stops them) | — |
| T6 | Move to the edge of your 4 m area and look around | No props inside the fighting area; he never walks into cars (he stays within 10 m of the middle) | — |
| T7 | Performance | Smooth, no stutter when turning (watch for hitches looking toward the stores and hills) | Report |
| T8 | Look | Too dark/bright? Haze too strong? Does it feel like the photo? | Report |

**All 14 props have arrived** (art conversation, 2026-11-06), so this first build already uses them instead of the
placeholders. Extra checks for the props:

| # | Do this | Expected |
| --- | --- | --- |
| T9 | Look at the parked cars | Sedans, SUVs and white vans in different paint colors, sitting on the ground, facing along the stalls, no logos |
| T10 | Palm trees | Fronds visible from below and above (not see-through from one side) |
| T11 | Store fronts ahead | "PLAZA SOL MARKET" facing you behind the sidewalk (the same front is used three times; side ones tinted) |
| T12 | Watch tower, stop sign, cart corral ("CART RETURN"), carts, wrecked car | All right-side up, normal size, standing on the ground |
| T13 | Far hills ahead | The hillside-town backdrop behind the stores, blending into the code-built hills on the sides |

When more Meshy props are added later: build again, check each prop's size, that it faces the right way and sits on the
ground (the build log lists `RingFighter arena: using prop ...` and any auto-scaling), then commit
`Assets/Art/Environment` (Git LFS handles the `.fbx`/`.png`).

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
| R23 | Recharge stance: fist + ring in front of your face (passed?) | — |
| R22 | Enemy EVADE (label, untouchable flicker, no counter-shot) and DODGE label | Passed 2026-11-06 ("a lot more manageable") |
| R21 | Snap turn (ring-hand stick), smoother enemy animation, walk/strafe clips | Passed 2026-11-05 ("much smoother") |
| R20 | Enemy teleport: warning + destination marker, appears only to your sides | Passed 2026-11-05 ("better") |
| R19 | Enemy AI basics: keeps distance and circles, dodges some shots, punishes openings, escapes your blast by teleporting | Passed 2026-11-05 ("hectic") |
| R18 | Player Platform disc gone (pass/fail not reported yet) | — |
| R17 | Enemy01 animations: idle, aim/shoot from the right hand, flinch, lifted, knocked out, get up (no shooting while getting up) | Passed 2026-10-21 |
| R16 | Ring effects (bolts, laser, shields, whirlwind, blast, impacts, pause effects panel) | Passed 2026-10-21 |
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
