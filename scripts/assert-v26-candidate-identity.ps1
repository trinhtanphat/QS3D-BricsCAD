[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][string]$ChecksumPath,
    [Parameter(Mandatory = $true)][string]$ProvenancePath,
    [string]$UpdateManifestPath,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{40}$')][string]$ExpectedSourceCommit,
    [Parameter(Mandatory = $true)][string]$ExpectedReleaseTag,
    [string]$AdmittedScript
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$maxTextBytes = 65536

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

function Open-Held([string]$Path, [string]$Label) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    $stream = [IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $current = Resolve-OrdinaryFile -Path $item.FullName -Label $Label
        if ($item.Length -ne $stream.Length -or $item.LastWriteTimeUtc.Ticks -ne $current.LastWriteTimeUtc.Ticks -or $current.Length -ne $stream.Length) { throw "$Label changed while its generation lock was admitted." }
        return [pscustomobject]@{ Path=$current.FullName; Length=[int64]$stream.Length; LastWriteUtcTicks=[int64]$current.LastWriteTimeUtc.Ticks; Stream=$stream }
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

function Read-HeldText([pscustomobject]$Held, [string]$Label) {
    if ($Held.Length -gt $maxTextBytes) { throw "$Label exceeds the $maxTextBytes-byte safety limit." }
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

    try { $provenance = (Read-HeldText -Held $provenanceHeld -Label 'V26 candidate provenance') | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "V26 candidate provenance JSON is invalid: $($_.Exception.Message)" }
    if ([string]$provenance.product -ne 'QS3D' -or [string]$provenance.target -ne 'BricsCAD V26 x64') { throw 'V26 candidate provenance product/target identity is invalid.' }
    if (-not [string]::Equals([string]$provenance.releaseTag, $ExpectedReleaseTag, [StringComparison]::Ordinal)) { throw 'V26 candidate provenance release tag mismatch.' }
    if (-not [string]::Equals([string]$provenance.sourceCommit, $ExpectedSourceCommit, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 candidate provenance source commit mismatch.' }
    if (-not [string]::Equals([string]$provenance.packageSha256, $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 candidate provenance package digest mismatch.' }

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
    if (-not [string]::Equals(('v' + [string]$metadata.productVersion), $ExpectedReleaseTag, [StringComparison]::Ordinal)) { throw 'V26 candidate ZIP productVersion does not match the expected release tag.' }
    if (-not [string]::Equals([string]$metadata.productVersion, [string]$provenance.productVersion, [StringComparison]::Ordinal)) { throw 'V26 candidate ZIP/provenance productVersion mismatch.' }

    if ($null -ne $updateHeld) {
        try { $update = (Read-HeldText -Held $updateHeld -Label 'V26 update manifest') | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "V26 update manifest JSON is invalid: $($_.Exception.Message)" }
        if ([string]$update.product -ne 'QS3D' -or [string]$update.target -ne 'BricsCAD V26 x64') { throw 'V26 update manifest product/target identity is invalid.' }
        if (-not [string]::Equals([string]$update.productVersion, [string]$metadata.productVersion, [StringComparison]::Ordinal)) { throw 'V26 update manifest productVersion mismatch.' }
        if (-not [string]::Equals([string]$update.sha256, $zipHash, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 update manifest package digest mismatch.' }
    }

    foreach ($item in $held) { Assert-Held -Held $item -Label 'V26 candidate identity input' }
    $identity = [pscustomobject]@{ SourceCommit=$ExpectedSourceCommit.ToLowerInvariant(); ReleaseTag=$ExpectedReleaseTag; ProductVersion=[string]$metadata.productVersion; PackageSha256=$zipHash; Signed=($null -ne $updateHeld) }
    if (-not [string]::IsNullOrWhiteSpace($AdmittedScript)) {
        $scriptItem = Resolve-OrdinaryFile -Path $AdmittedScript -Label 'V26 admitted publication script'
        & $scriptItem.FullName
        foreach ($item in $held) { Assert-Held -Held $item -Label 'V26 candidate identity input after publication' }
    }
    $identity
}
finally { for ($i=$held.Count-1; $i -ge 0; $i--) { $held[$i].Stream.Dispose() } }
