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

function Read-StrictUtf8Json([string]$Path, [string]$Label) {
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
    private const uint FileReadAttributes = 0x00000080;
    private const uint FileShareRead = 0x00000001;
    private const uint FileShareWrite = 0x00000002;
    private const uint FileShareDelete = 0x00000004;
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

    public static SafeFileHandle OpenOwnedProvenanceGeneration(string path)
    {
        var handle = CreateFileW(
            path,
            GenericRead | DeleteAccess | FileReadAttributes,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            OpenExisting,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to bind owned provenance generation handle: " + path);
        }
        try { Information(handle); return handle; }
        catch { handle.Dispose(); throw; }
    }

    public static string GetOwnedProvenanceGenerationIdentity(SafeFileHandle handle)
    {
        var information = Information(handle);
        return string.Format("{0:X8}:{1:X8}{2:X8}", information.VolumeSerialNumber, information.FileIndexHigh, information.FileIndexLow);
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

function Open-OwnedProvenanceGeneration([string]$Path, [string]$Label) {
    $item = Resolve-OrdinaryFile -Path $Path -Label $Label
    $handle = [Qs3dProvenanceGenerationNative]::OpenOwnedProvenanceGeneration($item.FullName)
    try {
        $identity = [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($handle)
        return [pscustomobject]@{ Handle=$handle; Identity=$identity; Path=$item.FullName; Label=$Label }
    }
    catch { $handle.Dispose(); throw }
}

function Get-OwnedProvenanceGenerationIdentity($Generation) {
    return [Qs3dProvenanceGenerationNative]::GetOwnedProvenanceGenerationIdentity($Generation.Handle)
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

$hostState = Read-StrictUtf8Json -Path $HostReferenceStatePath -Label 'V26 host-reference state'
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
    $tempStream = [IO.File]::Open($tempPath, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
    try {
        $writer = [IO.StreamWriter]::new($tempStream, $strictUtf8, 4096, $true)
        try {
            $writer.Write(($provenance | ConvertTo-Json -Depth 5))
            $writer.Write([Environment]::NewLine)
            $writer.Flush()
        }
        finally { $writer.Dispose() }
        $tempStream.Flush($true)
    }
    finally { $tempStream.Dispose() }

    $tempGeneration = Open-OwnedProvenanceGeneration -Path $tempPath -Label 'V26 provenance staging generation'
    $publicationCommitted = $false
    try {
        if (Test-Path -LiteralPath $outputFull) {
            $null = Resolve-OrdinaryFile -Path $outputFull -Label 'V26 provenance output'
            [IO.File]::Replace($tempPath, $outputFull, $null)
        }
        else {
            [IO.File]::Move($tempPath, $outputFull)
        }
        $publicationCommitted = $true
    }
    finally {
        if ($publicationCommitted) {
            Close-OwnedProvenanceGeneration -Generation $tempGeneration
        }
        else {
            Remove-OwnedProvenanceGeneration -Generation $tempGeneration
        }
        $tempGeneration = $null
    }

    [pscustomobject]@{ SourceCommit = $provenance.sourceCommit; PackageSha256 = $zipHash; ProductVersion = $productVersion; InstallerSha256 = $InstallerSha256; HostReferences = @($hostReferences) }
}
finally { $zipStream.Dispose() }
