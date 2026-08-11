[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$PackageDirectory,
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'QS3D\BricsCAD-V25'),
    [ValidateSet('OnCommand', 'OnStartup')]
    [string]$LoadMode = 'OnCommand',
    [string[]]$VersionKeys,
    [string[]]$LanguageKeys,
    [switch]$Force,
    [switch]$RequireSigned,
    [ValidatePattern('^[0-9A-Fa-f]{40}$')]
    [string]$ExpectedSignerThumbprint
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Get-RegistryTargets {
    param([string[]]$RequestedVersions, [string[]]$RequestedLanguages)

    $root = 'HKCU:\Software\Bricsys\BricsCAD'
    if (-not (Test-Path -LiteralPath $root)) {
        throw 'BricsCAD user registry was not found. Start BricsCAD V25 once, close it, then run the installer again.'
    }

    $versions = @(Get-ChildItem -LiteralPath $root | Where-Object { $_.PSChildName -match '^V25' })
    if ($RequestedVersions -and $RequestedVersions.Count -gt 0) {
        $versions = @($versions | Where-Object { $RequestedVersions -contains $_.PSChildName })
    }
    if ($versions.Count -eq 0) { throw 'No matching BricsCAD V25 registry version key was found.' }

    $targets = @()
    foreach ($version in $versions) {
        $languages = @(Get-ChildItem -LiteralPath $version.PSPath | Where-Object { $_.PSChildName -match '^[A-Za-z]{2}_[A-Za-z]{2}$' })
        if ($RequestedLanguages -and $RequestedLanguages.Count -gt 0) {
            $languages = @($languages | Where-Object { $RequestedLanguages -contains $_.PSChildName })
        }
        foreach ($language in $languages) {
            $targets += [pscustomobject]@{
                Version = $version.PSChildName
                Language = $language.PSChildName
                AppKey = (Join-Path $language.PSPath 'Applications\QS3D')
            }
        }
    }
    if ($targets.Count -eq 0) { throw 'No matching BricsCAD V25 language key was found.' }
    return $targets
}

function Normalize-Thumbprint {
    param([string]$Thumbprint)
    if ([string]::IsNullOrWhiteSpace($Thumbprint)) { return '' }
    return $Thumbprint.Replace(' ', '').ToUpperInvariant()
}

function Assert-AuthenticodeSigner {
    param([string]$Path, [string]$ExpectedSigner, [string]$Label)
    $signature = Get-AuthenticodeSignature -FilePath $Path
    if ($signature.Status -ne [System.Management.Automation.SignatureStatus]::Valid) {
        throw "$Label signature is not valid: $($signature.Status)"
    }
    if (-not $signature.SignerCertificate) { throw "$Label signature has no signer certificate." }
    if ($ExpectedSigner.Length -gt 0) {
        $actualSigner = Normalize-Thumbprint $signature.SignerCertificate.Thumbprint
        if ($actualSigner -ne $ExpectedSigner) { throw "$Label signer mismatch. Expected $ExpectedSigner, got $actualSigner." }
    }
}

function Assert-PackageIntegrity {
    param([string]$Directory, [switch]$SignedRequired, [string]$SignerThumbprint)

    $manifest = Join-Path $Directory 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Missing hash manifest: $manifest" }
    $verified = 0
    foreach ($line in Get-Content -LiteralPath $manifest) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-Fa-f]{64})\s{2}(.+)$') { throw "Invalid SHA256SUMS entry: $line" }
        $expected = $Matches[1].ToUpperInvariant()
        $name = $Matches[2].Trim()
        if ($name -eq 'SHA256SUMS.txt') { throw 'SHA256SUMS.txt must not hash itself.' }
        if ([IO.Path]::IsPathRooted($name) -or $name.Contains('\') -or $name.Contains(':')) {
            throw "Unsafe SHA256SUMS entry: $name"
        }
        $segments = @($name.Split('/'))
        if ($segments.Count -eq 0 -or @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
            throw "Unsafe SHA256SUMS entry: $name"
        }
        $packageRoot = [IO.Path]::GetFullPath($Directory).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
        $file = [IO.Path]::GetFullPath((Join-Path $Directory ($name.Replace('/', [IO.Path]::DirectorySeparatorChar))))
        if (-not $file.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw "Unsafe SHA256SUMS entry: $name" }
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing package payload: $name" }
        $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actual -ne $expected) { throw "SHA-256 mismatch for $name" }
        $verified++
    }
    if ($verified -eq 0) { throw 'SHA256SUMS.txt contains no payload entries.' }

    $expectedSigner = Normalize-Thumbprint $SignerThumbprint
    $signedPayloadNames = @(
        'QS3D.BricsCAD.V25.dll',
        'QS3D.Core.dll',
        'install-v25-autoload.ps1',
        'uninstall-v25-autoload.ps1',
        'update-v25.ps1'
    )
    foreach ($name in $signedPayloadNames) {
        $path = Join-Path $Directory $name
        if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Required executable payload is missing: $name" }
        if ($SignedRequired -or $expectedSigner.Length -gt 0) {
            Assert-AuthenticodeSigner -Path $path -ExpectedSigner $expectedSigner -Label ("QS3D executable payload " + $name)
        }
    }

    $commandsPath = Join-Path $Directory 'COMMANDS.txt'
    if (-not (Test-Path -LiteralPath $commandsPath -PathType Leaf)) { throw 'COMMANDS.txt is missing.' }
    $commands = @(Get-Content -LiteralPath $commandsPath | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Sort-Object -Unique)
    if ($commands.Count -eq 0 -or -not ($commands -contains 'QS3D')) { throw 'COMMANDS.txt does not contain the QS3D entry command.' }
    return $commands
}

