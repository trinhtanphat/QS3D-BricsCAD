[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$PackagePath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$script:ExpectedPackageName = 'QS3D-BricsCAD-V26.zip'
$script:ExpectedChecksumName = 'QS3D-BricsCAD-V26.zip.sha256'
$script:MaxChecksumBytes = 1024

function Assert-NoReparseDirectoryChain {
    param([Parameter(Mandatory = $true)][IO.DirectoryInfo]$Directory,[Parameter(Mandatory = $true)][string]$Label)
    $cursor = $Directory
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label path contains a reparse-point directory: $($cursor.FullName)" }
        $cursor = $cursor.Parent
    }
}

function Resolve-OrdinaryNonReparseFile {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "$Label must be an ordinary file: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be a reparse-point file: $Path" }
    Assert-NoReparseDirectoryChain -Directory $item.Directory -Label $Label
    return $item
}

function Resolve-OrdinaryNonReparseDirectory {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if (-not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be a reparse-point directory: $Path" }
    Assert-NoReparseDirectoryChain -Directory $item -Label $Label
    return $item
}

function Assert-SafeExistingOutputLeaf {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return $false }
    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($item.PSIsContainer) { throw "V26 checksum destination must not be a directory: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "V26 checksum destination must not be a reparse-point file: $Path" }
    return $true
}

if (-not ('Qs3dChecksumGenerationNative' -as [type])) {
    Add-Type -Language CSharp -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

public static class Qs3dChecksumGenerationNative
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
    private struct NativeFileTime
    {
        public uint Low;
        public uint High;
    }

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
        string fileName,
        uint desiredAccess,
        uint shareMode,
        IntPtr securityAttributes,
        uint creationDisposition,
        uint flagsAndAttributes,
        IntPtr templateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle file,
        out ByHandleFileInformation information);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle file,
        FileInfoByHandleClass informationClass,
        ref FileDispositionInformation information,
        uint bufferSize);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool WriteFile(
        SafeFileHandle file,
        byte[] buffer,
        uint bytesToWrite,
        out uint bytesWritten,
        IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool FlushFileBuffers(SafeFileHandle file);

    private static ByHandleFileInformation Information(SafeFileHandle handle)
    {
        if (handle == null || handle.IsInvalid || handle.IsClosed)
            throw new InvalidOperationException("Owned checksum generation handle is not open.");
        ByHandleFileInformation information;
        if (!GetFileInformationByHandle(handle, out information))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "GetFileInformationByHandle failed for owned checksum generation.");
        if ((information.FileAttributes & FileAttributeReparsePoint) != 0)
            throw new InvalidOperationException("Owned checksum generation resolved to a reparse-point file.");
        return information;
    }

    private static SafeFileHandle OpenCore(string path, uint access, uint disposition)
    {
        var handle = CreateFileW(
            path,
            access,
            FileShareRead | FileShareWrite | FileShareDelete,
            IntPtr.Zero,
            disposition,
            FileAttributeNormal | FileFlagOpenReparsePoint,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            var error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to bind owned checksum generation handle: " + path);
        }
        try
        {
            Information(handle);
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static SafeFileHandle OpenOwnedChecksumGeneration(string path)
    {
        return OpenCore(path, GenericRead | DeleteAccess | FileReadAttributes, OpenExisting);
    }

    public static SafeFileHandle CreateOwnedChecksumGeneration(string path, byte[] bytes)
    {
        var handle = OpenCore(path, GenericRead | GenericWrite | DeleteAccess | FileReadAttributes, CreateNew);
        try
        {
            uint written;
            if (bytes.Length > 0 && (!WriteFile(handle, bytes, (uint)bytes.Length, out written, IntPtr.Zero) || written != (uint)bytes.Length))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to write owned checksum generation.");
            if (!FlushFileBuffers(handle))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to flush owned checksum generation.");
            return handle;
        }
        catch
        {
            handle.Dispose();
            throw;
        }
    }

    public static string GetOwnedChecksumGenerationIdentity(SafeFileHandle handle)
    {
        var information = Information(handle);
        return string.Format("{0:X8}:{1:X8}{2:X8}", information.VolumeSerialNumber, information.FileIndexHigh, information.FileIndexLow);
    }

    public static long GetOwnedChecksumGenerationLength(SafeFileHandle handle)
    {
        var information = Information(handle);
        return ((long)information.FileSizeHigh << 32) | information.FileSizeLow;
    }

    public static void RemoveOwnedChecksumGeneration(SafeFileHandle handle)
    {
        Information(handle);
        var disposition = new FileDispositionInformation { DeleteFile = true };
        if (!SetFileInformationByHandle(handle, FileInfoByHandleClass.FileDispositionInfo, ref disposition, (uint)Marshal.SizeOf(typeof(FileDispositionInformation))))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "SetFileInformationByHandle(FileDispositionInfo) failed for owned checksum generation.");
    }
}
'@
}

