[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][string]$ChecksumPath,
    [Parameter(Mandatory = $true)][string]$ProvenancePath,
    [string]$UpdateManifestPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{40}$')][string]$ExpectedSourceCommit,
    [Parameter(Mandatory = $true)][string]$ExpectedReleaseTag,
    [string]$ExpectedPackageReleaseTag,
    [string]$ExpectedPackageUri,
    [string]$ExpectedSignerThumbprint,
    [int]$ExpectedManifestSchemaVersion = 2,
    [string]$ExpectedInstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256,
    [string]$AdmittedScript
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
if (-not ('QS3DV26HeldFileIdentity' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class QS3DV26HeldFileIdentity
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    public static extern uint GetFinalPathNameByHandleW(
        SafeFileHandle hFile,
        StringBuilder lpszFilePath,
        uint cchFilePath,
        uint dwFlags);
}
'@
}
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$maxTextBytes = 65536
$maxAdmittedScriptBytes = 262144
$effectivePackageTag = if ([string]::IsNullOrWhiteSpace($ExpectedPackageReleaseTag)) { $ExpectedReleaseTag } else { $ExpectedPackageReleaseTag }
$requiredHostNames = @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')

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

function Get-HeldFinalPath {
    param([Parameter(Mandatory = $true)][IO.FileStream]$Stream)
    if ($Stream.SafeFileHandle.IsInvalid -or $Stream.SafeFileHandle.IsClosed) {
        throw 'V26 held candidate stream does not have a live file handle.'
    }

    $capacity = 512
    while ($capacity -le 32768) {
        $builder = [Text.StringBuilder]::new($capacity)
        $length = [QS3DV26HeldFileIdentity]::GetFinalPathNameByHandleW($Stream.SafeFileHandle, $builder, [uint32]$builder.Capacity, 0)
        if ($length -eq 0) {
            $win32 = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
            throw "GetFinalPathNameByHandleW failed for V26 held candidate stream (Win32=$win32)."
        }
        if ($length -lt [uint32]$builder.Capacity) {
            $resolved = $builder.ToString()
            if ($resolved.StartsWith('\\?\UNC\', [StringComparison]::OrdinalIgnoreCase)) {
                $resolved = '\\' + $resolved.Substring(8)
            }
            elseif ($resolved.StartsWith('\\?\', [StringComparison]::OrdinalIgnoreCase)) {
                $resolved = $resolved.Substring(4)
            }
            return [IO.Path]::GetFullPath($resolved)
        }
        $capacity = [int]$length + 1
    }
    throw 'V26 held candidate final path exceeded the 32768-character safety bound.'
}

function Open-Held([string]$Path, [string]$Label) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    $canonicalPath = [IO.Path]::GetFullPath($item.FullName)
    $stream = [IO.File]::Open($canonicalPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $openedFinalPath = Get-HeldFinalPath -Stream $stream
        if (-not [string]::Equals($openedFinalPath, $canonicalPath, [StringComparison]::OrdinalIgnoreCase)) {
            throw "$Label opened handle resolved to a different final path; refusing candidate admission."
        }
        $current = Resolve-OrdinaryFile -Path $canonicalPath -Label $Label
        if ($item.Length -ne $stream.Length -or $item.LastWriteTimeUtc.Ticks -ne $current.LastWriteTimeUtc.Ticks -or $current.Length -ne $stream.Length) { throw "$Label changed while its generation lock was admitted." }
        return [pscustomobject]@{ Path=$canonicalPath; Length=[int64]$stream.Length; LastWriteUtcTicks=[int64]$current.LastWriteTimeUtc.Ticks; Stream=$stream }
    } catch { $stream.Dispose(); throw }
}

function Assert-Held([pscustomobject]$Held, [string]$Label) {
    $current = Resolve-OrdinaryFile -Path $Held.Path -Label $Label
    if ($Held.Length -ne $Held.Stream.Length -or $Held.Length -ne $current.Length -or $Held.LastWriteUtcTicks -ne $current.LastWriteTimeUtc.Ticks) { throw "$Label pathname no longer resolves to the held admitted generation." }
}

function Get-HeldSha256([pscustomobject]$Held) {
    $Held.Stream.Position = 0
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Held.Stream))).Replace('-', '').ToUpperInvariant() }
    finally { $sha.Dispose(); $Held.Stream.Position = 0 }
}