function Get-RegistryValueSnapshot {
    param([string]$Path, [string]$Name)
    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{ Exists = $false; Name = $Name; Value = $null; Kind = $null }
    }
    $key = Get-Item -LiteralPath $Path
    try {
        if (-not ($key.GetValueNames() -contains $Name)) {
            return [pscustomobject]@{ Exists = $false; Name = $Name; Value = $null; Kind = $null }
        }
        return [pscustomobject]@{
            Exists = $true
            Name = $Name
            Value = $key.GetValue($Name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
            Kind = $key.GetValueKind($Name).ToString()
        }
    }
    finally { $key.Close() }
}

function Get-RegistryValuesSnapshot {
    param([string]$Path)
    if (-not (Test-Path -LiteralPath $Path)) { return @() }
    $key = Get-Item -LiteralPath $Path
    try {
        $values = @()
        foreach ($name in $key.GetValueNames()) {
            $values += [pscustomobject]@{
                Name = $name
                Value = $key.GetValue($name, $null, [Microsoft.Win32.RegistryValueOptions]::DoNotExpandEnvironmentNames)
                Kind = $key.GetValueKind($name).ToString()
            }
        }
        return $values
    }
    finally { $key.Close() }
}

function Get-DemandLoadSnapshot {
    param([string]$AppKey)
    $commandsKey = Join-Path $AppKey 'Commands'
    return [pscustomobject]@{
        AppKey = $AppKey
        Exists = (Test-Path -LiteralPath $AppKey)
        Loader = Get-RegistryValueSnapshot -Path $AppKey -Name 'Loader'
        LoadCtrls = Get-RegistryValueSnapshot -Path $AppKey -Name 'LoadCtrls'
        Description = Get-RegistryValueSnapshot -Path $AppKey -Name 'Description'
        CommandsExists = (Test-Path -LiteralPath $commandsKey)
        Commands = @(Get-RegistryValuesSnapshot -Path $commandsKey)
    }
}

function Set-RegistryValueSnapshot {
    param([string]$Path, $Snapshot)
    if ($Snapshot.Exists) {
        New-Item -Path $Path -Force | Out-Null
        $key = Get-Item -LiteralPath $Path
        try {
            $kind = [Microsoft.Win32.RegistryValueKind][Enum]::Parse([Microsoft.Win32.RegistryValueKind], [string]$Snapshot.Kind)
            $key.SetValue([string]$Snapshot.Name, $Snapshot.Value, $kind)
        }
        finally { $key.Close() }
    }
    elseif (Test-Path -LiteralPath $Path) {
        Remove-ItemProperty -LiteralPath $Path -Name ([string]$Snapshot.Name) -Force -ErrorAction SilentlyContinue
    }
}

function Restore-DemandLoadSnapshot {
    param($Snapshot)
    $appKey = [string]$Snapshot.AppKey
    if (-not $Snapshot.Exists) {
        Remove-Item -LiteralPath $appKey -Recurse -Force -ErrorAction SilentlyContinue
        return
    }

    New-Item -Path $appKey -Force | Out-Null
    Set-RegistryValueSnapshot -Path $appKey -Snapshot $Snapshot.Loader
    Set-RegistryValueSnapshot -Path $appKey -Snapshot $Snapshot.LoadCtrls
    Set-RegistryValueSnapshot -Path $appKey -Snapshot $Snapshot.Description

    $commandsKey = Join-Path $appKey 'Commands'
    Remove-Item -LiteralPath $commandsKey -Recurse -Force -ErrorAction SilentlyContinue
    if ($Snapshot.CommandsExists) {
        New-Item -Path $commandsKey -Force | Out-Null
        $key = Get-Item -LiteralPath $commandsKey
        try {
            foreach ($value in @($Snapshot.Commands)) {
                $kind = [Microsoft.Win32.RegistryValueKind][Enum]::Parse([Microsoft.Win32.RegistryValueKind], [string]$value.Kind)
                $key.SetValue([string]$value.Name, $value.Value, $kind)
            }
        }
        finally { $key.Close() }
    }
}

