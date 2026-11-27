<#
.SYNOPSIS
  Builds the Lifetopia World Web player with Unity 6000.3.29f1 in batch mode, safely.

.DESCRIPTION
  - Refuses to run while a Unity Editor has this project open, or while another build runs.
  - Refuses to start when free RAM is below MinFreeMemoryGB (8 GB laptop) unless -IgnoreLowMemory.
  - Builds into Builds/Web/_incoming/<Name>, writes version.json, verifies the result and only then
    moves it to Builds/Web/<Name>. A failed build never replaces the last good build.
  - Variant Local  : GameplayPrototype scene with local artwork (active Web build profile, unchanged).
    Variant Public : FirstPlayable placeholder scene (what the public repository can build).

.EXAMPLE
  .\Tools\Build-Web.ps1                      # Local variant, name like M3-02-20261126-1530
  .\Tools\Build-Web.ps1 -Variant Public
  .\Tools\Build-Web.ps1 -Name M3-02-rc1 -Milestone M3.2 -GameVersion 0.3.2
  .\Tools\Build-Web.ps1 -DryRun              # checks only, prints the Unity command

.NOTES
  Exit code 0 = success (last line prints the build folder), 1 = failure.
#>
[CmdletBinding()]
param(
    [ValidateSet('Local', 'Public')][string]$Variant = 'Local',
    [string]$Name,
    [string]$GameVersion,
    [string]$Milestone,
    [switch]$Replace,
    [switch]$IgnoreLowMemory,
    [int]$TimeoutMinutes = 60,
    [switch]$DryRun
)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'LifetopiaWeb.psm1') -Force
$cfg = Get-LtConfig
$root = Get-LtProjectRoot
Set-Location $root

if (-not $GameVersion) { $GameVersion = $cfg.GameVersion }
if (-not $Milestone) { $Milestone = $cfg.Milestone }
if (-not $Name) { $Name = ConvertTo-LtBuildName $Milestone }
if ($Name -notmatch '^[A-Za-z0-9][A-Za-z0-9._-]{0,63}$') { Write-LtErr "Invalid build name '$Name' (letters, digits, . _ - only)"; exit 1 }

function Stop-Build([string]$Message) { Write-LtErr $Message; Write-Host ''; Write-LtErr 'BUILD ABORTED'; exit 1 }

Write-LtStep "Lifetopia Web build: $Name ($Variant variant, v$GameVersion $Milestone)"

# 1. Unity version and executable
$projVersion = ([regex]::Match((Get-Content -Raw 'ProjectSettings/ProjectVersion.txt'), 'm_EditorVersion:\s*(\S+)')).Groups[1].Value
if ($projVersion -ne $cfg.UnityVersion) { Stop-Build "Project uses Unity $projVersion but config expects $($cfg.UnityVersion)" }
if (-not (Test-Path -LiteralPath $cfg.UnityPath)) { Stop-Build "Unity $($cfg.UnityVersion) not found at $($cfg.UnityPath) (set LIFETOPIA_UNITY_PATH)" }
Write-LtOk "Unity $projVersion"

# 2. No Editor on this project, no other build
if (Test-LtUnityProjectOpen) { Stop-Build 'A Unity Editor has this project open. Save your work, close Unity, then run again.' }
Write-LtOk 'Unity Editor is not using this project'
$buildRoot = Resolve-LtPath $cfg.BuildRoot
if (-not (Test-Path $buildRoot)) { New-Item -ItemType Directory -Path $buildRoot | Out-Null }
$lockPath = Join-Path $buildRoot '.build.lock'
$lock = $null
try { $lock = [System.IO.File]::Open($lockPath, 'OpenOrCreate', 'ReadWrite', 'None') }
catch { Stop-Build 'Another Lifetopia build is running (Builds/Web/.build.lock is in use).' }

