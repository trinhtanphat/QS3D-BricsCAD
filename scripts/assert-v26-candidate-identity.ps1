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
Add-Type -AssemblyName System.IO.Compression
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
    if ($Stream.SafeFileHandle.IsInvalid -or $Stream.SafeFileHandle.IsClosed) { throw 'V26 held candidate stream does not have a live file handle.' }
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
            if ($resolved.StartsWith('\\?\UNC\', [StringComparison]::OrdinalIgnoreCase)) { $resolved = '\\' + $resolved.Substring(8) }
            elseif ($resolved.StartsWith('\\?\', [StringComparison]::OrdinalIgnoreCase)) { $resolved = $resolved.Substring(4) }
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
        if (-not [string]::Equals($openedFinalPath, $canonicalPath, [StringComparison]::OrdinalIgnoreCase)) { throw "$Label opened handle resolved to a different final path; refusing candidate admission." }
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

function Normalize-JsonIdentityText([string]$JsonText) {
    if ($null -eq $JsonText) { throw 'V26 admitted JSON identity document is null.' }
    if ($JsonText.Length -gt 0 -and [int][char]$JsonText[0] -eq 0xFEFF) {
        if ($JsonText.Length -gt 1 -and [int][char]$JsonText[1] -eq 0xFEFF) { throw 'V26 admitted JSON identity document contains multiple leading BOM markers.' }
        return $JsonText.Substring(1)
    }
    return $JsonText
}

function Get-JsonPropertyOccurrenceCount([string]$JsonText, [string]$PropertyName) {
    $firstNonWhitespace = 0
    while ($firstNonWhitespace -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$firstNonWhitespace])) { $firstNonWhitespace++ }
    if ($firstNonWhitespace -ge $JsonText.Length -or $JsonText[$firstNonWhitespace] -ne '{') { throw 'V26 admitted JSON identity document must have a top-level object.' }
    $count = 0
    $objectDepth = 0
    $arrayDepth = 0
    $i = 0
    while ($i -lt $JsonText.Length) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $tokenStart = $i
            $i++
            $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw 'V26 admitted JSON contains an unterminated string token.' }
            $tokenEnd = $i
            $lookahead = $tokenEnd + 1
            while ($lookahead -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$lookahead])) { $lookahead++ }
            if ($objectDepth -eq 1 -and $arrayDepth -eq 0 -and $lookahead -lt $JsonText.Length -and $JsonText[$lookahead] -eq ':') {
                $rawPropertyToken = $JsonText.Substring($tokenStart, $tokenEnd - $tokenStart + 1)
                try { $decodedPropertyName = [string]($rawPropertyToken | ConvertFrom-Json -ErrorAction Stop) }
                catch { throw 'V26 admitted JSON contains malformed property encoding.' }
                if ([string]::Equals($decodedPropertyName, $PropertyName, [StringComparison]::OrdinalIgnoreCase)) { $count++ }
            }
            $i = $tokenEnd + 1
            continue
        }
        switch ($ch) {
            '{' { $objectDepth++ }
            '}' { $objectDepth--; if ($objectDepth -lt 0) { throw 'V26 admitted JSON has invalid object nesting.' } }
            '[' { $arrayDepth++ }
            ']' { $arrayDepth--; if ($arrayDepth -lt 0) { throw 'V26 admitted JSON has invalid array nesting.' } }
        }
        $i++
    }
    if ($objectDepth -ne 0 -or $arrayDepth -ne 0) { throw 'V26 admitted JSON has unbalanced container nesting.' }
    return $count
}

