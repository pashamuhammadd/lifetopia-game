# Backlog — technical debt and visual improvements

Opened at M2 close (2026-11-26). Priority: **P0** blocks public release · **P1** before/during M3 · **P2** later.
Source: `Progress/M2-art-calibration-final.md`, `DECISIONS.md` (M2 calibration decisions).

## Release gates (P0)
| ID | Item | Notes |
|---|---|---|
| G-1 | Real-phone test: Android Chrome + iOS Safari minimum | FPS, memory, touch, rotate overlay, safe area, load time. Emulation results do not count. **2026-11-26: founder-reported manual playtest on one real phone passed 10/10 functional checks** (build `M3-02-20261126-1527`). Still open: record device model + browser, cover both Android Chrome and iOS Safari, measure FPS, memory and load time **2026-11-26 (M3.3): founder-reported phone playtest of farming 12/12 passed;** device performance measurement (model, browser, FPS, memory, load time) still open (founder request). **2026-11-26 (M3.4): founder-reported Preview playtest incl. real phone 15/15 passed;** device performance measurement and **iOS Safari test** still open (founder request). **2026-11-26 (M3.5): founder-reported Preview playtest incl. real phone 16/16 passed;** device performance measurement and iOS Safari still open (kept at M3.5 close). **2026-11-27 (M3.6): founder-reported Preview playtest incl. phone 18/18 passed;** device metrics and iOS Safari still open |
| G-2 | Art distribution rights review (A0 + A0-02) | **Partly decided 2026-11-26:** publishing inside Web builds on Cloudflare Pages approved by founder. PNGs, masters and backups stay out of the GitHub repository (still local-only) |
| G-3 | Repo license decision | Public repo ≠ open source; owner decision |

## Pending verification (open tasks from approved milestones)
| ID | From | Item | When |
|---|---|---|---|
| V-1 | M3.1 | ~~Web check of the ASCII "1-5" hint fix~~ **Done 2026-11-26 in the M3.2 Web build** ("Hotbar: 1-5" shown correctly) | Closed |
| V-2 | M3.1 | Real-phone test of inventory/hotbar touch (tap, drag & drop, backdrop over joystick, touch target sizes) — part of G-1. **Partly done 2026-11-26** (founder report: inventory opens/closes, hotbar selectable, UI not cut off); drag & drop and touch-target sizes not reported | Before public release |
| V-3 | M3.1/M3.3 | Gamepad test: North button toggles bag; movement blocked while open; hotbar selection and farming interact (South) — bindings exist, untested (no device; reconfirmed by founder 2026-11-26) | When a gamepad is available |

## Art quality backlog (Art Bible final targets)
| ID | Pri | Item |
|---|---|---|
| A-1 | P1 | Walk: 8 unique poses per direction (currently 4 keys × 2 slots) |
| A-2 | P1 | Idle: 4 hand-drawn frames per direction (currently procedural breathing, 3 states) |
| A-3 | P1 | Single master character design across all sheets (A0-02 is a re-rendition of A0-01) |
| A-4 | P1 | Grass/path transition tiles + 2–3 variants each (repetition visible when zoomed; hard edge) |
| A-5 | P1 | Starter house re-export so the door is ≥ character height (door ≈ 87% now); do not scale in Unity |
| A-6 | P1 | Final art for shop, water/shoreline and INFO sign (still greybox) |
| A-7 | P2 | Female base avatar; modular layers (hair/body/outfit/accessory) on the same pivot/frame grid |
| A-8 | P2 | NPCs (3) with idle + portrait; action animations (water, harvest, fish) |
| A-9 | P2 | Faint light fringe on palm leaves at extreme zoom (alpha edge bleed) |

