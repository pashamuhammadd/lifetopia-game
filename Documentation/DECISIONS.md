# Decision register — 25 November 2026

## Approved direction
GDD 2.0 eight founder decisions remain binding. Later approved Island Explorer all-ages concept, neutral male/female base, 4-way turnaround and modular visual direction supersede earlier generic character concepts. Reference files in `reference/` are visual boards, not implementation deliverables. The character illustration should read young adult rather than child; no forced gender-locked cosmetics in future design. First prototype may use one baked male avatar; female and modular variations follow calibration. Both remain product goals.

## Technical reconciliation (proposed; validate at M2)
| Topic | Original draft / later discussion | Working choice |
| --- | --- | --- |
| Character proportions | Art Bible 2–2.5 heads; later approved concept 2.5–3 heads | 2.5–3 heads, matching approved neutral base silhouette; calibrate at runtime |
| Walk frames | Art Bible 6; later turnaround 8 | 8 per direction, 8–10 fps starting point; verify smoothness, may simplify after test |
| Idle | 4 per direction | 4 per direction at ~4 fps |
| Character pivot | Art Bible foot point (64,16) bottom-left origin; presentation board says bottom center | Use actual foot anchor (64,16) on 128x128 padded canvas, normalized (0.5,0.125); feet align across frames. If canvas or padding revised, update all assets together. Do not set bottom edge pivot blindly. |
| Sheet rows | Art Bible D/L/R/U; later boards D/R/L/U | Canonical D,L,R,U in file order from top to bottom; explicit metadata, never infer direction from infographic labels |
| Runtime paths | Art Bible Assets/Art/Lifetopia; later foundation plan Assets/_Lifetopia | Assets/_Lifetopia/Art/...; references outside Assets |
| Scope | GDD vertical slice includes gameplay; later Prototype 0.1 is movement only | M1 first movement scene, M2 art calibration, M3–M4 full vertical slice |
| Input/portrait | PC and HP from start; landscape baseline | landscape first, test mobile; portrait rotate message proposal, not final |
| Rendering | sprite 2.5D, no 3D world | Built-In 2D; orthographic camera, 64 PPU, 64px tiles, Bilinear, no mipmaps initially |

## Verified repository snapshot
GitHub main checked during kit preparation: public `lifetopia-game`; ProjectVersion 6000.3.29f1; manifest has `com.unity.pipeline` 1.0.1, Input System 1.20.1, 2D feature 2.0.2, Test Framework 1.6.0. Existing Unity .gitignore covers standard caches; .gitattributes uses LFS for images/audio and Unity YAML/text rules. Source links: https://github.com/pashamuhammadd/lifetopia-game/blob/main/Packages/manifest.json and https://github.com/pashamuhammadd/lifetopia-game/blob/main/.gitattributes . Local runtime and current branch must be rechecked by Claude. Earlier platform README describes shared identity; actual auth implementation not audited.

## Still open
Game economy numbers, map 40x40, NPC names, time cycle 24 min, 25 MB compressed target, 30/60 FPS targets remain draft, not guarantees. License, commercial distribution rights, privacy/moderation policy and prior grant milestone alignment require owner review. No claim of production readiness or grant approval.
