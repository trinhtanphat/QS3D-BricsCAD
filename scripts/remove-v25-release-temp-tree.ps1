[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [string]$ExpectedParent = $env:RUNNER_TEMP
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

if (-not ('Qs3dReleaseCleanupNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class Qs3dReleaseCleanupNative
{
    private const uint FILE_READ_ATTRIBUTES = 0x00000080;
    private const uint DELETE_ACCESS = 0x00010000;
    private const uint FILE_SHARE_READ = 0x00000001;
    private const uint FILE_SHARE_WRITE = 0x00000002;
    private const uint OPEN_EXISTING = 3;
    private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
    private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
    private const uint FILE_NAME_NORMALIZED = 0x0;
    private const uint VOLUME_NAME_DOS = 0x0;
    private const int FileDispositionInfoEx = 21;
    private const uint FILE_DISPOSITION_FLAG_DELETE = 0x00000001;
    private const uint FILE_DISPOSITION_FLAG_POSIX_SEMANTICS = 0x00000002;
    private const uint FILE_DISPOSITION_FLAG_IGNORE_READONLY_ATTRIBUTE = 0x00000010;

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

    [StructLayout(LayoutKind.Sequential)]
    private struct FILE_DISPOSITION_INFO_EX
    {
        public uint Flags;
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

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool SetFileInformationByHandle(
        SafeFileHandle hFile,
        int FileInformationClass,
        ref FILE_DISPOSITION_INFO_EX lpFileInformation,
        uint dwBufferSize);

    public static SafeFileHandle OpenPathNoFollow(string path)
    {
        SafeFileHandle handle = CreateFileW(
            path,
            DELETE_ACCESS | FILE_READ_ATTRIBUTES,
            FILE_SHARE_READ | FILE_SHARE_WRITE,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_OPEN_REPARSE_POINT | FILE_FLAG_BACKUP_SEMANTICS,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to hold V25 release cleanup target without following reparse points: " + path);
        }
        return handle;
    }

    public static uint GetAttributes(SafeFileHandle handle)
    {
        BY_HANDLE_FILE_INFORMATION info;
        if (!GetFileInformationByHandle(handle, out info))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to query held V25 release cleanup target.");
        return info.FileAttributes;
    }

    public static string GetFinalDosPath(SafeFileHandle handle)
    {
        StringBuilder buffer = new StringBuilder(32768);
        uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
        if (length == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to resolve held V25 release cleanup target path.");
        if (length >= buffer.Capacity)
            throw new IOException("Resolved held V25 release cleanup target path exceeded the supported length.");
        string value = buffer.ToString();
        if (value.StartsWith(@"\\?\UNC\", StringComparison.OrdinalIgnoreCase))
            return @"\\" + value.Substring(8);
        if (value.StartsWith(@"\\?\", StringComparison.OrdinalIgnoreCase))
            return value.Substring(4);
        return value;
    }

    public static void SetDispositionDelete(SafeFileHandle handle)
    {
        FILE_DISPOSITION_INFO_EX info = new FILE_DISPOSITION_INFO_EX {
            Flags = FILE_DISPOSITION_FLAG_DELETE |
                    FILE_DISPOSITION_FLAG_POSIX_SEMANTICS |
                    FILE_DISPOSITION_FLAG_IGNORE_READONLY_ATTRIBUTE
        };
        if (!SetFileInformationByHandle(
                handle,
                FileDispositionInfoEx,
                ref info,
                (uint)Marshal.SizeOf(typeof(FILE_DISPOSITION_INFO_EX))))
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to delete held V25 release cleanup target by handle.");
    }
}
'@
}

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)
    return [IO.Path]::GetFullPath($LiteralPath).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-ExactHeldPath {
    param(
        [Parameter(Mandatory = $true)][Microsoft.Win32.SafeHandles.SafeFileHandle]$Handle,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )
    $expected = Get-CanonicalFullPath -LiteralPath $ExpectedPath
    $actual = Get-CanonicalFullPath -LiteralPath ([Qs3dReleaseCleanupNative]::GetFinalDosPath($Handle))
    if (-not [string]::Equals($expected, $actual, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Held V25 release cleanup target changed path/generation. Expected=$expected Actual=$actual"
    }
}

function Remove-HeldTree {
    param([Parameter(Mandatory = $true)][string]$LiteralPath)

    $canonical = Get-CanonicalFullPath -LiteralPath $LiteralPath
    $handle = [Qs3dReleaseCleanupNative]::OpenPathNoFollow($canonical)
    try {
        Assert-ExactHeldPath -Handle $handle -ExpectedPath $canonical
        $attributes = [IO.FileAttributes][Qs3dReleaseCleanupNative]::GetAttributes($handle)
        $isDirectory = ($attributes -band [IO.FileAttributes]::Directory) -ne 0
        $isReparse = ($attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0

        if ($isDirectory -and -not $isReparse) {
            # The current directory generation remains held with delete/rename sharing denied
            # while every direct child is acquired no-follow and removed by its own handle.
            $children = @(Get-ChildItem -LiteralPath $canonical -Force -ErrorAction Stop)
            foreach ($child in $children) {
                Remove-HeldTree -LiteralPath $child.FullName
            }
        }

        [Qs3dReleaseCleanupNative]::SetDispositionDelete($handle)
    }
    finally {
        $handle.Dispose()
    }
}

if ([string]::IsNullOrWhiteSpace($ExpectedParent)) {
    throw 'ExpectedParent is required for V25 release temp cleanup.'
}

$canonical = Get-CanonicalFullPath -LiteralPath $Path
$parent = [IO.Directory]::GetParent($canonical)
if ($null -eq $parent) {
    throw "V25 release cleanup target has no parent: $canonical"
}
$expectedParentCanonical = Get-CanonicalFullPath -LiteralPath $ExpectedParent
$actualParentCanonical = Get-CanonicalFullPath -LiteralPath $parent.FullName
if (-not [string]::Equals($expectedParentCanonical, $actualParentCanonical, [StringComparison]::OrdinalIgnoreCase)) {
    throw "V25 release cleanup target must be an immediate child of the admitted temp parent. ExpectedParent=$expectedParentCanonical ActualParent=$actualParentCanonical"
}

if (-not (Test-Path -LiteralPath $canonical)) {
    return
}

Remove-HeldTree -LiteralPath $canonical
