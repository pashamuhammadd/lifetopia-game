# Decision register — 25 November 2026

## Approved direction
GDD 2.0 eight founder decisions remain binding. Later approved Island Explorer all-ages concept, neutral male/female base, 4-way turnaround and modular visual direction supersede earlier generic character concepts. Reference files in `reference/` are visual boards, not implementation deliverables. The character illustration should read young adult rather than child; no forced gender-locked cosmetics in future design. First prototype may use one baked male avatar; female and modular variations follow calibration. Both remain product goals.

## Technical reconciliation (validated at M2 — see M2 calibration decisions below)
| Topic | Original draft / later discussion | Working choice |
| --- | --- | --- |
| Character proportions | Art Bible 2–2.5 heads; later approved concept 2.5–3 heads | 2.5–3 heads, matching approved neutral base silhouette; calibrate at runtime |
| Walk frames | Art Bible 6; later turnaround 8 | 8 per direction, 8–10 fps starting point; verify smoothness, may simplify after test |
| Idle | 4 per direction | 4 per direction at ~4 fps |
| Character pivot | Art Bible foot point (64,16) bottom-left origin; presentation board says bottom center | Use actual foot anchor (64,16) on 128x128 padded canvas, normalized (0.5,0.125); feet align across frames. If canvas or padding revised, update all assets together. Do not set bottom edge pivot blindly. |
| Sheet rows | Art Bible D/L/R/U; later boards D/R/L/U | Canonical D,L,R,U in file order from top to bottom; explicit metadata, never infer direction from infographic labels |
| Runtime paths | Art Bible Assets/Art/Lifetopia; later foundation plan Assets/_Lifetopia | Assets/_Lifetopia/Art/...; references outside Assets |
| Scope | GDD vertical slice includes gameplay; later Prototype 0.1 is movement only | M1 first movement scene, M2 art calibration, M3–M4 full vertical slice |
| Input/portrait | PC and HP from start; landscape baseline | landscape first, test mobile; portrait rotate overlay implemented in M2 (see M2 decisions below) |
| Rendering | sprite 2.5D, no 3D world | Built-In 2D; orthographic camera, 64 PPU, 64px tiles, Bilinear, no mipmaps initially |

## Verified repository snapshot
GitHub main checked during kit preparation: public `lifetopia-game`; ProjectVersion 6000.3.29f1; manifest has `com.unity.pipeline` 1.0.1, Input System 1.20.1, 2D feature 2.0.2, Test Framework 1.6.0. Existing Unity .gitignore covers standard caches; .gitattributes uses LFS for images/audio and Unity YAML/text rules. Source links: https://github.com/pashamuhammadd/lifetopia-game/blob/main/Packages/manifest.json and https://github.com/pashamuhammadd/lifetopia-game/blob/main/.gitattributes . Local runtime and current branch must be rechecked by Claude. Earlier platform README describes shared identity; actual auth implementation not audited.

## Still open
Game economy numbers, map 40x40, NPC names, time cycle 24 min, 25 MB compressed target, 30/60 FPS targets remain draft, not guarantees. License, commercial distribution rights, privacy/moderation policy and prior grant milestone alignment require owner review. No claim of production readiness or grant approval.

## M2 calibration decisions — 26 November 2026 (founder approved)
M2 is closed as a **calibration milestone** with the exceptions below. The Art Bible / table above remain the
**final quality target**; the exceptions apply to the calibration assets only and are tracked in `BACKLOG.md`.