function Get-RunningBricsCADProcessDetails {
    $details = @()
    foreach ($process in @(Get-Process -Name bricscad -ErrorAction SilentlyContinue)) {
        $processPath = '<unavailable>'
        try {
            if (-not [string]::IsNullOrWhiteSpace([string]$process.Path)) {
                $processPath = [string]$process.Path
            }
        }
        catch {
            $processPath = '<unavailable>'
        }
        $details += "Name=$($process.ProcessName) PID=$($process.Id) Path=$processPath"
    }
    return $details
}

function Assert-DemandLoadRegistration {
    param(
        $Target,
        [string]$ExpectedLoader,
        [int]$ExpectedLoadCtrls,
        [string[]]$ExpectedCommands
    )

    if (-not (Test-Path -LiteralPath $Target.AppKey)) {
        throw "DemandLoad registration was not created for $($Target.Version)/$($Target.Language): $($Target.AppKey)"
    }

    $appKey = Get-Item -LiteralPath $Target.AppKey
    try {
        $actualLoader = [string]$appKey.GetValue('Loader', '')
        $actualLoadCtrls = [int]$appKey.GetValue('LoadCtrls', -1)
        $actualDescription = [string]$appKey.GetValue('Description', '')
    }
    finally { $appKey.Close() }

    if (-not [string]::Equals($actualLoader, $ExpectedLoader, [StringComparison]::OrdinalIgnoreCase)) {
        throw "DemandLoad Loader mismatch for $($Target.Version)/$($Target.Language). Expected '$ExpectedLoader', got '$actualLoader'."
    }
    if ($actualLoadCtrls -ne $ExpectedLoadCtrls) {
        throw "DemandLoad LoadCtrls mismatch for $($Target.Version)/$($Target.Language). Expected $ExpectedLoadCtrls, got $actualLoadCtrls."
    }
    if ($actualDescription -ne 'QS3D for BricsCAD V25') {
        throw "DemandLoad Description mismatch for $($Target.Version)/$($Target.Language)."
    }

    $commandsKeyPath = Join-Path $Target.AppKey 'Commands'
    if (-not (Test-Path -LiteralPath $commandsKeyPath)) {
        throw "DemandLoad Commands key is missing for $($Target.Version)/$($Target.Language)."
    }
    $commandsKey = Get-Item -LiteralPath $commandsKeyPath
    try {
        $registeredNames = @($commandsKey.GetValueNames())
        foreach ($command in $ExpectedCommands) {
            if (-not ($registeredNames -contains $command)) {
                throw "DemandLoad command registration is missing '$command' for $($Target.Version)/$($Target.Language)."
            }
            $mappedCommand = [string]$commandsKey.GetValue($command, '')
            if ($mappedCommand -ne $command) {
                throw "DemandLoad command mapping mismatch for '$command' on $($Target.Version)/$($Target.Language)."
            }
        }
    }
    finally { $commandsKey.Close() }
}

$runningBricsCAD = @(Get-RunningBricsCADProcessDetails)
if ($runningBricsCAD.Count -gt 0) {
    throw ('Close all BricsCAD processes before installing or upgrading QS3D. Detected: ' + ($runningBricsCAD -join ' | '))
}

Write-Warning 'QS3D managed plugin requires BricsCAD V25 Pro or higher. BricsCAD Shape/Lite cannot load the BRX/.NET plugin.'

$scriptDirectory = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($scriptDirectory) -and -not [string]::IsNullOrWhiteSpace([string]$MyInvocation.MyCommand.Path)) {
    $scriptDirectory = Split-Path -Parent ([IO.Path]::GetFullPath([string]$MyInvocation.MyCommand.Path))
}
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    $PackageDirectory = $scriptDirectory
}
if ([string]::IsNullOrWhiteSpace($PackageDirectory)) {
    throw 'PackageDirectory could not be resolved from the installer script location. Pass -PackageDirectory explicitly.'
}

$package = (Resolve-Path -LiteralPath $PackageDirectory).Path
$commands = Assert-PackageIntegrity -Directory $package -SignedRequired:$RequireSigned -SignerThumbprint $ExpectedSignerThumbprint
$targets = @(Get-RegistryTargets -RequestedVersions $VersionKeys -RequestedLanguages $LanguageKeys)

foreach ($target in $targets) {
    if ((Test-Path -LiteralPath $target.AppKey) -and -not $Force) {
        throw "QS3D DemandLoad registration already exists for $($target.Version)/$($target.Language). Use -Force for an intentional upgrade."
    }
}

