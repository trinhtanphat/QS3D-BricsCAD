[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$ZipPath,

    [Parameter(Mandatory = $true)]
    [string]$DestinationRoot,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [long]::MaxValue)]
    [long]$MaxPackageBytes,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, [long]::MaxValue)]
    [long]$MaxExpandedBytes,

    [Parameter(Mandatory = $true)]
    [ValidateRange(1, 20000)]
    [int]$MaxEntries
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.IO.Compression.FileSystem

if (-not ('Qs3dCommercialArchiveNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;

public static class Qs3dCommercialArchiveNative
{
    private const uint FILE_READ_ATTRIBUTES = 0x00000080;
    private const uint FILE_SHARE_READ = 0x00000001;
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
            FILE_READ_ATTRIBUTES,
            FILE_SHARE_READ,
            IntPtr.Zero,
            OPEN_EXISTING,
            FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
            IntPtr.Zero);
        if (handle == null || handle.IsInvalid)
        {
            int error = Marshal.GetLastWin32Error();
            if (handle != null) handle.Dispose();
            throw new Win32Exception(error, "Unable to open extraction directory without following reparse points: " + path);
        }

        BY_HANDLE_FILE_INFORMATION info;
        if (!GetFileInformationByHandle(handle, out info))
        {
            int error = Marshal.GetLastWin32Error();
            handle.Dispose();
            throw new Win32Exception(error, "Unable to query extraction directory handle: " + path);
        }
        if ((info.FileAttributes & FILE_ATTRIBUTE_DIRECTORY) == 0)
        {
            handle.Dispose();
            throw new IOException("Extraction path is not a directory: " + path);
        }
        if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
        {
            handle.Dispose();
            throw new IOException("Extraction directory is a reparse point: " + path);
        }
        return handle;
    }

    public static string GetFinalDosPath(SafeFileHandle handle)
    {
        StringBuilder buffer = new StringBuilder(32768);
        uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, FILE_NAME_NORMALIZED | VOLUME_NAME_DOS);
        if (length == 0)
            throw new Win32Exception(Marshal.GetLastWin32Error(), "Unable to resolve extraction directory handle path.");
        if (length >= buffer.Capacity)
            throw new IOException("Resolved extraction directory path exceeded the supported length.");
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

function Get-NormalizedFullPath {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [IO.Path]::GetFullPath($Path).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Open-HeldSafeDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )

    $expectedFull = Get-NormalizedFullPath -Path $ExpectedPath
    $handle = [Qs3dCommercialArchiveNative]::OpenDirectoryNoFollow($Path)
    try {
        $finalFull = Get-NormalizedFullPath -Path ([Qs3dCommercialArchiveNative]::GetFinalDosPath($handle))
        if (-not [string]::Equals($finalFull, $expectedFull, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Commercial archive extraction directory handle resolved to an unexpected generation/path. Expected=$expectedFull Actual=$finalFull"
        }
        return [pscustomobject]@{
            Path = $expectedFull
            Handle = $handle
        }
    }
    catch {
        $handle.Dispose()
        throw
    }
}

function Ensure-HeldSafeDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$BoundaryRoot,
        [Parameter(Mandatory = $true)][Collections.Generic.Dictionary[string, object]]$Holds,
        [Parameter(Mandatory = $true)][Collections.Generic.List[object]]$HoldOrder
    )

    $boundaryFull = Get-NormalizedFullPath -Path $BoundaryRoot
    $rootPrefix = $boundaryFull + [IO.Path]::DirectorySeparatorChar
    $pathFull = Get-NormalizedFullPath -Path $Path
    if (-not [string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase) -and
        -not $pathFull.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) {
        throw "Commercial archive extraction directory escaped the destination root: $pathFull"
    }

    if ($Holds.ContainsKey($pathFull)) { return }
    if (-not $Holds.ContainsKey($boundaryFull)) {
        throw "Commercial archive extraction destination root generation is not pinned: $boundaryFull"
    }

    if ([string]::Equals($pathFull, $boundaryFull, [StringComparison]::OrdinalIgnoreCase)) { return }
    $relative = $pathFull.Substring($rootPrefix.Length)
    $current = $boundaryFull
    foreach ($segment in $relative.Split([IO.Path]::DirectorySeparatorChar)) {
        if ([string]::IsNullOrWhiteSpace($segment)) {
            throw "Commercial archive extraction directory has an empty path segment: $pathFull"
        }
        if (-not $Holds.ContainsKey($current)) {
            throw "Commercial archive extraction parent generation is not pinned: $current"
        }
        $next = Get-NormalizedFullPath -Path (Join-Path $current $segment)
        if (-not $Holds.ContainsKey($next)) {
            if (Test-Path -LiteralPath $next) {
                if (-not (Test-Path -LiteralPath $next -PathType Container)) {
                    throw "Commercial archive extraction directory target exists as a non-directory: $next"
                }
            }
            else {
                [IO.Directory]::CreateDirectory($next) | Out-Null
            }
            $hold = Open-HeldSafeDirectory -Path $next -ExpectedPath $next
            $Holds.Add($next, $hold)
            $HoldOrder.Add($hold)
        }
        $current = $next
    }
}

