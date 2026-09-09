[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$maxGeneratedScriptBytes = 1MB
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)

if (-not ('Qs3d.V26.FinalizerTempGenerationNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Qs3d.V26
{
    public static class FinalizerTempGenerationNative
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint DELETE = 0x00010000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        private const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;
        private const int ERROR_FILE_NOT_FOUND = 2;
        private const int ERROR_PATH_NOT_FOUND = 3;
        private const int FileDispositionInfo = 4;

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint LowDateTime;
            public uint HighDateTime;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct BY_HANDLE_FILE_INFORMATION
        {
            public uint FileAttributes;
            public FILETIME CreationTime;
            public FILETIME LastAccessTime;
            public FILETIME LastWriteTime;
            public uint VolumeSerialNumber;
            public uint FileSizeHigh;
            public uint FileSizeLow;
            public uint NumberOfLinks;
            public uint FileIndexHigh;
            public uint FileIndexLow;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct FILE_DISPOSITION_INFO
        {
            [MarshalAs(UnmanagedType.Bool)]
            public bool DeleteFile;
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

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetFileInformationByHandle(
            SafeFileHandle hFile,
            int FileInformationClass,
            ref FILE_DISPOSITION_INFO lpFileInformation,
            uint dwBufferSize);

        private static string GetIdentityCore(SafeFileHandle handle)
        {
            BY_HANDLE_FILE_INFORMATION info;
            if (!GetFileInformationByHandle(handle, out info))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not identify the generated V26 finalizer generation.");
            ulong fileIndex = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
            return info.VolumeSerialNumber.ToString("X8") + ":" + fileIndex.ToString("X16");
        }

        public static string GetIdentity(SafeFileHandle handle)
        {
            if (handle == null || handle.IsInvalid || handle.IsClosed)
                throw new ArgumentException("A live generated-script handle is required.", "handle");
            return GetIdentityCore(handle);
        }

        public static bool DeleteIfSameGeneration(string path, string expectedIdentity)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException("A generated-script path is required.", "path");
            if (String.IsNullOrWhiteSpace(expectedIdentity)) throw new ArgumentException("A generated-script identity is required.", "expectedIdentity");

            using (SafeFileHandle handle = CreateFileW(
                path,
                GENERIC_READ | DELETE,
                FILE_SHARE_READ,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero))
            {
                if (handle.IsInvalid)
                {
                    int error = Marshal.GetLastWin32Error();
                    if (error == ERROR_FILE_NOT_FOUND || error == ERROR_PATH_NOT_FOUND) return false;
                    throw new Win32Exception(error, "Could not open the generated V26 finalizer for exact-generation cleanup.");
                }

                BY_HANDLE_FILE_INFORMATION info;
                if (!GetFileInformationByHandle(handle, out info))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not identify the cleanup handle for the generated V26 finalizer.");
                if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
                    throw new InvalidOperationException("Generated V26 finalizer cleanup path became reparse-backed; refusing deletion.");
                ulong fileIndex = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
                string actualIdentity = info.VolumeSerialNumber.ToString("X8") + ":" + fileIndex.ToString("X16");
                if (!String.Equals(actualIdentity, expectedIdentity, StringComparison.Ordinal))
                    throw new InvalidOperationException("Generated V26 finalizer pathname now names a different file generation; refusing cleanup.");

                FILE_DISPOSITION_INFO disposition = new FILE_DISPOSITION_INFO { DeleteFile = true };
                if (!SetFileInformationByHandle(handle, FileDispositionInfo, ref disposition, (uint)Marshal.SizeOf(typeof(FILE_DISPOSITION_INFO))))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not mark the exact generated V26 finalizer generation for deletion.");
                return true;
            }
        }
    }
}
'@
}

function Assert-NoReparseDirectoryChain {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)

    $cursor = [IO.Path]::GetFullPath($Path)
    while ($true) {
        if (Test-Path -LiteralPath $cursor) {
            $item = Get-Item -LiteralPath $cursor -Force -ErrorAction Stop
            if (-not $item.PSIsContainer) { throw "$Label ancestor must be a directory: $cursor" }
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label path contains a reparse-point directory: $cursor"
            }
        }
        $parent = [IO.Directory]::GetParent($cursor)
        if ($null -eq $parent) { break }
        $cursor = $parent.FullName
    }
}

function Resolve-OrdinaryNonReparseFile {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)

    $full = [IO.Path]::GetFullPath($Path)
    $parent = [IO.Path]::GetDirectoryName($full)
    if ([string]::IsNullOrWhiteSpace($parent)) { throw "$Label requires an ordinary parent directory: $full" }
    Assert-NoReparseDirectoryChain -Path $parent -Label $Label
    $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo])) { throw "$Label must be an ordinary file: $full" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must not be reparse-backed: $full" }
    return $item
}

