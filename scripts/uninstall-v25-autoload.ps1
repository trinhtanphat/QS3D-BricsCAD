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

    $metadataPath = Join-Path $installFull 'PACKAGE-METADATA.json'
    $pluginPath = Join-Path $installFull 'QS3D.BricsCAD.V25.dll'
    $corePath = Join-Path $installFull 'QS3D.Core.dll'
    if (-not (Test-Path -LiteralPath $metadataPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $pluginPath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $corePath -PathType Leaf)) {
        throw 'Refusing recursive removal because the target does not contain the canonical QS3D package identity files.'
    }

    try {
        $metadata = Get-Content -LiteralPath $metadataPath -Raw | ConvertFrom-Json
        if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V25 x64') {
            throw 'package identity does not match QS3D / BricsCAD V25 x64'
        }
        if (-not $metadata.PSObject.Properties['version'] -or -not $metadata.PSObject.Properties['productVersion']) {
            throw 'package version identity is incomplete'
        }

        $metadataAssemblyVersion = [Version]::Parse([string]$metadata.version)
        $metadataProductVersion = ([string]$metadata.productVersion).Trim()
        if ([string]::IsNullOrWhiteSpace($metadataProductVersion)) {
            throw 'package productVersion is empty'
        }

        foreach ($identityPath in @($pluginPath, $corePath)) {
            $identityName = [IO.Path]::GetFileName($identityPath)
            $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($identityPath).Version
            if (-not $assemblyVersion -or $assemblyVersion -ne $metadataAssemblyVersion) {
                throw "$identityName assembly version does not match PACKAGE-METADATA version"
            }
            $productVersion = ([string][Diagnostics.FileVersionInfo]::GetVersionInfo($identityPath).ProductVersion).Trim()
            if ([string]::IsNullOrWhiteSpace($productVersion) -or
                -not [string]::Equals($productVersion, $metadataProductVersion, [StringComparison]::Ordinal)) {
                throw "$identityName product version does not match PACKAGE-METADATA productVersion"
            }
        }
    }
    catch {
        throw "Refusing recursive removal because PACKAGE-METADATA/DLL identity is not a valid QS3D V25 installation: $($_.Exception.Message)"
    }

    return $installFull
}

