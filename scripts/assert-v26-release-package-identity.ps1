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

function Get-StreamingSha256 {
    param(
        [Parameter(Mandatory = $true)]
        [IO.FileInfo]$File,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $stream = [IO.File]::Open($File.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $sha = [Security.Cryptography.SHA256]::Create()
        try {
            return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '')
        }
        finally {
            $sha.Dispose()
        }
    }
    catch {
        throw "$Label could not be fingerprinted safely: $($_.Exception.Message)"
    }
    finally {
        $stream.Dispose()
    }
}

function Get-StableFileState {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $file = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    $hash = Get-StreamingSha256 -File $file -Label $Label
    $current = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label

    if (-not [string]::Equals($file.FullName, $current.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $file.Length -ne $current.Length -or
        $file.LastWriteTimeUtc.Ticks -ne $current.LastWriteTimeUtc.Ticks) {
        throw "$Label changed while its stable file state was being captured."
    }

    return [pscustomobject]@{
        Path = $current.FullName
        Length = [int64]$current.Length
        LastWriteUtcTicks = [int64]$current.LastWriteTimeUtc.Ticks
        Sha256 = $hash
    }
}

function Assert-StableFileState {
    param(
        [Parameter(Mandatory = $true)]
        [pscustomobject]$Expected,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $actual = Get-StableFileState -Path $Expected.Path -Label $Label
    if (-not [string]::Equals($Expected.Path, $actual.Path, [StringComparison]::OrdinalIgnoreCase) -or
        $Expected.Length -ne $actual.Length -or
        $Expected.LastWriteUtcTicks -ne $actual.LastWriteUtcTicks -or
        -not [string]::Equals($Expected.Sha256, $actual.Sha256, [StringComparison]::Ordinal)) {
        throw "$Label changed after admission and before identity validation completed."
    }
}

function Read-BoundedStrictUtf8File {
    param(
        [Parameter(Mandatory = $true)]
        [IO.FileInfo]$File,

        [Parameter(Mandatory = $true)]
        [string]$Label
    )

    $stream = [IO.File]::Open($File.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        if ($stream.Length -gt $script:MaxMetadataBytes) {
            throw "$Label exceeds the $($script:MaxMetadataBytes)-byte safety limit."
        }
        if ($stream.Length -gt [int]::MaxValue) {
            throw "$Label is too large to materialize safely."
        }

        $bytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) {
                throw "$Label ended before the declared file length was read."
            }
            $offset += $read
        }
        if ($stream.ReadByte() -ne -1) {
            throw "$Label changed while it was being read."
        }

        try {
            return $script:StrictUtf8.GetString($bytes)
        }
        catch [Text.DecoderFallbackException] {
            throw "$Label is not strict UTF-8."
        }
    }
    finally {
        $stream.Dispose()
    }
}

$metadataState = Get-StableFileState -Path $MetadataPath -Label 'V26 package metadata'
$pluginState = Get-StableFileState -Path $PluginPath -Label 'V26 plugin assembly'
$coreState = Get-StableFileState -Path $CorePath -Label 'V26 Core assembly'

$metadataFile = Resolve-OrdinaryNonReparseFile -Path $metadataState.Path -Label 'V26 package metadata'
$metadataText = Read-BoundedStrictUtf8File -File $metadataFile -Label 'V26 package metadata'
Assert-StableFileState -Expected $metadataState -Label 'V26 package metadata'
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

$pluginFile = Resolve-OrdinaryNonReparseFile -Path $pluginState.Path -Label 'V26 plugin assembly'
$pluginVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginFile.FullName).Version
Assert-StableFileState -Expected $pluginState -Label 'V26 plugin assembly'

$coreFile = Resolve-OrdinaryNonReparseFile -Path $coreState.Path -Label 'V26 Core assembly'
$coreVersion = [Reflection.AssemblyName]::GetAssemblyName($coreFile.FullName).Version
Assert-StableFileState -Expected $coreState -Label 'V26 Core assembly'

if ($pluginVersion -ne $packageVersion -or $coreVersion -ne $packageVersion) {
    throw 'V26 package managed assembly identity mismatch.'
}

[pscustomobject]@{
    ProductVersion = [string]$metadata.productVersion
    AssemblyVersion = $packageVersion.ToString()
    MetadataBytes = $metadataState.Length
}
