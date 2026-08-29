[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ExpectedPackageName = 'QS3D-BricsCAD-V26.zip'
$script:ExpectedChecksumName = 'QS3D-BricsCAD-V26.zip.sha256'
$script:MaxChecksumBytes = 1024

function Assert-NoReparseDirectoryChain {
    param([Parameter(Mandatory = $true)][IO.DirectoryInfo]$Directory,[Parameter(Mandatory = $true)][string]$Label)
    $cursor = $Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
}

function Resolve-OrdinaryNonReparseFile {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "$Label must be an ordinary file: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be a reparse-point file: $Path" }
    Assert-NoReparseDirectoryChain -Directory $item.Directory -Label $Label
    return $item
}

function Resolve-OrdinaryNonReparseDirectory {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be a reparse-point directory: $Path" }
    Assert-NoReparseDirectoryChain -Directory $item -Label $Label
    return $item
}

function Assert-SafeExistingOutputLeaf {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "V26 checksum destination must not be a directory: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "V26 checksum destination must not be a reparse-point file: $Path" }
    return $true
}

function Remove-SafeChecksumLeaf {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    if (-not (Test-Path -LiteralPath $Path)) { return }
    [void](Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label)
    Remove-Item -LiteralPath $Path -Force -ErrorAction Stop
}

function Read-BoundedChecksumBytes {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    if ($item.Length -lt 1 -or $item.Length -gt $script:MaxChecksumBytes) { throw "$Label must be between 1 and $($script:MaxChecksumBytes) bytes: $Path" }
    $bytes = [IO.File]::ReadAllBytes($item.FullName)
    if ($bytes.Length -ne $item.Length) { throw "$Label changed while being read: $Path" }
    return [byte[]]$bytes
}

$package = Resolve-OrdinaryNonReparseFile -Path $PackagePath -Label 'V26 package ZIP'
if (-not [string]::Equals($package.Name, $script:ExpectedPackageName, [StringComparison]::Ordinal)) { throw "V26 checksum source must be named $($script:ExpectedPackageName): $($package.Name)" }
$packageCanonicalPath = $package.FullName
$packageLength = [int64]$package.Length
$packageLastWriteUtcTicks = [int64]$package.LastWriteTimeUtc.Ticks

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
if (-not [string]::Equals([IO.Path]::GetFileName($outputFullPath), $script:ExpectedChecksumName, [StringComparison]::Ordinal)) { throw "V26 checksum destination must be named $($script:ExpectedChecksumName): $outputFullPath" }
$outputParentPath = [IO.Path]::GetDirectoryName($outputFullPath)
if ([string]::IsNullOrWhiteSpace($outputParentPath)) { throw 'V26 checksum destination must have a parent directory.' }
$outputParent = Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent'
$hadExistingOutput = Assert-SafeExistingOutputLeaf -Path $outputFullPath
$originalOutputBytes = $null
if ($hadExistingOutput) { $originalOutputBytes = Read-BoundedChecksumBytes -Path $outputFullPath -Label 'Existing V26 checksum destination snapshot' }