function Get-OwnedChecksumGenerationIdentity {
    param([Parameter(Mandatory = $true)]$Generation)
    return [Qs3dChecksumGenerationNative]::GetOwnedChecksumGenerationIdentity($Generation.Handle)
}

function Open-OwnedChecksumGeneration {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    $handle = [Qs3dChecksumGenerationNative]::OpenOwnedChecksumGeneration($item.FullName)
    try {
        $identity = [Qs3dChecksumGenerationNative]::GetOwnedChecksumGenerationIdentity($handle)
        return [pscustomobject]@{ Handle = $handle; Identity = $identity; Path = $item.FullName; Label = $Label }
    }
    catch {
        $handle.Dispose()
        throw
    }
}

function New-OwnedChecksumGeneration {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][byte[]]$Bytes,
        [Parameter(Mandatory = $true)][string]$Label
    )
    if (Test-Path -LiteralPath $Path) { throw "Refusing to reuse owned checksum generation path: $Path" }
    $parentPath = [IO.Path]::GetDirectoryName([IO.Path]::GetFullPath($Path))
    [void](Resolve-OrdinaryNonReparseDirectory -Path $parentPath -Label "$Label parent")
    $handle = [Qs3dChecksumGenerationNative]::CreateOwnedChecksumGeneration([IO.Path]::GetFullPath($Path), $Bytes)
    try {
        $identity = [Qs3dChecksumGenerationNative]::GetOwnedChecksumGenerationIdentity($handle)
        return [pscustomobject]@{ Handle = $handle; Identity = $identity; Path = [IO.Path]::GetFullPath($Path); Label = $Label }
    }
    catch {
        $handle.Dispose()
        throw
    }
}

function Close-OwnedChecksumGeneration {
    param($Generation)
    if ($null -eq $Generation) { return }
    $Generation.Handle.Dispose()
}

function Remove-OwnedChecksumGeneration {
    param([Parameter(Mandatory = $true)]$Generation)
    $currentIdentity = Get-OwnedChecksumGenerationIdentity -Generation $Generation
    if (-not [string]::Equals($currentIdentity, $Generation.Identity, [StringComparison]::Ordinal)) {
        throw "$($Generation.Label) identity changed while owned: expected $($Generation.Identity), got $currentIdentity"
    }
    try {
        [Qs3dChecksumGenerationNative]::RemoveOwnedChecksumGeneration($Generation.Handle)
    }
    finally {
        $Generation.Handle.Dispose()
    }
}

function Read-BoundedChecksumBytes {
    param([Parameter(Mandatory = $true)][string]$Path,[Parameter(Mandatory = $true)][string]$Label)
    $item = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    if ($item.Length -lt 1 -or $item.Length -gt $script:MaxChecksumBytes) { throw "$Label must be between 1 and $($script:MaxChecksumBytes) bytes: $Path" }
    $bytes = [IO.File]::ReadAllBytes($item.FullName)
    if ($bytes.Length -ne $item.Length) { throw "$Label changed while being read: $Path" }
    return [byte[]]$bytes
}

$package = Resolve-OrdinaryNonReparseFile -Path $PackagePath -Label 'V26 package ZIP'
if (-not [string]::Equals($package.Name, $script:ExpectedPackageName, [StringComparison]::Ordinal)) { throw "V26 checksum source must be named $($script:ExpectedPackageName): $($package.Name)" }
$packageCanonicalPath = $package.FullName
$packageLength = [int64]$package.Length
$packageLastWriteUtcTicks = [int64]$package.LastWriteTimeUtc.Ticks

$outputFullPath = [IO.Path]::GetFullPath($OutputPath)
if (-not [string]::Equals([IO.Path]::GetFileName($outputFullPath), $script:ExpectedChecksumName, [StringComparison]::Ordinal)) { throw "V26 checksum destination must be named $($script:ExpectedChecksumName): $outputFullPath" }
$outputParentPath = [IO.Path]::GetDirectoryName($outputFullPath)
if ([string]::IsNullOrWhiteSpace($outputParentPath)) { throw 'V26 checksum destination must have a parent directory.' }
$outputParent = Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent'
$hadExistingOutput = Assert-SafeExistingOutputLeaf -Path $outputFullPath
$originalOutputBytes = $null
$originalOutputGeneration = $null
if ($hadExistingOutput) {
    $originalOutputBytes = Read-BoundedChecksumBytes -Path $outputFullPath -Label 'Existing V26 checksum destination snapshot'
    $originalOutputGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath -Label 'Existing V26 checksum destination generation'
}

