# Changelog
## Unreleased
### M3.2-D Cloudflare Web Deployment Foundation — approved by founder 2026-11-26
- Reusable PowerShell tooling (Windows PowerShell 5.1): `Tools/Build-Web.ps1` (safe Unity 6000.3.29f1 batch build: Editor-closed and single-build checks, RAM gate, build into `_incoming`, verify, then promote; never overwrites the last good build), `Tools/Verify-WebBuild.ps1` (build folder, live headers, repository secret scan) and `Tools/Deploy-Web.ps1` (preview by default; production only with `-Production -SkipBuild` and typed confirmation; artwork publication gate; allow-listed staging upload; post-deploy header checks; production unchanged check for previews; local deploy history). Settings in `Tools/WebDeploy.config.psd1`.
- Build variants: `Local` (GameplayPrototype, local art) and `Public` (FirstPlayable placeholder via new Editor command `Lifetopia.EditorTools.WebBuildCommand.Build`).
- Generated `_headers` matching the build's real compression (Brotli: `application/wasm` + `Content-Encoding: br`, `no-cache, no-transform`, `noindex`, `nosniff`); `404.html` so missing files return 404 instead of index.html.
- Web template: browser title "Lifetopia World", placeholder SVG favicon, version information (Development Build, version, milestone, build date) from `version.json` on the loading screen and as a tiny non-interactive corner label.
- Production deploys of placeholder builds blocked unless `-AllowPlaceholderProduction`; founder decisions recorded (original art allowed in Cloudflare Web builds, never in GitHub; `Local` variant for playtests).
- Documentation: `Documentation/WEB-DEPLOYMENT.md` (build, deploy, preview, production update, rollback, troubleshooting, phone playtest). `.wrangler/` cache ignored by Git.
- Verified: `Build-Web.ps1 -Variant Public` → `M3-02-20261126-1457` (3 m 36 s, 8.72 MiB Brotli, 0 compiler errors); founder-approved preview deployment `3d3d4263` (`https://preview.lifetopia-play.pages.dev`): correct wasm/Brotli headers, 404 for missing paths, version info shown, 0 console errors, 60 FPS, movement/hotbar/bag/clock/dialogue working; production `2cbfb971` unchanged.
### M3.2 Game Time & Day/Night Cycle — approved by founder 2026-11-26
- Central game clock (`Lifetopia.TimeSystem`): day 1 starts at 08:00, 1 game hour = 60 real seconds (24-minute day), midnight rollover with a day counter, configurable speed via `GameClockConfig`.
- Time runs only during active play: paused while the bag or a dialogue is open, when the browser tab/window loses focus (new `WebFocusBridge.FocusGained`), at `Time.timeScale = 0`, or by any system through `PauseClock`; no offline progression; long frames are clamped.
- Event-driven `IGameClock` API (minute, hour, day, period, jump) for future farming, fishing, NPCs, quests and market; cached clock strings.
- Day/night tint: Morning, Daytime, Sunset, Night colours from `DayNightPalette`, smooth interpolation across midnight, one camera quad with a small multiply shader (Built-In 2D / WebGL, no lighting, no post-processing, UI never tinted).
- Time HUD (`TimeHUD` prefab): 24-hour time, day number and period with replaceable placeholder icons.
- Development-only time keys (Shift+F1…F8: sunrise, noon, sunset, night, 23:58, +1 hour, speed, pause).
- Serializable `GameClockData` with validated restore, ready for local save.
- 39 new EditMode tests (116 total).
- Local Web build 9.37 MiB, 0 errors, 60 FPS desktop; clock pause on bag/blur verified in the browser; M3.1 "Hotbar: 1-5" text fix confirmed on Web.
### M3.1 Item, Inventory & Hotbar — approved by founder 2026-11-26
- Item definitions as ScriptableObjects (`ItemDefinition`, `ItemDatabase`) with stable IDs; six categories (Tool, Seed, Crop, Fish, Material, Furniture); eight items in `Data/Items`.
- Runtime state in plain C#: 12-slot `Inventory` (stack 99, all-or-nothing operations: add, remove, move, swap, merge), 5-slot `Hotbar` (item references only, live counts), `Wallet`, `PlayerProfile` with a serializable, validated `PlayerProfileData`.
- Starter kit (`StarterProfile`): 100 coins, Hoe, Watering Can, Fishing Rod (never used up) and 3 Tropical Seeds, granted once per new profile.
- `InventoryHUD` prefab: Backpack window (4×3 grid, details, close), bottom hotbar, coin counter, Bag button, toast messages; Art Bible colours, code-generated rounded corners and soft shadows, category-coloured placeholder icons replaceable via Inspector.
- Input: B / I / Bag button toggle the bag, keys 1–5 and taps select hotbar slots, drag and drop with mouse or touch; gameplay movement and world interaction are blocked while the bag is open.
- Development-only test keys (`InventoryDebugTools`, Editor and Development Builds only).
- 50 new EditMode tests (77 total).
- Public `FirstPlayable` greybox scene wired for inventory testing; local-only `GameplayPrototype` scene (art-based) not committed.
- Fix: keyboard hint and bag hint use ASCII "1-5" (the built-in Web font has no en dash; Web build showed "Hotbar: 15"). Fixed in Editor; **not yet re-verified in a Web build** (next milestone build).
- Local Web build 9.31 MiB, 0 errors; desktop and mobile-emulation checks passed. Pending: real-phone test, gamepad test.
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