function Read-HeldText([pscustomobject]$Held, [string]$Label, [int64]$MaxBytes = $maxTextBytes) {
    if ($Held.Length -gt $MaxBytes) { throw "$Label exceeds the $MaxBytes-byte safety limit." }
    $Held.Stream.Position = 0
    $bytes = [byte[]]::new([int]$Held.Length)
    $offset = 0
    while ($offset -lt $bytes.Length) {
        $read = $Held.Stream.Read($bytes, $offset, $bytes.Length - $offset)
        if ($read -le 0) { throw "$Label ended before its held length was read." }
        $offset += $read
    }
    $Held.Stream.Position = 0
    try { return $strictUtf8.GetString($bytes) }
    catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
}

function Get-JsonPropertyOccurrenceCount([string]$JsonText, [string]$PropertyName) {
    $count = 0
    $propertyPattern = '"(?:\\["\\/bfnrt]|\\u[0-9A-Fa-f]{4}|[^"\\\x00-\x1F])*"\s*:'
    foreach ($match in [Text.RegularExpressions.Regex]::Matches($JsonText, $propertyPattern)) {
        $colon = $match.Value.LastIndexOf(':')
        if ($colon -le 0) { continue }
        $encodedName = $match.Value.Substring(0, $colon).Trim()
        try { $decodedName = [string]($encodedName | ConvertFrom-Json -ErrorAction Stop) }
        catch { continue }
        if ([string]::Equals($decodedName, $PropertyName, [StringComparison]::Ordinal)) { $count++ }
    }
    return $count
}

if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256) -or $ExpectedInstallerSha256 -cnotmatch '^[0-9A-Fa-f]{64}$') {
    throw 'Expected admitted V26 installer SHA-256 must be 64-hex.'
}
$expectedInstallerSha256Canonical = $ExpectedInstallerSha256.ToLowerInvariant()

