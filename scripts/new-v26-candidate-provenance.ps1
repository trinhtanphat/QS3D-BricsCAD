[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][ValidatePattern('^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')][string]$ReleaseTag,
    [ValidatePattern('^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')][string]$PackageReleaseTag,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{40}$')][string]$SourceCommit,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$maxMetadataBytes = 65536

function Resolve-OrdinaryFile([string]$Path, [string]$Label) {
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse file: $Path" }
    $cursor = $item.Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
    return $item
}

function Get-HeldSha256([IO.FileStream]$Stream) {
    $Stream.Position = 0
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '').ToUpperInvariant() }
    finally { $sha.Dispose(); $Stream.Position = 0 }
}

$zipItem = Resolve-OrdinaryFile -Path $PackageZip -Label 'V26 package ZIP'
$zipStream = [IO.File]::Open($zipItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $zipHash = Get-HeldSha256 -Stream $zipStream
    $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        $entries = @($archive.Entries | Where-Object { [string]::Equals([string]$_.FullName, 'PACKAGE-METADATA.json', [StringComparison]::Ordinal) })
        if ($entries.Count -ne 1) { throw "V26 package ZIP must contain exactly one PACKAGE-METADATA.json entry; found $($entries.Count)." }
        $entry = $entries[0]
        if ($entry.Length -gt $maxMetadataBytes) { throw 'V26 PACKAGE-METADATA.json exceeds the metadata safety limit.' }
        $entryStream = $entry.Open()
        try {
            $reader = [IO.StreamReader]::new($entryStream, $strictUtf8, $false, 4096, $true)
            try { $metadataText = $reader.ReadToEnd() }
            finally { $reader.Dispose() }
        }
        finally { $entryStream.Dispose() }
    }
    finally { $archive.Dispose() }

    try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "V26 PACKAGE-METADATA.json is invalid JSON: $($_.Exception.Message)" }
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64') { throw 'V26 package product/target identity is invalid.' }
    if ([string]$metadata.framework -ne 'net8.0-windows') { throw 'V26 package framework identity is invalid.' }
    $productVersion = [string]$metadata.productVersion
    $effectivePackageTag = if ([string]::IsNullOrWhiteSpace($PackageReleaseTag)) { $ReleaseTag } else { $PackageReleaseTag }
    if (-not [string]::Equals(('v' + $productVersion), $effectivePackageTag, [StringComparison]::Ordinal)) { throw "V26 package release tag $effectivePackageTag does not match package productVersion $productVersion." }

    $provenance = [ordered]@{
        product = 'QS3D'
        target = 'BricsCAD V26 x64'
        releaseTag = $ReleaseTag
        productVersion = $productVersion
        sourceCommit = $SourceCommit.ToLowerInvariant()
        packageSha256 = $zipHash
    }
    $parent = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    $provenance | ConvertTo-Json | Set-Content -LiteralPath $OutputPath -Encoding UTF8
    [pscustomobject]@{ SourceCommit = $provenance.sourceCommit; PackageSha256 = $zipHash; ProductVersion = $productVersion }
}
finally { $zipStream.Dispose() }
