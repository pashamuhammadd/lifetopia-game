# Changelog
## Unreleased
### M3.5 Market & Economy — closed 2026-11-26 (founder acceptance 16/16)
- NPC Asset Pack 01: 17 runtime PNG imported locally (PPU 64, feet pivot 0.5/0.125, bilinear, no mipmaps); Reference/ReferenceReuse not imported (existing item/coin icons reused).
- Rio (`Lifetopia.Npc`: `NpcView`, `NpcVisualSet`, `MerchantNpc`): static merchant in front of the market stall, foot collider, Y-sorted like the player, turns to face the player; placeholder "Rio (TEMP)" in the public scene. Maya and Kai not placed.
- Rio's Market (`MarketPanelUI`, `MarketItemRowView`): Buy/Sell tabs, real item icons, owned count, unit price, quantity - / + / Max, total, balance after, Purchase/Sell, Close, success/failure text; keyboard (W/S, A/D, Tab, E, Esc), mouse and touch; responsive landscape.
- Economy (`Lifetopia.Economy`: `MarketConfig`, `MarketService`, `MarketText`): atomic trades on the existing wallet and inventory; seed buy 10, fruit sell 18, fish sell 12 (prices from item assets); handles not enough coins, inventory full incl. partial stacks, not enough items, invalid/overflow quantities, coin cap, re-entrancy and double submission. Hotbar 0-count seed reference becomes usable again after buying (M3.3 behaviour).
- Market pauses game time and blocks movement like the bag; restored on close.
- `PlayerInputReader.NavigateInput` for menu navigation while movement is locked.
- 28 new EditMode tests (204 total).
- Local build `M3-05-20261126-2211` deployed to Preview (`ad464534`); production unchanged (`5784f2eb`).
- Founder acceptance (2026-11-26): founder-reported manual playtest on Cloudflare Preview incl. a real phone, **16/16 passed**; M3.5 approved. Prices fixed as baseline (seed buy 10, fruit sell 18, fish sell 12).
### M3.4 Fishing System & Environment Visual Update — closed 2026-11-26 (founder acceptance 15/15)
- Gameplay Asset Pack 01 C/D/E: 46 PNG imported locally (SHA256 verified; 64 PPU; manifest pivots).
- Dual-grid terrain (`Lifetopia.World.DualGridMask/DualGridPainter/DualGridTileSet`): grass/path and sand/water transitions drawn on a half-cell-offset visual tilemap from the unchanged logical tilemaps; 0 unsupported vertices.
- Environment: market stall and info sign art (local scene), hibiscus planters, wood crate, tropical rocks, rope fences (`Prefabs/World/Env/EnvProp_*` with footprint colliders, placeholders in the public scene), dock rope posts.
- South beach (2 sand rows), sea, sand rim around the pond, 2 x 3 walkable wooden dock; world extended south to y -15.
- Fishing (`Lifetopia.Fishing`): select the Fishing Rod, Interact at the dock end; cast, wait for a bite, stop the moving marker in the green zone; one Reef Fish per successful attempt (existing items, no coins); too early / missed / too late / cancel lose nothing; 8 s limit; no reward duplication; inventory-full check at start and award; own focus-aware mini-game timer.
- Fishing panel (English): status, target zone with dark boundaries, high-contrast marker, Reel In / Cancel buttons; hotbar hidden while fishing. Bobber, line, ripple and splash effects.
- 24 new EditMode tests (173 total).
- Founder review (2026-11-26): world extension, dual-grid and shoreline approved; **game time and crop growth keep running while fishing** — `GameplayInputGate` now separates the movement lock from the game-time pause (`Block(source, pausesGameTime)`, `TimePauseChanged`); bag and dialogue still pause time. +3 tests (176 total). Art Bible terrain rule updated to dual-grid. Local build `M3-04-20261126-2103` deployed to Preview (`5aacffc2`); production unchanged.
- Founder acceptance (2026-11-26): founder-reported manual playtest on Cloudflare Preview incl. a real phone, **15/15 passed**; M3.4 approved. Fishing configuration fixed as baseline.
- Released: code commit `7a7ee0b`; production deployment `5784f2eb` of build `M3-04-20261126-2103` (identical to the accepted preview `5aacffc2`); rollback target `6cd1a5bf` (M3.3).
- Builds: Public `M3-04-20261126-1844` (8.71 MiB) and Local `M3-04-20261126-1852` (9.98 MiB); Local deployed to preview `4d322ee0` (approved); production unchanged (`6cd1a5bf`).
### M3.3 Farming System + Gameplay Asset Pack 01 A/B — completed and approved by founder 2026-11-26
- Farming (`Lifetopia.Farming`): 6 starter plots south-west of the starter house; Hoe tills (separate `Tilled` flag), Tropical Seeds plant (1 seed consumed after validation), Watering Can waters once, crop grows 120 s of active play (`IGameClock.ActivePlaySeconds`: stops while the bag/dialogue is open, on focus loss or pause; unaffected by dev speed/time jumps), harvest gives 1 Tropical Fruit. GDD stages Empty/Planted/Watered/Ready, no wilting.
- All-or-nothing actions: no seed lost on invalid planting, no duplicate harvest, full bag keeps the crop Ready with an "Inventory full" message. Serializable `FarmFieldData` (save hook, not persisted yet).
- Selected hotbar item decides the action; English prompts ("Till soil", "Plant seeds", "Water crop", "Growing - 1:05 left", "Harvest Tropical Fruit"); target outline and non-colour status badges (water drop / check). Keyboard E and touch Interact.
- Gameplay Asset Pack 01: 22 of 33 PNG imported locally (8 item icons, coin/backpack/close/sun/moon/water drop/check, 3 soil, 4 pineapple stages; 64 PPU, manifest pivots). Pineapple is the visual of the existing `crop_tropical_fruit` item; no new item IDs or prices.
- Local-only art binding: `ItemIcons` registry + `ItemIconOverrides` (by stable item ID) and `LocalArtBindings` in the local scene; tracked items/prefabs/public scene reference no local art, and FirstPlayable keeps generated placeholders.
- Dev only: Shift+F9 adds 30 s of crop growth.
- Review revisions: per-stage crop scale in the Inspector (ready 100 %; seed/sprout/growing about 12.5/30/67 % from measured sprite pixels); hotbar keeps a 0-count reference (dimmed "0", unusable, reactivates on refill; saves keep it); crop Sort Point Pivot for correct Y-sort with Sorting Group objects; cream disc behind status badges; farm moved to the south edge of the main path clear of tree canopies. 149 EditMode tests.
- 30 new EditMode tests (146 total).
- Preview deployed (approved): Local build `M3-03-20261126-1741` → `https://preview.lifetopia-play.pages.dev` (deployment `c6332a73`); production unchanged (`002b0658`).
- Founder-reported manual playtest (Cloudflare preview + real phone): 12/12 passed (icons, seed/growth/mature sizes, till, plant, water, harvest into inventory, soil stays tilled, phone controls, no crash). Device model, browser and metrics not recorded.
- Production: Build `M3-03-20261126-1741` (same bytes as the approved preview) deployed as `6cd1a5bf` on `https://play.lifetopiaworld.io`; rollback target `002b0658`.
- Fix `Tools/Deploy-Web.ps1`: production deployment id was read as a list on Windows PowerShell 5.1 once two production deployments existed (false "production changed" alarm).
- Earlier local Web builds (not deployed): Public `M3-03-20261126-1625` 8.73 MiB and Local `M3-03-20261126-1637` 9.72 MiB; local browser test of the full farming loop in real time (120 s, pause while bag open), 60 FPS desktop, 0 console errors.
### M3.2-D Cloudflare Web Deployment Foundation — completed and approved by founder 2026-11-26
- Reusable PowerShell tooling (Windows PowerShell 5.1): `Tools/Build-Web.ps1` (safe Unity 6000.3.29f1 batch build: Editor-closed and single-build checks, RAM gate, build into `_incoming`, verify, then promote; never overwrites the last good build), `Tools/Verify-WebBuild.ps1` (build folder, live headers, repository secret scan) and `Tools/Deploy-Web.ps1` (preview by default; production only with `-Production -SkipBuild` and typed confirmation; artwork publication gate; allow-listed staging upload; post-deploy header checks; production unchanged check for previews; local deploy history). Settings in `Tools/WebDeploy.config.psd1`.
- Build variants: `Local` (GameplayPrototype, local art) and `Public` (FirstPlayable placeholder via new Editor command `Lifetopia.EditorTools.WebBuildCommand.Build`).
- Generated `_headers` matching the build's real compression (Brotli: `application/wasm` + `Content-Encoding: br`, `no-cache, no-transform`, `noindex`, `nosniff`); `404.html` so missing files return 404 instead of index.html.
- Web template: browser title "Lifetopia World", placeholder SVG favicon, version information (Development Build, version, milestone, build date) from `version.json` on the loading screen and as a tiny non-interactive corner label.
- Production deploys of placeholder builds blocked unless `-AllowPlaceholderProduction`; founder decisions recorded (original art allowed in Cloudflare Web builds, never in GitHub; `Local` variant for playtests).
- Documentation: `Documentation/WEB-DEPLOYMENT.md` (build, deploy, preview, production update, rollback, troubleshooting, phone playtest). `.wrangler/` cache ignored by Git.
- Verified: `Build-Web.ps1 -Variant Public` → `M3-02-20261126-1457` (3 m 36 s, 8.72 MiB Brotli, 0 compiler errors); founder-approved preview deployment `3d3d4263` (`https://preview.lifetopia-play.pages.dev`): correct wasm/Brotli headers, 404 for missing paths, version info shown, 0 console errors, 60 FPS, movement/hotbar/bag/clock/dialogue working; production `2cbfb971` unchanged.
- Original-art (`Local`) build `M3-02-20261126-1527` (9.37 MiB) deployed by the founder with the scripts: preview `bf3c9624`, then production `002b0658` on `https://play.lifetopiaworld.io` (live files byte-identical to the approved build; title "Lifetopia World", version info, Brotli/wasm headers, 404 for missing paths, 0 console errors). Previous production `2cbfb971` is the rollback target.
- Founder-reported manual playtest on a real phone (preview build `M3-02-20261126-1527`): 10/10 checks passed (Game loads; joystick works; character animation smooth; inventory opens/closes; hotbar selectable; in-game clock runs; day/night cycle works; rotate-device overlay works; UI not cut off; no crash). Device, browser, FPS, memory and duration were not recorded.
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
