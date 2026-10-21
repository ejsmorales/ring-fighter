# Ring Fighter — Test plan

Claude updates this file with every change. **"This build"** lists what is new and must be checked; the
**regression** list is the quick check that older things still work. Report results by test number, e.g.
"T3 fail: laser has no hit sparks, panel said 'Laser HIT target'".

Before testing: press **Y** (left controller) to show the debug panel. Its "Recent" list says what fired and why
something didn't. The top lines show the ring hand, focus distance and the physics self-test.
(Everything in this build is untested: built in the cloud, not yet run on the headset.)

## This build — ring effects pass (branch `claude/vfx`, 2026-10-20)

Every ring effect now has a lightsaber glow: a white-hot core inside a saturated green halo, faked with additive
shaders (no post-processing). Enemy bolts are yellow (orange when aimed at your head). Gameplay is unchanged: damage,
energy, timing and hit detection are exactly as before; only what you see changed.

**Before testing, rebuild the scene** (the default `build-quest.ps1` does this; **do not** use `-SkipScene`). The
scene needs the new "VFX System" object. If it is missing, the debug panel's Recent list shows `VFX unavailable
(rebuild scene)` and the old plain shapes are drawn. After a normal build the panel shows `VFX ready (High)`.

| # | Do this | Expected | Panel shows |
| --- | --- | --- | --- |
| T1 | Start the game, open the panel (Y) | Recent list has the VFX line. The ring on your hand and the dummy look as before | `VFX ready (High)` |
| T2 | Ring hand alone: fire bolts at the dummy | Each bolt is a bright green streak with a white core, a soft halo and a short fading tail. On hit: white-green flash, a ring that expands toward you, a spray of sparks, damage number as before | `Bolt fired`, `HIT for 10` |
| T3 | Fire a bolt at the floor | A small green burst where it lands | `Shot hit Floor (not a target)` |
| T4 | Focus stance, hold to fire the laser at the dummy | A thin white blade inside a bright green shimmering body and a wider halo; a flare at the ring; a hot spot with sparks where it lands; it fades out quickly when you stop | `Laser on (focused)`, `Laser HIT target` |
| T5 | Both triggers, hands together to one side: charge a blast | A glowing orb between your hands that swells and throbs faster as it charges, sparks pulled inward; a ring flashes when full | `Blast charging` |
| T6 | Release to fire the blast at the dummy | Big pulsing orb with a comet tail and embers; **explosion** (flash, shell, ring, sparks) on the dummy | `Blast fired ..%`, `HIT for ..` |
| T7 | Fire a blast at the floor, and one up at the sky | An explosion where it hits the floor (with a flat glowing ring on the ground); one in the distance when the sky shot times out (~3 s) | `Shot hit Floor (not a target)`, `Shot missed (expired)` |
| T8 | Raise a forearm and hold the trigger: shield | A hex-grid barrier with a bright rim that pops in. Let an enemy bolt hit it: a ripple spreads from the hit point and the barrier flares | `Shield .. up`, `Shield absorbed bolt` |
| T9 | Raise both arms: double shield, then take a bolt | Bigger, brighter, slightly whiter barrier. The bolt turns green (no flicker) and flies back, ripple on impact | `Shield DOUBLED`, `Bolt REFLECTED` |
| T10 | Watch the dummy's bolts (and get hit by one) | Yellow streak with a hostile flicker, hot core, ember trail. Head-aimed bolts are orange with a stronger flicker. Red hit flash on you as before | `HEADSHOT` lines as before |
| T11 | Shoot one of your bolts into an enemy bolt | Bright flash and sparks **plus** the small dark burst as before | `Bolts cancelled` / `Your shot broke through` |
| T12 | Start a whirlwind and let it travel | A flat spinning green energy circle on the floor with spiral arms and a low swirling rim, sparks drifting up. No more stacked discs | `Whirlwind .. started` |
| T13 | Steer the whirlwind onto the dummy | The rim rises into a tall funnel of wind streaks with an inner funnel; sparks spiral up. Steer off: it settles back down. **Lift and damage work exactly as before** (see R16) | `Whirlwind caught the dummy` |
| T14 | Press the left menu button to pause. Press **A** (right controller) twice, then **B** twice | A panel under the PAUSED sign: A cycles Quality High, Low, Minimal; B toggles Bloom. A/B do **not** change the aim tilt while paused. Resume and fire: Low has fewer glow layers and about 40% of the sparks; Minimal has no sparks | `VFX quality: Low`, `Bloom ON` |
| T15 | Switch Bloom on and fight for a minute | Glow looks stronger and softer everywhere. Report if there is no visible change, or if the frame rate dips | `Bloom ON` |
| T16 | Spam everything at once on High (bolts, laser, blasts, whirlwind, shield) with the dummy firing back | Report whether it stays smooth. If not, note which quality (Low or Minimal) first feels smooth. For hard numbers use Meta's OVR Metrics Tool | — |
| T17 | Look at everything with fresh eyes | Report what is too big, too bright, too faint, or hard to read (for example a halo that is too large close up, enemy bolts hard to see) | — |

If an effect is **pink/magenta, invisible, or still the old plain shape**, tell me which one and send the `Recent`
lines plus, if you can, the lines containing `RF_` from `Builds\unity-build.log`.

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
| R15 | Whirlwind keeps going while the trigger is held; contact lifts the dummy, it drops and takes ~40 fall damage | Passed 2026-10-21 |
| R16 | Whirlwind lift (fix from 2026-10-20, not yet confirmed): steer it onto the dummy with the trigger held | Dummy is **not** pushed away; within a moment it rises ~2.2 m, hangs, drops; ~40 fall damage; vortex damage keeps ticking; dummy turns light blue and shoots ~20% slower for 6 s. Panel: `Whirlwind caught the dummy`, `Dummy lifted by whirlwind`, `Dummy landed: fall damage 40` |
