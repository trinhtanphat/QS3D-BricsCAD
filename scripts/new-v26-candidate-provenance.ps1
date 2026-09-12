[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$PackageZip,
    [Parameter(Mandatory = $true)][ValidatePattern('^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')][string]$ReleaseTag,
    [ValidatePattern('^v(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)\.(?:0|[1-9][0-9]*)(?:-[0-9A-Za-z.-]+)?(?:\+[0-9A-Za-z.-]+)?$')][string]$PackageReleaseTag,
    [Parameter(Mandatory = $true)][ValidatePattern('^[0-9A-Fa-f]{40}$')][string]$SourceCommit,
    [string]$HostReferenceStatePath = $env:V26_HOST_REFERENCE_STATE,
    [string]$InstallerSha256 = $env:BRICSCAD_V26_PINNED_MSI_SHA256,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)
$maxMetadataBytes = 65536
$requiredHostNames = @('bricscad.exe', 'BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')

function Resolve-OrdinaryFile([string]$Path, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path must not be empty." }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse file: $Path" }
    $cursor = $item.Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
    return $item
}

function Resolve-OrdinaryDirectory([string]$Path, [string]$Label) {
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path must not be empty." }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse directory: $Path" }
    $cursor = $item
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
    return $item
}

function Get-HeldSha256([IO.Stream]$Stream) {
    $Stream.Position = 0
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($Stream))).Replace('-', '').ToLowerInvariant() }
    finally { $sha.Dispose(); $Stream.Position = 0 }
}

function Get-OrdinaryFileIdentity([string]$Path, [string]$Label) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    $stream = [IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $hash = Get-HeldSha256 -Stream $stream
        $current = Resolve-OrdinaryFile -Path $item.FullName -Label $Label
        if ($current.Length -ne $stream.Length -or $current.LastWriteTimeUtc.Ticks -ne $item.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its generation was being admitted."
        }
        return [pscustomobject]@{ Path=$current.FullName; Length=[long]$stream.Length; LastWriteUtcTicks=[long]$current.LastWriteTimeUtc.Ticks; Sha256=$hash }
    }
    finally { $stream.Dispose() }
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
        if ([string]::Equals($decodedName, $PropertyName, [StringComparison]::OrdinalIgnoreCase)) { $count++ }
    }
    return $count
}

function Assert-JsonPropertyCounts([string]$JsonText, [hashtable]$ExpectedPropertyCounts, [string]$Label) {
    foreach ($propertyName in @($ExpectedPropertyCounts.Keys)) {
        $expectedCount = [int]$ExpectedPropertyCounts[$propertyName]
        $actualCount = Get-JsonPropertyOccurrenceCount -JsonText $JsonText -PropertyName $propertyName
        if ($actualCount -ne $expectedCount) {
            throw "$Label must contain exactly $expectedCount $propertyName properties; found $actualCount."
        }
    }
}

function Read-StrictUtf8Json([string]$Path, [string]$Label, [hashtable]$ExpectedPropertyCounts) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    if ($item.Length -gt $maxMetadataBytes) { throw "$Label exceeds the $maxMetadataBytes-byte safety limit." }
    $stream = [IO.File]::Open($item.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $bytes = [byte[]]::new([int]$stream.Length)
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) { throw "$Label ended before its held length was read." }
            $offset += $read
        }
        $current = Resolve-OrdinaryFile -Path $item.FullName -Label $Label
        if ($current.Length -ne $stream.Length -or $current.LastWriteTimeUtc.Ticks -ne $item.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its generation was being admitted."
        }
        try { $text = $strictUtf8.GetString($bytes) }
        catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
        Assert-JsonPropertyCounts -JsonText $text -ExpectedPropertyCounts $ExpectedPropertyCounts -Label $Label
        try { return $text | ConvertFrom-Json -ErrorAction Stop }
        catch { throw "$Label JSON is invalid: $($_.Exception.Message)" }
    }
    finally { $stream.Dispose() }
}

