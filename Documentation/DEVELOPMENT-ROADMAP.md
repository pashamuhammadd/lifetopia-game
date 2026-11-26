# Development roadmap — implementation gates

| Gate | Deliverable | Acceptance evidence |
| --- | --- | --- |
| M0 Foundation | Safe repo, docs, Web smoke scene/build | Actual CLI/editor ready; no compile errors; Git audit; desktop HTTP smoke and mobile if device available; pending tests noted |
| M1 FirstPlayable greybox | One player, camera, boundaries, keyboard + touch, interaction prompt | Play Mode & Web tests; no diagonal acceleration, no camera jitter, touch release, collision and focus; no gameplay economy |
| M2 Art calibration | One avatar, grass/path, palm, house, HUD; sample idle/walk | Check seam, scale, foot anchor, Y-sort, mobile readability and performance; founder approves art before mass production |
| M3 Gameplay | Farming, fishing, inventory, market, tutorial quest, local save | Tests for crop states, inventory capacity, transactions, one-time reward, reload/recovery |
| M4 Vertical slice | 5–8 minute loop in tropical village | End-to-end browser tests, target 5 independent playtesters, log defects and actual device data |
| M5 Connected alpha | Shared Lifetopia Account, authoritative save, basic social integration | Auth audit, test environment, session/ownership/expiry tests, privacy review |
| M6 Online beta | Real-time plaza, chat, visit, reconnect | Server-authoritative movement/rewards, rate limits, reporting, moderation, load tests |
| M7 Optional Web3 | Wallet link, devnet ownership experiment | Security review, signed challenge, replay protection, no client secrets; no mainnet without approval |

## Status (2026-11-26)

| Gate | Status | Evidence |
| --- | --- | --- |
| M0 | Done, committed 09e19ca | `Progress/M0-foundation.md` |
| M1 | Done, committed 5104ce0 | `Progress/M1-first-playable.md` |
| M2 | **Closed as calibration milestone** (founder approved 2026-11-26) with accepted exceptions: 4-key walk, procedural idle, minor rendition change. Open gates before public release: real-phone test, art distribution rights. Art kept local. | `Progress/M2-A0-static.md`, `Progress/M2-art-calibration-final.md` |
| M3 | Not started; requires explicit founder go-ahead | Backlog: `BACKLOG.md` |

M0 is the only task authorized by the foundation prompt. No commitment to fixed delivery dates. M2 and M3 can iterate, but do not mass-produce sprites until calibration passes. Keep a dated milestone report in `Documentation/Progress/` with actual tests and screenshots approved for public release. Check previous grant agreement before changing milestones or live game.
