# Changelog
## Unreleased
### M2 Art calibration — closed 2026-11-26 as calibration milestone (founder approved with exceptions)
- (Local only, art-dependent) `ArtCalibration` scene (copy of FirstPlayable) with A0 art: male Island Explorer, starter house, coconut palm, hibiscus shrub, grass/path tiles and HUD panel; Web build profile now starts with this scene.
- Four-direction idle (4 slots, 4 fps) and walk (8 slots, 12 fps) for the male avatar from the A0-02 pack, played by `SpriteCharacterView` + `CharacterAnimationSet` (`anim_player_male_a0`). Walk only plays while the player actually moves; last facing kept on stop; no flipping or fake motion.
- Portrait overlay "Please rotate your device" in the Web template for touch devices.
- New EditMode tests for frame timing, clip fallback and walk-state rules (27 total).
- Accepted calibration exceptions: walk uses 4 unique key poses per direction (8 slots), idle uses procedural breathing, A0-02 is a minor re-rendition of the A0-01 design. Final Art Bible targets remain in `Documentation/BACKLOG.md`.
- Open before public release: real-phone test and art distribution rights.
- Art PNGs and art-dependent scene/prefabs/data stay local (not in the public repository) until the owner approves distribution. Public commit contains source code, tests, Web template and documentation only.

### M1 FirstPlayable (5104ce0)
- Greybox player, camera, collisions, keyboard + touch controls, interaction prompt and dialogue.

### M0 Foundation (09e19ca)
- Foundation documentation, FoundationSmoke scene and Web build profile.
- Foundation documentation proposed for the Unity 2D revival.
- Unity 6.3 LTS and Unity Pipeline 1.0.1 present on GitHub main at kit preparation; CLI/MCP connection reported working by founder's Claude session.