function Read-HeldStrictUtf8 {
    param(
        [Parameter(Mandatory = $true)][IO.FileStream]$Stream,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][int64]$MaxBytes
    )

    if (-not $Stream.CanRead) { throw "$Label held stream is not readable." }
    $length = [int64]$Stream.Length
    if ($length -lt 1 -or $length -gt $MaxBytes -or $length -gt [int]::MaxValue) {
        throw "$Label held generation has invalid bounded size: $length bytes."
    }
    $Stream.Position = 0
    $bytes = [byte[]]::new([int]$length)
    try {
        $offset = 0
        while ($offset -lt $bytes.Length) {
            $read = $Stream.Read($bytes, $offset, $bytes.Length - $offset)
            if ($read -le 0) { throw "$Label ended before its held length was read." }
            $offset += $read
        }
        if ($Stream.ReadByte() -ne -1 -or $Stream.Length -ne $length) { throw "$Label changed while its held bytes were being read." }
        try { return $strictUtf8.GetString($bytes) }
        catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
    }
    finally {
        [Array]::Clear($bytes, 0, $bytes.Length)
        $Stream.Position = 0
    }
}

function Assert-HeldGeneratedScript {
    param(
        [Parameter(Mandatory = $true)][IO.FileStream]$Stream,
        [Parameter(Mandatory = $true)][IO.FileInfo]$Admitted,
        [Parameter(Mandatory = $true)][string]$ExpectedPath
    )

    $current = Resolve-OrdinaryNonReparseFile -Path $ExpectedPath -Label 'Generated V26 finalizer script'
    if (-not [string]::Equals($current.FullName, $Admitted.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath($Stream.Name), $Admitted.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        [int64]$Stream.Length -ne [int64]$Admitted.Length -or
        [int64]$current.Length -ne [int64]$Admitted.Length -or
        [int64]$current.LastWriteTimeUtc.Ticks -ne [int64]$Admitted.LastWriteTimeUtc.Ticks) {
        throw 'Generated V26 finalizer pathname or metadata no longer matches the held admitted generation.'
    }
}

function Get-HeldGeneratedScriptIdentity {
    param([Parameter(Mandatory = $true)][IO.FileStream]$Stream)
    return [Qs3d.V26.FinalizerTempGenerationNative]::GetIdentity($Stream.SafeFileHandle)
}

function Remove-ExactGeneratedScriptGeneration {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedIdentity
    )
    return [Qs3d.V26.FinalizerTempGenerationNative]::DeleteIfSameGeneration([IO.Path]::GetFullPath($Path), $ExpectedIdentity)
}

$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }

# The generated finalizer inherits the V25 containment contract, which derives the
# repository root from the generated script's PSScriptRoot. Keep the transient
# generated script in this canonical scripts directory so its parent remains the
# real repository root; generating under the process temp root would rebase the
# containment boundary to %TEMP% and reject legitimate repo-local dist outputs.
$tempScript = Join-Path $PSScriptRoot ('.finalize-v26-signed-package.generated.' + [Guid]::NewGuid().ToString('N') + '.ps1')
$generatedStream = $null
$generatedIdentity = $null
$primaryFailure = $null
try {
    & $generator -SourceScript 'finalize-v25-signed-package.ps1' -OutputPath $tempScript
    if (-not $?) { throw 'Could not generate the V26 signed-package finalizer.' }

    $generatedItem = Resolve-OrdinaryNonReparseFile -Path $tempScript -Label 'Generated V26 finalizer script'
    $generatedStream = [IO.File]::Open($generatedItem.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
    $generatedIdentity = Get-HeldGeneratedScriptIdentity -Stream $generatedStream
    $generated = Read-HeldStrictUtf8 -Stream $generatedStream -Label 'Generated V26 finalizer script' -MaxBytes $maxGeneratedScriptBytes
    if ($generated -match '(?i)v25') { throw 'Generated V26 finalizer contains a V25 token.' }

    $forward = @{
        PackageDirectory = $PackageDirectory
        PackageZip = $PackageZip
        ExpectedSignerThumbprint = $ExpectedSignerThumbprint
    }
    if ($PSBoundParameters.ContainsKey('WhatIf')) { $forward['WhatIf'] = [bool]$PSBoundParameters['WhatIf'] }
    if ($PSBoundParameters.ContainsKey('Confirm')) { $forward['Confirm'] = [bool]$PSBoundParameters['Confirm'] }

    # FileShare.Read deliberately keeps write/delete sharing closed while the
    # canonical path is invoked, preserving $PSScriptRoot without allowing a
    # second generated-script generation to replace the one validated above.
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
    & $tempScript @forward
    if (-not $?) { throw 'V26 signed-package finalization failed.' }
    Assert-HeldGeneratedScript -Stream $generatedStream -Admitted $generatedItem -ExpectedPath $tempScript
}
catch {
    $primaryFailure = $_
    throw
}
finally {
    if ($null -ne $generatedStream) {
        $generatedStream.Dispose()
        $generatedStream = $null
    }

    if ($null -ne $generatedIdentity) {
        if ($null -eq $primaryFailure) {
            [void](Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity)
        }
        else {
            try {
                [void](Remove-ExactGeneratedScriptGeneration -Path $tempScript -ExpectedIdentity $generatedIdentity)
            }
            catch {
                # Preserve the primary transformer/finalizer failure. Exact-generation
                # cleanup is fail-safe: an identity mismatch, reparse replacement, or
                # sharing conflict refuses deletion rather than unlinking a generation
                # we do not own.
            }
        }
    }
}
