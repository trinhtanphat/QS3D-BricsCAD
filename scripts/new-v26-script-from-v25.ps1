[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [ValidateSet(
        'install-v25-autoload.ps1',
        'uninstall-v25-autoload.ps1',
        'update-v25.ps1',
        'finalize-v25-signed-package.ps1',
        'new-v25-update-manifest.ps1'
    )]
    [string]$SourceScript,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Assert-OrdinaryPathItem {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][bool]$Directory
    )

    $item = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    if ($Directory -and -not $item.PSIsContainer) { throw "$Label must be a directory: $Path" }
    if (-not $Directory -and $item.PSIsContainer) { throw "$Label must be a regular file: $Path" }
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

function Assert-SafeExistingOutputLeaf {
    param([Parameter(Mandatory = $true)][string]$Path)
    if (Test-Path -LiteralPath $Path) {
        Assert-OrdinaryPathItem -Path $Path -Label 'V26 generated script output' -Directory $false | Out-Null
    }
}

if (-not ('Qs3dV26TemplateAdmission.NativeMethods' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text;

namespace Qs3dV26TemplateAdmission
{
    [StructLayout(LayoutKind.Sequential)]
    public struct ByHandleFileInformation
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

    public static class NativeMethods
    {
        public const uint FILE_SHARE_READ = 0x00000001;
        public const uint FILE_SHARE_WRITE = 0x00000002;
        public const uint OPEN_EXISTING = 3;
        public const uint FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;
        public const uint FILE_FLAG_OPEN_REPARSE_POINT = 0x00200000;

        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetFileInformationByHandle(
            SafeFileHandle hFile,
            out ByHandleFileInformation lpFileInformation);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern uint GetFinalPathNameByHandle(
            SafeFileHandle hFile,
            StringBuilder lpszFilePath,
            uint cchFilePath,
            uint dwFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            string lpFileName,
            uint dwDesiredAccess,
            uint dwShareMode,
            IntPtr lpSecurityAttributes,
            uint dwCreationDisposition,
            uint dwFlagsAndAttributes,
            IntPtr hTemplateFile);
    }
}
'@
}

function Get-AdmittedHandleInformation {
    param([Parameter(Mandatory = $true)][Microsoft.Win32.SafeHandles.SafeFileHandle]$Handle)

    $info = New-Object Qs3dV26TemplateAdmission.ByHandleFileInformation
    if (-not [Qs3dV26TemplateAdmission.NativeMethods]::GetFileInformationByHandle($Handle, [ref]$info)) {
        throw "Unable to inspect admitted V25 template handle. Win32=$([Runtime.InteropServices.Marshal]::GetLastWin32Error())"
    }
    return $info
}

function Get-AdmittedHandlePath {
    param([Parameter(Mandatory = $true)][Microsoft.Win32.SafeHandles.SafeFileHandle]$Handle)

    $capacity = 32768
    $builder = New-Object Text.StringBuilder $capacity
    $length = [Qs3dV26TemplateAdmission.NativeMethods]::GetFinalPathNameByHandle($Handle, $builder, [uint32]$capacity, 0)
    if ($length -eq 0 -or $length -ge $capacity) {
        throw "Unable to resolve admitted V25 template handle path. Win32=$([Runtime.InteropServices.Marshal]::GetLastWin32Error())"
    }

    $path = $builder.ToString()
    if ($path.StartsWith('\\?\UNC\', [StringComparison]::OrdinalIgnoreCase)) {
        $path = '\\' + $path.Substring(8)
    }
    elseif ($path.StartsWith('\\?\', [StringComparison]::OrdinalIgnoreCase)) {
        $path = $path.Substring(4)
    }
    return [IO.Path]::GetFullPath($path)
}

function Test-SameHandleIdentity {
    param(
        [Parameter(Mandatory = $true)]$Before,
        [Parameter(Mandatory = $true)]$After
    )

    return $Before.VolumeSerialNumber -eq $After.VolumeSerialNumber -and
        $Before.FileIndexHigh -eq $After.FileIndexHigh -and
        $Before.FileIndexLow -eq $After.FileIndexLow
}

function Get-HandleLength {
    param([Parameter(Mandatory = $true)]$Information)
    return (([uint64]$Information.FileSizeHigh -shl 32) -bor [uint64]$Information.FileSizeLow)
}

function Open-AdmittedOutputParent {
    param([Parameter(Mandatory = $true)][string]$Path)

    $handle = [Qs3dV26TemplateAdmission.NativeMethods]::CreateFile(
        $Path,
        0,
        [Qs3dV26TemplateAdmission.NativeMethods]::FILE_SHARE_READ -bor [Qs3dV26TemplateAdmission.NativeMethods]::FILE_SHARE_WRITE,
        [IntPtr]::Zero,
        [Qs3dV26TemplateAdmission.NativeMethods]::OPEN_EXISTING,
        [Qs3dV26TemplateAdmission.NativeMethods]::FILE_FLAG_BACKUP_SEMANTICS -bor [Qs3dV26TemplateAdmission.NativeMethods]::FILE_FLAG_OPEN_REPARSE_POINT,
        [IntPtr]::Zero)
    if ($handle.IsInvalid) {
        $errorCode = [Runtime.InteropServices.Marshal]::GetLastWin32Error()
        $handle.Dispose()
        throw "Unable to open V26 output parent held generation. Win32=$errorCode"
    }

    try {
        $info = Get-AdmittedHandleInformation -Handle $handle
        if (($info.FileAttributes -band [uint32][IO.FileAttributes]::Directory) -eq 0) {
            throw 'V26 output parent held generation must be a directory.'
        }
        if (($info.FileAttributes -band [uint32][IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw 'V26 output parent held generation must not be reparse-backed.'
        }
        $resolved = Get-AdmittedHandlePath -Handle $handle
        $expected = [IO.Path]::GetFullPath($Path)
        if (-not [string]::Equals($resolved.TrimEnd('\'), $expected.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
            throw 'V26 output parent held generation resolved to an unexpected path.'
        }
        return [pscustomobject]@{ Handle = $handle; Information = $info; Path = $resolved }
    }
    catch {
        $handle.Dispose()
        throw
    }
}

function Assert-AdmittedOutputParentBinding {
    param([Parameter(Mandatory = $true)]$Admission)

    if ($Admission.Handle.IsClosed -or $Admission.Handle.IsInvalid) {
        throw 'V26 output parent held generation is no longer available.'
    }
    $currentInfo = Get-AdmittedHandleInformation -Handle $Admission.Handle
    if (-not (Test-SameHandleIdentity -Before $Admission.Information -After $currentInfo)) {
        throw 'V26 output parent held generation identity changed before publication.'
    }
    $currentPath = Get-AdmittedHandlePath -Handle $Admission.Handle
    if (-not [string]::Equals($currentPath.TrimEnd('\'), $Admission.Path.TrimEnd('\'), [StringComparison]::OrdinalIgnoreCase)) {
        throw 'V26 output parent held generation path changed before publication.'
    }

    $pathAdmission = Open-AdmittedOutputParent -Path $Admission.Path
    try {
        if (-not (Test-SameHandleIdentity -Before $Admission.Information -After $pathAdmission.Information)) {
            throw 'V26 output parent pathname no longer resolves to the admitted held generation.'
        }
    }
    finally {
        $pathAdmission.Handle.Dispose()
    }
}

$sourcePath = Join-Path $PSScriptRoot $SourceScript
if (-not (Test-Path -LiteralPath $sourcePath -PathType Leaf)) {
    throw "V25 template script was not found: $sourcePath"
}

$sourceFull = [IO.Path]::GetFullPath($sourcePath)
Assert-DirectoryAncestorChain -Path (Split-Path -Parent $sourceFull) -Label 'V25 template ancestor'
Assert-OrdinaryPathItem -Path $sourceFull -Label 'V25 template script' -Directory $false | Out-Null

$outputFull = [IO.Path]::GetFullPath($OutputPath)
if ([string]::Equals($sourceFull, $outputFull, [StringComparison]::OrdinalIgnoreCase)) {
    throw 'V26 generation output must not overwrite its V25 source template.'
}
if (-not [string]::Equals([IO.Path]::GetExtension($outputFull), '.ps1', [StringComparison]::OrdinalIgnoreCase)) {
    throw "V26 generated script must use the .ps1 extension: $outputFull"
}

$sourceBytes = $null
$templateHash = $null
$sourceStream = [IO.File]::Open($sourceFull, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
try {
    $beforeInfo = Get-AdmittedHandleInformation -Handle $sourceStream.SafeFileHandle
    $beforePath = Get-AdmittedHandlePath -Handle $sourceStream.SafeFileHandle
    if (-not [string]::Equals($beforePath, $sourceFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'V25 template pathname changed or resolved through a redirected target during admission.'
    }
    if (($beforeInfo.FileAttributes -band [uint32][IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw 'Admitted V25 template handle must not be reparse-backed.'
    }

    $expectedLength = Get-HandleLength -Information $beforeInfo
    if ([uint64]$sourceStream.Length -ne $expectedLength) {
        throw 'Admitted V25 template length disagrees with handle metadata before capture.'
    }

    $memory = New-Object IO.MemoryStream
    try {
        $sourceStream.CopyTo($memory)
        $sourceBytes = $memory.ToArray()
    }
    finally {
        $memory.Dispose()
    }

    $afterInfo = Get-AdmittedHandleInformation -Handle $sourceStream.SafeFileHandle
    $afterPath = Get-AdmittedHandlePath -Handle $sourceStream.SafeFileHandle
    if (-not (Test-SameHandleIdentity -Before $beforeInfo -After $afterInfo)) {
        throw 'Admitted V25 template file identity changed during capture.'
    }
    if (-not [string]::Equals($afterPath, $beforePath, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Admitted V25 template handle path changed during capture.'
    }
    if ((Get-HandleLength -Information $afterInfo) -ne $expectedLength -or [uint64]$sourceBytes.LongLength -ne $expectedLength) {
        throw 'Admitted V25 template length changed during capture.'
    }

    $sha256 = [Security.Cryptography.SHA256]::Create()
    try {
        $templateHash = ([BitConverter]::ToString($sha256.ComputeHash($sourceBytes))).Replace('-', '').ToUpperInvariant()
    }
    finally {
        $sha256.Dispose()
    }
}
finally {
    $sourceStream.Dispose()
}

$utf8 = New-Object Text.UTF8Encoding($false, $true)
$offset = 0
if ($sourceBytes.Length -ge 3 -and $sourceBytes[0] -eq 0xEF -and $sourceBytes[1] -eq 0xBB -and $sourceBytes[2] -eq 0xBF) {
    $offset = 3
}
$text = $utf8.GetString($sourceBytes, $offset, $sourceBytes.Length - $offset)
if ([string]::IsNullOrWhiteSpace($text)) { throw "V25 template script is empty: $sourceFull" }
if ($text.IndexOf('V25', [StringComparison]::Ordinal) -lt 0 -and $text.IndexOf('v25', [StringComparison]::Ordinal) -lt 0) {
    throw "V25 template contains no host-major token to transform: $SourceScript"
}

$generated = $text.Replace('V25', 'V26').Replace('v25', 'v26')
if ($SourceScript -eq 'install-v25-autoload.ps1') {
    $payloadAnchor = "        'QS3D.BricsCAD.V26.dll',`r`n        'QS3D.Core.dll',"
    $payloadReplacement = "        'QS3D.BricsCAD.V26.dll',`r`n        'QS3D.BricsCAD.V26.runtimeconfig.json',`r`n        'QS3D.Core.dll',"
    if ($generated.IndexOf($payloadAnchor, [StringComparison]::Ordinal) -lt 0) {
        $payloadAnchor = "        'QS3D.BricsCAD.V26.dll',`n        'QS3D.Core.dll',"
        $payloadReplacement = "        'QS3D.BricsCAD.V26.dll',`n        'QS3D.BricsCAD.V26.runtimeconfig.json',`n        'QS3D.Core.dll',"
    }
    if ($generated.IndexOf($payloadAnchor, [StringComparison]::Ordinal) -lt 0) {
        throw 'Generated V26 installer payload anchor changed; refusing to omit the required runtimeconfig.'
    }
    $generated = $generated.Replace($payloadAnchor, $payloadReplacement)
}

if ($generated.IndexOf('V25', [StringComparison]::Ordinal) -ge 0 -or
    $generated.IndexOf('v25', [StringComparison]::Ordinal) -ge 0) {
    throw "Generated V26 script still contains a V25/v25 token: $SourceScript"
}

$requiredTokens = switch ($SourceScript) {
    'install-v25-autoload.ps1' { @('QS3D.BricsCAD.V26.dll', 'QS3D.BricsCAD.V26.runtimeconfig.json', 'BricsCAD V26 x64', 'BricsCAD-V26', '^V26', 'QS3D-BricsCAD-V26-Update-'); break }
    'uninstall-v25-autoload.ps1' { @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'BricsCAD-V26', '^V26', 'QS3D-BricsCAD-V26-Update-'); break }
    'update-v25.ps1' { @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'BricsCAD-V26', 'QS3D-BricsCAD-V26.update.json', 'QS3D-BricsCAD-V26.zip', 'install-v26-autoload.ps1', 'QS3D-BricsCAD-V26-Update-'); break }
    'finalize-v25-signed-package.ps1' { @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'QS3D-BricsCAD-V26.zip', 'install-v26-autoload.ps1', 'uninstall-v26-autoload.ps1', 'update-v26.ps1'); break }
    'new-v25-update-manifest.ps1' { @('QS3D.BricsCAD.V26.dll', 'BricsCAD V26 x64', 'QS3D-BricsCAD-V26.zip', 'QS3D-BricsCAD-V26.update.json', 'install-v26-autoload.ps1', 'uninstall-v26-autoload.ps1', 'update-v26.ps1'); break }
    default { throw "Unsupported V25 template: $SourceScript" }
}

foreach ($token in $requiredTokens) {
    if ($generated.IndexOf($token, [StringComparison]::Ordinal) -lt 0) {
        throw "Generated V26 script is missing required transformed token '$token' from $SourceScript"
    }
}

$parent = Split-Path -Parent $outputFull
if ([string]::IsNullOrWhiteSpace($parent)) { throw "V26 generated script output requires a parent directory: $outputFull" }
Assert-DirectoryAncestorChain -Path $parent -Label 'V26 output ancestor'
if (-not (Test-Path -LiteralPath $parent)) {
    [IO.Directory]::CreateDirectory($parent) | Out-Null
}
Assert-DirectoryAncestorChain -Path $parent -Label 'V26 output ancestor'
Assert-OrdinaryPathItem -Path $parent -Label 'V26 output parent' -Directory $true | Out-Null
if (Test-Path -LiteralPath $outputFull) {
    Assert-SafeExistingOutputLeaf -Path $outputFull
    throw 'V26 generation output must be fresh; refusing to replace an existing destination generation.'
}

$outputParentHandle = Open-AdmittedOutputParent -Path $parent
$stagePath = Join-Path $parent ('.' + [IO.Path]::GetFileName($outputFull) + '.' + [Guid]::NewGuid().ToString('N') + '.tmp')
try {
    Assert-AdmittedOutputParentBinding -Admission $outputParentHandle
    if (Test-Path -LiteralPath $outputFull) { throw 'V26 generation output must be fresh; destination appeared before staging.' }
    [IO.File]::WriteAllText($stagePath, $generated, (New-Object Text.UTF8Encoding($false)))
    Assert-OrdinaryPathItem -Path $stagePath -Label 'V26 generated script staging file' -Directory $false | Out-Null
    Assert-AdmittedOutputParentBinding -Admission $outputParentHandle
    if (Test-Path -LiteralPath $outputFull) { throw 'V26 generation output must be fresh; destination appeared before publication.' }
    [IO.File]::Move($stagePath, $outputFull)
}
finally {
    if (Test-Path -LiteralPath $stagePath) {
        Assert-AdmittedOutputParentBinding -Admission $outputParentHandle
        Assert-OrdinaryPathItem -Path $stagePath -Label 'V26 generated script staging file' -Directory $false | Out-Null
        Remove-Item -LiteralPath $stagePath -Force
    }
    $outputParentHandle.Handle.Dispose()
}
Assert-OrdinaryPathItem -Path $outputFull -Label 'V26 generated script output' -Directory $false | Out-Null

Write-Host "Generated V26 script: $outputFull"
Write-Host "Template: $SourceScript"
Write-Host "Template SHA256: $templateHash"