function Get-JsonTopLevelArrayObjectTexts([string]$JsonText, [string]$ArrayPropertyName, [string]$Label) {
    $firstNonWhitespace = 0
    while ($firstNonWhitespace -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$firstNonWhitespace])) { $firstNonWhitespace++ }
    if ($firstNonWhitespace -ge $JsonText.Length -or $JsonText[$firstNonWhitespace] -ne '{') { throw "$Label must have a top-level object." }
    $objectDepth = 0; $arrayDepth = 0; $arrayStart = -1; $i = 0
    while ($i -lt $JsonText.Length -and $arrayStart -lt 0) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $tokenStart = $i; $i++; $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw "$Label contains an unterminated string token." }
            $tokenEnd = $i; $lookahead = $tokenEnd + 1
            while ($lookahead -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$lookahead])) { $lookahead++ }
            if ($objectDepth -eq 1 -and $arrayDepth -eq 0 -and $lookahead -lt $JsonText.Length -and $JsonText[$lookahead] -eq ':') {
                $rawPropertyToken = $JsonText.Substring($tokenStart, $tokenEnd - $tokenStart + 1)
                try { $decodedPropertyName = [string]($rawPropertyToken | ConvertFrom-Json -ErrorAction Stop) }
                catch { throw "$Label contains malformed property encoding." }
                if ([string]::Equals($decodedPropertyName, $ArrayPropertyName, [StringComparison]::OrdinalIgnoreCase)) {
                    $valueStart = $lookahead + 1
                    while ($valueStart -lt $JsonText.Length -and [char]::IsWhiteSpace($JsonText[$valueStart])) { $valueStart++ }
                    if ($valueStart -ge $JsonText.Length -or $JsonText[$valueStart] -ne '[') { throw "$Label property '$ArrayPropertyName' must be an array." }
                    $arrayStart = $valueStart
                    break
                }
            }
            $i = $tokenEnd + 1; continue
        }
        switch ($ch) { '{' { $objectDepth++ } '}' { $objectDepth-- } '[' { $arrayDepth++ } ']' { $arrayDepth-- } }
        $i++
    }
    if ($arrayStart -lt 0) { throw "$Label is missing top-level array property '$ArrayPropertyName'." }
    $items = [Collections.Generic.List[string]]::new()
    $arrayDepth = 0; $objectDepth = 0; $objectStart = -1; $i = $arrayStart
    while ($i -lt $JsonText.Length) {
        $ch = $JsonText[$i]
        if ($ch -eq '"') {
            $i++; $closed = $false
            while ($i -lt $JsonText.Length) {
                if ($JsonText[$i] -eq '\') { $i += 2; continue }
                if ($JsonText[$i] -eq '"') { $closed = $true; break }
                $i++
            }
            if (-not $closed) { throw "$Label contains an unterminated string token in '$ArrayPropertyName'." }
            $i++; continue
        }
        switch ($ch) {
            '[' { $arrayDepth++ }
            ']' {
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0) { return @($items) }
                $arrayDepth--
                if ($arrayDepth -lt 0) { throw "$Label has invalid array nesting in '$ArrayPropertyName'." }
            }
            '{' { if ($arrayDepth -eq 1 -and $objectDepth -eq 0) { $objectStart = $i }; $objectDepth++ }
            '}' {
                $objectDepth--
                if ($objectDepth -lt 0) { throw "$Label has invalid object nesting in '$ArrayPropertyName'." }
                if ($arrayDepth -eq 1 -and $objectDepth -eq 0 -and $objectStart -ge 0) {
                    $items.Add($JsonText.Substring($objectStart, $i - $objectStart + 1)); $objectStart = -1
                }
            }
        }
        $i++
    }
    throw "$Label array property '$ArrayPropertyName' is unterminated."
}

function Assert-JsonPropertyCounts([string]$JsonText, [hashtable]$ExpectedPropertyCounts, [string]$Label) {
    foreach ($propertyName in @($ExpectedPropertyCounts.Keys)) {
        $expectedCount = [int]$ExpectedPropertyCounts[$propertyName]
        $actualCount = Get-JsonPropertyOccurrenceCount -JsonText $JsonText -PropertyName $propertyName
        if ($actualCount -ne $expectedCount) { throw "$Label must contain exactly $expectedCount top-level $propertyName properties; found $actualCount." }
    }
}

function Assert-JsonArrayObjectPropertyCounts([string]$JsonText, [string]$ArrayPropertyName, [int]$ExpectedObjectCount, [hashtable]$ExpectedPropertyCounts, [string]$Label) {
    $objects = @(Get-JsonTopLevelArrayObjectTexts -JsonText $JsonText -ArrayPropertyName $ArrayPropertyName -Label $Label)
    if ($objects.Count -ne $ExpectedObjectCount) { throw "$Label property '$ArrayPropertyName' must contain exactly $ExpectedObjectCount object records; found $($objects.Count)." }
    for ($recordIndex = 0; $recordIndex -lt $objects.Count; $recordIndex++) {
        Assert-JsonPropertyCounts -JsonText $objects[$recordIndex] -ExpectedPropertyCounts $ExpectedPropertyCounts -Label "$Label $ArrayPropertyName[$recordIndex]"
    }
}

