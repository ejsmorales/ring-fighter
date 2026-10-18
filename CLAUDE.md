# Ring Fighter — notes for Claude

Quest 3 VR fighting game (Green Lantern-style ring). Unity 6000.6.6f1, URP, OpenXR, Android build target.

Read these before making changes:
- `docs/GAME_DESIGN.md` — what the game is, move list, roadmap, playtest log
- `docs/ARCHITECTURE.md` — how the code is structured and how to add a move
- `docs/SETUP_AND_TROUBLESHOOTING.md` — machines, workflow, known problems and fixes

Working agreements:
- The owner has programming experience but is new to Unity and game dev: give click-by-click Unity steps.
- No Unity or C# compiler is available in Claude's environment. Syntax-check C# with tree-sitter and say clearly
  that changes are untested until built.
- Push work to a `claude/...` branch; the owner pulls, builds to the headset, and reports back.
- Update the docs (design decisions, playtest log, troubleshooting) as part of each change.
- Keep tuning values as public fields with tooltips. New moves extend `Move`.
