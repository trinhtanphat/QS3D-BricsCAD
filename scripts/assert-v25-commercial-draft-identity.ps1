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
$MaxVerifierScriptBytes = 262144
$MaxSignedPayloadEntryBytes = 268435456
$MaxSignedPayloadTotalBytes = 536870912
$StrictReleaseTagPattern = '^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'
$RequiredSignedPayloadEntries = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'install-v25-autoload.ps1',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1',
    'unblock-v25-netload.ps1'
)

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

function Test-HeldZipPayloadSignatures {
    param($ZipHeld, [Parameter(Mandatory = $true)][string]$ExpectedThumbprint)

    Add-Type -AssemblyName System.IO.Compression -ErrorAction Stop
    if ([string]::IsNullOrWhiteSpace($env:RUNNER_TEMP)) { throw 'RUNNER_TEMP is required for held V25 signature verification.' }
    $runnerTemp = Get-CanonicalFullPath -LiteralPath $env:RUNNER_TEMP
    $runnerTempItem = Get-Item -LiteralPath $runnerTemp -Force -ErrorAction Stop
    if (-not $runnerTempItem.PSIsContainer -or (($runnerTempItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
        throw 'RUNNER_TEMP must be an ordinary non-reparse directory for held V25 signature verification.'
    }

    $workspace = Join-Path $runnerTemp ('qs3d-v25-held-signature-' + [Guid]::NewGuid().ToString('N'))
    if (Test-Path -LiteralPath $workspace) { throw 'Held V25 signature verification workspace unexpectedly already exists.' }
    $workspaceItem = New-Item -ItemType Directory -Path $workspace -ErrorAction Stop
    try {
        if (($workspaceItem.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'Held V25 signature verification workspace must not be a reparse point.'
        }

        $extracted = New-Object System.Collections.Generic.List[string]
        $ZipHeld.Stream.Seek(0, [IO.SeekOrigin]::Begin) | Out-Null
        $archive = [IO.Compression.ZipArchive]::new($ZipHeld.Stream, [IO.Compression.ZipArchiveMode]::Read, $true)
        try {
            [int64]$totalBytes = 0
            foreach ($requiredName in $RequiredSignedPayloadEntries) {
                $matches = @($archive.Entries | Where-Object {
                    [string]::Equals($_.FullName.Replace('\\','/'), $requiredName, [StringComparison]::OrdinalIgnoreCase)
                })
                if ($matches.Count -ne 1) {
                    throw "Downloaded V25 draft ZIP must contain exactly one signed payload entry named $requiredName; found $($matches.Count)."
                }
                $entry = $matches[0]
                if ([int64]$entry.Length -lt 0 -or [int64]$entry.Length -gt $MaxSignedPayloadEntryBytes) {
                    throw "Downloaded V25 draft signed payload entry $requiredName exceeds the bounded extraction limit."
                }
                $totalBytes += [int64]$entry.Length
                if ($totalBytes -gt $MaxSignedPayloadTotalBytes) {
                    throw 'Downloaded V25 draft signed payload exceeds the bounded total extraction limit.'
                }

                $destination = Join-Path $workspace $requiredName
                $destinationFull = Get-CanonicalFullPath -LiteralPath $destination
                $workspacePrefix = (Get-CanonicalFullPath -LiteralPath $workspace).TrimEnd([IO.Path]::DirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
                if (-not $destinationFull.StartsWith($workspacePrefix, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Signed payload extraction escaped the private verification workspace: $requiredName"
                }

                $entryStream = $entry.Open()
                try {
                    $output = [IO.File]::Open($destinationFull, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
                    try { $entryStream.CopyTo($output) }
                    finally { $output.Dispose() }
                }
                finally { $entryStream.Dispose() }

                $written = Get-Item -LiteralPath $destinationFull -Force -ErrorAction Stop
                if ($written.PSIsContainer -or (($written.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) -or [int64]$written.Length -ne [int64]$entry.Length) {
                    throw "Extracted signed payload generation is invalid for $requiredName."
                }
                $extracted.Add($destinationFull)
            }
        }
        finally { $archive.Dispose() }

        $verifyScript = Join-Path $PSScriptRoot 'verify-v25-signatures.ps1'
        $verifyHeld = $null
        try {
            $verifyHeld = Open-HeldGeneration -LiteralPath $verifyScript -Label 'V25 Authenticode verifier'
            $verifyScriptText = Read-HeldStrictUtf8 -Held $verifyHeld -MaxBytes $MaxVerifierScriptBytes -Label 'V25 Authenticode verifier'
            try { $verifyScriptBlock = [ScriptBlock]::Create($verifyScriptText) }
            catch { throw "V25 Authenticode verifier cannot be parsed from its held generation: $($_.Exception.Message)" }
            & $verifyScriptBlock -Path $extracted.ToArray() -ExpectedThumbprint $ExpectedThumbprint
        }
        finally {
            if ($null -ne $verifyHeld -and $null -ne $verifyHeld.Stream) { $verifyHeld.Stream.Dispose() }
        }
    }
    finally {
        Remove-Item -LiteralPath $workspace -Recurse -Force -ErrorAction SilentlyContinue
    }
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

    Test-HeldZipPayloadSignatures -ZipHeld $zipHeld -ExpectedThumbprint $expectedSigner

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
