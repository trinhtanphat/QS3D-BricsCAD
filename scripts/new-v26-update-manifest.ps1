[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Low')]
param(
    [string]$PackageDirectory = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26'),
    [string]$PackageZip = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26.zip'),

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^https://')]
    [string]$PackageUri,

    [Parameter(Mandatory = $true)]
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint,

    [string]$OutputPath = (Join-Path (Split-Path -Parent $PSScriptRoot) 'dist\QS3D-BricsCAD-V26.update.json')
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$maxGeneratedScriptBytes = 2MB
$strictUtf8 = [Text.UTF8Encoding]::new($false, $true)

if (-not ('Qs3d.V26.ManifestTempGenerationNative' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Qs3d.V26
{
    public static class ManifestTempGenerationNative
    {
        private const uint GENERIC_READ = 0x80000000;
        private const uint DELETE = 0x00010000;
        private const uint FILE_SHARE_READ = 0x00000001;
        private const uint FILE_SHARE_WRITE = 0x00000002;
        private const uint OPEN_EXISTING = 3;
        private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;
        private const uint FILE_ATTRIBUTE_REPARSE_POINT = 0x00000400;
        private const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
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
            IntPtr lpFileInformation,
            uint dwBufferSize);

        private static string Identity(BY_HANDLE_FILE_INFORMATION info)
        {
            ulong fileIndex = ((ulong)info.FileIndexHigh << 32) | info.FileIndexLow;
            return info.VolumeSerialNumber.ToString("X8") + ":" + fileIndex.ToString("X16");
        }

        private static void MarkDeleteOnClose(SafeFileHandle handle, string label)
        {
            IntPtr buffer = Marshal.AllocHGlobal(1);
            try
            {
                // FILE_DISPOSITION_INFO.DeleteFile is the Win32 BOOLEAN type: exactly one byte.
                Marshal.WriteByte(buffer, 0, 1);
                if (!SetFileInformationByHandle(handle, FileDispositionInfo, buffer, 1))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), label);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        public static string GetIdentity(SafeFileHandle handle)
        {
            if (handle == null || handle.IsInvalid || handle.IsClosed)
                throw new ArgumentException("A live generated-template handle is required.", "handle");
            BY_HANDLE_FILE_INFORMATION info;
            if (!GetFileInformationByHandle(handle, out info))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not identify the generated V26 manifest generation.");
            if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
                throw new InvalidOperationException("Generated V26 manifest generation is reparse-backed; refusing admission.");
            return Identity(info);
        }

        public static bool DeleteFileIfSameGeneration(string path, string expectedIdentity)
        {
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
                    throw new Win32Exception(error, "Could not open the generated V26 manifest dependency for exact-generation cleanup.");
                }

                BY_HANDLE_FILE_INFORMATION info;
                if (!GetFileInformationByHandle(handle, out info))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not identify the generated V26 manifest cleanup handle.");
                if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
                    throw new InvalidOperationException("Generated V26 manifest cleanup path became reparse-backed; refusing deletion.");
                if (!String.Equals(Identity(info), expectedIdentity, StringComparison.Ordinal))
                    throw new InvalidOperationException("Generated V26 manifest pathname now names a different generation; refusing deletion.");

                MarkDeleteOnClose(handle, "Could not mark the exact generated V26 manifest dependency for deletion.");
                return true;
            }
        }

        public static SafeFileHandle OpenWorkspace(string path)
        {
            SafeFileHandle handle = CreateFileW(
                path,
                GENERIC_READ | DELETE,
                FILE_SHARE_READ | FILE_SHARE_WRITE,
                IntPtr.Zero,
                OPEN_EXISTING,
                FILE_FLAG_BACKUP_SEMANTICS | FILE_FLAG_OPEN_REPARSE_POINT,
                IntPtr.Zero);
            if (handle.IsInvalid)
            {
                int error = Marshal.GetLastWin32Error();
                handle.Dispose();
                throw new Win32Exception(error, "Could not hold the V26 manifest temporary workspace generation.");
            }

            try
            {
                BY_HANDLE_FILE_INFORMATION info;
                if (!GetFileInformationByHandle(handle, out info))
                    throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not identify the V26 manifest temporary workspace generation.");
                if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
                    throw new InvalidOperationException("V26 manifest temporary workspace is reparse-backed; refusing admission.");
                return handle;
            }
            catch
            {
                handle.Dispose();
                throw;
            }
        }

        public static void DeleteHeldWorkspace(SafeFileHandle handle)
        {
            if (handle == null || handle.IsInvalid || handle.IsClosed)
                throw new ArgumentException("A live V26 manifest workspace handle is required.", "handle");

            BY_HANDLE_FILE_INFORMATION info;
            if (!GetFileInformationByHandle(handle, out info))
                throw new Win32Exception(Marshal.GetLastWin32Error(), "Could not re-identify the held V26 manifest workspace before cleanup.");
            if ((info.FileAttributes & FILE_ATTRIBUTE_REPARSE_POINT) != 0)
                throw new InvalidOperationException("Held V26 manifest workspace became reparse-backed; refusing cleanup.");

            MarkDeleteOnClose(handle, "Could not mark the held V26 manifest workspace generation for deletion.");
        }
    }
}
'@
}

