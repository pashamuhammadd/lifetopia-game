<#
.SYNOPSIS
  Verifies a Unity Web build folder (and optionally a live deployment) for Cloudflare Pages.

.EXAMPLE
  .\Tools\Verify-WebBuild.ps1 -BuildPath Builds/Web/M3-02
  .\Tools\Verify-WebBuild.ps1 -Url https://play.lifetopiaworld.io
  .\Tools\Verify-WebBuild.ps1 -BuildPath Builds/Web/M3-02 -ScanRepo

.NOTES
  Exit code 0 = passed, 1 = failed. Never uploads or changes anything.
#>
[CmdletBinding()]
param(
    [string]$BuildPath,
    [string[]]$Url,
    [switch]$ScanRepo,
    [switch]$Strict
)
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'LifetopiaWeb.psm1') -Force

if (-not $BuildPath -and -not $Url -and -not $ScanRepo) {
    Write-LtErr 'Give -BuildPath, -Url and/or -ScanRepo.'
    exit 1
}
$failed = $false

if ($BuildPath) {
    Write-LtStep "Verifying build folder $BuildPath"
    $r = Test-LtWebBuild -BuildPath $BuildPath
    Write-LtVerifyResult $r
    if (-not $r.Ok) { $failed = $true }
    if ($Strict -and $r.Warnings.Count -gt 0) { Write-LtErr '-Strict: warnings count as failures'; $failed = $true }
    if ($r.Ok) {
        Write-LtInfo '_headers that Deploy-Web.ps1 will generate for this build:'
        (New-LtHeadersContent -Compression $r.Layout.Compression) -split "`n" | ForEach-Object { Write-LtInfo "  | $_" }
    }
}

foreach ($u in @($Url)) {
    if (-not $u) { continue }
    Write-LtStep "Verifying live deployment $u"
    $rr = Test-LtRemoteDeployment -BaseUrl $u
    foreach ($w in $rr.Warnings) { Write-LtWarn $w }
    foreach ($e in $rr.Errors) { Write-LtErr $e }
    if ($rr.Ok) { Write-LtOk "Live deployment headers OK: $u" } else { $failed = $true }
}

if ($ScanRepo) {
    Write-LtStep 'Scanning repository files (tracked + untracked, not ignored) for secrets'
    $s = Find-LtRepoSecrets
    Write-LtInfo "$($s.Count) files scanned"
    if ($s.Hits.Count -eq 0) { Write-LtOk 'No secret patterns found' }
    else { foreach ($h in $s.Hits) { Write-LtErr $h }; $failed = $true }
}

if ($failed) { Write-Host ''; Write-LtErr 'Verification FAILED'; exit 1 }
Write-Host ''; Write-LtOk 'Verification passed'
exit 0
