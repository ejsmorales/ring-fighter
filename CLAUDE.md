# Ring Fighter — notes for Claude

Quest 3 VR fighting game (Green Lantern-style ring). Unity 6000.6.6f1, URP, OpenXR, Android build target.

Read these before making changes:
- `docs/GAME_DESIGN.md` — what the game is, move list, roadmap, playtest log
- `docs/ARCHITECTURE.md` — how the code is structured and how to add a move
- `docs/SETUP_AND_TROUBLESHOOTING.md` — machines, workflow, known problems and fixes

Working agreements:
- The owner has programming experience but is new to Unity and game dev: give click-by-click Unity steps.
- Where Claude runs decides what it can verify:
  - **On the owner's Windows PC (Claude Code local, preferred):** Unity 6000.6.6f1 is installed. Build with
    `powershell -ExecutionPolicy Bypass -File tools\build-quest.ps1` (Unity editor must be closed; add `-NoInstall`
    to only compile/build). Read `Builds\unity-build.log` for compiler errors. With the Quest plugged in, watch the
    game's log live with `adb logcat -s Unity` (adb path: see `tools\quest-wireless.ps1`). Every MoveLog line appears there
    prefixed `[RingFighter]`. Still ask the owner how moves *feel*.
  - **In a cloud session (no Unity, no headset):** syntax-check C# with tree-sitter and say clearly that changes
    are untested until built.
- Push work to a `claude/...` branch; the owner pulls, builds to the headset, and reports back.
- Update the docs (design decisions, playtest log, troubleshooting) as part of each change.
- Keep tuning values as public fields with tooltips. New moves extend `Move`.
- Visual effects: components in `Assets/Scripts/VFX`, shaders/materials/textures in `Assets/VFX/{Shaders,Materials,Textures}`.
  Effects only read gameplay state; keep gameplay logic out of them. Every effect shader needs a material created in
  `RingFighterSceneBuilder` and referenced by the scene's VFX System (`VfxLibrary`), never loaded only with
  `Shader.Find`/`Resources`, or the Quest build strips it. New assets need `.meta` files.
- Art: final models/textures in `Assets/Art/<Thing>/` (Git LFS), drafts outside the repo. Import settings, materials and
  Animator controllers for art are created by editor code at build time (see `Assets/Editor/EnemyArtSetup.cs`), not by hand.
- Update `docs/TEST_PLAN.md` with every change: replace "This build" with numbered tests for what changed
  (do / expected / what the debug panel shows); keep the regression list current.

Current state and where to pick up: `docs/GAME_DESIGN.md` (playtest log, newest at the bottom) and
`docs/TEST_PLAN.md` ("This build"). Active branch: `claude/vr-setup` (not yet merged to `main`). `claude/vfx` (ring effects) passed testing and was
merged into it on 2026-10-21. Enemy character integration is on `claude/enemy-model` (art files committed from the owner's PC).