$registrySnapshots = @($targets | ForEach-Object { Get-DemandLoadSnapshot -AppKey $_.AppKey })
$installFull = [IO.Path]::GetFullPath($InstallDirectory)
$parent = Split-Path -Parent $installFull
if ([string]::IsNullOrWhiteSpace($parent)) { throw 'InstallDirectory must have a parent directory.' }
$stage = Join-Path $parent ('.qs3d-stage-' + [Guid]::NewGuid().ToString('N'))
$backup = $null
$payloadCommitted = $false
$payload = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'COMMANDS.txt',
    'PACKAGE-METADATA.json',
    'README.txt',
    'SHA256SUMS.txt',
    'uninstall-v25-autoload.ps1',
    'update-v25.ps1'
)

try {
    if ($PSCmdlet.ShouldProcess($installFull, 'Install QS3D V25 payload')) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
        New-Item -ItemType Directory -Path $stage -Force | Out-Null
        foreach ($name in $payload) {
            $source = Join-Path $package $name
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing installer payload: $name" }
            $destination = Join-Path $stage $name
            Copy-Item -LiteralPath $source -Destination $destination -Force
            Unblock-File -LiteralPath $destination -ErrorAction Stop
        }

        if (Test-Path -LiteralPath $installFull) {
            if (-not $Force) { throw "Install directory already exists: $installFull" }
            $backup = $installFull + '.backup-' + [Guid]::NewGuid().ToString('N')
            Move-Item -LiteralPath $installFull -Destination $backup
        }
        Move-Item -LiteralPath $stage -Destination $installFull
        $payloadCommitted = $true
    }

    $loader = Join-Path $installFull 'QS3D.BricsCAD.V25.dll'
    $loadCtrls = if ($LoadMode -eq 'OnStartup') { 2 } else { 4 }
    foreach ($target in $targets) {
        if ($PSCmdlet.ShouldProcess("$($target.Version)/$($target.Language)", "Register QS3D DemandLoad ($LoadMode)")) {
            New-Item -Path $target.AppKey -Force | Out-Null
            New-ItemProperty -Path $target.AppKey -Name 'Loader' -Value $loader -PropertyType String -Force | Out-Null
            New-ItemProperty -Path $target.AppKey -Name 'LoadCtrls' -Value $loadCtrls -PropertyType DWord -Force | Out-Null
            New-ItemProperty -Path $target.AppKey -Name 'Description' -Value 'QS3D for BricsCAD V25' -PropertyType String -Force | Out-Null
            $commandsKey = Join-Path $target.AppKey 'Commands'
            Remove-Item -LiteralPath $commandsKey -Recurse -Force -ErrorAction SilentlyContinue
            New-Item -Path $commandsKey -Force | Out-Null
            foreach ($command in $commands) {
                New-ItemProperty -Path $commandsKey -Name $command -Value $command -PropertyType String -Force | Out-Null
            }
            Assert-DemandLoadRegistration -Target $target -ExpectedLoader $loader -ExpectedLoadCtrls $loadCtrls -ExpectedCommands $commands
        }
    }

    if ($backup -and (Test-Path -LiteralPath $backup)) { Remove-Item -LiteralPath $backup -Recurse -Force }
    Write-Host "QS3D installed: $installFull"
    Write-Host "DemandLoad mode: $LoadMode"
    Write-Host "Registered targets: $($targets.Count)"
    Write-Host 'Host requirement: BricsCAD V25 Pro or higher. Shape/Lite cannot load the QS3D BRX/.NET plugin.'
    Write-Host 'Security settings were not weakened. Production -RequireSigned verifies both DLLs and all packaged PowerShell executable payloads.'
}
catch {
    $originalError = $_
    $rollbackFailures = @()

    for ($index = $registrySnapshots.Count - 1; $index -ge 0; $index--) {
        try { Restore-DemandLoadSnapshot -Snapshot $registrySnapshots[$index] }
        catch { $rollbackFailures += ("registry " + $registrySnapshots[$index].AppKey + ": " + $_.Exception.Message) }
    }

    try {
        if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force }
        if ($backup -and (Test-Path -LiteralPath $backup)) {
            if (Test-Path -LiteralPath $installFull) { Remove-Item -LiteralPath $installFull -Recurse -Force }
            Move-Item -LiteralPath $backup -Destination $installFull
        }
        elseif ($payloadCommitted -and (Test-Path -LiteralPath $installFull)) {
            Remove-Item -LiteralPath $installFull -Recurse -Force
        }
    }
    catch { $rollbackFailures += ("payload: " + $_.Exception.Message) }

    if ($rollbackFailures.Count -gt 0) {
        Write-Warning ("QS3D installer rollback encountered error(s): " + ($rollbackFailures -join ' | '))
    }
    throw $originalError
}
