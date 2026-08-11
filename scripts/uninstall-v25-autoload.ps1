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

function Get-RegistryTreeSnapshot {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $key = Get-Item -LiteralPath $Path
    try {
        $values = @()
        foreach ($name in $key.GetValueNames()) {
            $values += [pscustomobject]@{
                Name = [string]$name
                Value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                Kind = $key.GetValueKind($name).ToString()
            }
        }
        $childNames = @($key.GetSubKeyNames())
    }
    finally {
        $key.Close()
    }

    $children = @()
    foreach ($childName in $childNames) {
        $child = Get-RegistryTreeSnapshot -Path (Join-Path $Path $childName)
        if ($null -ne $child) { $children += $child }
    }

    return [pscustomobject]@{
        Path = $Path
        Values = @($values)
        Children = @($children)
    }
}

function Restore-RegistryTreeSnapshot {
    param($Snapshot)

    if ($null -eq $Snapshot) { return }
    $path = [string]$Snapshot.Path
    if (Test-Path -LiteralPath $path) {
        Remove-Item -LiteralPath $path -Recurse -Force -ErrorAction Stop
    }
    New-Item -Path $path -Force | Out-Null

    $key = Get-Item -LiteralPath $path
    try {
        foreach ($value in @($Snapshot.Values)) {
            $kind = [Microsoft.Win32.RegistryValueKind][Enum]::Parse(
                [Microsoft.Win32.RegistryValueKind],
                [string]$value.Kind)
            $key.SetValue([string]$value.Name, $value.Value, $kind)
        }
    }
    finally {
        $key.Close()
    }

    foreach ($child in @($Snapshot.Children)) {
        Restore-RegistryTreeSnapshot -Snapshot $child
    }
}

function Get-DemandLoadTargets {
    param([string[]]$RequestedVersions, [string[]]$RequestedLanguages)

    $root = 'HKCU:\Software\Bricsys\BricsCAD'
    if (-not (Test-Path -LiteralPath $root)) { return @() }

    $targets = @()
    $versions = @(Get-ChildItem -LiteralPath $root | Where-Object { $_.PSChildName -match '^V25' })
    if ($RequestedVersions -and $RequestedVersions.Count -gt 0) {
        $versions = @($versions | Where-Object { $RequestedVersions -contains $_.PSChildName })
    }
    foreach ($version in $versions) {
        $languages = @(Get-ChildItem -LiteralPath $version.PSPath | Where-Object { $_.PSChildName -match '^[A-Za-z]{2}_[A-Za-z]{2}$' })
        if ($RequestedLanguages -and $RequestedLanguages.Count -gt 0) {
            $languages = @($languages | Where-Object { $RequestedLanguages -contains $_.PSChildName })
        }
        foreach ($language in $languages) {
            $appKey = Join-Path $language.PSPath 'Applications\QS3D'
            if (Test-Path -LiteralPath $appKey) {
                $targets += [pscustomobject]@{
                    Version = $version.PSChildName
                    Language = $language.PSChildName
                    AppKey = $appKey
                }
            }
        }
    }
    return @($targets)
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

    $registryPlan = @()
    foreach ($target in @(Get-DemandLoadTargets -RequestedVersions $VersionKeys -RequestedLanguages $LanguageKeys)) {
        if ($PSCmdlet.ShouldProcess("$($target.Version)/$($target.Language)", 'Remove QS3D DemandLoad registration')) {
            $snapshot = Get-RegistryTreeSnapshot -Path $target.AppKey
            if ($null -ne $snapshot) {
                $registryPlan += [pscustomobject]@{
                    Target = $target
                    Snapshot = $snapshot
                }
            }
        }
    }

    $stageFiles = $false
    if (-not $KeepFiles -and -not [string]::IsNullOrWhiteSpace($installFull) -and (Test-Path -LiteralPath $installFull -PathType Container)) {
        $stageFiles = $PSCmdlet.ShouldProcess($installFull, 'Remove QS3D installed files')
    }

    $quarantine = $null
    $removedSnapshots = @()
    try {
        if ($stageFiles) {
            $parent = Split-Path -Parent $installFull
            if ([string]::IsNullOrWhiteSpace($parent)) { throw 'InstallDirectory must have a parent directory for rollback-safe uninstall.' }
            $quarantine = Join-Path $parent ('.qs3d-uninstall-' + [Guid]::NewGuid().ToString('N'))
            Move-Item -LiteralPath $installFull -Destination $quarantine -ErrorAction Stop
        }

        foreach ($entry in $registryPlan) {
            $removedSnapshots += $entry.Snapshot
            Remove-Item -LiteralPath $entry.Target.AppKey -Recurse -Force -ErrorAction Stop
        }
    }
    catch {
        $originalError = $_
        $rollbackFailures = @()
        $filesRestored = $true

        if ($quarantine -and (Test-Path -LiteralPath $quarantine)) {
            try {
                if (Test-Path -LiteralPath $installFull) {
                    throw "Canonical install path unexpectedly exists during uninstall rollback: $installFull"
                }
                Move-Item -LiteralPath $quarantine -Destination $installFull -ErrorAction Stop
            }
            catch {
                $filesRestored = $false
                $rollbackFailures += ('files: ' + $_.Exception.Message)
            }
        }

        if ($filesRestored -or -not $quarantine) {
            for ($index = $removedSnapshots.Count - 1; $index -ge 0; $index--) {
                try { Restore-RegistryTreeSnapshot -Snapshot $removedSnapshots[$index] }
                catch { $rollbackFailures += ('registry: ' + $_.Exception.Message) }
            }
        }
        else {
            $rollbackFailures += 'registry: skipped restore because the canonical install directory could not be restored.'
        }

        if ($rollbackFailures.Count -gt 0) {
            Write-Warning ('QS3D uninstall rollback encountered error(s): ' + ($rollbackFailures -join ' | '))
        }
        throw $originalError
    }

    if ($quarantine -and (Test-Path -LiteralPath $quarantine)) {
        try {
            Remove-Item -LiteralPath $quarantine -Recurse -Force -ErrorAction Stop
        }
        catch {
            Write-Warning (
                "QS3D uninstall committed, but cleanup of quarantine '$quarantine' failed: $($_.Exception.Message). " +
                'DemandLoad is removed and the canonical install path is no longer active; delete the quarantine directory manually after checking file locks.')
        }
    }

    Write-Host 'QS3D DemandLoad registration removed for the selected BricsCAD V25 targets.'
}
finally {
    Exit-Qs3dUpdateMutex -Mutex $updateMutex
}
