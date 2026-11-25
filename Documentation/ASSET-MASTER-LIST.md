# Asset production master list — v1.0

All runtime entries below are pending unless separately verified and accepted. Concept art, character turnarounds and modular boards are approved design references only. No image from a presentation board is game-ready by default.

| ID | Batch | Deliverable | Acceptance |
| --- | --- | --- | --- |
| A0-01 | Calibration | male avatar front idle base, 128x128 | alpha, 64 PPU, foot anchor, all-ages silhouette |
| A0-02 | Calibration | male idle/walk down sample | 4/8 frames, stable foot/face/clothes, loop |
| A0-03 | Calibration | grass + dirt/path tile | 64x64, 3x3 seam test |
| A0-04 | Calibration | palm | transparent, root pivot, correct sorting |
| A0-05 | Calibration | starter tropical house | footprint/collider/door, oblique view |
| A0-06 | Calibration | HUD sample | scalable, readable, safe area, no baked text |
| A1 | Terrain | grass variants, path/sand/shore/water, soil and dock | 64px grid, adjacency transitions + corners where needed |
| A2 | Characters | male/female base 4 directions; idle 4x4, walk 8x4 per avatar; 3 NPC later | direction D/L/R/U rows, matched canvas & anchor; baked first, modular after proof |
| A3 | Environment | market, house, palm, fruit trees, shrubs, flowers, rocks, fence, signs, dock | scale, shadows, sorting, collision, no horizon |
| A4 | Gameplay | seed, crop 4 states, fish, tools, coin, inventory icons, action animations | 128px item canvas, clear silhouettes; no fake sprite animation |
| A5 | Polish | home interior, furniture, UI panels, quest icons, VFX, SFX | rights, contrast, optimization, approved gameplay |

Naming: `char_player_male_idle_d_v01.png`, `char_player_male_walk_4dir_v01.png`, `terrain_grass_01_v01.png`, `bld_starter_house_v01.png`. Use lower_snake_case, stable IDs. For sheets, top-to-bottom rows D,L,R,U; columns chronological; 128px cells. Master exports outside repo if unapproved; runtime PNG RGBA in Assets after QC. Track source, rights, prompt/version and approval in asset register. Check LFS quota before uploading bulk PNGs. Use temporary geometric art for M1, not concept-board crops.

QC: actual alpha check; exact canvas/grid; common feet and scale; edge halos; 4 direction consistency; onion skin; seamless tiles; no texts/watermarks; correct importer; actual Unity screenshot and performance. All calibration choices are subject to founder approval.