$zipFull = (Resolve-Path -LiteralPath $ZipPath -ErrorAction Stop).Path
if (-not (Test-Path -LiteralPath $zipFull -PathType Leaf)) {
    throw "Commercial candidate archive is not a file: $zipFull"
}

$destinationFull = Get-NormalizedFullPath -Path $DestinationRoot
if (Test-Path -LiteralPath $destinationFull) {
    throw "Commercial candidate extraction destination must not already exist: $destinationFull"
}
$destinationParent = [IO.Path]::GetDirectoryName($destinationFull)
if ([string]::IsNullOrWhiteSpace($destinationParent) -or -not (Test-Path -LiteralPath $destinationParent -PathType Container)) {
    throw "Commercial candidate extraction destination parent does not exist: $destinationParent"
}
$destinationParent = Get-NormalizedFullPath -Path $destinationParent

$directoryHolds = [Collections.Generic.Dictionary[string, object]]::new([StringComparer]::OrdinalIgnoreCase)
$holdOrder = [Collections.Generic.List[object]]::new()
$zipStream = $null
$archive = $null
$completed = $false
$entryCount = 0
[long]$expandedBytes = 0
[long]$materializedBytes = 0

try {
    $parentHold = Open-HeldSafeDirectory -Path $destinationParent -ExpectedPath $destinationParent
    $directoryHolds.Add($destinationParent, $parentHold)
    $holdOrder.Add($parentHold)

    [IO.Directory]::CreateDirectory($destinationFull) | Out-Null
    $rootHold = Open-HeldSafeDirectory -Path $destinationFull -ExpectedPath $destinationFull
    $directoryHolds.Add($destinationFull, $rootHold)
    $holdOrder.Add($rootHold)

    $zipStream = [IO.File]::Open($zipFull, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    if ($zipStream.Length -le 0 -or $zipStream.Length -gt $MaxPackageBytes) {
        throw "Commercial candidate archive size $($zipStream.Length) bytes is outside the allowed range (max $MaxPackageBytes)."
    }
    $expectedZipSha256 = [string]$env:QS3D_V25_COMMERCIAL_ZIP_SHA256
    if ($expectedZipSha256 -notmatch '^[0-9a-fA-F]{64}$') {
        throw 'Commercial candidate extraction requires one admitted QS3D V25 ZIP SHA-256 digest.'
    }
    $zipSha = [Security.Cryptography.SHA256]::Create()
    try {
        $zipStream.Position = 0
        $parsedDigestBytes = $zipSha.ComputeHash($zipStream)
        $parsedDigest = -join ($parsedDigestBytes | ForEach-Object { $_.ToString('x2') })
    }
    finally {
        $zipSha.Dispose()
    }
    if (-not [string]::Equals($parsedDigest, $expectedZipSha256, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Commercial candidate ZIP generation changed between admission and exact-stream extraction.'
    }
    $zipStream.Position = 0

    $archive = [IO.Compression.ZipArchive]::new($zipStream, [IO.Compression.ZipArchiveMode]::Read, $true)
    $rootPrefix = $destinationFull + [IO.Path]::DirectorySeparatorChar
    $seenTargets = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $fileRelatives = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $records = [Collections.Generic.List[object]]::new()
    $invalidFileNameChars = [IO.Path]::GetInvalidFileNameChars()

    foreach ($entry in $archive.Entries) {
        $entryCount++
        if ($entryCount -gt $MaxEntries) { throw "Commercial candidate archive exceeds the allowed entry count ($MaxEntries)." }
        $name = [string]$entry.FullName
        if ([string]::IsNullOrWhiteSpace($name) -or $name.IndexOf([char]0) -ge 0 -or [IO.Path]::IsPathRooted($name) -or $name.IndexOf([char]92) -ge 0 -or $name.Contains(':')) { throw "Unsafe commercial candidate archive entry: $name" }
        $isDirectory = $name.EndsWith('/', [StringComparison]::Ordinal)
        $relative = $name.TrimEnd('/')
        if ([string]::IsNullOrWhiteSpace($relative)) { throw "Unsafe commercial candidate archive entry: $name" }
        $segments = @($relative.Split('/'))
        if ($segments.Count -eq 0) { throw "Unsafe commercial candidate archive entry: $name" }
        foreach ($segment in $segments) {
            if ([string]::IsNullOrWhiteSpace($segment) -or $segment -eq '.' -or $segment -eq '..' -or $segment.IndexOfAny($invalidFileNameChars) -ge 0 -or $segment.EndsWith('.', [StringComparison]::Ordinal) -or $segment.EndsWith(' ', [StringComparison]::Ordinal) -or $segment -match '^(?i:con|prn|aux|nul|com(?:[1-9]|¹|²|³)|lpt(?:[1-9]|¹|²|³))(?:[.]|$)') { throw "Unsafe commercial candidate archive entry segment '$segment' in '$name'." }
        }
        if (-not $isDirectory) {
            if ($entry.Length -lt 0 -or $entry.Length -gt $MaxExpandedBytes -or $expandedBytes -gt ($MaxExpandedBytes - [long]$entry.Length)) { throw "Commercial candidate archive exceeds the allowed declared expanded size ($MaxExpandedBytes bytes)." }
            $expandedBytes += [int64]$entry.Length
            if ($expandedBytes -gt $MaxExpandedBytes) { throw "Commercial candidate archive exceeds the allowed declared expanded size ($MaxExpandedBytes bytes)." }
        }
        $normalizedRelative = $segments -join '/'
        for ($i = 1; $i -lt $segments.Count; $i++) {
            $parentRelative = ($segments[0..($i - 1)] -join '/')
            if ($fileRelatives.Contains($parentRelative)) { throw "Commercial candidate archive entry traverses an entry already admitted as a file: $name" }
        }
        if (-not $isDirectory) {
            $childPrefix = $normalizedRelative + '/'
            foreach ($existing in $records) { if (([string]$existing.Relative).StartsWith($childPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Commercial candidate archive file conflicts with an already admitted child path: $name" } }
            if (-not $fileRelatives.Add($normalizedRelative)) { throw "Duplicate commercial candidate archive file entry: $name" }
        }
        $target = Get-NormalizedFullPath -Path (Join-Path $destinationFull ($normalizedRelative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
        if (-not $target.StartsWith($rootPrefix, [StringComparison]::OrdinalIgnoreCase)) { throw "Commercial candidate archive entry escaped the destination root: $name" }
        if (-not $seenTargets.Add($target)) { throw "Duplicate or case-aliased commercial candidate archive target: $name" }
        $records.Add([pscustomobject]@{ Entry = $entry; Name = $name; Relative = $normalizedRelative; Target = $target; IsDirectory = $isDirectory })
    }
    if ($entryCount -eq 0) { throw 'Commercial candidate archive contains no entries.' }

    $buffer = New-Object byte[] 81920
    foreach ($record in $records) {
        if ($record.IsDirectory) { Ensure-HeldSafeDirectory -Path $record.Target -BoundaryRoot $destinationFull -Holds $directoryHolds -HoldOrder $holdOrder; continue }
        $parent = Get-NormalizedFullPath -Path ([IO.Path]::GetDirectoryName([string]$record.Target))
        Ensure-HeldSafeDirectory -Path $parent -BoundaryRoot $destinationFull -Holds $directoryHolds -HoldOrder $holdOrder
        if (-not $directoryHolds.ContainsKey($parent)) { throw "Commercial archive extraction parent generation was not held before file creation: $parent" }
        $input = $record.Entry.Open(); $output = $null; $entryStartBytes = $materializedBytes
        try {
            $output = [IO.File]::Open([string]$record.Target, [IO.FileMode]::CreateNew, [IO.FileAccess]::Write, [IO.FileShare]::None)
            while (($read = $input.Read($buffer, 0, $buffer.Length)) -gt 0) {
                if ($materializedBytes -gt ($MaxExpandedBytes - [int64]$read)) { throw "Commercial candidate archive exceeded the actual materialization budget ($MaxExpandedBytes bytes)." }
                $output.Write($buffer, 0, $read); $materializedBytes += [int64]$read
            }
            $output.Flush()
        }
        finally { if ($output) { $output.Dispose() }; $input.Dispose() }
        $actualEntryBytes = $materializedBytes - $entryStartBytes
        if ($actualEntryBytes -ne [long]$record.Entry.Length) { throw "Commercial candidate archive entry materialized a byte count different from its admitted ZIP metadata: $($record.Name)" }
        $written = Get-Item -LiteralPath ([string]$record.Target) -Force -ErrorAction Stop
        if (($written.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0 -or [long]$written.Length -ne $actualEntryBytes) { throw "Commercial candidate archive entry did not materialize as the exact admitted ordinary file: $($record.Name)" }
    }
    if ($materializedBytes -ne $expandedBytes) { throw "Commercial candidate archive actual expanded byte count differs from admitted ZIP metadata. Declared=$expandedBytes Actual=$materializedBytes" }
    $completed = $true
}
finally {
    if ($archive) { $archive.Dispose() }
    if ($zipStream) { $zipStream.Dispose() }
    for ($i = $holdOrder.Count - 1; $i -ge 0; $i--) { try { $holdOrder[$i].Handle.Dispose() } catch { } }
    if (-not $completed -and (Test-Path -LiteralPath $destinationFull)) { Remove-Item -LiteralPath $destinationFull -Recurse -Force -ErrorAction SilentlyContinue }
}

Write-Host "Safely extracted V25 commercial candidate archive: entries=$entryCount declaredExpandedBytes=$expandedBytes materializedBytes=$materializedBytes destination=$destinationFull"