## Technical debt
| ID | Pri | Item |
|---|---|---|
| T-1 | P1 | Sprite Atlas + texture compression for character frames/props (48 frames ≈ 3 MiB uncompressed); verify transparency after compression |
| T-2 | P1 | `Web.asset` `preloadedAssets` line flips on every build (Input System build processor); keep out of commits or find a stable fix |
| T-3 | P1 | Mobile camera zoom/readability: character ≈ 58 CSS px at 844×390; decide after G-1 |
| T-4 | P2 | `PlaceholderCharacterView` + greybox `Body`/`Label_TEMP` remain (disabled) on `Player_A0`; remove when the greybox `FirstPlayable` scene is retired |
| T-5 | P2 | Two scenes with overlapping content (`FirstPlayable` greybox, `ArtCalibration`); choose the M3 base scene |
| T-6 | P2 | Editor automation: keyboard injection breaks after the Editor loses focus (Input System background reset); tests need Play Mode restart — consider PlayMode test assembly instead of ad-hoc scripts |
| T-7 | P2 | PlayMode automated tests for animation state (currently EditMode for math + scripted Play Mode checks) |
| T-8 | P2 | Unity Personal splash shown at start (licence-dependent) |
| T-9 | P2 | Fonts: legacy Text; move to TextMeshPro with a licensed font |
| T-10 | P2 | Git LFS quota check before committing bulk art |
| T-11 | P1 | UI font: LegacyRuntime (Arial) → licensed Fredoka + Nunito Sans via TextMeshPro (with T-9) |
| T-12 | P2 | Runtime-generated rounded UI sprites look square in Edit Mode; replace with final 9-slice UI art |
| T-13 | P2 | Dev item keys use F-keys (browser conflicts); add on-screen dev panel if Web dev testing is needed |
| T-14 | P2 | Inventory: no split-stack / quantity picker, no discard |
| T-15 | P2 | Hotbar selection has no "use" action yet (farming/fishing milestones) |
| T-16 | P1 | `PlayerProfileHost` always starts a new profile; local save must load `PlayerProfileData` and grant the starter kit only when no save exists |
| T-17 | P2 | Gamepad / Tab navigation in inventory UI |
| T-18 | P2 | Move ad-hoc Editor regression scripts to a PlayMode test assembly (with T-6/T-7) |
| T-20 | P2 | Touch Interact button looks "active" while the bag is open (M1 modal behaviour meant for dialogue); hide/dim for non-dialogue modals |
| T-21 | P1 | Built-in Web font lacks some glyphs (en dash showed as "Hotbar: 15"); use ASCII until T-11; rebuild Web to confirm the "1-5" fix |
| T-22 | P1 | Dev laptop has 8 GB RAM; with the Editor open only 0.2–0.6 GB was free and the M3.1 Web build took 22.7 min (wasm link/`wasm-opt`) and the first browser load stalled at 90 %. Build once per milestone, with the Editor closed or in batchmode; close other apps before Web tests |
| T-23 | P2 | Web build optimisation to evaluate (each needs founder approval and a before/after measurement): IL2CPP code generation "Optimize Size", managed stripping level, texture compression/atlas (T-1), unused modules; current total 9.31 MiB |
| T-24 | P2 | Consider a CI/remote machine for Web builds so local RAM limits do not block milestone checks. M3.2: first batchmode attempt was killed for low memory; second (Editor closed, 1.8 GB free) succeeded in 13.6 min |
| T-25 | P2 | Day/night is one full-screen tint; no local night lights (lanterns/windows). If needed, simple additive glow sprites — not real-time lighting |
| T-26 | P2 | Day/night overlay also tints camera background outside the map (not visible while the camera is clamped to the map) |
| T-27 | P2 | `TimeDebugTools.OnGUI` builds a string per frame (dev builds only) |
| T-28 | P1 | No real pause menu yet; `GameClockHost.PauseClock/ResumeClock` ready |
| T-29 | P1 | Local save must store/restore `GameClockData` together with `PlayerProfileData`; no progress on load |
| T-30 | P2 | Time-period icons are placeholder circles; art needed (sunrise, sun, sunset, moon) |
| T-31 | P2 | In the Editor, an unfocused Editor window pauses game time in Play Mode (correct focus behaviour; remember when testing) |
| T-32 | – | ~~`Local` variant through `Build-Web.ps1`~~ done 2026-11-26 (`M3-02-20261126-1527`, deployed to preview and production) (closed) |
| T-33 | P2 | Web caching: all files `no-cache` (ETag revalidation) because build file names have no hash. Enable "Name Files As Hashes" then serve `Build/*` as `immutable` (needs founder approval + measurement) |
| T-34 | P2 | Preview URLs (`*.lifetopia-play.pages.dev`) are public. For private playtests consider Cloudflare Access (free tier) on preview deployments — founder decision, no paid plan |
| T-35 | P2 | Cloudflare Pages production branch verified only indirectly (active Production deployment on `main`); confirm in dashboard Settings → Builds & deployments |
| T-36 | – | ~~`.meta` for new Editor folder and favicon~~ generated by the Public batch build 2026-11-26 (closed) |
| T-37 | – | ~~Crop size vs plot~~ decided 2026-11-26: ready crop 100 %, per-stage scale in `FarmVisualSet` (seed 0.68 / sprout 0.81 / young 0.78 / ready 1.0); mature leaves may overlap the plot behind (closed) |
| T-38 | P3 | Crops now use Sort Point **Pivot** (consistent with Sorting Group roots of player/house/trees). Remaining loose world sprites (e.g. shop placeholder parts) still use Center; review when they get art |
| T-39 | – | ~~Hotbar slot cleared when a stack runs out~~ changed 2026-11-26: reference kept, dimmed with 0, reactivates on refill (closed) |
| T-40 | – | ~~Tree canopy over plots~~ farm moved to the south edge of the main path (x -12..-10, y -1.5/-2.5) in both scenes (closed) |
| T-41 | P1 | Plot data persistence (founder request 2026-11-26, confirmed at M3.3 close) — kept at M3.4 close as **progress save** (inventory, hotbar, coins, clock, farm plots; fishing has no state to save): farm state is not saved yet; `FarmFieldHost.CaptureState/TryRestoreState` ready for the local save milestone (with `GameClockData`, `PlayerProfileData`) |
| T-42 | P2 | Action animations for hoe, watering can and harvest (character + crop), floating reward; currently a short scale pulse + toast (founder request 2026-11-26) |
| T-45 | P2 | Sound effects for till, plant, water, harvest and "inventory full" (with mute option, GDD feedback) (founder request 2026-11-26) |
| T-46 | P2 | Fishing character animation (cast/reel); now idle pose + line, bobber, ripple, splash only (kept at M3.4 close; together with T-42 farming action animations) |
| T-46b | P2 | Fishing sound effects (cast, bite, reel, catch, fail); none yet (kept at M3.4 close; together with T-45 farming SFX) |
| T-47 | P2 | Gamepad for fishing (South = reel in via UiSubmit, Start = cancel) — bindings exist, untested (no device); part of V-3 (kept at M3.4 close) |
| T-48 | P3 | Use of deep water: `water_deep` imported as reserve only; deep/shallow transition art and level use needed |
| T-49 | P3 | Beach sand reuses the path texture (C15); dedicated sand/grass transition and beach decoration could follow |
| T-50 | P2 | Fishing mini-game balancing after playtest (zone width/position, marker speed, bite delay); start values approved, all in Inspector |
| T-51 | P3 | "INFO" label on the sign is still the placeholder TextMesh positioned on the board art |
| T-43 | P2 | Gameplay Pack 01 sprites imported uncompressed for calibration; atlas + compression after review (see T-23) |
| T-44 | P3 | Virtual joystick can cover the left plot column when the farm is at the left screen edge on phones |
| T-52 | P1 | Local progress save must include coins and market results (part of T-41); reload still resets the session (kept at M3.5 close) |
| T-53 | P3 | Rio/NPC idle breathing (optional, feet fixed) and a small trade animation/feedback; with T-42/T-46 character action animations (kept at M3.5 close) |
| T-54 | P2 | Market sound effects (buy, sell, refused) with the mute option (with T-45/T-46b) (kept at M3.5 close) |
| T-55 | P2 | (kept at M3.5 close) Gamepad for the market (stick = navigate, South = confirm, Start = close; Tab has no gamepad equivalent yet); part of V-3 |
| T-56 | P3 | Place Maya and Kai (assets imported locally, not in game) in a later milestone |
| T-57 | P1 | (M3.6) House layout persistence: `HouseLayoutData` (versioned, validated `TryLoad`) is ready; store it with the M3.7 local save (with T-41/T-52). Reload still resets the house |
| T-58 | P2 | (M3.6) Art QC: `bed_north` calibrated 2026-11-27 with uniform visual scale 1.3 (founder). Still open: `table_west` much smaller than `table_north` — calibrate/re-export before enabling more rotations; a re-exported bed_north at final size would let the scale return to 1.0 |
| T-59 | P2 | (M3.6) Enable west variants for table/stool/chair/wardrobe/bookshelf/chest after art calibration; currently reserve |
| T-60 | P2 | (M3.6) Reserve decor (rattan chair, chest, rug_rect, lantern, flower vase, hibiscus, palm, small plant, basket, shell) imported without item/price; needs founder pricing + footprint approval |
| T-61 | P3 | (M3.6) Small decor on tables/shelves (stacking) and wall decorations — not supported; floor only |
| T-62 | P2 | (M3.6) Bed is decorative only: no sleep/skip-day without founder approval |
| T-63 | P2 | (M3.6) Housing SFX (place, store, invalid) and placement animation (with T-45) |
| T-64 | P2 | (M3.6) Gamepad for Decorating Mode (bindings via NavigateInput/UiSubmit exist, untested; part of V-3) |
| T-65 | P2 | (M3.6) PlayMode test assembly for scene transitions/decorating (currently scripted Play Mode checks via MCP; with T-18) |
| T-66 | P3 | (M3.6) Market "Owned" shows bag count only; placed furniture not counted |
| T-19 | P2 | `Data/` is now partly public (`Data/Items`); `Data/Characters` stays local — keep `.git/info/exclude` in sync |

## Item icons to produce (M3.1, Art Bible §05: 128×128, 12 px padding)
| ID | Pri | Icon |
|---|---|---|
| I-1 | P1 | Hoe, Watering Can, Fishing Rod, Tropical Seeds, Tropical Fruit, Reef Fish |
| I-2 | P2 | Driftwood, Wooden Stool |
| I-3 | P1 | UI: coin, backpack (Bag button), close; P2: 9-slice cream/wood panel, slot and button frames |
