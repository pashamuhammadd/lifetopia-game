# Backlog — technical debt and visual improvements

Opened at M2 close (2026-11-26). Priority: **P0** blocks public release · **P1** before/during M3 · **P2** later.
Source: `Progress/M2-art-calibration-final.md`, `DECISIONS.md` (M2 calibration decisions).

## Release gates (P0)
| ID | Item | Notes |
|---|---|---|
| G-1 | Real-phone test: Android Chrome + iOS Safari minimum | FPS, memory, touch, rotate overlay, safe area, load time. Emulation results do not count. |
| G-2 | Art distribution rights review (A0 + A0-02) | PNGs stay local until approved; see commit procedure in the M2 final report |
| G-3 | Repo license decision | Public repo ≠ open source; owner decision |

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
