[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSourceSha,
    [Parameter(Mandatory = $true)]
    [string]$BricsCadDir,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [string]$PythonPath = "",
    [switch]$SkipRuntime,
    [switch]$SkipScreenshot,
    [switch]$Package,
    [string]$ReleaseTag = "",
    [switch]$SignPackage,
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$SigningCertThumbprint = "",
    [ValidatePattern('^https://')]
    [string]$TimestampUrl = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$expectedSourceShaNormalized = $ExpectedSourceSha.Trim().ToLowerInvariant()

Push-Location $repoRoot
try {
    $headSha = (& git rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $headSha -notmatch '^[0-9A-Fa-f]{40}$') {
        throw "Could not resolve an exact Git HEAD SHA before local qualification."
    }
    if ($headSha.ToLowerInvariant() -ne $expectedSourceShaNormalized) {
        throw "Exact source SHA mismatch: handoff expects $expectedSourceShaNormalized but checkout is $($headSha.ToLowerInvariant()). Fetch and checkout the exact handoff SHA before qualification."
    }

    $dirty = @(& git status --porcelain)
    if ($LASTEXITCODE -ne 0) {
        throw "git status failed before local qualification."
    }
    if ($dirty.Count -gt 0) {
        throw "Working tree is dirty. Pinned local qualification requires a clean exact handoff SHA."
    }
}
finally {
    Pop-Location
}

$runnerArgs = @{
    BricsCadDir = $BricsCadDir
}
if (-not [string]::IsNullOrWhiteSpace($Profile)) { $runnerArgs.Profile = $Profile }
if (-not [string]::IsNullOrWhiteSpace($ArtifactDir)) { $runnerArgs.ArtifactDir = $ArtifactDir }
if (-not [string]::IsNullOrWhiteSpace($PythonPath)) { $runnerArgs.PythonPath = $PythonPath }
if ($SkipRuntime) { $runnerArgs.SkipRuntime = $true }
if ($SkipScreenshot) { $runnerArgs.SkipScreenshot = $true }
if ($Package) { $runnerArgs.Package = $true }
if (-not [string]::IsNullOrWhiteSpace($ReleaseTag)) { $runnerArgs.ReleaseTag = $ReleaseTag }
if ($SignPackage) {
    $runnerArgs.SignPackage = $true
    $runnerArgs.SigningCertThumbprint = $SigningCertThumbprint
    $runnerArgs.TimestampUrl = $TimestampUrl
}

& (Join-Path $PSScriptRoot "run-local-v25-qualification.ps1") @runnerArgs

$effectiveArtifactDir = if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    Join-Path $repoRoot "artifacts\local-v25-qualification"
}
else {
    [IO.Path]::GetFullPath($ArtifactDir)
}
$reportPath = Join-Path $effectiveArtifactDir "qualification.json"
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw "Pinned qualification completed without qualification.json: $reportPath"
}

$report = Get-Content -LiteralPath $reportPath -Raw | ConvertFrom-Json
if ($null -eq $report.exactSha -or [string]::IsNullOrWhiteSpace([string]$report.exactSha)) {
    throw "qualification.json does not contain exactSha; exact-source evidence cannot be accepted."
}
$reportedExactSha = ([string]$report.exactSha).Trim().ToLowerInvariant()
if ($reportedExactSha -ne $expectedSourceShaNormalized) {
    throw "qualification.json exactSha mismatch: expected $expectedSourceShaNormalized but report contains $reportedExactSha."
}

Write-Host "Pinned exact source SHA verified before and after qualification: $expectedSourceShaNormalized"
