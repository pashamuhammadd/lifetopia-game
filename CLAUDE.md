# Lifetopia World — project instructions

## Product and sources of truth
- Game client: `pashamuhammadd/lifetopia-game` (public, main); separate existing web monorepo `pashamuhammadd/lifetopia-platform`.
- Unity 6000.3.29f1 LTS, 2D Built-In Render Pipeline, C#, Unity Input System. Unity Pipeline 1.0.1 already configured; use existing Unity CLI/MCP and installed plugin. No migration or package upgrades without approval.
- Read `Documentation/DECISIONS.md`, `GDD-2.0.md`, `ART-BIBLE-1.0.md`, `TECHNICAL-ARCHITECTURE.md`, `DEVELOPMENT-ROADMAP.md`, `ASSET-MASTER-LIST.md`, `PLATFORM-INTEGRATION.md` within Documentation, then relevant code. Decisions file reconciles later discussion with original drafts.
- Chibi Tropical Cozy; Island Explorer modern chibi for all ages, young-adult visual impression, orthographic XY 2D, fixed oblique art perspective. Free starter home; tropical village; single-player first, multiplayer later; web PC and mobile; Web3 after gameplay. Game UI English; explanations to founder Indonesian.

## Work protocol
- Start each task with current milestone, brief plan, `git status`, and Unity status if applicable. Respect pre-existing edits. Use official Unity plugin/CLI/MCP for scene operations when available; verify actual result, do not infer success. No parallel writes to editor and scene YAML. Use supported Editor APIs or inspector operations; save scenes/prefabs and verify compilation.
- Build incrementally; no feature beyond approved milestone. Do not delete existing files, upgrade Unity, introduce dependencies, change project identity, deploy, commit or push unless founder explicitly approves. If tools unavailable, state blocker and provide manual steps; never fabricate tests.
- C# namespace `Lifetopia`; prefer composition, small focused classes, serialized references and explicit null handling. Separate domain data, runtime state, UI and external services. Avoid unnecessary global singletons, per-frame allocations, reflection and broad polling. Prefer Web-compatible APIs and no native-only plugins. Use Input System only; test keyboard, touch, focus and UI capture. Use ScriptableObjects for authored definitions, not mutable player saves.
- New assets under `Assets/_Lifetopia/`; keep template files intact until reviewed. Keep `.meta` files alongside tracked assets. Do not write GUIDs manually. Do not commit Library, Temp, Obj, Logs, UserSettings or build artifacts. Check `.gitignore`/`.gitattributes` rather than replacing. All PNG in existing LFS rules; monitor storage.
- References outside Assets; do not slice presentation boards or pass them off as finished art. Use clearly labeled temporary shapes for greybox. Runtime sprite delivery needs alpha, consistent scale/pivot, source rights, naming and QC. Founder approves actual visual output before mass production.

## Web build and deployment (Cloudflare Pages)
- Follow `Documentation/WEB-DEPLOYMENT.md`. Tools: `Tools/Build-Web.ps1`, `Tools/Verify-WebBuild.ps1`, `Tools/Deploy-Web.ps1` (Windows PowerShell; settings in `Tools/WebDeploy.config.psd1`). Existing project `lifetopia-play`, production `https://play.lifetopiaworld.io` (branch `main`), previews default to branch `preview`.
- Never deploy (preview or production) without the founder's explicit approval of that specific deployment; use `-DryRun` for testing. Never add `-ConfirmProduction`, `-AllowPlaceholderProduction` or `-IgnoreLowMemory` on your own.
- Artwork (founder decision 2026-11-26): original Lifetopia World artwork MAY be published inside Web builds on Cloudflare Pages (preview and production), so `-ArtworkApproved` may be used for `Local` builds once that deployment is approved. Original PNGs, master artwork and backups must NEVER go to the GitHub repository (keep `.git/info/exclude`, stage explicitly, never `git add -A`, never stage `Web.asset` while it lists GameplayPrototype).
- Build variants: deployments for founder playtesting use the `Local` variant (GameplayPrototype, original art). The `Public` variant (FirstPlayable placeholder) is only for verifying that the public repository builds; never replace production with it.
- Keep the current production deployment unless the founder approves a specific replacement. After every preview deploy, confirm the production deployment id is unchanged and report the preview URL.
- No new Pages projects, no DNS/domain changes, no auto-deploy, no rollback without approval. Wrangler login stays outside the repo; never store tokens.
- 8 GB RAM laptop: Web builds only with the Editor closed, one at a time, and only when a milestone needs one; test deployment scripts with an existing build (`-SkipBuild`).

## Public repository security
- No secrets, PAT, access/refresh tokens, private wallet keys, seed phrases, service role keys, personal data, paid assets or unreviewed master art. No real credentials even in examples or logs. Keep personal Claude/MCP config local, do not publish local port or user-specific paths. Review staged paths/diffs before proposing commit; never commit the kit archive. No production writes or schema migrations. Do not assume public repo equals open-source license. Do not apply a license without owner approval.

## Done means evidence
For each milestone report: files changed, scene and prefab structure, Inspector steps, CLI/MCP results, Console error count, tests actually run (and not run), manual reproduction steps, limitations, next recommended task. Founder is learning Unity; explain C# and UI concepts in Indonesian. Request review before commit/push. Never claim mobile/browser performance without measured device results.