if (-not ('Qs3dProvenanceGenerationNative' -as [type])) {
    Add-Type -Language CSharp -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class Qs3dProvenanceGenerationNative
{
    private const uint DeleteAccess = 0x00010000;
    private const uint GenericRead = 0x80000000;
    private const uint GenericWrite = 0x40000000;
    private const uint FileReadAttributes = 0x00000080;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
    private const uint CreateNew = 1;
    private const uint OpenExisting = 3;
    private const uint FileAttributeNormal = 0x00000080;
    private const uint FileFlagOpenReparsePoint = 0x00200000;
    private const uint FileAttributeReparsePoint = 0x00000400;

    private enum FileInfoByHandleClass
    {
        FileRenameInfo = 3,
        FileDispositionInfo = 4
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeFileTime { public uint Low; public uint High; }

    [StructLayout(LayoutKind.Sequential)]
    private struct ByHandleFileInformation
    {
        public uint FileAttributes;
        public NativeFileTime CreationTime;
        public NativeFileTime LastAccessTime;
        public NativeFileTime LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileDispositionInformation
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool DeleteFile;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string fileName, uint desiredAccess, uint shareMode, IntPtr securityAttributes,
        uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(SafeFileHandle file, out ByHandleFileInformation information);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle file, FileInfoByHandleClass informationClass,
        ref FileDispositionInformation information, uint bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle file, byte[] buffer, uint bytesToWrite,
        out uint bytesWritten, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool ReadFile(
        SafeFileHandle file, byte[] buffer, uint bytesToRead,
        out uint bytesRead, IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushFileBuffers(SafeFileHandle file);

    private static ByHandleFileInformation Information(SafeFileHandle handle)
    {
        if (handle == null || handle.IsInvalid || handle.IsClosed)
            throw new InvalidOperationException("Owned provenance generation handle is not open.");
        ByHandleFileInformation information;
        if (!GetFileInformationByHandle(handle, out information))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFileInformationByHandle failed for owned provenance generation.");
        if ((information.FileAttributes & FileAttributeReparsePoint) != 0)
            throw new InvalidOperationException("Owned provenance generation resolved to a reparse-point file.");
        return information;
    }

    private static string Identity(ByHandleFileInformation information)
    {
        return string.Format("{0:X8}:{1:X8}{2:X8}", information.VolumeSerialNumber, information.FileIndexHigh, information.FileIndexLow);
    }

    public static SafeFileHandle CreateOwnedProvenanceGeneration(string path, byte[] bytes)
    {
        var handle = CreateFileW(
            path,
            GenericRead | GenericWrite | DeleteAccess | FileReadAttributes,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            CreateNew,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to create owned provenance generation: " + path);
        }
        try
        {
            Information(handle);
            uint written;
            if (bytes.Length > 0 && (!WriteFile(handle, bytes, (uint)bytes.Length, out written, IntPtr.Zero) || written != (uint)bytes.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to write owned provenance generation.");
            if (!FlushFileBuffers(handle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to flush owned provenance generation.");
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static SafeFileHandle OpenPinnedPublishedProvenanceGeneration(string path, string expectedIdentity)
    {
        var handle = CreateFileW(
            path,
            GenericRead | DeleteAccess | FileReadAttributes,
            FileShareRead,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to pin published provenance generation: " + path);
        }
        try
        {
            var information = Information(handle);
            var actualIdentity = Identity(information);
            if (!string.Equals(actualIdentity, expectedIdentity, StringComparison.Ordinal))
                throw new InvalidOperationException("Published provenance generation identity changed before pin: expected " + expectedIdentity + ", got " + actualIdentity + ".");
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static byte[] ReadPinnedPublishedProvenanceBytes(SafeFileHandle handle, int expectedLength)
    {
        var information = Information(handle);
        long length = ((long)information.FileSizeHigh << 32) | information.FileSizeLow;
        if (length != expectedLength)
            throw new InvalidOperationException("Published provenance byte length changed while pinned.");
        var bytes = new byte[expectedLength];
        if (expectedLength == 0) return bytes;
        uint read;
        if (!ReadFile(handle, bytes, (uint)expectedLength, out read, IntPtr.Zero) || read != (uint)expectedLength)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to read pinned published provenance generation.");
        return bytes;
    }

    public static string GetOwnedProvenanceGenerationIdentity(SafeFileHandle handle)
    {
        return Identity(Information(handle));
    }

    public static void RemoveOwnedProvenanceGeneration(SafeFileHandle handle)
    {
        Information(handle);
        var disposition = new FileDispositionInformation { DeleteFile = true };
        if (!SetFileInformationByHandle(handle, FileInfoByHandleClass.FileDispositionInfo, ref disposition, (uint)Marshal.SizeOf(typeof(FileDispositionInformation))))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetFileInformationByHandle(FileDispositionInfo) failed for owned provenance generation.");
    }
}
'@
}

function New-OwnedProvenanceGeneration([string]$Path, [byte[]]$Bytes, [string]$Label) {
    if (Test-Path -LiteralPath $Path) { throw "Refusing to reuse V26 provenance staging path: $Path" }
    $parentPath = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Path))
    $null = Resolve-OrdinaryDirectory -Path $parentPath -Label "$Label parent"
    $handle = [Qs3dProvenanceGenerationNative]::CreateOwnedProvenanceGeneration([IO.Path]::GetFullPath($Path), $Bytes)
    try {
        $identity = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($handle)
        return [pscustomobject]@{ Handle=$handle; Identity=$identity; Path=[IO.Path]::GetFullPath($Path); Label=$Label }
    }
    catch { $handle.Dispose(); throw }
}

function Get-OwnedProvenanceGenerationIdentity($Generation) {
    return [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($Generation.Handle)
}

function Open-PinnedPublishedProvenanceGeneration([string]$Path, [string]$ExpectedIdentity) {
    $fullPath = [IO.Path]::GetFullPath($Path)
    $handle = [Qs3dProvenanceGenerationNative]::OpenPinnedPublishedProvenanceGeneration($fullPath, $ExpectedIdentity)
    try {
        $identity = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($handle)
        return [pscustomobject]@{ Handle=$handle; Identity=$identity; Path=$fullPath; Label='V26 published provenance generation' }
    }
    catch { $handle.Dispose(); throw }
}

function Assert-PinnedPublishedProvenanceBytes($Generation, [byte[]]$ExpectedBytes) {
    $currentIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $Generation
    if (-not [string]::Equals($currentIdentity, $Generation.Identity, [StringComparison]::Ordinal)) {
        throw "$($Generation.Label) identity changed while pinned: expected $($Generation.Identity), got $currentIdentity"
    }
    $actualBytes = [Qs3dProvenanceGenerationNative]::ReadPinnedPublishedProvenanceBytes($Generation.Handle, $ExpectedBytes.Length)
    if ($actualBytes.Length -ne $ExpectedBytes.Length) { throw "$($Generation.Label) byte length changed while pinned." }
    for ($i = 0; $i -lt $ExpectedBytes.Length; $i++) {
        if ($actualBytes[$i] -ne $ExpectedBytes[$i]) { throw "$($Generation.Label) bytes differ from the staged provenance generation." }
    }
}

function Remove-OwnedProvenanceGeneration($Generation) {
    $currentIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $Generation
    if (-not [string]::Equals($currentIdentity, $Generation.Identity, [StringComparison]::Ordinal)) {
        throw "$($Generation.Label) identity changed while owned: expected $($Generation.Identity), got $currentIdentity"
    }
    try { [Qs3dProvenanceGenerationNative]::RemoveOwnedProvenanceGeneration($Generation.Handle) }
    finally { $Generation.Handle.Dispose() }
}

function Close-OwnedProvenanceGeneration($Generation) {
    if ($null -ne $Generation) { $Generation.Handle.Dispose() }
}

if ([string]::IsNullOrWhiteSpace($InstallerSha256) -or $InstallerSha256 -cnotmatch '^[0-9a-f]{64}$') {
    throw 'V26 admitted installer SHA-256 must be canonical lowercase 64-hex.'
}

$hostExpectedPropertyCounts = @{
    Version = 1
    Files = 1
    Name = $requiredHostNames.Count
    Path = $requiredHostNames.Count
    Sha256 = $requiredHostNames.Count
    Length = $requiredHostNames.Count
}
$hostState = Read-StrictUtf8Json -Path $HostReferenceStatePath -Label 'V26 host-reference state' -ExpectedPropertyCounts $hostExpectedPropertyCounts
if ([int]$hostState.Version -ne 1) { throw 'V26 host-reference state version must be 1.' }
$hostFiles = @($hostState.Files)
if ($hostFiles.Count -ne $requiredHostNames.Count) { throw 'V26 host-reference state must contain exactly four required files.' }
$hostReferences = foreach ($name in $requiredHostNames) {
    $matches = @($hostFiles | Where-Object { [string]::Equals([string]$_.Name, $name, [StringComparison]::Ordinal) })
    if ($matches.Count -ne 1) { throw "V26 host-reference state must contain exactly one $name record." }
    $record = $matches[0]
    $sha256 = [string]$record.Sha256
    $length = [long]$record.Length
    $path = [string]$record.Path
    if ($sha256 -cnotmatch '^[0-9a-f]{64}$') { throw "V26 host-reference SHA-256 must be canonical lowercase hex for $name." }
    if ($length -le 0) { throw "V26 host-reference length must be positive for $name." }
    $actual = Get-OrdinaryFileIdentity -Path $path -Label "V26 host reference $name"
    if ($actual.Length -ne $length -or -not [string]::Equals($actual.Sha256, $sha256, [StringComparison]::Ordinal)) {
        throw "V26 host reference $name changed after its admitted generation was captured."
    }
    [ordered]@{ name = $name; length = $length; sha256 = $sha256 }
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

    $metadataExpectedPropertyCounts = @{
        product = 1
        target = 1
        framework = 1
        productVersion = 1
    }
    Assert-JsonPropertyCounts -JsonText $metadataText -ExpectedPropertyCounts $metadataExpectedPropertyCounts -Label 'V26 PACKAGE-METADATA.json'
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
        installerSha256 = $InstallerSha256
        hostReferences = @($hostReferences)
    }

    $outputFull = [IO.Path]::GetFullPath($OutputPath)
    $parent = Split-Path -Parent $outputFull
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) { New-Item -ItemType Directory -Path $parent | Out-Null }
    $null = Resolve-OrdinaryDirectory -Path $parent -Label 'V26 provenance output directory'
    if (Test-Path -LiteralPath $outputFull) { $null = Resolve-OrdinaryFile -Path $outputFull -Label 'V26 provenance output' }

    $tempPath = Join-Path $parent ('.' + [IO.Path]::GetFileName($outputFull) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
    $provenanceText = ($provenance | ConvertTo-Json -Depth 5) + [Environment]::NewLine
    $provenanceBytes = $strictUtf8.GetBytes($provenanceText)
    if ($provenanceBytes.Length -gt $maxMetadataBytes) { throw 'V26 candidate provenance exceeds the metadata safety limit.' }

    $tempGeneration = New-OwnedProvenanceGeneration -Path $tempPath -Bytes $provenanceBytes -Label 'V26 provenance staging generation'
    $publishedGeneration = $null
    $publicationCommitted = $false
    try {
        $attemptIdentity = Get-OwnedProvenanceGenerationIdentity -Generation $tempGeneration
        if (Test-Path -LiteralPath $outputFull) {
            $null = Resolve-OrdinaryFile -Path $outputFull -Label 'V26 provenance output'
            [IO.File]::Replace($tempPath, $outputFull, $null)
        }
        else {
            [IO.File]::Move($tempPath, $outputFull)
        }

        Close-OwnedProvenanceGeneration -Generation $tempGeneration
        $tempGeneration = $null
        $publishedGeneration = Open-PinnedPublishedProvenanceGeneration -Path $outputFull -ExpectedIdentity $attemptIdentity
        Assert-PinnedPublishedProvenanceBytes -Generation $publishedGeneration -ExpectedBytes $provenanceBytes
        $publicationCommitted = $true
    }
    finally {
        if ($publicationCommitted) {
            if ($null -ne $publishedGeneration) { Close-OwnedProvenanceGeneration -Generation $publishedGeneration }
        }
        elseif ($null -ne $publishedGeneration) {
            Remove-OwnedProvenanceGeneration -Generation $publishedGeneration
        }
        elseif ($null -ne $tempGeneration) {
            Remove-OwnedProvenanceGeneration -Generation $tempGeneration
        }
        $publishedGeneration = $null
        $tempGeneration = $null
    }

    [pscustomobject]@{ SourceCommit = $provenance.sourceCommit; PackageSha256 = $zipHash; ProductVersion = $productVersion; InstallerSha256 = $InstallerSha256; HostReferences = @($hostReferences) }
}
finally { $zipStream.Dispose() }
