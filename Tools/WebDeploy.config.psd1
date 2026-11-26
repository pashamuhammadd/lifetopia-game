# Lifetopia World - Web build and Cloudflare Pages deployment settings.
# Used by Tools/Build-Web.ps1, Tools/Verify-WebBuild.ps1 and Tools/Deploy-Web.ps1.
# No secrets here: Wrangler keeps its login outside the repository (wrangler login).
@{
    # Unity (must match ProjectSettings/ProjectVersion.txt). Override the path with $env:LIFETOPIA_UNITY_PATH.
    UnityVersion     = '6000.3.29f1'
    UnityPath        = 'C:\Program Files\Unity\Hub\Editor\6000.3.29f1\Editor\Unity.exe'
    BuildProfile     = 'Assets/_Lifetopia/Settings/BuildProfiles/Web.asset'

    # Variant 'Local'  = GameplayPrototype with local (unpublished) artwork. Never in the public repo.
    # Variant 'Public' = FirstPlayable greybox placeholder scene that the public repository can build.
    LocalScene       = 'Assets/_Lifetopia/Scenes/GameplayPrototype.unity'
    PublicScene      = 'Assets/_Lifetopia/Scenes/FirstPlayable.unity'

    # Build output (Builds/ is ignored by Git).
    BuildRoot        = 'Builds/Web'
    MinFreeMemoryGB  = 1.5

    # Version information shown on the loading screen (version.json).
    GameVersion      = '0.3.3'
    Milestone        = 'M3.3'
    Channel          = 'Development Build'

    # Cloudflare Pages (existing project; the scripts never create projects or change domains).
    PagesProject     = 'lifetopia-play'
    ProductionBranch = 'main'
    PreviewBranch    = 'preview'
    ProductionUrls   = @('https://play.lifetopiaworld.io', 'https://lifetopia-play.pages.dev')

    # Cloudflare Pages Free limits: 25 MiB per file, 20,000 files per deployment.
    MaxFileBytes     = 26214400
    MaxFileCount     = 20000
}