try {
    # 3. Memory
    $free = Get-LtFreeMemoryGB
    if ($free -lt $cfg.MinFreeMemoryGB) {
        if ($IgnoreLowMemory) { Write-LtWarn "Only $free GB RAM free (recommended $($cfg.MinFreeMemoryGB) GB). Continuing because -IgnoreLowMemory." }
        else { Stop-Build "Only $free GB RAM free; need $($cfg.MinFreeMemoryGB) GB. Close the browser/Editor/other apps (or use -IgnoreLowMemory)." }
    } else { Write-LtOk "$free GB RAM free" }

    # 4. Scene
    if ($Variant -eq 'Local') {
        $scene = $cfg.LocalScene
        if (-not (Test-Path -LiteralPath $scene)) { Stop-Build "Local scene $scene is not present (it is local-only). Use -Variant Public." }
        $first = Get-LtProfileFirstScene $cfg.BuildProfile
        if ($first -ne $scene) { Stop-Build "The Web build profile's first scene is '$first', expected '$scene'. Fix the local build profile scene list first." }
        $contentMode = 'local-art'
    } else {
        $scene = $cfg.PublicScene
        if (-not (Test-Path -LiteralPath $scene)) { Stop-Build "Public scene $scene not found" }
        $contentMode = 'public-placeholder'
    }
    Write-LtOk "Main scene: $scene ($contentMode)"

    # 5. Folders
    $incoming = Join-Path $buildRoot "_incoming\$Name"
    $final = Join-Path $buildRoot $Name
    $logDir = Join-Path $buildRoot '_logs'
    $logFile = Join-Path $logDir "$Name.log"
    if ((Test-Path -LiteralPath $final) -and -not $Replace) { Stop-Build "Builds/Web/$Name already exists. Choose another -Name or pass -Replace (old one is kept in Builds/Web/_previous)." }

    $unityArgs = @('-batchmode', '-quit', '-projectPath', $root, '-activeBuildProfile', $cfg.BuildProfile, '-logFile', $logFile)
    $relIncoming = "$($cfg.BuildRoot)/_incoming/$Name"
    if ($Variant -eq 'Local') { $unityArgs += @('-build', $relIncoming) }
    else {
        $unityArgs += @('-executeMethod', 'Lifetopia.EditorTools.WebBuildCommand.Build', '-ltScene', $scene, '-ltOutput', $relIncoming)
        if ($cfg.PublicExtraScenes) {
            foreach ($extraScene in $cfg.PublicExtraScenes) { if (-not (Test-Path -LiteralPath $extraScene)) { Stop-Build "Extra scene $extraScene not found" } }
            $unityArgs += @('-ltExtraScenes', ($cfg.PublicExtraScenes -join ';'))
            Write-LtOk "Extra scenes: $($cfg.PublicExtraScenes -join ', ')"
        }
    }

    if ($DryRun) {
        Write-LtStep 'Dry run: Unity was NOT started'
        Write-LtInfo ('"{0}" {1}' -f $cfg.UnityPath, (($unityArgs | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }) -join ' '))
        exit 0
    }

    foreach ($d in @($incoming, $logDir)) { if (-not (Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null } }
    if (Test-Path -LiteralPath $incoming) { Remove-Item -LiteralPath $incoming -Recurse -Force }

    # 6. Build
    Write-LtStep "Running Unity batch build (log: $($cfg.BuildRoot)/_logs/$Name.log). This takes 10-20 minutes."
    $quoted = $unityArgs | ForEach-Object { if ($_ -match '\s') { "`"$_`"" } else { $_ } }
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    $proc = Start-Process -FilePath $cfg.UnityPath -ArgumentList $quoted -PassThru -WindowStyle Hidden
    $minFree = 99.0
    while (-not $proc.WaitForExit(15000)) {
        $f = Get-LtFreeMemoryGB; if ($f -lt $minFree) { $minFree = $f }
        Write-LtInfo ('{0:mm\:ss} elapsed, {1} GB RAM free' -f $sw.Elapsed, $f)
        if ($sw.Elapsed.TotalMinutes -gt $TimeoutMinutes) { $proc.Kill(); Stop-Build "Build exceeded $TimeoutMinutes minutes and was stopped." }
    }
    $sw.Stop()
    $exit = $proc.ExitCode
    Write-LtInfo ('Unity exited with code {0} after {1:mm\:ss} (lowest free RAM {2} GB)' -f $exit, $sw.Elapsed, $minFree)

    $log = if (Test-Path -LiteralPath $logFile) { Get-Content -Raw -LiteralPath $logFile } else { '' }
    $compileErrors = @([regex]::Matches($log, '(?m)^.*error CS\d{4}.*$') | ForEach-Object { $_.Value.Trim() } | Select-Object -Unique)
    if ($compileErrors.Count -gt 0) { $compileErrors | Select-Object -First 10 | ForEach-Object { Write-LtErr $_ }; Stop-Build 'Compilation failed.' }
    if ($log -match 'Scripts have compiler errors') { Stop-Build 'Compilation failed (Scripts have compiler errors).' }
    $succeeded = ($log -match 'Build Finished, Result: Success') -or ($log -match '\[LifetopiaBuild\] Result=Succeeded')
    if ($exit -ne 0 -or -not $succeeded) { Stop-Build "Unity build failed (exit $exit). See $logFile" }
    Write-LtOk 'Unity build succeeded'

    # 7. version.json
    $git = Get-LtGitInfo
    $version = [ordered]@{
        product     = 'Lifetopia World'
        channel     = $cfg.Channel
        version     = $GameVersion
        milestone   = $Milestone
        buildName   = $Name
        buildDate   = (Get-Date).ToUniversalTime().ToString('yyyy-MM-ddTHH:mm:ssZ')
        commit      = $git.Commit
        dirty       = $git.Dirty
        contentMode = $contentMode
        unity       = $projVersion
    }
    [System.IO.File]::WriteAllText((Join-Path $incoming 'version.json'), ($version | ConvertTo-Json) + "`n", (New-Object System.Text.UTF8Encoding($false)))

    # 8. Verify before promoting
    Write-LtStep 'Verifying new build'
    $r = Test-LtWebBuild -BuildPath $incoming
    Write-LtVerifyResult $r
    if (-not $r.Ok) { Stop-Build "New build failed verification; it stays in $($cfg.BuildRoot)/_incoming/$Name and the previous build is untouched." }

    # 9. Promote
    if (Test-Path -LiteralPath $final) {
        $prevDir = Join-Path $buildRoot '_previous'
        if (-not (Test-Path $prevDir)) { New-Item -ItemType Directory -Path $prevDir | Out-Null }
        $kept = Join-Path $prevDir ("{0}-{1}" -f $Name, (Get-Date -Format 'yyyyMMdd-HHmmss'))
        Move-Item -LiteralPath $final -Destination $kept
        Write-LtInfo "Previous Builds/Web/$Name kept as $kept"
    }
    Move-Item -LiteralPath $incoming -Destination $final
    [System.IO.File]::WriteAllText((Join-Path $buildRoot 'last-successful-build.txt'), "$($cfg.BuildRoot)/$Name`n")
    Write-LtOk ('Build ready: {0}/{1} ({2:N2} MiB)' -f $cfg.BuildRoot, $Name, ($r.Layout.TotalBytes / 1MB))
    Write-Output "$($cfg.BuildRoot)/$Name"
    exit 0
}
finally {
    if ($lock) { $lock.Close() }
}
