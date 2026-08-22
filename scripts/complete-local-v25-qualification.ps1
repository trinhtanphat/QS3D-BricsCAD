[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$BricsCadDir,
    [Parameter(Mandatory = $true)]
    [string]$InteractiveMatrixEvidence,
    [string]$Profile = "",
    [string]$ArtifactDir = "",
    [string]$PythonPath = "",
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
if ([string]::IsNullOrWhiteSpace($ArtifactDir)) {
    $ArtifactDir = Join-Path $repoRoot "artifacts\local-v25-qualification"
}
$ArtifactDir = [IO.Path]::GetFullPath($ArtifactDir)
$evidencePath = [IO.Path]::GetFullPath($InteractiveMatrixEvidence)
$reportPath = Join-Path $ArtifactDir "qualification.json"

$runnerArgs = @{
    BricsCadDir = $BricsCadDir
    Profile = $Profile
    ArtifactDir = $ArtifactDir
    SkipScreenshot = [bool]$SkipScreenshot
}
if (-not [string]::IsNullOrWhiteSpace($PythonPath)) { $runnerArgs.PythonPath = $PythonPath }
if ($Package) {
    $runnerArgs.Package = $true
    $runnerArgs.ReleaseTag = $ReleaseTag
}
if ($SignPackage) {
    $runnerArgs.SignPackage = $true
    $runnerArgs.SigningCertThumbprint = $SigningCertThumbprint
    $runnerArgs.TimestampUrl = $TimestampUrl
}

& (Join-Path $PSScriptRoot "run-local-v25-qualification.ps1") @runnerArgs
if (-not (Test-Path -LiteralPath $reportPath -PathType Leaf)) {
    throw "Base V25 qualification completed without qualification.json: $reportPath"
}

$report = Get-Content -LiteralPath $reportPath -Raw -Encoding UTF8 | ConvertFrom-Json
if ([string]$report.status -ne "PASS" -or [string]$report.automatedGateStatus -ne "PASS") {
    throw "Automated exact-SHA qualification gates are not PASS."
}
if ([bool]$report.runtimeSkipped -or [string]$report.runtimeSmokeStatus -ne "PASS") {
    throw "Licensed V25 runtime smoke must PASS; skipped/static execution cannot close qualification."
}
if ([string]$report.exactSha -notmatch '^[0-9A-Fa-f]{40}$') {
    throw "qualification.json does not contain a valid exact SHA."
}
if ([string]$report.pluginSha256 -notmatch '^[0-9A-Fa-f]{64}$') {
    throw "qualification.json does not contain a valid plugin SHA-256."
}

& (Join-Path $PSScriptRoot "test-local-v25-interactive-matrix-evidence.ps1") `
    -EvidencePath $evidencePath `
    -ExpectedSha ([string]$report.exactSha) `
    -ExpectedPluginSha256 ([string]$report.pluginSha256)

$evidenceDigest = (Get-FileHash -LiteralPath $evidencePath -Algorithm SHA256).Hash.ToLowerInvariant()
$stableCustomerReleaseQualified = $false
if ($Package -and $SignPackage) {
    $stableCustomerReleaseQualified = (
        [string]$report.packageStatus -eq "PASS" -and
        [string]$report.signingStatus -eq "PASS" -and
        [bool]$report.packageQualified -and
        [bool]$report.signingQualified
    )
}

$report.fullInteractiveMatrixStatus = "PASS"
$report.customerReleaseQualified = [bool]$stableCustomerReleaseQualified
$report | Add-Member -NotePropertyName licensedV25RuntimeQualified -NotePropertyValue $true -Force
$report | Add-Member -NotePropertyName interactiveMatrixEvidenceSha256 -NotePropertyValue $evidenceDigest -Force
$report | Add-Member -NotePropertyName qualificationCompletedUtc -NotePropertyValue ([DateTime]::UtcNow.ToString("O")) -Force
$scope = [string]$report.qualificationScope
if ($scope -notmatch '(?:^|\+)full-interactive-matrix(?:\+|$)') {
    $report.qualificationScope = if ([string]::IsNullOrWhiteSpace($scope)) { "full-interactive-matrix" } else { "$scope+full-interactive-matrix" }
}
$report | ConvertTo-Json -Depth 10 | Set-Content -LiteralPath $reportPath -Encoding UTF8

Write-Host ""
Write-Host ("LICENSED V25 RUNTIME QUALIFICATION: PASS for exact SHA {0}" -f ([string]$report.exactSha))
Write-Host ("Interactive matrix evidence SHA-256: {0}" -f $evidenceDigest)
if ($stableCustomerReleaseQualified) {
    Write-Host "SIGNED CUSTOMER-RELEASE PACKAGE QUALIFICATION: PASS."
}
else {
    Write-Host "Stable signed customer-release qualification is separate; use -Package -SignPackage with approved credentials when that gate is required."
}
