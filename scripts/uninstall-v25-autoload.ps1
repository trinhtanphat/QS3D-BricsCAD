[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'High')]
param(
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'QS3D\BricsCAD-V25'),
    [string[]]$VersionKeys,
    [string[]]$LanguageKeys,
    [switch]$KeepFiles,
    [switch]$Force
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$UpdateMutexPrefix = 'Global\QS3D-BricsCAD-V25-Update-'

function Enter-Qs3dUpdateMutex {
    $identity = [System.Security.Principal.WindowsIdentity]::GetCurrent()
    try { $sid = [string]$identity.User.Value }
    finally { $identity.Dispose() }
    if ([string]::IsNullOrWhiteSpace($sid)) { throw 'Could not resolve the current Windows user SID for QS3D uninstall serialization.' }

    $mutexName = $UpdateMutexPrefix + $sid
    $mutex = [System.Threading.Mutex]::new($false, $mutexName)
    $ownsMutex = $false
    try {
        try { $ownsMutex = $mutex.WaitOne(0) }
        catch [System.Threading.AbandonedMutexException] { $ownsMutex = $true }
        if (-not $ownsMutex) {
            throw 'Another QS3D install/update/uninstall is already active for this Windows user. Finish that operation before uninstalling.'
        }
        return $mutex
    }
    catch {
        $mutex.Dispose()
        throw
    }
}

function Exit-Qs3dUpdateMutex {
    param([System.Threading.Mutex]$Mutex)
    if ($null -eq $Mutex) { return }
    try { $Mutex.ReleaseMutex() }
    finally { $Mutex.Dispose() }
}

function Assert-InstallDirectorySafeToRemove {
    param([string]$Directory, [switch]$ForceDelete)

    $installFull = [IO.Path]::GetFullPath($Directory)
    if (-not (Test-Path -LiteralPath $installFull -PathType Container)) { return $installFull }

    $qs3dRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'QS3D')).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    $isDefaultScope = $installFull.StartsWith($qs3dRoot, [StringComparison]::OrdinalIgnoreCase)
    if (-not $isDefaultScope -and -not $ForceDelete) {
        throw 'Refusing to remove a custom install directory outside the QS3D LocalAppData scope. Use -Force only after verifying the path.'
    }

    if (-not $ForceDelete) {
        $metadataPath = Join-Path $installFull 'PACKAGE-METADATA.json'
        $pluginPath = Join-Path $installFull 'QS3D.BricsCAD.V25.dll'
        if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf) -or -not (Test-Path -LiteralPath $pluginPath -PathType Leaf)) {
            throw 'Refusing recursive removal because the target does not contain the QS3D package identity files. Use -Force only after verifying the path.'
        }
        try {
            $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
            if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V25 x64') {
                throw 'package identity does not match QS3D / BricsCAD V25 x64'
            }
        }
        catch {
            throw "Refusing recursive removal because PACKAGE-METADATA.json is not a valid QS3D V25 identity marker: $($_.Exception.Message)"
        }
    }

    return $installFull
}

if (Get-Process -Name bricscad -ErrorAction SilentlyContinue) {
    throw 'Close all BricsCAD processes before uninstalling QS3D.'
}

$updateMutex = Enter-Qs3dUpdateMutex
try {
    $installFull = $null
    if (-not $KeepFiles) {
        $installFull = Assert-InstallDirectorySafeToRemove -Directory $InstallDirectory -ForceDelete:$Force
    }

    $root = 'HKCU:\Software\Bricsys\BricsCAD'
    if (Test-Path -LiteralPath $root) {
        $versions = @(Get-ChildItem -LiteralPath $root | Where-Object { $_.PSChildName -match '^V25' })
        if ($VersionKeys -and $VersionKeys.Count -gt 0) { $versions = @($versions | Where-Object { $VersionKeys -contains $_.PSChildName }) }
        foreach ($version in $versions) {
            $languages = @(Get-ChildItem -LiteralPath $version.PSPath | Where-Object { $_.PSChildName -match '^[A-Za-z]{2}_[A-Za-z]{2}$' })
            if ($LanguageKeys -and $LanguageKeys.Count -gt 0) { $languages = @($languages | Where-Object { $LanguageKeys -contains $_.PSChildName }) }
            foreach ($language in $languages) {
                $appKey = Join-Path $language.PSPath 'Applications\QS3D'
                if ((Test-Path -LiteralPath $appKey) -and $PSCmdlet.ShouldProcess("$($version.PSChildName)/$($language.PSChildName)", 'Remove QS3D DemandLoad registration')) {
                    Remove-Item -LiteralPath $appKey -Recurse -Force
                }
            }
        }
    }

    if (-not $KeepFiles -and (Test-Path -LiteralPath $installFull)) {
        if ($PSCmdlet.ShouldProcess($installFull, 'Remove QS3D installed files')) {
            Remove-Item -LiteralPath $installFull -Recurse -Force
        }
    }

    Write-Host 'QS3D DemandLoad registration removed for the selected BricsCAD V25 targets.'
}
finally {
    Exit-Qs3dUpdateMutex -Mutex $updateMutex
}
