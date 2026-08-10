param(
    [string]$BricsCadDir = $env:BRICSCAD_V25_DIR,
    [string]$Profile = $env:BRICSCAD_V25_PROFILE,
    [string]$ArtifactDir = "",
    [switch]$RunRuntime,
    [switch]$SkipScreenshot,
    [switch]$BuildPackage,
    [switch]$AllowDirty
)

$ErrorActionPreference = "Stop"
Set-StrictMode -Version Latest

function Invoke-NativeChecked {
    param(
        [Parameter(Mandatory = $true)][string]$FilePath,
        [Parameter(Mandatory = $true)][string[]]$Arguments
    )
    Write-Host ("> " + $FilePath + " " + [string]::Join(" ", $Arguments))
    & $FilePath @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "Command failed with exit code $LASTEXITCODE: $FilePath $([string]::Join(' ', $Arguments))"
    }
}

if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) {
    throw "Local BricsCAD V25 qualification requires Windows."
}

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location -LiteralPath $repoRoot

if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot "artifacts\local-v25-qualification"
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
New-Item -ItemType Directory -Force -Path $ArtifactDir | Out-Null

$head = (& git rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($head)) {
    throw "Unable to resolve current Git commit."
}
$branch = (& git rev-parse --abbrev-ref HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw "Unable to resolve current Git branch." }
$dirty = @(& git status --porcelain)
if ($LASTEXITCODE -ne 0) { throw "Unable to inspect Git working tree." }
if (-not $AllowDirty -and $dirty.Count -gt 0) {
    throw "Working tree is dirty. Commit/stash local changes or pass -AllowDirty only for exploratory qualification."
}

if ([string]::IsNullOrWhiteSpace($BricsCadDir)) {
    throw "BricsCadDir is required. Set BRICSCAD_V25_DIR or pass -BricsCadDir."
}
$BricsCadDir = [IO.Path]::GetFullPath($BricsCadDir)
$bricscadExe = Join-Path $BricsCadDir "bricscad.exe"
$brxMgd = Join-Path $BricsCadDir "BrxMgd.dll"
$tdMgd = Join-Path $BricsCadDir "TD_Mgd.dll"
foreach ($required in @($bricscadExe, $brxMgd, $tdMgd)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Required BricsCAD V25 file is missing: $required"
    }
}

$env:BRICSCAD_V25_DIR = $BricsCadDir
if (-not [string]::IsNullOrWhiteSpace($Profile)) {
    $env:BRICSCAD_V25_PROFILE = $Profile
}

$started = Get-Date
Write-Host "QS3D local V25 qualification"
Write-Host "Commit: $head"
Write-Host "Branch: $branch"
Write-Host "BricsCAD: $bricscadExe"
Write-Host "Artifacts: $ArtifactDir"

Invoke-NativeChecked "python" @("scripts/preflight-ci-manual-only.py")
Invoke-NativeChecked "python" @("scripts/preflight.py")
Invoke-NativeChecked "python" @("scripts/preflight-all.py")

$powerShellScripts = Get-ChildItem -LiteralPath (Join-Path $repoRoot "scripts") -Filter "*.ps1" -File | Sort-Object Name
foreach ($script in $powerShellScripts) {
    $tokens = $null
    $parseErrors = $null
    [System.Management.Automation.Language.Parser]::ParseFile($script.FullName, [ref]$tokens, [ref]$parseErrors) | Out-Null
    if ($parseErrors.Count -gt 0) {
        $parseErrors | Format-Table -AutoSize | Out-String | Write-Host
        throw "PowerShell parse failure: $($script.FullName)"
    }
}

Invoke-NativeChecked "dotnet" @("build", "src/QS3D.Core/QS3D.Core.csproj", "-c", "Release")
Invoke-NativeChecked "dotnet" @("run", "--project", "tests/QS3D.Core.SmokeTests/QS3D.Core.SmokeTests.csproj", "-c", "Release")
Invoke-NativeChecked "dotnet" @("build", "src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj", "-c", "Release", "-p:Platform=x64")

$plugin = Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.BricsCAD.V25.dll"
$core = Join-Path $repoRoot "src\QS3D.BricsCAD.V25\bin\x64\Release\net48\QS3D.Core.dll"
foreach ($required in @($plugin, $core)) {
    if (-not (Test-Path -LiteralPath $required -PathType Leaf)) {
        throw "Expected build artifact is missing: $required"
    }
}

if ($BuildPackage) {
    & (Join-Path $PSScriptRoot "package-v25.ps1")
    if ($LASTEXITCODE -ne 0) { throw "package-v25.ps1 failed with exit code $LASTEXITCODE." }
}

$runtimeArtifactDir = Join-Path $ArtifactDir "runtime"
if ($RunRuntime) {
    $runtimeArgs = @{
        BricsCadDir = $BricsCadDir
        PluginDll = $plugin
        Profile = $Profile
        ArtifactDir = $runtimeArtifactDir
    }
    if ($SkipScreenshot) { $runtimeArgs.SkipScreenshot = $true }
    & (Join-Path $PSScriptRoot "test-bricscad-v25-runtime.ps1") @runtimeArgs
}

$metadata = [ordered]@{
    status = "PASS"
    commit = $head
    branch = $branch
    dirty = ($dirty.Count -gt 0)
    started_at = $started.ToUniversalTime().ToString("O")
    completed_at = (Get-Date).ToUniversalTime().ToString("O")
    machine = $env:COMPUTERNAME
    user = [Environment]::UserName
    interactive = [Environment]::UserInteractive
    bricscad_dir = $BricsCadDir
    bricscad_file_version = (Get-Item -LiteralPath $bricscadExe).VersionInfo.FileVersion
    plugin_dll = $plugin
    plugin_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $plugin).Hash
    core_dll = $core
    core_sha256 = (Get-FileHash -Algorithm SHA256 -LiteralPath $core).Hash
    package_built = [bool]$BuildPackage
    runtime_requested = [bool]$RunRuntime
    runtime_artifact_dir = if ($RunRuntime) { $runtimeArtifactDir } else { $null }
}
$metadataPath = Join-Path $ArtifactDir "qualification-metadata.json"
$metadata | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $metadataPath -Encoding UTF8

Write-Host "QS3D local V25 qualification PASS"
Write-Host "Metadata: $metadataPath"
if ($RunRuntime) { Write-Host "Runtime evidence: $runtimeArtifactDir" }
Write-Host "This script does not dispatch GitHub Actions or publish a release."