| Topic | Calibration result (accepted) | Final target (unchanged, backlog) |
| --- | --- | --- |
| Walk | 4 unique key poses per direction, each held 2 slots → 8 slots at 12 fps (6 pose changes/s) | 8 unique poses per direction, 8–10 fps starting point (table above) |
| Idle | 1 drawn pose + procedural anchored breathing (3 unique states, 4 slots at 4 fps) | 4 hand-drawn frames per direction at ~4 fps |
| Character rendition | A0-02 is a new rendition of the same male design; minor differences from the approved A0-01 static accepted | One consistent master design for all sheets |
| Walk vs input | Walk plays only with move intent **and** real Rigidbody speed ≥ 0.5 u/s; blocked → idle after 0.08 s; last facing kept on stop; no mirroring of L/R | Same rule |
| Import | Single sprites, Full Rect, 64 PPU, pivot (0.5, 0.125), Bilinear, Clamp, no mipmaps, uncompressed | Sprite Atlas + compression after transparency test (backlog) |
| Portrait | "Please rotate your device" overlay in the Web template on touch devices in portrait | Same; revisit if portrait gameplay is ever designed |
| Real phone | Not tested; **mandatory before any public release** | Android Chrome + iOS Safari minimum |
| Art distribution | A0/A0-02 PNGs are **local only**; never committed to a pushed branch or pushed without explicit founder approval | Owner rights review per asset |

## M3.1 implementation decisions — 26 November 2026 (founder approved)
| Topic | Working choice |
| --- | --- |
| Data vs state | Item definitions are immutable ScriptableObjects; inventory/hotbar/wallet are plain C# runtime state; saves use `PlayerProfileData` (IDs + numbers, versioned) |
| Item IDs | lower_snake_case, stable forever once released (e.g. `seed_tropical`) |
| Inventory rules | 12 slots, max 99 per stack, non-stackables one per slot; every operation all-or-nothing; full bag → message, nothing removed |
| Hotbar | 5 references to items in the inventory (no own quantities); one slot per item; cleared automatically at 0 |
| Tools | Category Tool is never consumed on use |
| Starter | 100 coins, 3 Tropical Seeds, Hoe, Watering Can, Fishing Rod; granted once (flag stored in save data) |
| Placeholder prices | Seeds buy 10 / crop 18 / fish 12 from GDD; other prices are test values, economy still draft |
| UI art | No new PNGs: rounded corners/shadows generated in code, category-coloured placeholder icons; final icons via the Icon field |
| Scenes | `FirstPlayable` (public greybox) carries the inventory test path; `GameplayPrototype` (copy of ArtCalibration) is local-only |
| UI text | ASCII only in UI strings until a licensed font is bundled (built-in Web font lacks e.g. en dash) |
| Web builds | Build once per milestone when features are stable; close the Unity Editor (or build in batchmode) on the 8 GB dev laptop |

## M3.5 market & economy decisions — 26 November 2026 (founder approved; accepted 16/16, milestone closed)
| Topic | Working choice |
| --- | --- |
| Economy core | `MarketService` trades on the existing `Wallet` + `Inventory` (no parallel currency/bag). Atomic: full validation (listed item, quantity 1-99, coins, free space incl. partial stacks, owned count, coin cap 9,999,999, 64-bit totals) then change with rollback; re-entrant trades refused |
| Prices | Unit prices come from item assets (`BuyPrice`/`SellPrice`, single source): seed_tropical buy 10, crop_tropical_fruit sell 18, fish_reef sell 12. `MarketConfig_Rio` lists only which items are bought/sold. Unlimited stock, no fees, no opening hours, no buy-back of seeds/tools |
| Starting state | 100 coins + 3 seeds + hoe/can/rod per new session (unchanged `StarterProfile`); no save yet, reload resets the session |
| Market UX | Interact near Rio opens "Rio's Market" directly (greeting in header). Menu modal: blocks movement/world interaction and pauses game time like the bag; hides hotbar; backdrop blocks joystick. One confirm per frame + 0.25 s cooldown |
| Rio | Static NPC in front of the market stall (10.05, 1.55), foot collider 0.6 x 0.3, same pivot/PPU/sorting as the player, faces the player on interact. Maya/Kai assets kept local, not placed |
| Public/local | Same prefabs (`Npc_Rio`, `MarketPanel`) and config in both scenes; original NPC sprites, portrait and market icons bound only in the local scene |
| Economy baseline | **Approved after founder acceptance 16/16:** seed_tropical buy 10, crop_tropical_fruit sell 18, fish_reef sell 12 (item assets); start 100 coins + 3 seeds + tools; max 99 per trade; 0.25 s confirm cooldown; unlimited stock, no fees/hours. Changes need founder approval and a playtest |
| Kept as approved | Rio in front of the stall; Interact opens the market directly; greeting "Welcome, traveler!" in the header; market pauses game time like the bag |

