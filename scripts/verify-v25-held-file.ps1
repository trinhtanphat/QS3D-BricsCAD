[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet('Hash', 'Copy')]
    [string]$Operation,

    [Parameter(Mandatory = $true)]
    [string]$Path,

    [string]$Destination
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not ('Qs3dHeldCopyNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class Qs3dHeldCopyNative
{
    private const uint FILE_LIST_DIRECTORY = 0x00000001;
    private const uint FILE_READ_ATTRIBUTES = 0x00000080;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_ATTRIBUTE_DIRECTORY = 0x00000010;
    private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
    private const uint FILE_NAME_NORMALIZED = 0x0;
    private const uint VOLUME_NAME_DOS = 0x0;

    [StructLayout(LayoutKind.Sequential)]
    private struct BY_HANDLE_FILE_INFORMATION
    {
        public uint FileAttributes;
        public System.Runtime.InteropServices.ComTypes.FILETIME CreationTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastAccessTime;
        public System.Runtime.InteropServices.ComTypes.FILETIME LastWriteTime;
        public uint VolumeSerialNumber;
        public uint FileSizeHigh;
        public uint FileSizeLow;
        public uint NumberOfLinks;
        public uint FileIndexHigh;
        public uint FileIndexLow;
    }

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern SafeFileHandle CreateFileW(
        string lpFileName,
        uint dwDesiredAccess,
        uint dwShareMode,
        IntPtr lpSecurityAttributes,
        uint dwCreationDisposition,
        uint dwFlagsAndAttributes,
        IntPtr hTemplateFile);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetFileInformationByHandle(
        SafeFileHandle hFile,
        out BY_HANDLE_FILE_INFORMATION lpFileInformation);

    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern uint GetFinalPathNameByHandleW(
        SafeFileHandle hFile,
        StringBuilder lpszFilePath,
        uint cchFilePath,
        uint dwFlags);

    public static SafeFileHandle OpenDirectoryNoFollow(string path)
    {
        SafeFileHandle handle = CreateFileW(
            path,
            FILE_LIST_DIRECTORY | FILE_READ_ATTRIBUTES,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to hold destination directory without following reparse points: " + path);
        }

        BY_HANDLE_FILE_INFORMATION info;
        if (!GetFileInformationByHandle(handle, out info))
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "Unable to query held destination directory: " + path);
        }
        if ((info.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
        {
            handle.Dispose();
            throw new IOException("Held destination path is not a directory: " + path);
        }
        if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
        {
            handle.Dispose();
            throw new IOException("Held destination directory is a reparse point: " + path);
        }
        return handle;
    }

    public static string GetFinalDosPath(SafeFileHandle handle)
    {
        StringBuilder buffer = new StringBuilder(32768);
        uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
        if (length == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to resolve held destination directory path.");
        if (length >= buffer.Capacity)
            throw new IOException("Resolved held destination directory path exceeded the supported length.");
        string value = buffer.ToString();
        if (value.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            return @"\\" + value.Substring(8);
        if (value.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
            return value.Substring(4);
        return value;
    }
}
'@
}

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    return [IO.Path]::GetFullPath($LiteralPath)
}

function Get-ComparableFullPath {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    return (Get-CanonicalFullPath -LiteralPath $LiteralPath).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-NoReparseAncestor {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $cursor = [IO.Directory]::GetParent((Get-CanonicalFullPath -LiteralPath $LiteralPath))
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Held V25 release input traverses a reparse-point ancestor: $($cursor.FullName)"
        }
        $cursor = $cursor.Parent
    }
}

function Open-HeldDestinationDirectoryChain {
    param([Parameter(Mandatory = $true)][string]$ParentPath)

    $parentFull = Get-CanonicalFullPath -LiteralPath $ParentPath
    if (-not (Test-Path -LiteralPath $parentFull -PathType Container)) {
        throw "Held V25 release copy destination parent does not exist: $parentFull"
    }

    $paths = [Collections.Generic.List[string]]::new()
    $cursor = Get-Item -LiteralPath $parentFull -Force -ErrorAction Stop
    while ($null -ne $cursor) {
        $paths.Add((Get-CanonicalFullPath -LiteralPath $cursor.FullName))
        $cursor = $cursor.Parent
    }

    $holds = [Collections.Generic.List[Microsoft.Win32.SafeHandles.SafeFileHandle]]::new()
    try {
        for ($i = $paths.Count - 1; $i -ge 0; $i--) {
            $expectedPath = Get-ComparableFullPath -LiteralPath $paths[$i]
            $handle = [Qs3dHeldCopyNative]::OpenDirectoryNoFollow($paths[$i])
            try {
                $actualPath = Get-ComparableFullPath -LiteralPath ([Qs3dHeldCopyNative]::GetFinalDosPath($handle))
                if (-not [string]::Equals($expectedPath, $actualPath, [StringComparison]::OrdinalIgnoreCase)) {
                    throw "Held V25 release copy destination directory changed generation/path. Expected=$expectedPath Actual=$actualPath"
                }
                $holds.Add($handle)
                $handle = $null
            }
            finally {
                if ($null -ne $handle) { $handle.Dispose() }
            }
        }
        Write-Output -NoEnumerate $holds
    }
    catch {
        for ($i = $holds.Count - 1; $i -ge 0; $i--) { $holds[$i].Dispose() }
        throw
    }
}

function Open-HeldGeneration {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $canonical = Get-CanonicalFullPath -LiteralPath $LiteralPath
    Assert-NoReparseAncestor -LiteralPath $canonical
    $admitted = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
    if ($admitted.PSIsContainer) { throw "Held V25 release input must be a file: $canonical" }
    if (($admitted.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "Held V25 release input must not be a reparse point: $canonical"
    }
    $admittedPath = Get-CanonicalFullPath -LiteralPath $admitted.FullName
    if (-not [string]::Equals($canonical, $admittedPath, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Held V25 release input canonical identity drifted before open: $canonical"
    }
    $admittedLength = [int64]$admitted.Length
    $admittedWriteTicks = [int64]$admitted.LastWriteTimeUtc.Ticks

    $stream = [IO.File]::Open($canonical, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $rebound = Get-Item -LiteralPath $canonical -Force -ErrorAction Stop
        $reboundPath = Get-CanonicalFullPath -LiteralPath $rebound.FullName
        if ($rebound.PSIsContainer -or (($rebound.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0)) {
            throw "Held V25 release input changed to a non-ordinary file after open: $canonical"
        }
        if (-not [string]::Equals($admittedPath, $reboundPath, [StringComparison]::OrdinalIgnoreCase) -or
            [int64]$rebound.Length -ne $admittedLength -or
            [int64]$rebound.LastWriteTimeUtc.Ticks -ne $admittedWriteTicks -or
            [int64]$stream.Length -ne $admittedLength) {
            throw "Held V25 release input generation changed across admission/open: $canonical"
        }
        return [pscustomobject]@{
            Stream = $stream
            CanonicalPath = $admittedPath
            Length = $admittedLength
            LastWriteTimeUtcTicks = $admittedWriteTicks
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Get-HeldStreamSha256 {
    param([Parameter(Mandatory = $true)][IO.Stream]$Stream)

    if (-not $Stream.CanSeek) { throw 'Held V25 release input stream must be seekable for exact-generation digest admission.' }
    $priorPosition = $Stream.Position
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        $Stream.Position = 0
        $digest = $sha.ComputeHash($Stream)
        return (-join ($digest | ForEach-Object { $_.ToString('x2') }))
    }
    finally {
        $Stream.Position = $priorPosition
        $sha.Dispose()
    }
}

function Publish-CommercialZipDigest {
    param(
        [Parameter(Mandatory = $true)][string]$CanonicalPath,
        [Parameter(Mandatory = $true)][string]$Digest
    )

    # This process-scoped admission value intentionally belongs to one exact release asset only.
    # Hashing update manifests or other files must never overwrite the ZIP generation admitted for extraction.
    if ([string]::Equals([IO.Path]::GetFileName($CanonicalPath), 'QS3D-BricsCAD-V25.zip', [StringComparison]::Ordinal)) {
        if ($Digest -notmatch '^[0-9a-f]{64}$') { throw 'Held V25 commercial ZIP digest is malformed.' }
        $env:QS3D_V25_COMMERCIAL_ZIP_SHA256 = $Digest.ToLowerInvariant()
    }
}

$held = Open-HeldGeneration -LiteralPath $Path
try {
    switch ($Operation) {
        'Hash' {
            $hex = Get-HeldStreamSha256 -Stream $held.Stream
            Publish-CommercialZipDigest -CanonicalPath $held.CanonicalPath -Digest $hex
            Write-Output $hex
        }
        'Copy' {
            if ([string]::IsNullOrWhiteSpace($Destination)) {
                throw 'Destination is required for Copy.'
            }
            $destinationFull = Get-CanonicalFullPath -LiteralPath $Destination
            $parent = Split-Path -Parent $destinationFull
            if ([string]::IsNullOrWhiteSpace($parent)) {
                throw "Held V25 release copy destination has no parent: $destinationFull"
            }
            if (Test-Path -LiteralPath $destinationFull) {
                throw "Held V25 release copy destination already exists: $destinationFull"
            }

            $destinationHolds = Open-HeldDestinationDirectoryChain -ParentPath $parent
            try {
                if (Test-Path -LiteralPath $destinationFull) {
                    throw "Held V25 release copy destination appeared after parent generation admission: $destinationFull"
                }
                $sourceDigest = Get-HeldStreamSha256 -Stream $held.Stream
                $held.Stream.Position = 0
                $output = [IO.File]::Open($destinationFull, [IO.FileMode]::CreateNew, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
                try {
                    $held.Stream.CopyTo($output)
                    $output.Flush($true)
                    if ([int64]$output.Length -ne [int64]$held.Length) {
                        throw "Held V25 release copy length mismatch: $destinationFull"
                    }
                    $destinationDigest = Get-HeldStreamSha256 -Stream $output
                    if (-not [string]::Equals($sourceDigest, $destinationDigest, [StringComparison]::OrdinalIgnoreCase)) {
                        throw "Held V25 release copy exact destination-stream SHA-256 mismatch: $destinationFull"
                    }
                    Publish-CommercialZipDigest -CanonicalPath $held.CanonicalPath -Digest $sourceDigest
                }
                finally {
                    $output.Dispose()
                }
                Write-Output $destinationFull
            }
            finally {
                for ($i = $destinationHolds.Count - 1; $i -ge 0; $i--) { $destinationHolds[$i].Dispose() }
            }
        }
    }
}
finally {
    $held.Stream.Dispose()
}
