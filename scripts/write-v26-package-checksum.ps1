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

function Assert-NoReparseDirectoryChain {
    param(
        [Parameter(Mandatory = $true)]
        [IO.DirectoryInfo]$Directory,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $cursor = $Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label path contains a reparse-point directory: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
}

function Resolve-OrdinaryNonReparseFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) {
        throw "$Label must be an ordinary file: $Path"
    }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be a reparse-point file: $Path"
    }
    Assert-NoReparseDirectoryChain -Directory $item.Directory -Label $Label
    return $item
}

function Resolve-OrdinaryNonReparseDirectory {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer) {
        throw "$Label must be a directory: $Path"
    }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be a reparse-point directory: $Path"
    }
    Assert-NoReparseDirectoryChain -Directory $item -Label $Label
    return $item
}

function Assert-SafeExistingOutputLeaf {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    if (-not (Test-Path -LiteralPath $Path)) {
        return $false
    }

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) {
        throw "V26 checksum destination must not be a directory: $Path"
    }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "V26 checksum destination must not be a reparse-point file: $Path"
    }
    return $true
}

$package = Resolve-OrdinaryNonReparseFile -Path $PackagePath -Label 'V26 package ZIP'
if (-not [string]::Equals($package.Name, $script:ExpectedPackageName, [StringComparison]::Ordinal)) {
    throw "V26 checksum source must be named $($script:ExpectedPackageName): $($package.Name)"
}

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
$outputParentPath = [IO.Path]::GetDirectoryName($outputFullPath)
if ([string]::IsNullOrWhiteSpace($outputParentPath)) {
    throw 'V26 checksum destination must have a parent directory.'
}
$outputParent = Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent'
$hadExistingOutput = Assert-SafeExistingOutputLeaf -Path $outputFullPath

$stream = [IO.File]::Open($package.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $digestBytes = $sha256.ComputeHash($stream)
    }
    finally {
        $sha256.Dispose()
    }
}
finally {
    $stream.Dispose()
}

$hash = ([BitConverter]::ToString($digestBytes)).Replace('-', '').ToLowerInvariant()
if ($hash -notmatch '^[0-9a-f]{64}$') {
    throw 'Computed V26 package SHA-256 digest is malformed.'
}
$record = "$hash  $($script:ExpectedPackageName)"
$recordBytes = [Text.Encoding]::ASCII.GetBytes($record + [Environment]::NewLine)

$nonce = [Guid]::NewGuid().ToString('N')
$tempPath = [IO.Path]::Combine($outputParent.FullName, ([IO.Path]::GetFileName($outputFullPath) + ".tmp-$nonce"))
$backupPath = [IO.Path]::Combine($outputParent.FullName, ([IO.Path]::GetFileName($outputFullPath) + ".bak-$nonce"))
$published = $false

try {
    if (Test-Path -LiteralPath $tempPath) { throw "Refusing to reuse checksum staging path: $tempPath" }
    if (Test-Path -LiteralPath $backupPath) { throw "Refusing to reuse checksum backup path: $backupPath" }

    [IO.File]::WriteAllBytes($tempPath, $recordBytes)
    $temp = Resolve-OrdinaryNonReparseFile -Path $tempPath -Label 'V26 checksum staging file'
    if ($temp.Length -ne $recordBytes.Length) {
        throw 'V26 checksum staging file length changed before publication.'
    }

    if ($hadExistingOutput) {
        [IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)
    }
    else {
        [IO.File]::Move($tempPath, $outputFullPath)
    }
    $published = $true

    $publishedItem = Resolve-OrdinaryNonReparseFile -Path $outputFullPath -Label 'Published V26 checksum'
    $publishedText = [IO.File]::ReadAllText($publishedItem.FullName, [Text.Encoding]::ASCII).TrimEnd("`r", "`n")
    if (-not [string]::Equals($publishedText, $record, [StringComparison]::Ordinal)) {
        throw 'Published V26 checksum bytes do not match the computed canonical record.'
    }
}
catch {
    if (-not $published -and (Test-Path -LiteralPath $backupPath)) {
        if (Test-Path -LiteralPath $outputFullPath) {
            Remove-Item -LiteralPath $outputFullPath -Force -ErrorAction Stop
        }
        [IO.File]::Move($backupPath, $outputFullPath)
    }
    throw
}
finally {
    if (Test-Path -LiteralPath $tempPath) {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
    if (Test-Path -LiteralPath $backupPath) {
        Remove-Item -LiteralPath $backupPath -Force -ErrorAction SilentlyContinue
    }
}

if (Test-Path -LiteralPath $tempPath) { throw "V26 checksum staging residue remains: $tempPath" }
if (Test-Path -LiteralPath $backupPath) { throw "V26 checksum backup residue remains: $backupPath" }

[pscustomobject]@{
    PackagePath = $package.FullName
    OutputPath = $outputFullPath
    Sha256 = $hash
    Record = $record
}
