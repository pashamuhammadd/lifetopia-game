# M2 / A0-02 — Art brief: male idle + walk (Down only)

Status: **waiting for art**. Unity side is ready (see `M2-A0-static.md` §13). This brief is the
request for the next calibration pack. It does not approve mass production.

Sources: `ART-BIBLE-1.0.md` §04, `DECISIONS.md` (walk 8 frames, idle 4 frames, pivot 64,16),
`ASSET-MASTER-LIST.md` A0-02.

## Deliverables

| File | Frames | Layout | Playback |
|---|---|---|---|
| `char_player_male_idle_d_v01.png` (replace or keep as frame 1) / `char_player_male_idle_d_sheet_v01.png` | 4 | one row, 4 columns, left → right in time | loop, ~4 fps |
| `char_player_male_walk_d_sheet_v01.png` | 8 | one row, 8 columns, left → right in time | loop, 8–10 fps (Unity starts at 9) |

Separate per-frame PNGs (`..._walk_d_01_v01.png` … `_08_`) are also fine; tell me which format.

## Hard rules (Unity import will reject or report a failure otherwise)

- Cell **128 × 128 px**, PNG RGBA, transparent background, no text, labels, grid lines or watermark.
- **Foot anchor at (64, 16)** from the bottom-left of every cell (same as the approved static
  sprite). Feet that touch the ground must sit on y = 16 in every frame — no vertical drift
  except the natural step lift.
- Same character as the approved A0-01 static sprite: face, hair, shirt, shorts, boots, palette,
  outline weight and size. Head-to-foot height ≈ 104 px (bounds of A0-01: x 38–90, y 8–112).
- No per-frame resizing or recentering; no motion blur; no drop shadow baked in (Unity draws the
  ground shadow).
- Down = front view, walking toward the camera.

## Motion notes

- Idle: gentle breathing — shoulders/chest rise 1–2 px, maybe a blink on one frame. Feet do not move.
- Walk: classic 8-frame cycle — contact, down, passing, up for each leg. Arms swing opposite to legs.
  Frame 8 must flow back into frame 1 without a jump.

## QC I will run after import

Onion-skin and pivot jitter check, frame-by-frame face/colour consistency, loop seam, scale against
the house door and palm, foot contact on the path tile, Web build size and browser FPS.
Left/right/up directions (A2) and the female avatar come only after this sample is approved.
