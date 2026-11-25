# Technical architecture — v1.0 draft

## Boundaries
`lifetopia-game` is a Unity client; `lifetopia-platform` is existing Next.js/Turborepo website, community and grants infrastructure. Prototype is offline single-player, no backend SDK or credentials. Use composition and stable interfaces so local save can later be replaced by authenticated APIs. Keep all new development isolated under `Assets/_Lifetopia`.

| Folder | Responsibility |
| --- | --- |
| Art/{Characters,Terrain,Buildings,Props,Items,UI,VFX} | approved runtime assets only |
| Animations, Audio, Materials | animations/controller, licensed audio, materials |
| Scenes | Bootstrap if needed, FirstPlayable, later Village and HomeInterior |
| Prefabs, Tilemaps | player, camera, world props, input UI, tile assets |
| Scripts/{Core,Input,Player,World,Interaction,UI,Data,Persistence,Integration,Editor} | one responsibility each; Editor code not shipped |
| Data | ScriptableObject definitions, initial config |
| UI | reusable canvas/panels |
| Tests/{EditMode,PlayMode} | test logic and scene behavior; assembly refs as needed |

Use a simple FirstPlayable scene before adding bootstrap or additive scenes. No empty-folder churn or speculative frameworks; create directories when actually needed. Preserve Unity-generated metadata. Prefer explicit dependencies and inspector wiring. `PlayerInputReader` captures movement and interaction; `PlayerMotor2D` applies normalized movement via Rigidbody2D (zero gravity, freeze rotation; use appropriate fixed-step update); `PlayerVisuals` tracks direction independently; `CameraFollow` follows in LateUpdate; `InteractionDetector` handles nearby interactable target. Names are proposals, not mandatory if existing code has better names.

## Rendering & art
XY orthographic; square grid; 1 unit = 64 world pixels. Ground sorting separated from Y-sorted world objects (axis 0,1,0; pivot sort point); SortingGroup for layered player and compound props. Test relative sorting instead of merely setting a numeric layer. Collider at foot/base, not entire tree canopy. Camera size ~5.5 initially. No real-time 3D lighting or unnecessary render features. Follow DECISIONS for padding, frame grid and pivot. Prototype can use baked sprites; modular architecture needs shared animation clock, direction-specific render order, identical anchors and verified layer alignment.

## Controls & UI
Input System actions Move, Interact, Pause; WASD/arrows and touch joystick. Clear touch state when finger released, UI loses focus or browser hidden. No diagonal speed advantage; suppress gameplay actions while modal is active. Responsive UGUI canvas scale-with-screen-size 1280x720; safe area and aspect tests; touch targets ~44 CSS px equivalent. Main UI in English. Do not use web-only keyboard assumptions. Test desktop and touch separately.

## Persistence (M3+)
Versioned local state DTOs: position, scene ID, clock, inventory, coins, farm state, quest flags. Validate load and migration; safe defaults and explicit reset/recovery. Use Unity Web-compatible persistence; test reload, blocked storage, quota, corrupted data and page visibility. No trusted online economy from local save. Definitions immutable in runtime; separate save interface only when needed.

## Web build & quality
M0 switch Build Profiles to Web after checking installed module. Save and review setting diffs. Build a blank scene to `Builds/Web/Smoke` (ignored); use Build and Run or a local HTTP server, never file://. Check browser Console and network. If HTTP compression problems occur, diagnose server headers/fallback configuration; don't deploy to live play domain. Prefer small textures/atlases, compressed assets tested for transparency, stripping and profiling rather than speculative aggressive tuning. Targets: 30 FPS on selected HP, 60 FPS selected PC, initial download <=25 MB if practical; not measured or guaranteed. Record build size, engine, browser/device, timings, errors and actual FPS. Mobile test not done unless tested on a real device; desktop emulation is supplementary only. M0 may be locally complete while mobile verification remains explicitly pending.
