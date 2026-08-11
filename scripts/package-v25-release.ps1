[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$packer = Join-Path $PSScriptRoot 'package-v25.ps1'
$metadataPath = Join-Path $root 'dist\QS3D-BricsCAD-V25\PACKAGE-METADATA.json'
$zipPath = Join-Path $root 'dist\QS3D-BricsCAD-V25.zip'

function Invoke-GitChecked {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = @(& git -C $root @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "Git command failed (git $($Arguments -join ' ')): $($output -join ' | ')"
    }
    return @($output | ForEach-Object { [string]$_ })
}

function Get-ExactHeadSha {
    $lines = @(Invoke-GitChecked -Arguments @('rev-parse', '--verify', 'HEAD'))
    if ($lines.Count -ne 1 -or $lines[0] -notmatch '^[0-9a-fA-F]{40}$') {
        throw "Could not resolve one exact 40-hex Git HEAD SHA."
    }
    return $lines[0].ToLowerInvariant()
}

function Assert-CleanRepository {
    param([string]$Phase)

    $status = @(Invoke-GitChecked -Arguments @('status', '--porcelain=v1', '--untracked-files=all'))
    if ($status.Count -gt 0) {
        throw "Refusing release packaging because the repository is dirty $Phase. Commit/stash/remove all tracked and untracked changes first."
    }
}

$headBefore = Get-ExactHeadSha
Assert-CleanRepository -Phase 'before package creation'

if (-not (Test-Path -LiteralPath $packer -PathType Leaf)) {
    throw "Missing canonical package helper: $packer"
}
& $packer
if ($LASTEXITCODE -ne 0) {
    throw "Canonical V25 package helper failed with exit code $LASTEXITCODE."
}

$headAfter = Get-ExactHeadSha
if (-not [string]::Equals($headBefore, $headAfter, [StringComparison]::OrdinalIgnoreCase)) {
    throw "Repository HEAD changed during release packaging. Before=$headBefore After=$headAfter. Package is not release-qualified."
}
Assert-CleanRepository -Phase 'after package creation'

if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf)) {
    throw "Canonical package metadata was not created: $metadataPath"
}
if (-not (Test-Path -LiteralPath $zipPath -PathType Leaf)) {
    throw "Canonical package ZIP was not created: $zipPath"
}
try {
    $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
}
catch {
    throw "Canonical package metadata is unreadable: $($_.Exception.Message)"
}
$metadataCommit = ([string]$metadata.gitCommit).Trim().ToLowerInvariant()
if ($metadataCommit -notmatch '^[0-9a-f]{40}$') {
    throw "PACKAGE-METADATA gitCommit is missing or invalid: '$metadataCommit'."
}
if (-not [string]::Equals($metadataCommit, $headBefore, [StringComparison]::OrdinalIgnoreCase)) {
    throw "PACKAGE-METADATA gitCommit $metadataCommit does not match the exact clean package source HEAD $headBefore."
}

Write-Host "Release package provenance verified: $headBefore"