$stream = [IO.File]::Open($packageCanonicalPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $reboundPackage = Resolve-OrdinaryNonReparseFile -Path $packageCanonicalPath -Label 'V26 package ZIP after open'
    if (-not [string]::Equals($packageCanonicalPath, $reboundPackage.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $packageLength -ne [int64]$stream.Length -or
        $packageLength -ne [int64]$reboundPackage.Length -or
        $packageLastWriteUtcTicks -ne [int64]$reboundPackage.LastWriteTimeUtc.Ticks) {
        throw 'V26 package ZIP changed between checksum admission and held-stream binding.'
    }

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try { $digestBytes = $sha256.ComputeHash($stream) } finally { $sha256.Dispose() }
} finally { $stream.Dispose() }

$hash = ([BitConverter]::ToString($digestBytes)).Replace('-', '').ToLowerInvariant()
if ($hash -notmatch '^[0-9a-f]{64}$') { throw 'Computed V26 package SHA-256 digest is malformed.' }
$record = "$hash  $($script:ExpectedPackageName)"
$recordBytes = [Text.Encoding]::ASCII.GetBytes($record + [Environment]::NewLine)
if ($recordBytes.Length -gt $script:MaxChecksumBytes) { throw 'Canonical V26 checksum record exceeds the bounded publication size.' }

$nonce = [Guid]::NewGuid().ToString('N')
$tempPath = [IO.Path]::Combine($outputParent.FullName, ([IO.Path]::GetFileName($outputFullPath) + ".tmp-$nonce"))
$backupPath = [IO.Path]::Combine($outputParent.FullName, ([IO.Path]::GetFileName($outputFullPath) + ".bak-$nonce"))
$publicationStarted = $false
$publicationCommitted = $false
$rollbackAttempted = $false
$tempGeneration = $null

try {
    if (Test-Path -LiteralPath $backupPath) { throw "Refusing to reuse checksum backup path: $backupPath" }
    $tempGeneration = New-OwnedChecksumGeneration -Path $tempPath -Bytes $recordBytes -Label 'V26 checksum staging generation'
    if ([Qs3dChecksumGenerationNative]::GetOwnedChecksumGenerationLength($tempGeneration.Handle) -ne $recordBytes.Length) {
        throw 'V26 checksum staging generation length changed before publication.'
    }

    $outputParent = Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum destination parent before publication'
    if ($hadExistingOutput) {
        [void](Assert-SafeExistingOutputLeaf -Path $outputFullPath)
    }
    elseif (Test-Path -LiteralPath $outputFullPath) {
        throw "V26 checksum destination appeared after preflight validation: $outputFullPath"
    }

    # Mark mutation intent before Replace/Move: either API can alter the destination
    # and still throw before returning. The held staging handle follows that exact generation
    # through rename/replace so rollback can delete only the generation this attempt created.
    $publicationStarted = $true
    if ($hadExistingOutput) {
        [IO.File]::Replace($tempPath, $outputFullPath, $backupPath, $true)
    }
    else {
        [IO.File]::Move($tempPath, $outputFullPath)
    }

    $publishedItem = Resolve-OrdinaryNonReparseFile -Path $outputFullPath -Label 'Published V26 checksum'
    $publishedGeneration = Open-OwnedChecksumGeneration -Path $outputFullPath -Label 'Published V26 checksum generation proof'
    try {
        if (-not [string]::Equals($publishedGeneration.Identity, $tempGeneration.Identity, [StringComparison]::Ordinal)) {
            throw 'Published V26 checksum pathname no longer names the generation created by this publication attempt.'
        }
    }
    finally {
        Close-OwnedChecksumGeneration -Generation $publishedGeneration
    }
    $publishedText = [IO.File]::ReadAllText($publishedItem.FullName, [Text.Encoding]::ASCII).TrimEnd("`r", "`n")
    if (-not [string]::Equals($publishedText, $record, [StringComparison]::Ordinal)) { throw 'Published V26 checksum bytes do not match the computed canonical record.' }
    $publicationCommitted = $true
}
catch {
    $publicationFailure = $_
    if ($publicationStarted -and -not $publicationCommitted) {
        $rollbackAttempted = $true
        try {
            [void](Resolve-OrdinaryNonReparseDirectory -Path $outputParentPath -Label 'V26 checksum rollback parent')

            if ($null -ne $tempGeneration) {
                Remove-OwnedChecksumGeneration -Generation $tempGeneration
                $tempGeneration = $null
            }

            if ($hadExistingOutput) {
                if (Test-Path -LiteralPath $backupPath) {
                    $backupProof = Open-OwnedChecksumGeneration -Path $backupPath -Label 'V26 checksum rollback backup generation proof'
                    try {
                        if (-not [string]::Equals($backupProof.Identity, $originalOutputGeneration.Identity, [StringComparison]::Ordinal)) {
                            throw 'V26 checksum rollback backup pathname no longer names the original destination generation.'
                        }
                    }
                    finally {
                        Close-OwnedChecksumGeneration -Generation $backupProof
                    }

                    if (Test-Path -LiteralPath $outputFullPath) {
                        $currentOutput = Open-OwnedChecksumGeneration -Path $outputFullPath -Label 'V26 checksum rollback destination occupancy'
                        try {
                            if (-not [string]::Equals($currentOutput.Identity, $originalOutputGeneration.Identity, [StringComparison]::Ordinal)) {
                                throw 'V26 checksum rollback destination is occupied by a foreign generation; refusing to replace it.'
                            }
                        }
                        finally {
                            Close-OwnedChecksumGeneration -Generation $currentOutput
                        }
                    }
                    else {
                        [IO.File]::Move($backupPath, $outputFullPath)
                    }

                    $restoredOutput = Open-OwnedChecksumGeneration -Path $outputFullPath -Label 'Restored V26 checksum destination generation'
                    try {
                        if (-not [string]::Equals($restoredOutput.Identity, $originalOutputGeneration.Identity, [StringComparison]::Ordinal)) {
                            throw 'V26 checksum rollback did not restore the original destination generation.'
                        }
                    }
                    finally {
                        Close-OwnedChecksumGeneration -Generation $restoredOutput
                    }
                }
                else {
                    # Replace can throw before creating its backup. In that case accept the state
                    # only if the still-published generation and original bytes are both proven unchanged.
                    $unchangedOutput = Open-OwnedChecksumGeneration -Path $outputFullPath -Label 'V26 checksum rollback unchanged-destination generation proof'
                    try {
                        if (-not [string]::Equals($unchangedOutput.Identity, $originalOutputGeneration.Identity, [StringComparison]::Ordinal)) {
                            throw 'V26 checksum replacement failed without a backup and the destination generation changed.'
                        }
                    }
                    finally {
                        Close-OwnedChecksumGeneration -Generation $unchangedOutput
                    }
                    $currentOutputBytes = Read-BoundedChecksumBytes -Path $outputFullPath -Label 'V26 checksum rollback unchanged-destination proof'
                    $originalBase64 = [Convert]::ToBase64String($originalOutputBytes)
                    $currentBase64 = [Convert]::ToBase64String($currentOutputBytes)
                    if (-not [string]::Equals($currentBase64, $originalBase64, [StringComparison]::Ordinal)) {
                        throw 'V26 checksum replacement failed without a backup and the original destination cannot be proven unchanged.'
                    }
                }

                Close-OwnedChecksumGeneration -Generation $originalOutputGeneration
                $originalOutputGeneration = $null
            }
        }
        catch {
            throw "V26 checksum publication failed and rollback could not safely restore the pre-publication state. Publication failure: $($publicationFailure.Exception.Message) Rollback failure: $($_.Exception.Message)"
        }
    }
    throw $publicationFailure
}
finally {
    if ($publicationCommitted) {
        if ($null -ne $tempGeneration) {
            Close-OwnedChecksumGeneration -Generation $tempGeneration
            $tempGeneration = $null
        }
        if ($null -ne $originalOutputGeneration) {
            Remove-OwnedChecksumGeneration -Generation $originalOutputGeneration
            $originalOutputGeneration = $null
        }
    }
    elseif (-not $rollbackAttempted) {
        if ($null -ne $tempGeneration) {
            Remove-OwnedChecksumGeneration -Generation $tempGeneration
            $tempGeneration = $null
        }
        if ($null -ne $originalOutputGeneration) {
            Close-OwnedChecksumGeneration -Generation $originalOutputGeneration
            $originalOutputGeneration = $null
        }
    }
    else {
        if ($null -ne $tempGeneration) {
            Close-OwnedChecksumGeneration -Generation $tempGeneration
            $tempGeneration = $null
        }
        if ($null -ne $originalOutputGeneration) {
            Close-OwnedChecksumGeneration -Generation $originalOutputGeneration
            $originalOutputGeneration = $null
        }
    }
}

if (Test-Path -LiteralPath $tempPath) { throw "V26 checksum staging residue remains: $tempPath" }
if (Test-Path -LiteralPath $backupPath) { throw "V26 checksum backup residue remains: $backupPath" }

[pscustomobject]@{ PackagePath = $packageCanonicalPath; OutputPath = $outputFullPath; Sha256 = $hash; Record = $record }