## M3.4 fishing & environment decisions — 26 November 2026 (founder approved; accepted 15/15, milestone closed)
| Topic | Working choice |
| --- | --- |
| Terrain visuals | **Approved.** Dual-grid (pack contract): visual tilemap offset half a cell, tile per vertex with corner mask NW 1 / NE 2 / SE 4 / SW 8; logical tilemaps and colliders unchanged. Replaces the Art Bible N/E/S/W edge mask for these tiles |
| Sand | Sand = path texture (C15); beach and pond rim are path cells so grass never touches water (no grass/water art). Shore masks 5/10 avoided in level design. Shoreline assembly from the pack approved |
| Map | **Approved.** World extended south to y -15; house, farm, plaza and spawn unchanged (WorldBounds 32 x 26 at (0,-2)); beach rows y -9/-8; sea y -10..-15; 2 x 3 walkable dock at x -1..0, y -10..-12 (water collider removed under the dock) |
| Fishing | Existing `tool_fishing_rod` (never consumed) and `fish_reef` (1 per catch); no bait/cost/rarity/RNG reward/coins. Idle → Casting 0.3 s → Wait 1.2-2.0 s → Timing (zone 28 %, marker 0.75 cycles/s) → result; 8 s limit; one press decides; one award per attempt ID. **Approved:** keep zone 28 %, marker speed and bite delay as start values; all adjustable in the Inspector (`Data/Fishing/FishingConfig.asset`). Cozy rule: failure never costs coins, items or the rod |
| Fishing time | **Founder decision (supersedes the first draft):** game time and crop growth KEEP RUNNING while fishing, including the mini game. Movement lock and game-time pause are separate: `GameplayInputGate.Block(source, pausesGameTime)`; fishing blocks with `false`, bag/dialogue keep the default (pause time). Game time still stops on focus loss/hidden tab, `timeScale 0` and dev pause. The mini game keeps its own clock (max step 0.1 s, stops on focus loss) |
| Public/local | Same geometry, colliders, fishing and decoration prefabs in both scenes; original art only in local scene/assets |
| Fishing baseline | **Baseline after founder acceptance 15/15 ("difficulty feels comfortable"):** cast 0.3 s, bite delay 1.2-2.0 s, max attempt 8 s, marker 0.75 cycles/s, zone centre 0.62, zone width 0.28 (28 %), max step 0.1 s, result display 1.4 s, catch 1 `fish_reef`, rod never consumed. Stored in `Data/Fishing/FishingConfig.asset` (Inspector); changes after this need founder approval and a playtest (backlog T-50) |
| Kept as approved | World extended to y -15; dual-grid terrain/shoreline; game time and crop growth keep running while fishing |

