[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$StatePath,
    [Parameter(Mandatory = $true)][string]$BricsCadDir
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

    Assert-NoExistingReparseComponent -Path $Path -Label $Label
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) {
        throw "$Label is missing: $Path"
    }
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

function Get-ByteArraySha256 {
    param([Parameter(Mandatory = $true)][byte[]]$Bytes)

    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $hashBytes = $sha.ComputeHash($Bytes)
        return ([BitConverter]::ToString($hashBytes)).Replace('-', '').ToUpperInvariant()
    }
    finally { $sha.Dispose() }
}

function Get-CurrentStableState {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [string]$Label = 'V25 compile reference'
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
        throw "$Label changed while its current generation was being verified: $Path"
    }

    return [pscustomobject]@{
        path = $secondPath
        length = $length
        lastWriteUtcTicks = $ticks
        sha256 = $hash
    }
}

Assert-NoExistingReparseComponent -Path $StatePath -Label 'V25 compile-reference state path'
Assert-NoExistingReparseComponent -Path $BricsCadDir -Label 'V25 compile-reference snapshot directory'
$stateBefore = Get-CurrentStableState -Path $StatePath -Label 'V25 compile-reference state'
if ($stateBefore.length -le 0 -or $stateBefore.length -gt $maxStateBytes) {
    throw "V25 compile-reference state size is outside the accepted bound: $($stateBefore.length) bytes."
}
$rawBytes = [IO.File]::ReadAllBytes($stateBefore.path)
if ($rawBytes.Length -ne $stateBefore.length -or $rawBytes.Length -gt $maxStateBytes) {
    throw 'V25 compile-reference state changed size while it was being materialized.'
}
$materializedHash = Get-ByteArraySha256 -Bytes $rawBytes
$stateAfter = Get-CurrentStableState -Path $StatePath -Label 'V25 compile-reference state'
if (-not [string]::Equals($stateBefore.path, $stateAfter.path, [StringComparison]::OrdinalIgnoreCase) -or
    $stateBefore.length -ne $stateAfter.length -or
    $stateBefore.lastWriteUtcTicks -ne $stateAfter.lastWriteUtcTicks -or
    -not [string]::Equals($stateBefore.sha256, $stateAfter.sha256, [StringComparison]::Ordinal) -or
    -not [string]::Equals($materializedHash, $stateBefore.sha256, [StringComparison]::Ordinal)) {
    throw 'V25 compile-reference state changed while it was being materialized.'
}

$utf8 = New-Object Text.UTF8Encoding($false, $true)
try { $raw = $utf8.GetString($rawBytes) }
catch { throw "V25 compile-reference state is not strict UTF-8: $($_.Exception.Message)" }
try { $state = $raw | ConvertFrom-Json }
catch { throw "V25 compile-reference state is invalid JSON: $($_.Exception.Message)" }

if ([int]$state.schemaVersion -ne 1) {
    throw "Unsupported V25 compile-reference state schemaVersion: $($state.schemaVersion)"
}
$expectedDir = Get-CanonicalAbsolutePath -Path $BricsCadDir
if (-not [string]::Equals(([string]$state.bricsCadDir), $expectedDir, [StringComparison]::OrdinalIgnoreCase)) {
    throw "V25 compile-reference state directory mismatch. Expected $expectedDir, got $($state.bricsCadDir)."
}

$entries = @($state.references)
if ($entries.Count -ne $requiredNames.Count) {
    throw "V25 compile-reference state must contain exactly $($requiredNames.Count) references."
}
foreach ($name in $requiredNames) {
    $matches = @($entries | Where-Object { [string]::Equals(([string]$_.name), $name, [StringComparison]::Ordinal) })
    if ($matches.Count -ne 1) { throw "V25 compile-reference state must contain exactly one entry for $name." }
    $expected = $matches[0]
    $path = Join-Path $expectedDir $name
    $current = Get-CurrentStableState -Path $path
    if (-not [string]::Equals(([string]$expected.path), $current.path, [StringComparison]::OrdinalIgnoreCase) -or
        [int64]$expected.length -ne $current.length -or
        [int64]$expected.lastWriteUtcTicks -ne $current.lastWriteUtcTicks -or
        -not [string]::Equals(([string]$expected.sha256).ToUpperInvariant(), $current.sha256, [StringComparison]::Ordinal)) {
        throw "V25 compile reference no longer matches its admitted generation: $name"
    }
}

Write-Host 'PASS: V25 compile references still match the exact admitted generations.'
