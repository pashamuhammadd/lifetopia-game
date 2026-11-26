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
| M3.1 Item, Inventory & Hotbar | **Approved by founder 2026-11-26**, committed 79cbae8. "1-5" text fix verified on Web (M3.2 build). Real phone: founder-reported playtest passed for inventory open/close and hotbar selection (2026-11-26); drag & drop on phone not reported. Pending: gamepad test. | `Progress/M3-01-inventory-hotbar.md` |
| M3.2 Game Time & Day/Night | **Approved by founder 2026-11-26** (incl. Web build 9.37 MiB). Real phone: founder-reported playtest passed (clock, day/night) 2026-11-26. Pending: gamepad. | `Progress/M3-02-game-time.md` |
| M3.2-D Cloudflare Web Deployment Foundation | **Completed and approved by founder 2026-11-26.** Production `play.lifetopiaworld.io` = deployment `002b0658` (original-art build `M3-02-20261126-1527`, via `Deploy-Web.ps1`); rollback target `2cbfb971`. Founder-reported real-phone playtest 10/10 passed. | `Progress/M3-02-D-cloudflare-deployment.md`, `WEB-DEPLOYMENT.md` |
| M3.3+ | Not started; requires explicit founder go-ahead (proposal: basic farming using `IGameClock`) | `Progress/M3-02-game-time.md` §12 |

M0 is the only task authorized by the foundation prompt. No commitment to fixed delivery dates. M2 and M3 can iterate, but do not mass-produce sprites until calibration passes. Keep a dated milestone report in `Documentation/Progress/` with actual tests and screenshots approved for public release. Check previous grant agreement before changing milestones or live game.