function Assert-OrdinaryPathItem {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][bool]$Directory
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($Directory -and -not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (-not $Directory -and ($item.PSIsContainer -or -not ($item -is [IO.FileInfo]))) { throw "$Label must be a regular file: $Path" }
    if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must not be reparse-backed: $Path"
    }
    return $item
}

function Assert-DirectoryAncestorChain {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $cursor = [IO.Path]::GetFullPath($Path)
    while ($true) {
        if (Test-Path -LiteralPath $cursor) {
            Assert-OrdinaryPathItem -Path $cursor -Label $Label -Directory $true | Out-Null
        }
        $parent = [IO.Directory]::GetParent($cursor)
        if (-not $parent) { break }
        $cursor = $parent.FullName
    }
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

function Assert-HeldGeneratedTemplate {
    param(
        [Parameter(Mandatory = $true)]$Admission,
        [Parameter(Mandatory = $true)][string]$ExpectedPath,
        [Parameter(Mandatory = $true)][string]$Label
    )
    if ($null -eq $Admission.Stream -or $Admission.Stream.SafeFileHandle.IsClosed -or $Admission.Stream.SafeFileHandle.IsInvalid) {
        throw "$Label held generation is no longer available."
    }
    $expected = [IO.Path]::GetFullPath($ExpectedPath)
    if (-not [string]::Equals([IO.Path]::GetFullPath([string]$Admission.Path), $expected, [StringComparison]::OrdinalIgnoreCase) -or
        -not [string]::Equals([IO.Path]::GetFullPath($Admission.Stream.Name), $expected, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label held generation path does not match the admitted workspace path."
    }
    $identity = [Qs3d.V26.ManifestTempGenerationNative]::GetIdentity($Admission.Stream.SafeFileHandle)
    if (-not [string]::Equals($identity, [string]$Admission.Identity, [StringComparison]::Ordinal)) {
        throw "$Label held generation identity changed after transformation admission."
    }
}

function Remove-ExactGeneratedScriptGeneration {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$ExpectedIdentity
    )
    return [Qs3d.V26.ManifestTempGenerationNative]::DeleteFileIfSameGeneration([IO.Path]::GetFullPath($Path), $ExpectedIdentity)
}

function Open-HeldManifestWorkspace {
    param([Parameter(Mandatory = $true)][string]$Path)
    return [Qs3d.V26.ManifestTempGenerationNative]::OpenWorkspace([IO.Path]::GetFullPath($Path))
}

function Remove-HeldManifestWorkspace {
    param([Parameter(Mandatory = $true)][Microsoft.Win32.SafeHandles.SafeFileHandle]$Handle)
    [Qs3d.V26.ManifestTempGenerationNative]::DeleteHeldWorkspace($Handle)
}

$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }
Assert-DirectoryAncestorChain -Path (Split-Path -Parent $generator) -Label 'V26 transformer ancestor'
Assert-OrdinaryPathItem -Path $generator -Label 'V26 script transformer' -Directory $false | Out-Null

$tempParent = [IO.Path]::GetFullPath([IO.Path]::GetTempPath())
Assert-DirectoryAncestorChain -Path $tempParent -Label 'V26 manifest temporary ancestor'
Assert-OrdinaryPathItem -Path $tempParent -Label 'V26 manifest temporary parent' -Directory $true | Out-Null

