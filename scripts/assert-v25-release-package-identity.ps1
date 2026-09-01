[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MetadataPath,

    [Parameter(Mandatory = $true)]
    [string]$ExpectedSourceCommit,

    [string]$ExpectedReleaseTag
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:MaxMetadataBytes = 65536
$script:StrictUtf8 = [Text.UTF8Encoding]::new($false, $true)

function Resolve-OrdinaryNonReparseFile {
    param([string]$Path, [string]$Label)

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo])) {
        throw "$Label must be an ordinary file: $Path"
    }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be a reparse-point file: $Path"
    }
    $cursor = $item.Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label path contains a reparse-point directory: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
    return $item
}

function Open-HeldMetadataFile {
    param([string]$Path)

    $label = 'V25 package metadata'
    $initial = Resolve-OrdinaryNonReparseFile -Path $Path -Label $label
    $stream = [IO.File]::Open(
        $initial.FullName,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        [IO.FileShare]::Read
    )
    try {
        $rebound = Resolve-OrdinaryNonReparseFile -Path $initial.FullName -Label $label
        if (-not [string]::Equals($initial.FullName, $rebound.FullName, [StringComparison]::OrdinalIgnoreCase) -or
            $initial.Length -ne $stream.Length -or
            $rebound.Length -ne $stream.Length -or
            $initial.LastWriteTimeUtc.Ticks -ne $rebound.LastWriteTimeUtc.Ticks) {
            throw "$label changed while its generation lock was being admitted."
        }
        return [pscustomobject]@{
            Path = $rebound.FullName
            Length = [int64]$stream.Length
            LastWriteUtcTicks = [int64]$rebound.LastWriteTimeUtc.Ticks
            Stream = $stream
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Assert-HeldMetadataBinding {
    param([pscustomobject]$Held)

    $label = 'V25 package metadata'
    $current = Resolve-OrdinaryNonReparseFile -Path $Held.Path -Label $label
    if (-not [string]::Equals($Held.Path, $current.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $Held.Length -ne $current.Length -or
        $Held.Length -ne $Held.Stream.Length -or
        $Held.LastWriteUtcTicks -ne $current.LastWriteTimeUtc.Ticks) {
        throw "$label pathname no longer resolves to the held admitted generation."
    }
}

function Read-HeldStrictUtf8Metadata {
    param([pscustomobject]$Held)

    if ($Held.Stream.Length -gt $script:MaxMetadataBytes) {
        throw "V25 package metadata exceeds the $($script:MaxMetadataBytes)-byte safety limit."
    }
    if ($Held.Stream.Length -gt [int]::MaxValue) {
        throw 'V25 package metadata is too large to materialize safely.'
    }

    $Held.Stream.Position = 0
    $bytes = [byte[]]::new([int]$Held.Stream.Length)
    $offset = 0
    while ($offset -lt $bytes.Length) {
        $read = $Held.Stream.Read($bytes, $offset, $bytes.Length - $offset)
        if ($read -le 0) { throw 'V25 package metadata ended before its held length was read.' }
        $offset += $read
    }
    if ($Held.Stream.ReadByte() -ne -1) {
        throw 'V25 package metadata held stream changed while it was being read.'
    }
    $Held.Stream.Position = 0
    try {
        return $script:StrictUtf8.GetString($bytes)
    }
    catch [Text.DecoderFallbackException] {
        throw 'V25 package metadata is not strict UTF-8.'
    }
}

if ($ExpectedSourceCommit -notmatch '^[0-9A-Fa-f]{40}$') {
    throw 'ExpectedSourceCommit must be one exact 40-hex Git commit SHA.'
}
$expectedSource = $ExpectedSourceCommit.ToLowerInvariant()
if (-not [string]::IsNullOrWhiteSpace($ExpectedReleaseTag) -and $ExpectedReleaseTag -notmatch '^v[0-9]+\.[0-9]+\.[0-9]+(?:-[0-9A-Za-z.-]+)?$') {
    throw "ExpectedReleaseTag is not a supported exact release tag: $ExpectedReleaseTag"
}

$held = Open-HeldMetadataFile -Path $MetadataPath
try {
    Assert-HeldMetadataBinding -Held $held
    $text = Read-HeldStrictUtf8Metadata -Held $held
    Assert-HeldMetadataBinding -Held $held
    try {
        $metadata = $text | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "V25 package metadata JSON is invalid: $($_.Exception.Message)"
    }

    if (-not [string]::Equals([string]$metadata.product, 'QS3D', [StringComparison]::Ordinal)) {
        throw 'V25 package metadata product identity is invalid.'
    }
    if (-not [string]::Equals([string]$metadata.target, 'BricsCAD V25 x64', [StringComparison]::Ordinal)) {
        throw 'V25 package metadata target identity is invalid.'
    }

    $metadataSource = ([string]$metadata.gitCommit).Trim()
    if ($metadataSource -notmatch '^[0-9A-Fa-f]{40}$') {
        throw "PACKAGE-METADATA gitCommit is missing or invalid: '$metadataSource'."
    }
    if (-not [string]::Equals($metadataSource.ToLowerInvariant(), $expectedSource, [StringComparison]::Ordinal)) {
        throw "PACKAGE-METADATA gitCommit $metadataSource does not match expected source commit $ExpectedSourceCommit."
    }

    $productVersion = ([string]$metadata.productVersion).Trim()
    if ([string]::IsNullOrWhiteSpace($productVersion)) {
        throw 'V25 package metadata productVersion is missing.'
    }
    if (-not [string]::IsNullOrWhiteSpace($ExpectedReleaseTag)) {
        if (-not [string]::Equals(('v' + $productVersion), $ExpectedReleaseTag, [StringComparison]::Ordinal)) {
            throw "Release tag $ExpectedReleaseTag does not exactly match source product version $productVersion."
        }
    }

    Assert-HeldMetadataBinding -Held $held
    [pscustomobject]@{
        SourceCommit = $metadataSource.ToLowerInvariant()
        ProductVersion = $productVersion
        MetadataBytes = $held.Length
    }
}
finally {
    $held.Stream.Dispose()
}
