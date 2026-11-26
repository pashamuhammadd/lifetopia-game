<#
.SYNOPSIS
  Deploys a verified Lifetopia World Web build to the existing Cloudflare Pages project (Direct Upload).

.DESCRIPTION
  Modes:
    1. Build + deploy to PREVIEW      .\Tools\Deploy-Web.ps1 [-Variant Local|Public] [-ArtworkApproved]
    2. Existing build -> PREVIEW      .\Tools\Deploy-Web.ps1 -SkipBuild -BuildPath Builds/Web/M3-02 [-ArtworkApproved]
    3. Approved build -> PRODUCTION   .\Tools\Deploy-Web.ps1 -Production -SkipBuild -BuildPath Builds/Web/M3-02 [-ArtworkApproved]
  Add -DryRun to do every check and staging step without uploading.

  Safety rules:
  - Default branch is 'preview'. Production ('main') only with -Production, only with -SkipBuild,
    and only after typing the project name (or -ConfirmProduction lifetopia-play).
  - Builds containing local artwork (or of unknown content) need -ArtworkApproved: preview URLs
    are public; noindex is not access control.
  - Stops when the build, verification, Wrangler login, project or branch checks fail.
  - Uploads only an allow-listed staging copy: index.html, Build/*, TemplateData/*, version.json,
    favicon, 404.html and a generated _headers. Never source, art masters, logs or secrets.
  - Never creates Cloudflare projects, never touches DNS or custom domains.
  - Refuses to put a public-placeholder build on production unless -AllowPlaceholderProduction
    (founder decision 2026-11-26: production keeps the original-art build).
#>
[CmdletBinding()]
param(
    [switch]$SkipBuild,
    [string]$BuildPath,
    [switch]$Production,
    [string]$Branch,
    [switch]$ArtworkApproved,
    [switch]$AllowPlaceholderProduction,
    [string]$ConfirmProduction,
    [switch]$DryRun,
    # Passed to Build-Web.ps1 when building:
    [ValidateSet('Local', 'Public')][string]$Variant = 'Local',
    [string]$Name,
    [string]$GameVersion,
    [string]$Milestone,
    [switch]$IgnoreLowMemory
)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'LifetopiaWeb.psm1') -Force
$cfg = Get-LtConfig
$root = Get-LtProjectRoot
Set-Location $root
$project = $cfg.PagesProject

function Stop-Deploy([string]$Message) { Write-LtErr $Message; Write-Host ''; Write-LtErr 'DEPLOYMENT ABORTED - nothing was uploaded'; exit 1 }

$target = if ($Production) { 'PRODUCTION' } else { 'PREVIEW' }
Write-LtStep "Lifetopia Web deploy -> Cloudflare Pages '$project' ($target)$(if ($DryRun) { ' [DRY RUN]' })"

# 0. Branch rules
if ($Production) {
    if (-not $SkipBuild) { Stop-Deploy 'Production deploys only an existing, already reviewed build: use -Production -SkipBuild -BuildPath <folder>.' }
    if ($Branch -and $Branch -ne $cfg.ProductionBranch) { Stop-Deploy "-Branch cannot be combined with -Production (production branch is '$($cfg.ProductionBranch)')." }
    $Branch = $cfg.ProductionBranch
} else {
    if (-not $Branch) { $Branch = $cfg.PreviewBranch }
    if ($Branch -eq $cfg.ProductionBranch) { Stop-Deploy "Branch '$Branch' is the production branch. Use -Production for production deploys." }
    if ($Branch -notmatch '^[a-z0-9][a-z0-9-]{0,27}$') { Stop-Deploy "Invalid preview branch name '$Branch' (lowercase letters, digits, '-')." }
}
Write-LtOk "Branch: $Branch"

# 1. Build (mode 1) or pick an existing build (modes 2/3)
if (-not $SkipBuild) {
    $buildArgs = @{ Variant = $Variant }
    if ($Name) { $buildArgs.Name = $Name }
    if ($GameVersion) { $buildArgs.GameVersion = $GameVersion }
    if ($Milestone) { $buildArgs.Milestone = $Milestone }
    if ($IgnoreLowMemory) { $buildArgs.IgnoreLowMemory = $true }
    if ($DryRun) { $buildArgs.DryRun = $true }
    $out = & (Join-Path $PSScriptRoot 'Build-Web.ps1') @buildArgs
    if ($LASTEXITCODE -ne 0) { Stop-Deploy 'Build failed.' }
    if ($DryRun) { Write-LtWarn 'Dry run: build skipped, so there is nothing to stage. Use -SkipBuild -BuildPath to dry-run a deploy.'; exit 0 }
    $BuildPath = @($out)[-1]
} elseif (-not $BuildPath) {
    $last = Join-Path (Resolve-LtPath $cfg.BuildRoot) 'last-successful-build.txt'
    if (-not (Test-Path $last)) { Stop-Deploy 'No -BuildPath given and no last successful build recorded.' }
    $BuildPath = (Get-Content $last -TotalCount 1).Trim()
    Write-LtInfo "Using last successful build: $BuildPath"
}

# 2. Verify the build
Write-LtStep "Verifying build $BuildPath"
$check = Test-LtWebBuild -BuildPath $BuildPath
Write-LtVerifyResult $check
if (-not $check.Ok) { Stop-Deploy 'Build verification failed.' }
$layout = $check.Layout

# 3. Artwork publication approval
$content = if ($layout.Version) { $layout.Version.contentMode } else { 'unknown' }
if ($content -ne 'public-placeholder') {
    if (-not $ArtworkApproved) {
        Stop-Deploy "This build's content is '$content' (may contain local artwork). Any Pages URL, including preview, is publicly reachable. Re-run with -ArtworkApproved only after the founder approves publishing this artwork."
    }
    Write-LtWarn "Content '$content' - publishing approved via -ArtworkApproved"
} else {
    if ($Production -and -not $AllowPlaceholderProduction) {
        Stop-Deploy 'This is a public-placeholder build. Production keeps the original-art build (founder decision 2026-11-26). Use -AllowPlaceholderProduction only with explicit founder approval.'
    }
    Write-LtOk 'Content: public placeholder build'
}

# 4. Wrangler, login, project, production branch
Write-LtStep 'Checking Wrangler and Cloudflare Pages project'
if (-not (Get-Command wrangler -ErrorAction SilentlyContinue)) { Stop-Deploy 'Wrangler CLI not found (npm install -g wrangler, then wrangler login).' }
$who = Invoke-LtNative 'wrangler' @('whoami')
if ($who.ExitCode -ne 0 -or ($who.Output -join "`n") -match 'not authenticated|You are not logged in') { Stop-Deploy 'Wrangler is not logged in. Run: wrangler login' }
Write-LtOk 'Wrangler logged in'
$list = Invoke-LtNative 'wrangler' @('pages', 'project', 'list')
if ($list.ExitCode -ne 0 -or -not (($list.Output -join "`n") -match "\b$([regex]::Escape($project))\b")) { Stop-Deploy "Cloudflare Pages project '$project' not found. This script never creates projects." }
Write-LtOk "Project '$project' exists"

function Get-ProductionDeployment {
    $r = Invoke-LtNative 'wrangler' @('pages', 'deployment', 'list', '--project-name', $project, '--environment', 'production', '--json')
    if ($r.ExitCode -ne 0) { return $null }
    $text = $r.Output -join "`n"
    $start = $text.IndexOf('['); $end = $text.LastIndexOf(']')
    if ($start -lt 0 -or $end -lt $start) { return $null }
    $items = @($text.Substring($start, $end - $start + 1) | ConvertFrom-Json)
    if ($items.Count -eq 0) { return $null }
    return $items[0]
}
$prod = Get-ProductionDeployment
if ($null -eq $prod) { Stop-Deploy 'Could not read the current production deployment.' }
if ($prod.Branch -ne $cfg.ProductionBranch) { Stop-Deploy "Current production deployment is on branch '$($prod.Branch)', config expects '$($cfg.ProductionBranch)'. Check the project's production branch in the dashboard." }
Write-LtOk "Current production: $($prod.Id) (branch $($prod.Branch), source $($prod.Source), $($prod.Status))"

# 5. Staging copy (allow-listed runtime files + generated _headers + 404.html)
Write-LtStep 'Preparing upload folder'
$stamp = Get-Date -Format 'yyyyMMdd-HHmmss'
$stage = Join-Path (Resolve-LtPath $cfg.BuildRoot) "_deploy\$($layout.Name)-$stamp"
New-LtStagingFolder -Layout $layout -StagePath $stage
$staged = Test-LtWebBuild -BuildPath $stage -Staged
if (-not $staged.Ok) { Write-LtVerifyResult $staged; Stop-Deploy 'Staged upload folder failed verification.' }
Write-LtOk ('Upload folder: {0} ({1} files, {2:N2} MiB)' -f $stage, $staged.Layout.Files.Count, ($staged.Layout.TotalBytes / 1MB))
$staged.Layout.Files | ForEach-Object { Write-LtInfo ('  {0,-40} {1,12:N0} bytes' -f $_.Rel, $_.Bytes) }

# 6. Production confirmation
if ($Production -and -not $DryRun) {
    if ($ConfirmProduction -ne $project) {
        Write-Host ''
        Write-Host "You are about to replace PRODUCTION (play.lifetopiaworld.io) with $($layout.Name)." -ForegroundColor Yellow
        $typed = $null
        try { $typed = Read-Host "Type the project name '$project' to continue" } catch { $typed = $null }
        if ($typed -ne $project) { Stop-Deploy 'Production deploy not confirmed.' }
    }
    Write-LtOk 'Production deploy confirmed'
}

# 7. Deploy
$git = Get-LtGitInfo
$message = "$($layout.Name)"
if ($layout.Version) { $message = "$($layout.Name) v$($layout.Version.version) $($layout.Version.milestone) ($content)" }
$wranglerArgs = @('pages', 'deploy', $stage, '--project-name', $project, '--branch', $Branch,
                  '--commit-hash', $git.Commit, '--commit-message', $message, "--commit-dirty=$($git.Dirty.ToString().ToLower())")
if ($DryRun) {
    Write-LtStep 'Dry run: nothing uploaded. Command that would run:'
    Write-LtInfo ('wrangler ' + (($wranglerArgs | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }) -join ' '))
    Write-LtInfo "Staging folder kept for inspection: $stage"
    exit 0
}

Write-LtStep "Uploading to Cloudflare Pages ($Branch)"
$dep = Invoke-LtNative 'wrangler' $wranglerArgs
$dep.Output | ForEach-Object { Write-LtInfo $_ }
if ($dep.ExitCode -ne 0) { Write-LtErr "wrangler exited with $($dep.ExitCode)"; exit 1 }
$urlMatch = [regex]::Match(($dep.Output -join "`n"), "https://[a-z0-9]+\.$([regex]::Escape($project))\.pages\.dev")
$deployUrl = if ($urlMatch.Success) { $urlMatch.Value } else { $null }
Write-LtOk "Deployed: $deployUrl"

# 8. Post-deploy checks
Write-LtStep 'Post-deploy checks'
$failed = $false
$urls = @()
if ($deployUrl) { $urls += $deployUrl }
if ($Production) { $urls += $cfg.ProductionUrls } else { $urls += "https://$Branch.$project.pages.dev" }
foreach ($u in $urls) {
    $rr = Test-LtRemoteDeployment -BaseUrl $u
    foreach ($w in $rr.Warnings) { Write-LtWarn $w }
    foreach ($e in $rr.Errors) { Write-LtErr $e }
    if (-not $rr.Ok) { $failed = $true }
}
$after = Get-ProductionDeployment
if (-not $Production) {
    if ($after -and $after.Id -eq $prod.Id) { Write-LtOk "Production unchanged ($($prod.Id))" }
    else { Write-LtErr 'Production deployment id changed during a preview deploy!'; $failed = $true }
} elseif ($after) { Write-LtInfo "New production deployment: $($after.Id). Previous (rollback target): $($prod.Id)" }

# 9. Local history (Builds/ is ignored by Git)
$hist = Join-Path (Resolve-LtPath $cfg.BuildRoot) 'deploy-history.csv'
if (-not (Test-Path $hist)) { 'timeUtc,target,branch,build,content,deploymentUrl,previousProductionId,commit,dirty' | Set-Content -Path $hist -Encoding ASCII }
('{0},{1},{2},{3},{4},{5},{6},{7},{8}' -f (Get-Date).ToUniversalTime().ToString('s'), $target, $Branch, $layout.Name, $content, $deployUrl, $prod.Id, $git.Commit, $git.Dirty) | Add-Content -Path $hist -Encoding ASCII

if ($failed) { Write-LtErr 'Deployed, but post-deploy checks FAILED. Review above; see Documentation/WEB-DEPLOYMENT.md (Rollback).'; exit 1 }
Write-Host ''
Write-LtOk "Done. $target deployment of $($layout.Name): $deployUrl"
exit 0