function Get-RegistryTreeSnapshot {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) { return $null }
    $key = Get-Item -LiteralPath $Path
    try {
        $values = @()
        $valueNames = @($key.GetValueNames())
        [Array]::Sort($valueNames, [StringComparer]::Ordinal)
        foreach ($name in $valueNames) {
            $values += [pscustomobject]@{
                Name = [string]$name
                Value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                Kind = $key.GetValueKind($name).ToString()
            }
        }
        $childNames = @($key.GetSubKeyNames())
        [Array]::Sort($childNames, [StringComparer]::Ordinal)
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

function Get-InstallPayloadSnapshot {
    param([Parameter(Mandatory = $true)][string]$Directory)

    $root = [IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if (-not (Test-Path -LiteralPath $root -PathType Container)) { throw "Install payload disappeared before uninstall admission: $root" }
    $items = @(Get-ChildItem -LiteralPath $root -Recurse -Force)
    foreach ($item in $items) {
        if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "Refusing uninstall payload snapshot containing ReparsePoint: $($item.FullName)"
        }
    }
    $relativePaths = @($items | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        $_.FullName.Substring($root.Length).TrimStart([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    })
    [Array]::Sort($relativePaths, [StringComparer]::Ordinal)
    $rows = foreach ($relative in $relativePaths) {
        $fullPath = Join-Path $root $relative
        $file = Get-Item -LiteralPath $fullPath -Force
        if (($file.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Refusing uninstall payload snapshot containing ReparsePoint: $fullPath" }
        [pscustomobject]@{ RelativePath = $relative; Length = [long]$file.Length; Sha256 = (Get-FileHash -LiteralPath $fullPath -Algorithm SHA256).Hash.ToLowerInvariant() }
    }
    return @($rows)
}

function Assert-InstallPayloadSnapshotEqual {
    param($Expected, $Actual)
    $expectedRows = @($Expected); $actualRows = @($Actual)
    if ($expectedRows.Count -ne $actualRows.Count) { throw 'Install payload changed while uninstall approval was pending.' }
    for ($i = 0; $i -lt $expectedRows.Count; $i++) {
        $e = $expectedRows[$i]; $a = $actualRows[$i]
        if (-not [string]::Equals([string]$e.RelativePath, [string]$a.RelativePath, [StringComparison]::Ordinal) -or [long]$e.Length -ne [long]$a.Length -or -not [string]::Equals([string]$e.Sha256, [string]$a.Sha256, [StringComparison]::OrdinalIgnoreCase)) {
            throw "Install payload changed while uninstall approval was pending: $($e.RelativePath)"
        }
    }
}

function Assert-RegistryTreeSnapshotEqual {
    param($Expected, $Actual)
    if ($null -eq $Expected -and $null -eq $Actual) { return }
    if ($null -eq $Expected -or $null -eq $Actual) { throw 'DemandLoad registry generation changed while uninstall approval was pending.' }
    $expectedJson = $Expected | ConvertTo-Json -Depth 100 -Compress
    $actualJson = $Actual | ConvertTo-Json -Depth 100 -Compress
    if (-not [string]::Equals($expectedJson, $actualJson, [StringComparison]::Ordinal)) {
        throw "DemandLoad registry generation changed while uninstall approval was pending: $($Expected.Path)"
    }
}

function Get-RegistryRemovalPlan {
    param([string[]]$RequestedVersions, [string[]]$RequestedLanguages)
    $plan = @()
    foreach ($target in @(Get-DemandLoadTargets -RequestedVersions $RequestedVersions -RequestedLanguages $RequestedLanguages)) {
        $snapshot = Get-RegistryTreeSnapshot -Path $target.AppKey
        if ($null -ne $snapshot) { $plan += [pscustomobject]@{ Target = $target; Snapshot = $snapshot } }
    }
    return @($plan)
}

function Assert-RegistryPlanEqual {
    param($Expected, $Actual)
    $expectedPlan = @($Expected); $actualPlan = @($Actual)
    if ($expectedPlan.Count -ne $actualPlan.Count) { throw 'DemandLoad registry plan changed while uninstall approval was pending.' }
    for ($i = 0; $i -lt $expectedPlan.Count; $i++) {
        $e = $expectedPlan[$i]; $a = $actualPlan[$i]
        foreach ($name in @('Version','Language','AppKey')) {
            if (-not [string]::Equals([string]$e.Target.$name, [string]$a.Target.$name, [StringComparison]::OrdinalIgnoreCase)) { throw 'DemandLoad registry plan changed while uninstall approval was pending.' }
        }
        Assert-RegistryTreeSnapshotEqual -Expected $e.Snapshot -Actual $a.Snapshot
    }
}

function Restore-RegistryTreeSnapshot {
    param($Snapshot)

    if ($null -eq $Snapshot) { return }
    $path = [string]$Snapshot.Path
    if (Test-Path -LiteralPath $path) {
        throw "Refusing registry rollback because DemandLoad path was recreated: $path"
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

    $registryPlan = @(Get-RegistryRemovalPlan -RequestedVersions $VersionKeys -RequestedLanguages $LanguageKeys)

    $stageFiles = (-not $KeepFiles -and
        -not [string]::IsNullOrWhiteSpace($installFull) -and
        (Test-Path -LiteralPath $installFull -PathType Container))
    $payloadSnapshot = @()
    if ($stageFiles) {
        $payloadSnapshot = @(Get-InstallPayloadSnapshot -Directory $installFull)
    }

    if ($registryPlan.Count -eq 0 -and -not $stageFiles) {
        Write-Host 'No selected QS3D V25 DemandLoad registrations or installed payload require removal.'
        return
    }

    $transactionTarget = if ($stageFiles) { $installFull } else { 'selected BricsCAD V25 DemandLoad registrations' }
    $transactionAction = "Remove QS3D V25 transaction: DemandLoad targets=$($registryPlan.Count); remove installed files=$stageFiles"
    if (-not ($PSCmdlet.ShouldProcess($transactionTarget, $transactionAction))) {
        return
    }

    $freshRegistryPlan = @(Get-RegistryRemovalPlan -RequestedVersions $VersionKeys -RequestedLanguages $LanguageKeys)
    Assert-RegistryPlanEqual -Expected $registryPlan -Actual $freshRegistryPlan
    $registryPlan = @($freshRegistryPlan)

    if ($stageFiles) {
        $freshInstallFull = Assert-InstallDirectorySafeToRemove -Directory $InstallDirectory -ForceDelete:$Force
        $freshPayloadSnapshot = Get-InstallPayloadSnapshot -Directory $freshInstallFull
        Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual $freshPayloadSnapshot
        $installFull = $freshInstallFull
        $payloadSnapshot = @($freshPayloadSnapshot)
    }

    $quarantine = $null
    $removedSnapshots = @()
    try {
        if ($stageFiles) {
            $parent = Split-Path -Parent $installFull
            if ([string]::IsNullOrWhiteSpace($parent)) { throw 'InstallDirectory must have a parent directory for rollback-safe uninstall.' }
            $quarantine = Join-Path $parent ('.qs3d-uninstall-' + [Guid]::NewGuid().ToString('N'))
            Assert-InstallPayloadSnapshotEqual -Expected $payloadSnapshot -Actual (Get-InstallPayloadSnapshot -Directory $installFull)
            Move-Item -LiteralPath $installFull -Destination $quarantine -ErrorAction Stop
        }

        foreach ($entry in $registryPlan) {
            Assert-RegistryTreeSnapshotEqual -Expected $entry.Snapshot -Actual (Get-RegistryTreeSnapshot -Path $entry.Target.AppKey)
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
