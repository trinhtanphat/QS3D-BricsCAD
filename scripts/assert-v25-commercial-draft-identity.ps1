[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackageZip,

    [Parameter(Mandatory = $true)]
    [string]$ChecksumPath,

    [Parameter(Mandatory = $true)]
    [string]$UpdateManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$ProvenancePath,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSourceCommit,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedReleaseTag,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$MaxMetadataBytes = 65536
$MaxProvenanceBytes = 65536
$MaxChecksumBytes = 4096
$StrictReleaseTagPattern = '^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    return [IO.Path]::GetFullPath($LiteralPath)
}

function Assert-NoReparseAncestor {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    $cursor = [IO.Directory]::GetParent((Get-CanonicalFullPath -LiteralPath $LiteralPath))
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Downloaded V25 draft input traverses a reparse-point ancestor: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
}

function Open-HeldGeneration {
    param([Parameter(Mandatory = $true)][string]$LiteralPath, [Parameter(Mandatory = $true)][string]$Label)
    $canonical = Get-CanonicalFullPath -LiteralPath $LiteralPath
    Assert-NoReparseAncestor -LiteralPath $canonical
    $admitted = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
    if ($admitted.PSIsContainer -or (($admitted.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw "$Label must be an ordinary non-reparse file: $canonical"
    }
    $admittedPath = Get-CanonicalFullPath -LiteralPath $admitted.FullName
    if (-not [string]::Equals($canonical, $admittedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label canonical identity drifted before open: $canonical"
    }
    $length = [int64]$admitted.Length
    $writeTicks = [int64]$admitted.LastWriteTimeUtc.Ticks
    $stream = [IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $rebound = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
        if ($rebound.PSIsContainer -or (($rebound.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) -or
            -not [string]::Equals($admittedPath, (Get-CanonicalFullPath -LiteralPath $rebound.FullName), [StringComparison]::OrdinalIgnoreCase) -or
            [int64]$rebound.Length -ne $length -or [int64]$rebound.LastWriteTimeUtc.Ticks -ne $writeTicks -or [int64]$stream.Length -ne $length) {
            throw "$Label generation changed across admission/open: $canonical"
        }
        return [pscustomobject]@{ Stream = $stream; Path = $admittedPath; Length = $length }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Read-HeldStrictUtf8 {
    param($Held, [int64]$MaxBytes, [string]$Label)
    if ([int64]$Held.Length -gt $MaxBytes) { throw "$Label exceeds the $MaxBytes-byte input limit." }
    $Held.Stream.Seek(0, [IO.SeekOrigin]::Begin) | Out-Null
    $reader = [IO.StreamReader]::new($Held.Stream, [Text.UTF8Encoding]::new($false, $true), $true, 4096, $true)
    try { return $reader.ReadToEnd() }
    catch { throw "$Label is not strict UTF-8: $($_.Exception.Message)" }
    finally { $reader.Dispose() }
}

function Get-HeldSha256 {
    param($Held)
    $Held.Stream.Seek(0, [IO.SeekOrigin]::Begin) | Out-Null
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $digest = $sha.ComputeHash($Held.Stream)
        return (-join ($digest | ForEach-Object { $_.ToString('x2') }))
    }
    finally { $sha.Dispose() }
}

function Read-ZipMetadataIdentity {
    param($ZipHeld)
    Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
    $ZipHeld.Stream.Seek(0, [IO.SeekOrigin]::Begin) | Out-Null
    $archive = [IO.Compression.ZipArchive]::new($ZipHeld.Stream, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        $entries = @($archive.Entries | Where-Object { [string]::Equals($_.FullName.Replace('\\','/'), 'PACKAGE-METADATA.json', [StringComparison]::OrdinalIgnoreCase) })
        if ($entries.Count -ne 1) { throw "Downloaded V25 draft ZIP must contain exactly one PACKAGE-METADATA.json entry; found $($entries.Count)." }
        $entry = $entries[0]
        if ([int64]$entry.Length -gt $MaxMetadataBytes) { throw "Downloaded V25 draft PACKAGE-METADATA.json exceeds $MaxMetadataBytes bytes." }
        $entryStream = $entry.Open()
        try {
            $reader = [IO.StreamReader]::new($entryStream, [Text.UTF8Encoding]::new($false, $true), $true, 4096, $true)
            try { $text = $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally { $entryStream.Dispose() }
        try { return $text | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "Downloaded V25 draft PACKAGE-METADATA.json is invalid JSON: $($_.Exception.Message)" }
    }
    finally { $archive.Dispose() }
}

if ($ExpectedReleaseTag -notmatch $StrictReleaseTagPattern) { throw "ExpectedReleaseTag is not a strict V25 release tag: $ExpectedReleaseTag" }
$expectedSource = $ExpectedSourceCommit.ToLowerInvariant()
$expectedSigner = $ExpectedSignerThumbprint.Replace(' ', '').ToUpperInvariant()
$expectedProductVersion = $ExpectedReleaseTag.Substring(1)

$zipHeld = $null
$checksumHeld = $null
$updateHeld = $null
$provenanceHeld = $null
try {
    $zipHeld = Open-HeldGeneration -LiteralPath $PackageZip -Label 'downloaded V25 draft ZIP'
    $checksumHeld = Open-HeldGeneration -LiteralPath $ChecksumPath -Label 'downloaded V25 draft checksum'
    $updateHeld = Open-HeldGeneration -LiteralPath $UpdateManifestPath -Label 'downloaded V25 draft update manifest'
    $provenanceHeld = Open-HeldGeneration -LiteralPath $ProvenancePath -Label 'downloaded V25 draft provenance'

    $zipHash = (Get-HeldSha256 -Held $zipHeld).ToLowerInvariant()
    $updateHash = (Get-HeldSha256 -Held $updateHeld).ToLowerInvariant()

    $checksumText = (Read-HeldStrictUtf8 -Held $checksumHeld -MaxBytes $MaxChecksumBytes -Label 'downloaded V25 draft checksum').Trim()
    if ($checksumText -notmatch '^([0-9a-fA-F]{64})  QS3D-BricsCAD-V25\.zip$') { throw 'Downloaded V25 draft checksum is malformed.' }
    if (-not [string]::Equals($Matches[1], $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'Downloaded V25 draft ZIP fails its SHA-256 checksum.' }

    $provenanceText = Read-HeldStrictUtf8 -Held $provenanceHeld -MaxBytes $MaxProvenanceBytes -Label 'downloaded V25 draft provenance'
    try { $provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Downloaded V25 draft provenance is invalid JSON: $($_.Exception.Message)" }
    if ([int]$provenance.schemaVersion -ne 1 -or [string]$provenance.product -ne 'QS3D' -or [string]$provenance.target -ne 'BricsCAD V25 x64' -or
        -not [string]::Equals([string]$provenance.releaseTag, $ExpectedReleaseTag, [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$provenance.productVersion, $expectedProductVersion, [StringComparison]::Ordinal) -or
        -not [string]::Equals(([string]$provenance.sourceCommit).Trim(), $expectedSource, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals(([string]$provenance.signerThumbprint).Replace(' ', ''), $expectedSigner, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([string]$provenance.packageFile, 'QS3D-BricsCAD-V25.zip', [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$provenance.packageSha256, $zipHash, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([string]$provenance.updateManifestFile, 'QS3D-BricsCAD-V25.update.json', [StringComparison]::Ordinal) -or
        -not [string]::Equals([string]$provenance.updateManifestSha256, $updateHash, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Downloaded V25 draft provenance does not exactly bind tag, product, source, signer and downloaded asset digests.'
    }

    $metadata = Read-ZipMetadataIdentity -ZipHeld $zipHeld
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V25 x64' -or
        -not [string]::Equals(([string]$metadata.productVersion).Trim(), $expectedProductVersion, [StringComparison]::Ordinal) -or
        -not [string]::Equals(([string]$metadata.gitCommit).Trim(), $expectedSource, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Downloaded V25 draft ZIP metadata does not exactly bind product, tag and source commit.'
    }

    [pscustomobject]@{
        ProductVersion = $expectedProductVersion
        SourceCommit = $expectedSource
        PackageSha256 = $zipHash
        UpdateManifestSha256 = $updateHash
        SignerThumbprint = $expectedSigner
    }
}
finally {
    foreach ($held in @($provenanceHeld, $updateHeld, $checksumHeld, $zipHeld)) {
        if ($null -ne $held -and $null -ne $held.Stream) { $held.Stream.Dispose() }
    }
}
