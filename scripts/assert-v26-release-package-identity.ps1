[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$MetadataPath,

    [Parameter(Mandatory = $true)]
    [string]$PluginPath,

    [Parameter(Mandatory = $true)]
    [string]$CorePath,

    [Parameter(Mandatory = $true)]
    [string]$ReleaseTag
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:MaxMetadataBytes = 65536
$script:MaxAssemblyBytes = 256MB
$script:StrictUtf8 = [Text.UTF8Encoding]::new($false, $true)

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

    $cursor = $item.Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label path contains a reparse-point directory: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }

    return $item
}

function Get-HeldStreamingSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [IO.FileStream]$Stream,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    try {
        $Stream.Position = 0
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
            $Stream.Position = 0
        }
    }
    catch {
        throw "$Label could not be fingerprinted safely from its held generation: $($_.Exception.Message)"
    }
}

function Open-LockedStableFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $initial = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    $stream = [IO.File]::Open(
        $initial.FullName,
        [IO.FileMode]::Open,
        [IO.FileAccess]::Read,
        [IO.FileShare]::Read
    )
    try {
        # FileShare.Read deliberately denies write/delete/replace while semantic
        # consumers use this exact admitted generation.
        $current = Resolve-OrdinaryNonReparseFile -Path $initial.FullName -Label $Label
        if (-not [string]::Equals($initial.FullName, $current.FullName, [StringComparison]::OrdinalIgnoreCase) -or
            $stream.Length -ne $current.Length -or
            $initial.LastWriteTimeUtc.Ticks -ne $current.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its generation lock was being admitted."
        }

        $hash = Get-HeldStreamingSha256 -Stream $stream -Label $Label
        $afterHash = Resolve-OrdinaryNonReparseFile -Path $initial.FullName -Label $Label
        if (-not [string]::Equals($current.FullName, $afterHash.FullName, [StringComparison]::OrdinalIgnoreCase) -or
            $stream.Length -ne $afterHash.Length -or
            $current.LastWriteTimeUtc.Ticks -ne $afterHash.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its held generation was being fingerprinted."
        }

        return [pscustomobject]@{
            Path = $afterHash.FullName
            Length = [int64]$stream.Length
            LastWriteUtcTicks = [int64]$afterHash.LastWriteTimeUtc.Ticks
            Sha256 = $hash
            Stream = $stream
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Assert-LockedPathBinding {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Held,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $current = Resolve-OrdinaryNonReparseFile -Path $Held.Path -Label $Label
    if (-not [string]::Equals($Held.Path, $current.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $Held.Length -ne $current.Length -or
        $Held.LastWriteUtcTicks -ne $current.LastWriteTimeUtc.Ticks -or
        $Held.Stream.Length -ne $Held.Length) {
        throw "$Label pathname no longer resolves to the held admitted generation."
    }
}

function Read-BoundedStrictUtf8Stream {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Held,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $stream = $Held.Stream
    if ($stream.Length -gt $script:MaxMetadataBytes) {
        throw "$Label exceeds the $($script:MaxMetadataBytes)-byte safety limit."
    }
    if ($stream.Length -gt [int]::MaxValue) {
        throw "$Label is too large to materialize safely."
    }

    $stream.Position = 0
    $bytes = [byte[]]::new([int]$stream.Length)
    $offset = 0
    while ($offset -lt $bytes.Length) {
        $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
        if ($read -le 0) {
            throw "$Label ended before the held file length was read."
        }
        $offset += $read
    }
    if ($stream.ReadByte() -ne -1) {
        throw "$Label held stream changed while it was being read."
    }
    $stream.Position = 0

    try {
        return $script:StrictUtf8.GetString($bytes)
    }
    catch [Text.DecoderFallbackException] {
        throw "$Label is not strict UTF-8."
    }
}

function Get-HeldAssemblyVersion {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Held,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    # release-v26.yml intentionally invokes this validator with Windows PowerShell.
    # ReflectionOnlyLoad(Byte[]) lets the semantic consumer examine the exact bytes
    # read from the already-admitted held generation without reopening a pathname.
    if ($PSVersionTable.PSEdition -ne 'Desktop') {
        throw "$Label held-generation assembly inspection requires Windows PowerShell/.NET Framework."
    }
    if ($Held.Stream.Length -gt $script:MaxAssemblyBytes) {
        throw "$Label exceeds the $($script:MaxAssemblyBytes)-byte assembly safety limit."
    }
    if ($Held.Stream.Length -gt [int]::MaxValue) {
        throw "$Label is too large to materialize safely."
    }

    $Held.Stream.Position = 0
    $bytes = [byte[]]::new([int]$Held.Stream.Length)
    $offset = 0
    while ($offset -lt $bytes.Length) {
        $read = $Held.Stream.Read($bytes, $offset, $bytes.Length - $offset)
        if ($read -le 0) {
            throw "$Label ended before the held file length was read."
        }
        $offset += $read
    }
    if ($Held.Stream.ReadByte() -ne -1) {
        throw "$Label held stream changed while its assembly identity was being read."
    }
    $Held.Stream.Position = 0

    try {
        $assembly = [Reflection.Assembly]::ReflectionOnlyLoad($bytes)
        return $assembly.GetName().Version
    }
    catch {
        throw "$Label could not be inspected safely from its held generation: $($_.Exception.Message)"
    }
    finally {
        $Held.Stream.Position = 0
    }
}

$heldFiles = New-Object 'System.Collections.Generic.List[object]'
try {
    # Admit all release-identity inputs first and keep every generation locked
    # until metadata/assembly semantic consumers and cross-identity checks finish.
    $metadataHeld = Open-LockedStableFile -Path $MetadataPath -Label 'V26 package metadata'
    $heldFiles.Add($metadataHeld) | Out-Null
    $pluginHeld = Open-LockedStableFile -Path $PluginPath -Label 'V26 plugin assembly'
    $heldFiles.Add($pluginHeld) | Out-Null
    $coreHeld = Open-LockedStableFile -Path $CorePath -Label 'V26 Core assembly'
    $heldFiles.Add($coreHeld) | Out-Null

    Assert-LockedPathBinding -Held $metadataHeld -Label 'V26 package metadata'
    $metadataText = Read-BoundedStrictUtf8Stream -Held $metadataHeld -Label 'V26 package metadata'
    try {
        $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop
    }
    catch {
        throw "V26 package metadata JSON is invalid: $($_.Exception.Message)"
    }

    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64') {
        throw 'V26 package product/target identity is invalid.'
    }
    if ([string]$metadata.framework -ne 'net8.0-windows') {
        throw 'V26 package framework identity is invalid.'
    }
    if (-not [string]::Equals(('v' + [string]$metadata.productVersion), $ReleaseTag, [StringComparison]::Ordinal)) {
        throw "Release tag $ReleaseTag does not exactly match V26 productVersion $($metadata.productVersion)."
    }
    try {
        $packageVersion = [Version]::Parse([string]$metadata.version)
    }
    catch {
        throw "PACKAGE-METADATA version is invalid: $($metadata.version)"
    }

    Assert-LockedPathBinding -Held $pluginHeld -Label 'V26 plugin assembly'
    $pluginVersion = Get-HeldAssemblyVersion -Held $pluginHeld -Label 'V26 plugin assembly'
    Assert-LockedPathBinding -Held $pluginHeld -Label 'V26 plugin assembly'

    Assert-LockedPathBinding -Held $coreHeld -Label 'V26 Core assembly'
    $coreVersion = Get-HeldAssemblyVersion -Held $coreHeld -Label 'V26 Core assembly'
    Assert-LockedPathBinding -Held $coreHeld -Label 'V26 Core assembly'

    if ($pluginVersion -ne $packageVersion -or $coreVersion -ne $packageVersion) {
        throw 'V26 package managed assembly identity mismatch.'
    }

    [pscustomobject]@{
        ProductVersion = [string]$metadata.productVersion
        AssemblyVersion = $packageVersion.ToString()
        MetadataBytes = $metadataHeld.Length
        MetadataSha256 = $metadataHeld.Sha256
        PluginSha256 = $pluginHeld.Sha256
        CoreSha256 = $coreHeld.Sha256
    }
}
finally {
    for ($index = $heldFiles.Count - 1; $index -ge 0; $index--) {
        $heldFiles[$index].Stream.Dispose()
    }
}
