# Shared helpers for Lifetopia World Web build, verification and Cloudflare Pages deployment.
# Windows PowerShell 5.1 compatible. ASCII only.

Set-StrictMode -Version 2.0

function Get-LtProjectRoot {
    return (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
}

function Get-LtConfig {
    $cfg = Import-PowerShellDataFile -Path (Join-Path $PSScriptRoot 'WebDeploy.config.psd1')
    if ($env:LIFETOPIA_UNITY_PATH) { $cfg.UnityPath = $env:LIFETOPIA_UNITY_PATH }
    return $cfg
}

function Resolve-LtPath([string]$Path) {
    if ([System.IO.Path]::IsPathRooted($Path)) { return [System.IO.Path]::GetFullPath($Path) }
    return [System.IO.Path]::GetFullPath((Join-Path (Get-LtProjectRoot) $Path))
}

function Write-LtStep([string]$Text) { Write-Host ''; Write-Host "==> $Text" -ForegroundColor Cyan }
function Write-LtOk([string]$Text)   { Write-Host "  [OK]   $Text" -ForegroundColor Green }
function Write-LtWarn([string]$Text) { Write-Host "  [WARN] $Text" -ForegroundColor Yellow }
function Write-LtErr([string]$Text)  { Write-Host "  [FAIL] $Text" -ForegroundColor Red }
function Write-LtInfo([string]$Text) { Write-Host "         $Text" }

# Runs a native command without PowerShell 5.1 turning stderr lines into terminating errors.
function Invoke-LtNative {
    param([string]$FilePath, [string[]]$Arguments)
    $ErrorActionPreference = 'Continue'
    $output = & $FilePath @Arguments 2>&1 | ForEach-Object { "$_" }
    return [pscustomobject]@{ ExitCode = $LASTEXITCODE; Output = @($output) }
}

function Get-LtGitInfo {
    $root = Get-LtProjectRoot
    $sha = (Invoke-LtNative 'git' @('-C', $root, 'rev-parse', '--short', 'HEAD')).Output | Select-Object -First 1
    $status = (Invoke-LtNative 'git' @('-C', $root, 'status', '--porcelain')).Output
    return [pscustomobject]@{ Commit = "$sha".Trim(); Dirty = (@($status | Where-Object { $_ }).Count -gt 0) }
}

function Get-LtFreeMemoryGB {
    $os = Get-CimInstance Win32_OperatingSystem
    return [math]::Round($os.FreePhysicalMemory / 1MB, 2)
}

# True when a Unity Editor (interactive or batch) currently has this project open.
function Test-LtUnityProjectOpen {
    $root = Get-LtProjectRoot
    $lock = Join-Path $root 'Temp\UnityLockfile'
    if (Test-Path $lock) {
        try {
            $fs = [System.IO.File]::Open($lock, 'Open', 'ReadWrite', 'None')
            $fs.Close()
        } catch {
            return $true
        }
    }
    $needle = $root.TrimEnd('\').Replace('\', '/').ToLowerInvariant()
    $procs = Get-CimInstance Win32_Process -Filter "Name='Unity.exe'" -ErrorAction SilentlyContinue
    foreach ($p in @($procs)) {
        if (-not $p.CommandLine) { continue }
        $cmd = $p.CommandLine.Replace('\', '/').ToLowerInvariant()
        if ($cmd.Contains('-projectpath') -and $cmd.Contains($needle)) { return $true }
    }
    return $false
}

function Get-LtProfileFirstScene([string]$ProfilePath) {
    $text = Get-Content -Raw -Path (Resolve-LtPath $ProfilePath)
    $m = [regex]::Match($text, 'm_Scenes:\s*\r?\n\s*-\s*m_enabled:\s*1\s*\r?\n\s*m_path:\s*(\S+)')
    if ($m.Success) { return $m.Groups[1].Value }
    return $null
}

function ConvertTo-LtBuildName([string]$Milestone) {
    $m = [regex]::Match($Milestone, '^M(\d+)\.(\d+)$')
    $base = if ($m.Success) { 'M{0}-{1:00}' -f [int]$m.Groups[1].Value, [int]$m.Groups[2].Value } else { ($Milestone -replace '[^A-Za-z0-9-]', '-') }
    return '{0}-{1}' -f $base, (Get-Date -Format 'yyyyMMdd-HHmm')
}

# ---------------------------------------------------------------- build verification

$script:LtAllowedRoot = @('index.html', 'version.json', 'favicon.svg', 'favicon.ico', '404.html', '_headers', 'robots.txt')
$script:LtForbiddenExt = @('.log', '.pdb', '.bak', '.meta', '.cs', '.unity', '.prefab', '.asset', '.mat', '.psd', '.kra', '.blend',
                           '.zip', '.7z', '.rar', '.env', '.pem', '.key', '.pfx', '.p12', '.map', '.ps1', '.psm1', '.psd1', '.md')
$script:LtSecretPatterns = @(
    @{ Name = 'GitHub token';           Regex = '(ghp|gho|ghu|ghs|ghr)_[A-Za-z0-9]{30,}|github_pat_[A-Za-z0-9_]{30,}' },
    @{ Name = 'Cloudflare token var';   Regex = 'CLOUDFLARE_API_(TOKEN|KEY)\s*[=:]\s*\S{8,}' },
    @{ Name = 'Private key block';      Regex = '-----BEGIN [A-Z ]*PRIVATE KEY-----' },
    @{ Name = 'AWS access key';         Regex = 'AKIA[0-9A-Z]{16}' },
    @{ Name = 'Slack token';            Regex = 'xox[baprs]-[A-Za-z0-9-]{10,}' },
    @{ Name = 'OpenAI/Anthropic key';   Regex = 'sk-(ant-)?[A-Za-z0-9_-]{32,}' },
    @{ Name = 'JWT';                    Regex = 'eyJ[A-Za-z0-9_-]{10,}\.eyJ[A-Za-z0-9_-]{10,}\.[A-Za-z0-9_-]{10,}' },
    @{ Name = 'Supabase service role';  Regex = 'service_role[''"]?\s*[:=]\s*[''"]eyJ' }
)

function Find-LtSecrets {
    param([string[]]$Files, [string]$BasePath)
    $hits = @()
    foreach ($f in $Files) {
        $info = Get-Item -LiteralPath $f -ErrorAction SilentlyContinue
        if ($null -eq $info -or $info.Length -gt 5MB) { continue }
        $text = $null
        try { $text = [System.IO.File]::ReadAllText($f) } catch { continue }
        if ($text.IndexOf([char]0) -ge 0) { continue }   # binary
        foreach ($p in $script:LtSecretPatterns) {
            if ([regex]::IsMatch($text, $p.Regex)) {
                $rel = if ($BasePath) { $f.Substring($BasePath.Length).TrimStart('\', '/') } else { $f }
                $hits += "$($p.Name) pattern in $rel"
            }
        }
    }
    return $hits
}

function Get-LtBuildLayout {
    param([string]$BuildPath)
    $root = Resolve-LtPath $BuildPath
    $layout = [ordered]@{
        Root = $root; Name = $null; Compression = $null; Loader = $null; Framework = $null; Wasm = $null; Data = $null
        Files = @(); Errors = @(); Warnings = @(); Version = $null; TotalBytes = 0
    }
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { $layout.Errors += "Build folder not found: $BuildPath"; return [pscustomobject]$layout }

    $files = @(Get-ChildItem -LiteralPath $root -Recurse -File -Force)
    foreach ($f in $files) {
        $rel = $f.FullName.Substring($root.Length).TrimStart('\').Replace('\', '/')
        $layout.Files += [pscustomobject]@{ Rel = $rel; Full = $f.FullName; Bytes = $f.Length }
        $layout.TotalBytes += $f.Length
    }

    $buildFiles = @($layout.Files | Where-Object { $_.Rel -like 'Build/*' })
    $loader = @($buildFiles | Where-Object { $_.Rel -match '^Build/[^/]+\.loader\.js$' })
    if ($loader.Count -ne 1) { $layout.Errors += "Expected exactly 1 Build/*.loader.js, found $($loader.Count)" }
    else {
        $layout.Loader = $loader[0]
        $layout.Name = [regex]::Match($loader[0].Rel, '^Build/(.+)\.loader\.js$').Groups[1].Value
    }
    if ($layout.Name) {
        $n = [regex]::Escape($layout.Name)
        foreach ($part in @(@{ Key = 'Framework'; Pat = "^Build/$n\.framework\.js(\.br|\.gz|\.unityweb)?$" },
                            @{ Key = 'Wasm';      Pat = "^Build/$n\.wasm(\.br|\.gz|\.unityweb)?$" },
                            @{ Key = 'Data';      Pat = "^Build/$n\.data(\.br|\.gz|\.unityweb)?$" })) {
            $hit = @($buildFiles | Where-Object { $_.Rel -match $part.Pat })
            if ($hit.Count -ne 1) { $layout.Errors += "Missing or duplicate Build/$($layout.Name).$($part.Key.ToLower()) file (found $($hit.Count))" }
            else { $layout[$part.Key] = $hit[0] }
        }
        if ($layout.Framework -and $layout.Wasm -and $layout.Data) {
            $exts = @($layout.Framework, $layout.Wasm, $layout.Data) | ForEach-Object { [regex]::Match($_.Rel, '\.(br|gz|unityweb)$').Groups[1].Value }
            $unique = @($exts | Select-Object -Unique)
            if ($unique.Count -ne 1) { $layout.Errors += "Mixed compression formats: $($exts -join ', ')" }
            elseif ($unique[0] -eq 'unityweb') { $layout.Errors += 'Decompression-fallback (.unityweb) builds are not supported by this pipeline; use Brotli or Gzip without fallback.' }
            else { $layout.Compression = if ($unique[0]) { $unique[0] } else { 'none' } }
        }
    }
    return [pscustomobject]$layout
}

function Test-LtWebBuild {
    <#
      Validates a Unity Web build folder for Cloudflare Pages. Returns an object with Ok, Errors, Warnings, Layout.
      -Staged: the folder is an upload staging folder (must already contain _headers and 404.html).
    #>
    param([string]$BuildPath, [switch]$Staged)
    $cfg = Get-LtConfig
    $L = Get-LtBuildLayout -BuildPath $BuildPath
    $errors = New-Object System.Collections.ArrayList
    $warnings = New-Object System.Collections.ArrayList
    foreach ($e in $L.Errors) { [void]$errors.Add($e) }
    if (-not (Test-Path -LiteralPath $L.Root)) { return [pscustomobject]@{ Ok = $false; Errors = @($errors); Warnings = @(); Layout = $L } }

    # index.html
    $indexPath = Join-Path $L.Root 'index.html'
    if (-not (Test-Path -LiteralPath $indexPath)) { [void]$errors.Add('index.html is missing') }
    elseif ($L.Name) {
        $html = Get-Content -Raw -LiteralPath $indexPath
        foreach ($f in @($L.Loader, $L.Framework, $L.Wasm, $L.Data)) {
            if ($f -and $html.IndexOf([System.IO.Path]::GetFileName($f.Rel)) -lt 0) { [void]$errors.Add("index.html does not reference $($f.Rel)") }
        }
        if ($html -notmatch 'content="LifetopiaResponsive"') {
            if ($html -match 'Please rotate your device') { [void]$warnings.Add('Built with an older LifetopiaResponsive template (no version info / favicon). Rebuild to get them.') }
            else { [void]$errors.Add('index.html was not generated from the LifetopiaResponsive template (wrong build profile?)') }
        }
        $title = [regex]::Match($html, '<title>([^<]*)</title>').Groups[1].Value
        if ($title -ne 'Lifetopia World') { [void]$warnings.Add("Browser title is '$title' (expected 'Lifetopia World')") }
    }

    # Allowed files only, Cloudflare limits
    foreach ($f in $L.Files) {
        $ext = [System.IO.Path]::GetExtension($f.Rel).ToLowerInvariant()
        $allowed = ($script:LtAllowedRoot -contains $f.Rel) -or
                   ($L.Name -and ($f.Rel -like "Build/$($L.Name).*") -and (@($L.Loader, $L.Framework, $L.Wasm, $L.Data) | Where-Object { $_ -and $_.Rel -eq $f.Rel })) -or
                   ($f.Rel -like 'TemplateData/*') -or ($f.Rel -like 'StreamingAssets/*')
        if ($f.Rel -like '*.symbols.json*') { $allowed = $false }
        if ($script:LtForbiddenExt -contains $ext) { [void]$errors.Add("Forbidden file type in build: $($f.Rel)") }
        elseif (-not $allowed) { [void]$errors.Add("Unexpected file in build (not a Web runtime file): $($f.Rel)") }
        if ($f.Bytes -gt $cfg.MaxFileBytes) { [void]$errors.Add(('{0} is {1:N1} MiB; Cloudflare Pages limit is {2:N0} MiB per file' -f $f.Rel, ($f.Bytes / 1MB), ($cfg.MaxFileBytes / 1MB))) }
        if ($f.Bytes -eq 0 -and $f.Rel -like 'Build/*') { [void]$errors.Add("Empty build file: $($f.Rel)") }
    }
    if ($L.Files.Count -gt $cfg.MaxFileCount) { [void]$errors.Add("Too many files: $($L.Files.Count) (limit $($cfg.MaxFileCount))") }
    if (@($L.Files | Where-Object { $_.Rel -like 'StreamingAssets/*' }).Count -gt 0) { [void]$warnings.Add('StreamingAssets present: review its contents before publishing.') }

    # version.json
    $verPath = Join-Path $L.Root 'version.json'
    if (Test-Path -LiteralPath $verPath) {
        try {
            $v = Get-Content -Raw -LiteralPath $verPath | ConvertFrom-Json
            foreach ($k in @('product', 'channel', 'version', 'milestone', 'buildDate', 'contentMode')) {
                if (-not ($v.PSObject.Properties.Name -contains $k)) { [void]$errors.Add("version.json is missing '$k'") }
            }
            $L.Version = $v
        } catch { [void]$errors.Add('version.json is not valid JSON') }
    } else { [void]$warnings.Add('version.json missing (legacy build): content mode unknown, no version on loading screen.') }

    if ($Staged) {
        if (-not (Test-Path -LiteralPath (Join-Path $L.Root '_headers'))) { [void]$errors.Add('Staged folder has no _headers') }
        if (-not (Test-Path -LiteralPath (Join-Path $L.Root '404.html'))) { [void]$errors.Add('Staged folder has no 404.html') }
    }

    # Secret scan of readable text files (compressed .br/.gz files are skipped)
    $textFiles = @($L.Files | Where-Object { $_.Rel -match '\.(html|js|json|svg|css|txt)$' -or $_.Rel -eq '_headers' } | ForEach-Object { $_.Full })
    foreach ($hit in (Find-LtSecrets -Files $textFiles -BasePath $L.Root)) { [void]$errors.Add("Possible secret: $hit") }

    return [pscustomobject]@{ Ok = ($errors.Count -eq 0); Errors = @($errors); Warnings = @($warnings); Layout = $L }
}

function Write-LtVerifyResult($Result) {
    $L = $Result.Layout
    if ($L.Name) {
        Write-LtInfo ('Build name: {0} | compression: {1} | files: {2} | total {3:N2} MiB' -f $L.Name, $L.Compression, $L.Files.Count, ($L.TotalBytes / 1MB))
        foreach ($f in @($L.Loader, $L.Framework, $L.Wasm, $L.Data)) { if ($f) { Write-LtInfo ('  {0,-40} {1,12:N0} bytes' -f $f.Rel, $f.Bytes) } }
    }
    if ($L.Version) { Write-LtInfo ("version.json: v{0} {1} {2} content={3} commit={4}" -f $L.Version.version, $L.Version.milestone, $L.Version.buildDate, $L.Version.contentMode, $L.Version.commit) }
    foreach ($w in $Result.Warnings) { Write-LtWarn $w }
    foreach ($e in $Result.Errors) { Write-LtErr $e }
    if ($Result.Ok) { Write-LtOk 'Build verification passed' } else { Write-LtErr "Build verification FAILED ($($Result.Errors.Count) error(s))" }
}

# ---------------------------------------------------------------- Cloudflare files

function New-LtHeadersContent {
    param([ValidateSet('br', 'gz', 'none')][string]$Compression)
    $lines = New-Object System.Collections.Generic.List[string]
    $lines.Add("# Generated by Tools/Deploy-Web.ps1 for a Unity Web build with compression '$Compression'. Do not edit by hand.")
    $lines.Add('/*')
    $lines.Add('  Cache-Control: no-cache, no-transform')
    $lines.Add('  X-Robots-Tag: noindex')
    $lines.Add('  X-Content-Type-Options: nosniff')
    $lines.Add('  Referrer-Policy: strict-origin-when-cross-origin')
    $lines.Add('')
    $types = @(@('wasm', 'application/wasm'), @('framework.js', 'application/javascript'), @('data', 'application/octet-stream'))
    $enc = switch ($Compression) { 'br' { 'br' } 'gz' { 'gzip' } default { $null } }
    foreach ($t in $types) {
        $suffix = if ($Compression -eq 'none') { '' } else { ".$Compression" }
        $lines.Add("/Build/*.$($t[0])$suffix")
        $lines.Add("  Content-Type: $($t[1])")
        if ($enc) { $lines.Add("  Content-Encoding: $enc") }
        $lines.Add('')
    }
    return ($lines -join "`n")
}

function New-LtStagingFolder {
    param($Layout, [string]$StagePath)
    if (Test-Path -LiteralPath $StagePath) { Remove-Item -LiteralPath $StagePath -Recurse -Force }
    New-Item -ItemType Directory -Path $StagePath | Out-Null
    foreach ($f in $Layout.Files) {
        if ($f.Rel -eq '_headers') { continue }   # always regenerated
        $dest = Join-Path $StagePath ($f.Rel.Replace('/', '\'))
        $dir = Split-Path $dest -Parent
        if (-not (Test-Path -LiteralPath $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
        Copy-Item -LiteralPath $f.Full -Destination $dest
    }
    $headers = New-LtHeadersContent -Compression $Layout.Compression
    [System.IO.File]::WriteAllText((Join-Path $StagePath '_headers'), $headers + "`n", (New-Object System.Text.UTF8Encoding($false)))
    if (-not (Test-Path -LiteralPath (Join-Path $StagePath '404.html'))) {
        Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Web\404.html') -Destination (Join-Path $StagePath '404.html')
    }
}

# ---------------------------------------------------------------- remote checks

function Get-LtRemoteHeaders {
    param([string]$Url, [string]$Method = 'HEAD')
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $req = [System.Net.HttpWebRequest]::Create($Url)
    $req.Method = $Method
    $req.AllowAutoRedirect = $false
    $req.AutomaticDecompression = [System.Net.DecompressionMethods]::None
    $req.Headers.Add('Accept-Encoding', 'br, gzip')
    $req.UserAgent = 'LifetopiaDeployCheck/1.0'
    $req.Timeout = 30000
    $resp = $null
    try { $resp = $req.GetResponse() }
    catch [System.Net.WebException] { $resp = $_.Exception.Response; if ($null -eq $resp) { return [pscustomobject]@{ Status = 0; Headers = @{}; Error = $_.Exception.Message } } }
    $h = @{}
    foreach ($k in $resp.Headers.AllKeys) { $h[$k.ToLowerInvariant()] = $resp.Headers[$k] }
    $status = [int]$resp.StatusCode
    $resp.Close()
    return [pscustomobject]@{ Status = $status; Headers = $h; Error = $null }
}

function Test-LtRemoteDeployment {
    <# Checks a deployed site: index, Brotli/Gzip headers of the runtime files, caching, 404 behaviour. #>
    param([string]$BaseUrl)
    $BaseUrl = $BaseUrl.TrimEnd('/')
    $errors = @(); $warnings = @()
    Write-LtInfo "Checking $BaseUrl"
    $index = Get-LtRemoteHeaders "$BaseUrl/"
    if ($index.Status -ne 200) { return [pscustomobject]@{ Ok = $false; Errors = @("GET / returned $($index.Status) $($index.Error)"); Warnings = @() } }
    Write-LtInfo ('  /  -> {0} {1} cache="{2}"' -f $index.Status, $index.Headers['content-type'], $index.Headers['cache-control'])

    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $html = (New-Object System.Net.WebClient).DownloadString("$BaseUrl/")
    $title = [regex]::Match($html, '<title>([^<]*)</title>').Groups[1].Value
    Write-LtInfo "  title: $title"
    $names = @([regex]::Matches($html, '"/?([^"/]+\.(?:loader\.js|framework\.js(?:\.br|\.gz)?|wasm(?:\.br|\.gz)?|data(?:\.br|\.gz)?))"') | ForEach-Object { $_.Groups[1].Value } | Select-Object -Unique)
    if ($names.Count -lt 4) { $errors += "Could not find 4 Unity build files in index.html (found $($names.Count))" }
    foreach ($n in $names) {
        $r = Get-LtRemoteHeaders "$BaseUrl/Build/$n"
        $ct = $r.Headers['content-type']; $ce = $r.Headers['content-encoding']
        Write-LtInfo ('  Build/{0,-34} -> {1} type={2} encoding={3} length={4}' -f $n, $r.Status, $ct, $ce, $r.Headers['content-length'])
        if ($r.Status -ne 200) { $errors += "Build/$n returned $($r.Status)"; continue }
        $expectEnc = if ($n -like '*.br') { 'br' } elseif ($n -like '*.gz') { 'gzip' } else { $null }
        if ($expectEnc -and $ce -ne $expectEnc) { $errors += "Build/$n Content-Encoding '$ce' (expected '$expectEnc')" }
        if ($n -match '\.wasm(\.br|\.gz)?$' -and $ct -notlike 'application/wasm*') { $errors += "Build/$n Content-Type '$ct' (expected application/wasm)" }
        if ($n -match '\.js(\.br|\.gz)?$' -and $ct -notlike '*javascript*') { $errors += "Build/$n Content-Type '$ct' (expected JavaScript)" }
        if ($ct -like 'text/html*') { $errors += "Build/$n served as HTML (file missing, SPA fallback)" }
    }
    $probe = Get-LtRemoteHeaders "$BaseUrl/__lifetopia_missing_$(Get-Random).txt"
    if ($probe.Status -eq 404) { Write-LtInfo '  missing path -> 404 (OK)' }
    else { $warnings += "Missing paths return $($probe.Status) instead of 404 (no 404.html in this deployment)" }
    $v = Get-LtRemoteHeaders "$BaseUrl/version.json"
    if ($v.Status -eq 200 -and $v.Headers['content-type'] -like 'application/json*') { Write-LtInfo '  version.json -> 200' }
    else { $warnings += 'version.json not served (legacy build)' }
    return [pscustomobject]@{ Ok = ($errors.Count -eq 0); Errors = $errors; Warnings = $warnings }
}

# ---------------------------------------------------------------- repository scan

function Find-LtRepoSecrets {
    $root = Get-LtProjectRoot
    $list = (Invoke-LtNative 'git' @('-C', $root, 'ls-files', '-co', '--exclude-standard')).Output
    $files = @($list | Where-Object { $_ -and ($_ -notmatch '\.(png|jpg|jpeg|psd|wav|mp3|ogg|ttf|otf|dll|so|a|unitypackage)$') } |
               ForEach-Object { Join-Path $root $_ } | Where-Object { Test-Path -LiteralPath $_ -PathType Leaf })
    return [pscustomobject]@{ Count = $files.Count; Hits = @(Find-LtSecrets -Files $files -BasePath $root) }
}

Export-ModuleMember -Function *-Lt*
