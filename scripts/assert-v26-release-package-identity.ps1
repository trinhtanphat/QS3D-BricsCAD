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

$metadataFile = Resolve-OrdinaryNonReparseFile -Path $MetadataPath -Label 'V26 package metadata'
$pluginFile = Resolve-OrdinaryNonReparseFile -Path $PluginPath -Label 'V26 plugin assembly'
$coreFile = Resolve-OrdinaryNonReparseFile -Path $CorePath -Label 'V26 Core assembly'

$metadataText = Read-BoundedStrictUtf8File -File $metadataFile -Label 'V26 package metadata'
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

$pluginVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginFile.FullName).Version
$coreVersion = [Reflection.AssemblyName]::GetAssemblyName($coreFile.FullName).Version
if ($pluginVersion -ne $packageVersion -or $coreVersion -ne $packageVersion) {
    throw 'V26 package managed assembly identity mismatch.'
}

[pscustomobject]@{
    ProductVersion = [string]$metadata.productVersion
    AssemblyVersion = $packageVersion.ToString()
    MetadataBytes = $metadataFile.Length
}
