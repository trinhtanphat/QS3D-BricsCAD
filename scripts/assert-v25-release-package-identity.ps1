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
$script:MaxAssemblyBytes = 134217728
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

function Open-HeldAssemblyFile {
    param([string]$Path, [string]$Label)

    $initial = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    if ([int64]$initial.Length -le 0 -or [int64]$initial.Length -gt $script:MaxAssemblyBytes) {
        throw "$Label size is outside the admitted 1..$($script:MaxAssemblyBytes)-byte range."
    }
    $stream = [IO.File]::Open(
        $initial.FullName,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        [IO.FileShare]::Read
    )
    try {
        $rebound = Resolve-OrdinaryNonReparseFile -Path $initial.FullName -Label $Label
        if (-not [string]::Equals($initial.FullName, $rebound.FullName, [StringComparison]::OrdinalIgnoreCase) -or
            $initial.Length -ne $stream.Length -or
            $rebound.Length -ne $stream.Length -or
            $initial.LastWriteTimeUtc.Ticks -ne $rebound.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its generation lock was being admitted."
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

function Assert-HeldAssemblyBinding {
    param([pscustomobject]$Held, [string]$Label)

    $current = Resolve-OrdinaryNonReparseFile -Path $Held.Path -Label $Label
    if (-not [string]::Equals($Held.Path, $current.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $Held.Length -ne $current.Length -or
        $Held.Length -ne $Held.Stream.Length -or
        $Held.LastWriteUtcTicks -ne $current.LastWriteTimeUtc.Ticks) {
        throw "$Label pathname no longer resolves to the held admitted generation."
    }
}

function Read-HeldAssemblyBytes {
    param([pscustomobject]$Held, [string]$Label)

    if ($Held.Stream.Length -le 0 -or $Held.Stream.Length -gt $script:MaxAssemblyBytes -or $Held.Stream.Length -gt [int]::MaxValue) {
        throw "$Label held stream size is outside the admitted assembly byte range."
    }
    $Held.Stream.Position = 0
    $bytes = [byte[]]::new([int]$Held.Stream.Length)
    $offset = 0
    while ($offset -lt $bytes.Length) {
        $read = $Held.Stream.Read($bytes, $offset, $bytes.Length - $offset)
        if ($read -le 0) { throw "$Label ended before its held length was read." }
        $offset += $read
    }
    if ($Held.Stream.ReadByte() -ne -1) {
        throw "$Label held stream changed while its semantic bytes were being read."
    }
    $Held.Stream.Position = 0
    return $bytes
}

function Get-HeldAssemblyVersion {
    param([pscustomobject]$Held, [string]$Label)

    Assert-HeldAssemblyBinding -Held $Held -Label $Label
    $bytes = Read-HeldAssemblyBytes -Held $Held -Label $Label
    try {
        # ReflectionOnlyLoad consumes the exact held bytes and does not execute
        # candidate assembly code or reopen the package pathname.
        $assembly = [Reflection.Assembly]::ReflectionOnlyLoad($bytes)
        $version = $assembly.GetName().Version
    }
    catch {
        throw "$Label is not a valid managed assembly for held semantic inspection: $($_.Exception.Message)"
    }
    if ($null -eq $version) {
        throw "$Label has no managed assembly version."
    }
    Assert-HeldAssemblyBinding -Held $Held -Label $Label
    return [Version]$version
}

if ($ExpectedSourceCommit -notmatch '^[0-9A-Fa-f]{40}$') {
    throw 'ExpectedSourceCommit must be one exact 40-hex Git commit SHA.'
}
$expectedSource = $ExpectedSourceCommit.ToLowerInvariant()
$strictReleaseTagPattern = '^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:-[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$'
if (-not [string]::IsNullOrWhiteSpace($ExpectedReleaseTag) -and $ExpectedReleaseTag -notmatch $strictReleaseTagPattern) {
    throw "ExpectedReleaseTag is not a supported exact release tag: $ExpectedReleaseTag"
}

$held = Open-HeldMetadataFile -Path $MetadataPath
$pluginHeld = $null
$coreHeld = $null
try {
    Assert-HeldMetadataBinding -Held $held
    $packageDirectory = Split-Path -Parent $held.Path
    $pluginPath = Join-Path $packageDirectory 'QS3D.BricsCAD.V25.dll'
    $corePath = Join-Path $packageDirectory 'QS3D.Core.dll'
    $pluginHeld = Open-HeldAssemblyFile -Path $pluginPath -Label 'V25 plugin assembly'
    $coreHeld = Open-HeldAssemblyFile -Path $corePath -Label 'V25 Core assembly'

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

    $assemblyVersionText = [string]$metadata.version
    if ([string]::IsNullOrWhiteSpace($assemblyVersionText) -or
        -not [string]::Equals($assemblyVersionText, $assemblyVersionText.Trim(), [StringComparison]::Ordinal)) {
        throw 'V25 package metadata version must be one non-empty canonical managed assembly version.'
    }
    try {
        $packageVersion = [Version]::Parse([string]$metadata.version)
    }
    catch {
        throw "PACKAGE-METADATA version is invalid: $($metadata.version)"
    }
    if (-not [string]::Equals($packageVersion.ToString(), $assemblyVersionText, [StringComparison]::Ordinal)) {
        throw "PACKAGE-METADATA version is not canonical: $assemblyVersionText"
    }

    $pluginVersion = Get-HeldAssemblyVersion -Held $pluginHeld -Label 'V25 plugin assembly'
    $coreVersion = Get-HeldAssemblyVersion -Held $coreHeld -Label 'V25 Core assembly'
    if ($pluginVersion -ne $packageVersion -or $coreVersion -ne $packageVersion) {
        throw "V25 package managed assembly identity mismatch. Metadata=$packageVersion Plugin=$pluginVersion Core=$coreVersion"
    }

    Assert-HeldMetadataBinding -Held $held
    Assert-HeldAssemblyBinding -Held $pluginHeld -Label 'V25 plugin assembly'
    Assert-HeldAssemblyBinding -Held $coreHeld -Label 'V25 Core assembly'
    [pscustomobject]@{
        SourceCommit = $metadataSource.ToLowerInvariant()
        ProductVersion = $productVersion
        AssemblyVersion = $packageVersion.ToString()
        MetadataBytes = $held.Length
        PluginBytes = $pluginHeld.Length
        CoreBytes = $coreHeld.Length
    }
}
finally {
    if ($null -ne $coreHeld) { $coreHeld.Stream.Dispose() }
    if ($null -ne $pluginHeld) { $pluginHeld.Stream.Dispose() }
    $held.Stream.Dispose()
}