$stream = [IO.File]::Open($packageCanonicalPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $reboundPackage = Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'
    if (-not [string]::Equals($packageCanonicalPath, $reboundPackage.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $packageLength -ne [int64]$stream.Length -or
        $packageLength -ne [int64]$reboundPackage.Length -or
        $packageLastWriteUtcTicks -ne [int64]$reboundPackage.LastWriteTimeUtc.Ticks) {
        throw 'V26 package ZIP changed between checksum admission and held-stream binding.'
    }

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try { $digestBytes = $sha256.ComputeHash($stream) } finally { $sha256.Dispose() }
} finally { $stream.Dispose() }

$hash = ([BitConverter]::ToString($digestBytes)).Replace('-', '').ToLowerInvariant()
if ($hash -notmatch '^[0-9a-f]{64}$') { throw 'Computed V26 package SHA-256 digest is malformed.' }
$record = "$hash  $($script:ExpectedPackageName)"
$recordBytes = [Text.Encoding]::ASCII.GetBytes($record + [Environment]::NewLine)
if ($recordBytes.Length -gt $script:MaxChecksumBytes) { throw 'Canonical V26 checksum record exceeds the bounded publication size.' }

$nonce = [Guid]::NewGuid().ToString('N')
$tempPath = [IO.Path]::Combine($outputParent.FullName, ([IO.Path]::GetFileName($outputFullPath) + ".tmp-$nonce"))
$backupPath = [IO.Path]::Combine($outputParent.FullName, ([IO.Path]::GetFileName($outputFullPath) + ".bak-$nonce"))
$publicationStarted = $false
$publicationCommitted = $false

try {
    if (Test-Path -LiteralPath $tempPath) { throw "Refusing to reuse checksum staging path: $tempPath" }
    if (Test-Path -LiteralPath $backupPath) { throw "Refusing to reuse checksum backup path: $backupPath" }
    [IO.File]::WriteAllBytes($tempPath, $recordBytes)
    $temp = Resolve-OrdinaryNonReparseFile -Path $tempPath -Label 'V26 checksum staging file'
    if ($temp.Length -ne $recordBytes.Length) { throw 'V26 checksum staging file length changed before publication.' }

    $outputParent = Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent before publication'
    if ($hadExistingOutput) {
        [void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)
    }
    elseif (Test-Path -LiteralPath $outputFullPath) {
        throw "V26 checksum destination appeared after preflight validation: $outputFullPath"
    }

    # Mark mutation intent before Replace/Move: either API can alter the destination
    # and still throw before returning. Rollback must therefore cover that window.
    $publicationStarted = $true
    if ($hadExistingOutput) {
        [IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)
    }
    else {
        [IO.File]::Move($tempPath, $outputFullPath)
    }

    $publishedItem = Resolve-OrdinaryNonReparseFile -Path $outputFullPath -Label 'Published V26 checksum'
    $publishedText = [IO.File]::ReadAllText($publishedItem.FullName, [Text.Encoding]::ASCII).TrimEnd("`r", "`n")
    if (-not [string]::Equals($publishedText, $record, [StringComparison]::Ordinal)) { throw 'Published V26 checksum bytes do not match the computed canonical record.' }
    $publicationCommitted = $true
}
catch {
    $publicationFailure = $_
    if ($publicationStarted -and -not $publicationCommitted) {
        try {
            [void](Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum rollback parent')
            if ($hadExistingOutput) {
                if (Test-Path -LiteralPath $backupPath) {
                    $backup = Resolve-OrdinaryNonReparseFile -Path $backupPath -Label 'V26 checksum rollback backup'
                    if (Test-Path -LiteralPath $outputFullPath) {
                        [void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)
                        Remove-Item -LiteralPath $outputFullPath -Force -ErrorAction Stop
                    }
                    [IO.File]::Move($backup.FullName, $outputFullPath)
                    [void](Resolve-OrdinaryNonReparseFile -Path $outputFullPath -Label 'Restored V26 checksum destination')
                }
                else {
                    # Replace can throw before creating its backup. In that case only
                    # accept the state if the original destination is provably unchanged.
                    $currentOutputBytes = Read-BoundedChecksumBytes -Path $outputFullPath -Label 'V26 checksum rollback unchanged-destination proof'
                    $originalBase64 = [Convert]::ToBase64String($originalOutputBytes)
                    $currentBase64 = [Convert]::ToBase64String($currentOutputBytes)
                    if (-not [string]::Equals($currentBase64, $originalBase64, [StringComparison]::Ordinal)) {
                        throw 'V26 checksum replacement failed without a backup and the original destination cannot be proven unchanged.'
                    }
                }
            }
            elseif (Test-Path -LiteralPath $outputFullPath) {
                [void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)
                Remove-Item -LiteralPath $outputFullPath -Force -ErrorAction Stop
            }
        }
        catch { throw "V26 checksum publication failed and rollback could not safely restore the pre-publication state. Publication failure: $($publicationFailure.Exception.Message) Rollback failure: $($_.Exception.Message)" }
    }
    throw $publicationFailure
}
finally {
    Remove-SafeChecksumLeaf -Path $tempPath -Label 'V26 checksum staging residue'
    if ($publicationCommitted) { Remove-SafeChecksumLeaf -Path $backupPath -Label 'V26 checksum committed backup residue' }
}

if (Test-Path -LiteralPath $tempPath) { throw "V26 checksum staging residue remains: $tempPath" }
if (Test-Path -LiteralPath $backupPath) { throw "V26 checksum backup residue remains: $backupPath" }

[pscustomobject]@{ PackagePath = $packageCanonicalPath; OutputPath = $outputFullPath; Sha256 = $hash; Record = $record }
