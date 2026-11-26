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