$held = New-Object 'System.Collections.Generic.List[object]'
try {
    $zipHeld = Open-Held -Path $PackageZip -Label 'V26 candidate ZIP'; $held.Add($zipHeld) | Out-Null
    $checksumHeld = Open-Held -Path $ChecksumPath -Label 'V26 candidate checksum'; $held.Add($checksumHeld) | Out-Null
    $provenanceHeld = Open-Held -Path $ProvenancePath -Label 'V26 candidate provenance'; $held.Add($provenanceHeld) | Out-Null
    $updateHeld = $null
    if (-not [string]::IsNullOrWhiteSpace($UpdateManifestPath)) { $updateHeld = Open-Held -Path $UpdateManifestPath -Label 'V26 update manifest'; $held.Add($updateHeld) | Out-Null }

    $zipHash = Get-HeldSha256 -Held $zipHeld
    $checksumText = (Read-HeldText -Held $checksumHeld -Label 'V26 candidate checksum').Trim()
    if ($checksumText -notmatch '^([0-9A-Fa-f]{64})  QS3D-BricsCAD-V26\.zip$') { throw 'V26 candidate checksum is malformed.' }
    if (-not [string]::Equals($Matches[1], $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 candidate checksum does not bind the held ZIP generation.' }

    $provenanceText = Read-HeldText -Held $provenanceHeld -Label 'V26 candidate provenance'
    if ((Get-JsonPropertyOccurrenceCount -JsonText $provenanceText -PropertyName 'installerSha256') -ne 1) {
        throw 'V26 candidate provenance must contain exactly one installerSha256 property.'
    }
    try { $provenance = $provenanceText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "V26 candidate provenance JSON is invalid: $($_.Exception.Message)" }
    if ([string]$provenance.product -ne 'QS3D' -or [string]$provenance.target -ne 'BricsCAD V26 x64') { throw 'V26 candidate provenance product/target identity is invalid.' }
    if (-not [string]::Equals([string]$provenance.releaseTag, $ExpectedReleaseTag, [StringComparison]::Ordinal)) { throw 'V26 candidate provenance release tag mismatch.' }
    if (-not [string]::Equals([string]$provenance.sourceCommit, $ExpectedSourceCommit, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 candidate provenance source commit mismatch.' }
    if (-not [string]::Equals([string]$provenance.packageSha256, $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 candidate provenance package digest mismatch.' }
    $installerSha256 = [string]$provenance.installerSha256
    if ($installerSha256 -cnotmatch '^[0-9a-f]{64}$') { throw 'V26 candidate provenance installer SHA-256 is noncanonical.' }
    if (-not [string]::Equals($installerSha256, $expectedInstallerSha256Canonical, [StringComparison]::Ordinal)) { throw 'V26 candidate provenance installer digest mismatch.' }

    $hostReferences = @($provenance.hostReferences)
    if ($hostReferences.Count -ne $requiredHostNames.Count) { throw 'V26 candidate provenance must contain exactly four held host-reference identities.' }
    foreach ($name in $requiredHostNames) {
        $matches = @($hostReferences | Where-Object { [string]::Equals([string]$_.name, $name, [StringComparison]::Ordinal) })
        if ($matches.Count -ne 1) { throw "V26 candidate provenance must contain exactly one $name host-reference identity." }
        $host = $matches[0]
        if ([string]$host.sha256 -cnotmatch '^[0-9a-f]{64}$') { throw "V26 candidate provenance host-reference SHA-256 is noncanonical for $name." }
        if ([long]$host.length -le 0) { throw "V26 candidate provenance host-reference length must be positive for $name." }
    }

    $archive = [IO.Compression.ZipArchive]::new($zipHeld.Stream, [IO.Compression.ZipArchiveMode]::Read, $true)
    try {
        $entries = @($archive.Entries | Where-Object { [string]::Equals([string]$_.FullName, 'PACKAGE-METADATA.json', [StringComparison]::Ordinal) })
        if ($entries.Count -ne 1) { throw "V26 candidate ZIP must contain exactly one PACKAGE-METADATA.json entry; found $($entries.Count)." }
        if ($entries[0].Length -gt $maxTextBytes) { throw 'V26 PACKAGE-METADATA.json exceeds the metadata safety limit.' }
        $entryStream = $entries[0].Open()
        try { $reader = [IO.StreamReader]::new($entryStream, $strictUtf8, $false, 4096, $true); try { $metadataText = $reader.ReadToEnd() } finally { $reader.Dispose() } }
        finally { $entryStream.Dispose() }
    } finally { $archive.Dispose(); $zipHeld.Stream.Position = 0 }
    try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "V26 PACKAGE-METADATA.json is invalid JSON: $($_.Exception.Message)" }
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.framework -ne 'net8.0-windows') { throw 'V26 candidate ZIP metadata identity is invalid.' }
    if (-not [string]::Equals(('v' + [string]$metadata.productVersion), $effectivePackageTag, [StringComparison]::Ordinal)) { throw 'V26 candidate ZIP productVersion does not match the expected package release tag.' }
    if (-not [string]::Equals([string]$metadata.productVersion, [string]$provenance.productVersion, [StringComparison]::Ordinal)) { throw 'V26 candidate ZIP/provenance productVersion mismatch.' }

    if ($null -ne $updateHeld) {
        if ([string]::IsNullOrWhiteSpace($ExpectedPackageUri)) { throw 'ExpectedPackageUri is required when UpdateManifestPath is supplied.' }
        if ([string]::IsNullOrWhiteSpace($ExpectedSignerThumbprint)) { throw 'ExpectedSignerThumbprint is required when UpdateManifestPath is supplied.' }
        if ($ExpectedSignerThumbprint -cnotmatch '^[0-9A-Fa-f]{40}$') { throw 'ExpectedSignerThumbprint must be exactly 40 hexadecimal characters when UpdateManifestPath is supplied.' }
        if ($ExpectedManifestSchemaVersion -le 0) { throw 'ExpectedManifestSchemaVersion must be positive when UpdateManifestPath is supplied.' }

        $expectedUri = $null
        if (-not [Uri]::TryCreate($ExpectedPackageUri, [UriKind]::Absolute, [ref]$expectedUri) -or
            $expectedUri.Scheme -ne [Uri]::UriSchemeHttps -or
            [string]::IsNullOrWhiteSpace($expectedUri.Host) -or
            -not [string]::IsNullOrEmpty($expectedUri.UserInfo)) {
            throw 'ExpectedPackageUri must be an absolute HTTPS URI without embedded credentials.'
        }
        $expectedSigner = $ExpectedSignerThumbprint.ToUpperInvariant()

        $updateText = Read-HeldText -Held $updateHeld -Label 'V26 update manifest'
        foreach ($propertyName in @('schemaVersion', 'packageUri', 'sha256', 'signerThumbprint')) {
            if ((Get-JsonPropertyOccurrenceCount -JsonText $updateText -PropertyName $propertyName) -ne 1) {
                throw "V26 update manifest must contain exactly one $propertyName property."
            }
        }
        try { $update = $updateText | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "V26 update manifest JSON is invalid: $($_.Exception.Message)" }
        if ([string]$update.product -ne 'QS3D' -or [string]$update.target -ne 'BricsCAD V26 x64') { throw 'V26 update manifest product/target identity is invalid.' }
        if (-not [string]::Equals([string]$update.productVersion, [string]$metadata.productVersion, [StringComparison]::Ordinal)) { throw 'V26 update manifest productVersion mismatch.' }
        if (-not [string]::Equals([string]$update.sha256, $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 update manifest package digest mismatch.' }
        if ([int]$update.schemaVersion -ne $ExpectedManifestSchemaVersion) { throw 'V26 update manifest schema version mismatch.' }

        $actualUri = $null
        $actualUriText = [string]$update.packageUri
        if (-not [Uri]::TryCreate($actualUriText, [UriKind]::Absolute, [ref]$actualUri) -or
            $actualUri.Scheme -ne [Uri]::UriSchemeHttps -or
            [string]::IsNullOrWhiteSpace($actualUri.Host) -or
            -not [string]::IsNullOrEmpty($actualUri.UserInfo)) {
            throw 'V26 update manifest package URI is not an absolute HTTPS URI without embedded credentials.'
        }
        if (-not [string]::Equals($actualUri.AbsoluteUri, $expectedUri.AbsoluteUri, [StringComparison]::Ordinal)) { throw 'V26 update manifest package URI mismatch.' }

        $actualSigner = [string]$update.signerThumbprint
        if ($actualSigner -cnotmatch '^[0-9A-F]{40}$') { throw 'V26 update manifest signer thumbprint must be canonical uppercase 40-hex.' }
        if (-not [string]::Equals($actualSigner, $expectedSigner, [StringComparison]::Ordinal)) { throw 'V26 update manifest signer thumbprint mismatch.' }
    }

    $admittedScriptBlock = $null
    if (-not [string]::IsNullOrWhiteSpace($AdmittedScript)) {
        $scriptHeld = Open-Held -Path $AdmittedScript -Label 'V26 admitted publication script'
        $held.Add($scriptHeld) | Out-Null
        $scriptText = Read-HeldText -Held $scriptHeld -Label 'V26 admitted publication script' -MaxBytes $maxAdmittedScriptBytes
        try { $admittedScriptBlock = [ScriptBlock]::Create($scriptText) }
        catch { throw "V26 admitted publication script cannot be parsed from its held generation: $($_.Exception.Message)" }
    }

    foreach ($item in $held) { Assert-Held -Held $item -Label 'V26 candidate identity input' }
    $identity = [pscustomobject]@{
        SourceCommit=$ExpectedSourceCommit.ToLowerInvariant()
        ReleaseTag=$ExpectedReleaseTag
        ProductVersion=[string]$metadata.productVersion
        PackageSha256=$zipHash
        InstallerSha256=$installerSha256
        Signed=($null -ne $updateHeld)
        ManifestSchemaVersion=$(if ($null -ne $updateHeld) { [int]$update.schemaVersion } else { $null })
        PackageUri=$(if ($null -ne $updateHeld) { [string]$update.packageUri } else { $null })
        SignerThumbprint=$(if ($null -ne $updateHeld) { [string]$update.signerThumbprint } else { $null })
        HostReferences=$hostReferences
    }
    if ($null -ne $admittedScriptBlock) {
        & $admittedScriptBlock
        foreach ($item in $held) { Assert-Held -Held $item -Label 'V26 candidate identity input after publication' }
    }
    $identity
}
finally { for ($i=$held.Count-1; $i -ge 0; $i--) { $held[$i].Stream.Dispose() } }
