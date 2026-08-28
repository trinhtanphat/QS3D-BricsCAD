[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$SnapshotDir,
    [Parameter(Mandatory = $true)][string]$StatePath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$requiredNames = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$maxStateBytes = 32768

function Get-CanonicalAbsolutePath {
    param([Parameter(Mandatory = $true)][string]$Path)

    $full = [IO.Path]::GetFullPath($Path)
    $root = [IO.Path]::GetPathRoot($full)
    if ([string]::IsNullOrWhiteSpace($root)) { throw "Path has no filesystem root: $Path" }
    if ($full.Length -gt $root.Length) {
        return $full.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    }
    return $full
}

function Test-CanonicalPathWithin {
    param(
        [Parameter(Mandatory = $true)][string]$Candidate,
        [Parameter(Mandatory = $true)][string]$Parent
    )

    $candidateFull = Get-CanonicalAbsolutePath -Path $Candidate
    $parentFull = Get-CanonicalAbsolutePath -Path $Parent
    if ([string]::Equals($candidateFull, $parentFull, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    $prefix = $parentFull.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    return $candidateFull.StartsWith($prefix, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-NoExistingReparseComponent {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $canonical = Get-CanonicalAbsolutePath -Path $Path
    $root = [IO.Path]::GetPathRoot($canonical)
    $relative = $canonical.Substring($root.Length)
    $current = $root
    foreach ($segment in @($relative -split '[\\/]' | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })) {
        $current = Join-Path $current $segment
        if (-not (Test-Path -LiteralPath $current)) { break }
        $item = Get-Item -LiteralPath $current -Force
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must not traverse a filesystem reparse point: $current"
        }
    }
}

function Assert-OrdinaryFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label is missing: $Path" }
    $item = Get-Item -LiteralPath $Path -Force
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $Path"
    }
    return $item
}

function Get-StreamingSha256 {
    param([Parameter(Mandatory = $true)][string]$Path)

    $stream = [IO.File]::Open($Path, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            $bytes = $sha.ComputeHash($stream)
            return ([BitConverter]::ToString($bytes)).Replace('-', '').ToUpperInvariant()
        }
        finally { $sha.Dispose() }
    }
    finally { $stream.Dispose() }
}

function Get-StableFileState {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label
    )

    $first = Assert-OrdinaryFile -Path $Path -Label $Label
    $firstPath = Get-CanonicalAbsolutePath -Path $first.FullName
    $length = [int64]$first.Length
    $ticks = [int64]$first.LastWriteTimeUtc.Ticks
    $hash = Get-StreamingSha256 -Path $firstPath

    $second = Assert-OrdinaryFile -Path $Path -Label $Label
    $secondPath = Get-CanonicalAbsolutePath -Path $second.FullName
    $secondHash = Get-StreamingSha256 -Path $secondPath
    if (-not [string]::Equals($firstPath, $secondPath, [StringComparison]::OrdinalIgnoreCase) -or
        $length -ne [int64]$second.Length -or
        $ticks -ne [int64]$second.LastWriteTimeUtc.Ticks -or
        -not [string]::Equals($hash, $secondHash, [StringComparison]::Ordinal)) {
        throw "$Label changed while its generation was being captured: $Path"
    }

    return [pscustomobject]@{
        path = $secondPath
        length = $length
        lastWriteUtcTicks = $ticks
        sha256 = $hash
    }
}

$sourceDir = Get-CanonicalAbsolutePath -Path $BricsCadDir
$snapshot = Get-CanonicalAbsolutePath -Path $SnapshotDir
$state = Get-CanonicalAbsolutePath -Path $StatePath
$snapshotRoot = [IO.Path]::GetPathRoot($snapshot)
if ([string]::Equals($snapshot, $snapshotRoot, [StringComparison]::OrdinalIgnoreCase)) {
    throw "SnapshotDir must not be a filesystem root: $snapshot"
}
if (Test-CanonicalPathWithin -Candidate $sourceDir -Parent $snapshot) {
    throw 'SnapshotDir must not equal or contain the V25 source reference directory.'
}
if (-not (Test-CanonicalPathWithin -Candidate $state -Parent $snapshot)) {
    throw 'StatePath must be contained by SnapshotDir.'
}

Assert-NoExistingReparseComponent -Path $sourceDir -Label 'V25 source reference directory'
Assert-NoExistingReparseComponent -Path $snapshot -Label 'V25 compile-reference snapshot directory'
Assert-NoExistingReparseComponent -Path $state -Label 'V25 compile-reference state path'

if (Test-Path -LiteralPath $snapshot) {
    $snapshotItem = Get-Item -LiteralPath $snapshot -Force
    if (-not $snapshotItem.PSIsContainer -or ($snapshotItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Existing SnapshotDir must be an ordinary non-reparse directory: $snapshot"
    }
    foreach ($child in @(Get-ChildItem -LiteralPath $snapshot -Force)) {
        if (($child.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "SnapshotDir contains a reparse-backed child and cannot be cleaned safely: $($child.FullName)"
        }
        if ($child.PSIsContainer) {
            throw "SnapshotDir contains an unexpected child directory and cannot be cleaned safely: $($child.FullName)"
        }
    }
    Get-ChildItem -LiteralPath $snapshot -Force -File | Remove-Item -Force
}
else {
    New-Item -ItemType Directory -Path $snapshot | Out-Null
}

$captured = @()
foreach ($name in $requiredNames) {
    $sourcePath = Join-Path $sourceDir $name
    $before = Get-StableFileState -Path $sourcePath -Label "V25 compile reference $name"

    $destinationPath = Join-Path $snapshot $name
    [IO.File]::Copy($before.path, $destinationPath, $false)

    $after = Get-StableFileState -Path $sourcePath -Label "V25 compile reference $name"
    if (-not [string]::Equals($before.path, $after.path, [StringComparison]::OrdinalIgnoreCase) -or
        $before.length -ne $after.length -or
        $before.lastWriteUtcTicks -ne $after.lastWriteUtcTicks -or
        -not [string]::Equals($before.sha256, $after.sha256, [StringComparison]::Ordinal)) {
        throw "V25 compile reference changed while it was copied into the build snapshot: $name"
    }

    $destination = Get-StableFileState -Path $destinationPath -Label "V25 compile-reference snapshot $name"
    if ($destination.length -ne $before.length -or
        -not [string]::Equals($destination.sha256, $before.sha256, [StringComparison]::Ordinal)) {
        throw "V25 compile-reference snapshot bytes do not match the admitted source generation: $name"
    }

    $captured += [pscustomobject]@{
        name = $name
        path = $destination.path
        length = $destination.length
        lastWriteUtcTicks = $destination.lastWriteUtcTicks
        sha256 = $destination.sha256
    }
}

$document = [pscustomobject]@{
    schemaVersion = 1
    bricsCadDir = $snapshot
    references = $captured
}
$json = $document | ConvertTo-Json -Depth 5
$utf8 = New-Object Text.UTF8Encoding($false, $true)
[IO.File]::WriteAllText($state, $json, $utf8)
$stateItem = Assert-OrdinaryFile -Path $state -Label 'V25 compile-reference state'
if ($stateItem.Length -le 0 -or $stateItem.Length -gt $maxStateBytes) {
    throw "V25 compile-reference state size is outside the accepted bound: $($stateItem.Length) bytes."
}

Write-Output $snapshot
