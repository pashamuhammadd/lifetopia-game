# Backlog — technical debt and visual improvements

Opened at M2 close (2026-11-26). Priority: **P0** blocks public release · **P1** before/during M3 · **P2** later.
Source: `Progress/M2-art-calibration-final.md`, `DECISIONS.md` (M2 calibration decisions).

## Release gates (P0)
| ID | Item | Notes |
|---|---|---|
| G-1 | Real-phone test: Android Chrome + iOS Safari minimum | FPS, memory, touch, rotate overlay, safe area, load time. Emulation results do not count. |
| G-2 | Art distribution rights review (A0 + A0-02) | PNGs stay local until approved; see commit procedure in the M2 final report |
| G-3 | Repo license decision | Public repo ≠ open source; owner decision |

## Pending verification (open tasks from approved milestones)
| ID | From | Item | When |
|---|---|---|---|
| V-1 | M3.1 | Web check of the ASCII "1-5" hint fix (Web build showed "Hotbar: 15"; fixed in Editor only) | Next milestone Web build |
| V-2 | M3.1 | Real-phone test of inventory/hotbar touch (tap, drag & drop, backdrop over joystick, touch target sizes) — part of G-1 | Before public release |
| V-3 | M3.1 | Gamepad test: North button toggles bag; movement blocked while open (binding exists, untested — no device) | When a gamepad is available |

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
| T-24 | P2 | Consider a CI/remote machine for Web builds so local RAM limits do not block milestone checks |
| T-19 | P2 | `Data/` is now partly public (`Data/Items`); `Data/Characters` stays local — keep `.git/info/exclude` in sync |

## Item icons to produce (M3.1, Art Bible §05: 128×128, 12 px padding)
| ID | Pri | Icon |
|---|---|---|
| I-1 | P1 | Hoe, Watering Can, Fishing Rod, Tropical Seeds, Tropical Fruit, Reef Fish |
| I-2 | P2 | Driftwood, Wooden Stool |
| I-3 | P1 | UI: coin, backpack (Bag button), close; P2: 9-slice cream/wood panel, slot and button frames |