## M3.3 farming decisions — 26 November 2026 (founder approved, milestone closed)
| Topic | Working choice |
| --- | --- |
| States | GDD stages Empty/Planted/Watered/Ready unchanged; hoe sets a separate `Tilled` flag |
| Growth time | 120 s of `IGameClock.ActivePlaySeconds` from watering (pauses with the clock; ignores dev speed and time jumps); Ready derived, no per-frame timers |
| After harvest | Plot stays tilled (`keepTilledAfterHarvest = true`); alternative (re-till) configurable |
| Actions | Selected hotbar item decides the action; any item may harvest; tools never consumed; 1 seed consumed only after validation; full bag keeps crop Ready |
| Crop | `tropical_fruit` uses existing `seed_tropical` → `crop_tropical_fruit`; pineapple is visual only; no new IDs or prices |
| Local art | Item icons by stable ID via runtime `ItemIcons` + local `ItemIconOverrides`; UI sprites via `LocalArtBindings` in the local scene only; tracked assets reference no local art; public scene uses generated placeholders |
| Layout | 6 plots (3 × 2) on the south edge of the main path, west of the starter house (centres x -12/-11/-10, y -1.5/-2.5), clear of tree canopies; soil `GroundDetail`, crops `World` with Sort Point Pivot (matches Sorting Group roots), badges `Overhead` on a cream disc |
| Crop growth visuals (founder) | Ready = 100 % sprite size; per-stage Inspector scale multiplies the sprite size: seed ≈ 12.5 %, sprout ≈ 30 %, growing ≈ 67 %, ready 100 % of the ready plant (measured visible pixels) |
| Crop visual sizes are a kept decision (founder, final) | Keep seed 0.68 / sprout 0.81 / growing 0.78 / ready 1.0 (`FarmVisualSet_Pack01`); new crops follow the same rule: measure visible pixels, ready 100 %, seed 10-15 %, sprout 25-35 %, growing 60-75 % |
| Hotbar at 0 (founder) | Slot keeps its item reference when the count reaches 0 (dimmed, shows 0, not usable) and reactivates automatically on refill unless the player replaced/cleared it; saves keep such references |

## M3.2-D deployment decisions — 26 November 2026 (founder approved)
| Topic | Working choice |
| --- | --- |
| Hosting | Existing Cloudflare Pages project `lifetopia-play` (Free, Direct Upload via Wrangler); no Git integration, no auto-deploy |
| Branches | Default deploy target `preview`; production `main` only with `-Production -SkipBuild` + typed confirmation, after founder approval |
| Artwork | Founder approved publishing original Lifetopia World artwork inside Web builds on Cloudflare Pages (preview + production). Original PNGs, masters and backups never go to GitHub. Scripts still require `-ArtworkApproved` for non-placeholder builds as a deliberate per-deploy acknowledgement |
| Build variants | `Local` (original art) for founder playtests and production; `Public` (placeholder) only to verify the public repository build — never replaces production (`-AllowPlaceholderProduction` guard) |
| Current production | Keep `2cbfb971` (M3-02, manual founder deploy) until a specific replacement is approved |
| Headers | `_headers` generated per deploy from the real compression; `no-cache, no-transform` everywhere until file names are hashed |
| Version info | `version.json` written by `Build-Web.ps1`, shown by the Web template; `productName` (project identity) unchanged |
| Rollback | Cloudflare dashboard "Rollback to this deployment" or redeploy an older verified build folder; never automatic |

## M3.2 implementation decisions — 26 November 2026 (founder approved)
| Topic | Working choice |
| --- | --- |
| Clock rules | Day 1 08:00; 60 real s per game hour (GDD); periods Morning 05–08, Daytime 08–17, Sunset 17–19, Night 19–05; speed configurable in `GameClockConfig` |
| What stops time | Focus/visibility loss, open modal (bag, dialogue), `Time.timeScale = 0`, explicit `PauseClock` sources; no offline progression; max 0.5 s real time per frame |
| Time source for gameplay | `IGameClock` (`TotalMinutes`, `DayChanged`, ...) only — never real-world time for crops or rewards |
| Day/night visuals | One multiply-blended camera quad (custom `Lifetopia/DayNightMultiply` shader) on the World layer; palette authored in sRGB and converted for Linear colour space; night channels ≥ 0.55 |
| Time format | 24-hour "HH:mm" everywhere in UI |
| Save format | `GameClockData` (day, minute of day, active play seconds, version); restore rejects invalid data |