if ([string]::IsNullOrWhiteSpace($ExpectedInstallerSha256) -or $ExpectedInstallerSha256 -cnotmatch '^[0-9A-Fa-f]{64}$') { throw 'Expected admitted V26 installer SHA-256 must be 64-hex.' }
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
    $provenanceText = Normalize-JsonIdentityText -JsonText $provenanceText
    $provenanceExpectedPropertyCounts = @{ product=1; target=1; releaseTag=1; sourceCommit=1; productVersion=1; packageSha256=1; installerSha256=1; hostReferences=1 }
    Assert-JsonPropertyCounts -JsonText $provenanceText -ExpectedPropertyCounts $provenanceExpectedPropertyCounts -Label 'V26 candidate provenance'
    $hostReferenceExpectedPropertyCounts = @{ name=1; sha256=1; length=1 }
    Assert-JsonArrayObjectPropertyCounts -JsonText $provenanceText -ArrayPropertyName 'hostReferences' -ExpectedObjectCount $requiredHostNames.Count -ExpectedPropertyCounts $hostReferenceExpectedPropertyCounts -Label 'V26 candidate provenance'
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
        $hostReference = $matches[0]
        if ([string]$hostReference.sha256 -cnotmatch '^[0-9a-f]{64}$') { throw "V26 candidate provenance host-reference SHA-256 is noncanonical for $name." }
        if ([long]$hostReference.length -le 0) { throw "V26 candidate provenance host-reference length must be positive for $name." }
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
    $metadataText = Normalize-JsonIdentityText -JsonText $metadataText
    $metadataExpectedPropertyCounts = @{ product=1; target=1; framework=1; productVersion=1 }
    Assert-JsonPropertyCounts -JsonText $metadataText -ExpectedPropertyCounts $metadataExpectedPropertyCounts -Label 'V26 PACKAGE-METADATA.json'
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
        if (-not [Uri]::TryCreate($ExpectedPackageUri, [UriKind]::Absolute, [ref]$expectedUri) -or $expectedUri.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($expectedUri.Host) -or -not [string]::IsNullOrEmpty($expectedUri.UserInfo)) { throw 'ExpectedPackageUri must be an absolute HTTPS URI without embedded credentials.' }
        $expectedSigner = $ExpectedSignerThumbprint.ToUpperInvariant()
        $updateText = Read-HeldText -Held $updateHeld -Label 'V26 update manifest'
        $updateText = Normalize-JsonIdentityText -JsonText $updateText
        $updateExpectedPropertyCounts = @{ product=1; target=1; productVersion=1; schemaVersion=1; packageUri=1; sha256=1; signerThumbprint=1 }
        Assert-JsonPropertyCounts -JsonText $updateText -ExpectedPropertyCounts $updateExpectedPropertyCounts -Label 'V26 update manifest'
        try { $update = $updateText | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "V26 update manifest JSON is invalid: $($_.Exception.Message)" }
        if ([string]$update.product -ne 'QS3D' -or [string]$update.target -ne 'BricsCAD V26 x64') { throw 'V26 update manifest product/target identity is invalid.' }
        if (-not [string]::Equals([string]$update.productVersion, [string]$metadata.productVersion, [StringComparison]::Ordinal)) { throw 'V26 update manifest productVersion mismatch.' }
        if (-not [string]::Equals([string]$update.sha256, $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 update manifest package digest mismatch.' }
        if ([int]$update.schemaVersion -ne $ExpectedManifestSchemaVersion) { throw 'V26 update manifest schema version mismatch.' }
        $actualUri = $null
        $actualUriText = [string]$update.packageUri
        if (-not [Uri]::TryCreate($actualUriText, [UriKind]::Absolute, [ref]$actualUri) -or $actualUri.Scheme -ne [Uri]::UriSchemeHttps -or [string]::IsNullOrWhiteSpace($actualUri.Host) -or -not [string]::IsNullOrEmpty($actualUri.UserInfo)) { throw 'V26 update manifest package URI is not an absolute HTTPS URI without embedded credentials.' }
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
