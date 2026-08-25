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

$MaxQualificationJsonBytes = 1048576
$StrictUtf8 = New-Object System.Text.UTF8Encoding($false, $true)

function Get-SafeInputFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][long]$MaxBytes
    )

    $resolved = [IO.Path]::GetFullPath($Path)
    if (-not (Test-Path -LiteralPath $resolved -PathType Leaf)) {
        throw "$Label does not exist as a file: $resolved"
    }

    $item = Get-Item -LiteralPath $resolved -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $resolved"
    }
    if ($item.Length -le 0 -or $item.Length -gt $MaxBytes) {
        throw "$Label must be non-empty and no larger than $MaxBytes bytes."
    }

    return $item
}

function Read-StrictUtf8File {
    param(
        [Parameter(Mandatory = $true)][IO.FileInfo]$File,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][long]$MaxBytes
    )

    $stream = $null
    try {
        $stream = New-Object IO.FileStream(
            $File.FullName,
            [IO.FileMode]::Open,
            [IO.FileAccess]::Read,
            [IO.FileShare]::Read
        )
        if ($stream.Length -le 0 -or $stream.Length -gt $MaxBytes) {
            throw "$Label changed size while opening; refusing to read $($stream.Length) bytes."
        }

        $buffer = New-Object byte[] ([int]$stream.Length)
        $offset = 0
        while ($offset -lt $buffer.Length) {
            $read = $stream.Read($buffer, $offset, $buffer.Length - $offset)
            if ($read -le 0) {
                throw "$Label ended before its validated length was read."
            }
            $offset += $read
        }
        if ($stream.ReadByte() -ne -1) {
            throw "$Label grew beyond the validated maximum while reading."
        }

        try {
            return $StrictUtf8.GetString($buffer)
        }
        catch {
            throw "$Label is not strict UTF-8: $($_.Exception.Message)"
        }
    }
    finally {
        if ($null -ne $stream) { $stream.Dispose() }
    }
}

function Write-JsonAtomically {
    param(
        [Parameter(Mandatory = $true)]$Value,
        [Parameter(Mandatory = $true)][string]$Destination
    )

    $directory = [IO.Path]::GetDirectoryName($Destination)
    $tempPath = Join-Path $directory (".qualification-{0}.tmp" -f ([Guid]::NewGuid().ToString("N")))
    try {
        $json = $Value | ConvertTo-Json -Depth 10
        $bytes = $StrictUtf8.GetBytes($json + [Environment]::NewLine)
        if ($bytes.Length -gt $MaxQualificationJsonBytes) {
            throw "Completed qualification.json would exceed $MaxQualificationJsonBytes bytes."
        }
        [IO.File]::WriteAllBytes($tempPath, $bytes)
        $temp = Get-Item -LiteralPath $tempPath -Force
        if (($temp.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or $temp.Length -ne $bytes.Length) {
            throw "Temporary qualification report is not the expected ordinary file."
        }
        [IO.File]::Replace($tempPath, $Destination, $null)
    }
    finally {
        if (Test-Path -LiteralPath $tempPath) {
            Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
        }
    }
}

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
$reportFile = Get-SafeInputFile -Path $reportPath -Label "qualification.json" -MaxBytes $MaxQualificationJsonBytes
$rawReport = Read-StrictUtf8File -File $reportFile -Label "qualification.json" -MaxBytes $MaxQualificationJsonBytes
try {
    $report = $rawReport | ConvertFrom-Json
}
catch {
    throw "qualification.json is not valid JSON: $($_.Exception.Message)"
}
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

$evidenceFile = Get-SafeInputFile -Path $evidencePath -Label "Interactive matrix evidence" -MaxBytes $MaxQualificationJsonBytes
$evidenceDigest = (Get-FileHash -LiteralPath $evidenceFile.FullName -Algorithm SHA256).Hash.ToLowerInvariant()
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
Write-JsonAtomically -Value $report -Destination $reportPath

Write-Host ""
Write-Host ("LICENSED V25 RUNTIME QUALIFICATION: PASS for exact SHA {0}" -f ([string]$report.exactSha))
Write-Host ("Interactive matrix evidence SHA-256: {0}" -f $evidenceDigest)
if ($stableCustomerReleaseQualified) {
    Write-Host "SIGNED CUSTOMER-RELEASE PACKAGE QUALIFICATION: PASS."
}
else {
    Write-Host "Stable signed customer-release qualification is separate; use -Package -SignPackage with approved credentials when that gate is required."
}