$tempRoot = Join-Path $tempParent ('qs3d-v26-manifest-' + [Guid]::NewGuid().ToString('N'))
$tempScript = Join-Path $tempRoot 'new-v26-update-manifest.generated.ps1'
$tempValidation = Join-Path $tempRoot 'new-v26-update-manifest-validation-core.ps1'
$tempNative = Join-Path $tempRoot 'Qs3dV26UpdateManifestPublicationNative.cs'
if (Test-Path -LiteralPath $tempRoot) { throw "V26 manifest temporary workspace already exists: $tempRoot" }
New-Item -ItemType Directory -Path $tempRoot | Out-Null
Assert-DirectoryAncestorChain -Path $tempRoot -Label 'V26 manifest temporary ancestor'
Assert-OrdinaryPathItem -Path $tempRoot -Label 'V26 manifest temporary workspace' -Directory $true | Out-Null
$workspaceHandle = Open-HeldManifestWorkspace -Path $tempRoot
$heldGenerations = [Collections.Generic.List[object]]::new()
$primaryFailure = $null
try {
    $generationPlan = @(
        @{ Source = 'new-v25-update-manifest-validation-core.ps1'; Output = $tempValidation; Label = 'Generated V26 update-manifest validation core' },
        @{ Source = 'Qs3dV25UpdateManifestPublicationNative.cs'; Output = $tempNative; Label = 'Generated V26 update-manifest native helper' },
        @{ Source = 'new-v25-update-manifest.ps1'; Output = $tempScript; Label = 'Generated V26 update-manifest script' }
    )

    foreach ($entry in $generationPlan) {
        $admission = & $generator -SourceScript $entry.Source -OutputPath $entry.Output -PassThruHeldGeneration
        if (-not $? -or $null -eq $admission -or $admission.Count -ne $null) {
            # A PSCustomObject has no Count property; arrays/multiple pipeline objects do.
            if ($admission -is [array] -or $null -eq $admission) {
                throw "Could not generate one held V26 manifest dependency from $($entry.Source)."
            }
        }
        Assert-HeldGeneratedTemplate -Admission $admission -ExpectedPath $entry.Output -Label $entry.Label
        $generatedText = Read-HeldStrictUtf8 -Stream $admission.Stream -Label $entry.Label -MaxBytes $maxGeneratedScriptBytes
        if ($generatedText -match '(?i)v25') { throw "$($entry.Label) contains a V25 token." }
        $heldGenerations.Add([pscustomobject]@{ Admission = $admission; Path = $entry.Output; Label = $entry.Label })
    }

    $main = $heldGenerations | Where-Object { $_.Path -eq $tempScript } | Select-Object -First 1
    $validation = $heldGenerations | Where-Object { $_.Path -eq $tempValidation } | Select-Object -First 1
    $native = $heldGenerations | Where-Object { $_.Path -eq $tempNative } | Select-Object -First 1
    if ($null -eq $main -or $null -eq $validation -or $null -eq $native) {
        throw 'V26 manifest generation graph is incomplete; wrapper, validation core and native helper are all mandatory.'
    }

    foreach ($held in $heldGenerations) {
        Assert-HeldGeneratedTemplate -Admission $held.Admission -ExpectedPath $held.Path -Label $held.Label
    }

    $forward = @{
        PackageDirectory = $PackageDirectory
        PackageZip = $PackageZip
        PackageUri = $PackageUri
        ExpectedSignerThumbprint = $ExpectedSignerThumbprint
        OutputPath = $OutputPath
    }
    if ($PSBoundParameters.ContainsKey('WhatIf')) { $forward['WhatIf'] = [bool]$PSBoundParameters['WhatIf'] }
    if ($PSBoundParameters.ContainsKey('Confirm')) { $forward['Confirm'] = [bool]$PSBoundParameters['Confirm'] }

    # All three transformed file generations stay open without write/delete sharing
    # across execution, so dot-source/Add-Type pathname reads cannot be redirected.
    & $tempScript @forward
    if (-not $?) { throw 'V26 update-manifest generation failed.' }

    foreach ($held in $heldGenerations) {
        Assert-HeldGeneratedTemplate -Admission $held.Admission -ExpectedPath $held.Path -Label $held.Label
    }
}
catch {
    $primaryFailure = $_
    throw
}
finally {
    for ($index = $heldGenerations.Count - 1; $index -ge 0; $index--) {
        $held = $heldGenerations[$index]
        try {
            $identity = [string]$held.Admission.Identity
            if ($null -ne $held.Admission.Stream) {
                $held.Admission.Stream.Dispose()
                $held.Admission.Stream = $null
            }
            [void](Remove-ExactGeneratedScriptGeneration -Path $held.Path -ExpectedIdentity $identity)
        }
        catch {
            if ($null -eq $primaryFailure) { throw }
            Write-Verbose "Secondary V26 manifest exact-generation cleanup failed while preserving the primary failure: $($_.Exception.Message)"
        }
    }

    if ($null -ne $workspaceHandle) {
        try { Remove-HeldManifestWorkspace -Handle $workspaceHandle }
        catch {
            if ($null -eq $primaryFailure) { throw }
            Write-Verbose "Secondary V26 manifest held-workspace cleanup failed while preserving the primary failure: $($_.Exception.Message)"
        }
        finally {
            $workspaceHandle.Dispose()
            $workspaceHandle = $null
        }
    }
}
