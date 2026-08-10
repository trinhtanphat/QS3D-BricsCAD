[CmdletBinding(SupportsShouldProcess = $true, ConfirmImpact = 'Medium')]
param(
    [string]$PackageDirectory = $PSScriptRoot,
    [string]$InstallDirectory = (Join-Path $env:LOCALAPPDATA 'QS3D\BricsCAD-V25'),
    [ValidateSet('OnCommand', 'OnStartup')]
    [string]$LoadMode = 'OnCommand',
    [string[]]$VersionKeys,
    [string[]]$LanguageKeys,
    [switch]$Force,
    [switch]$RequireSigned
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

function Assert-PackageIntegrity {
    param([string]$Directory, [switch]$SignedRequired)

    $manifest = Join-Path $Directory 'SHA256SUMS.txt'
    if (-not (Test-Path -LiteralPath $manifest -PathType Leaf)) { throw "Missing hash manifest: $manifest" }
    $verified = 0
    foreach ($line in Get-Content -LiteralPath $manifest) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-Fa-f]{64})\s{2}(.+)$') { throw "Invalid SHA256SUMS entry: $line" }
        $expected = $Matches[1].ToUpperInvariant()
        $name = $Matches[2].Trim()
        if ($name -eq 'SHA256SUMS.txt') { throw 'SHA256SUMS.txt must not hash itself.' }
        $file = Join-Path $Directory $name
        if (-not (Test-Path -LiteralPath $file -PathType Leaf)) { throw "Missing package payload: $name" }
        $actual = (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash.ToUpperInvariant()
        if ($actual -ne $expected) { throw "SHA-256 mismatch for $name" }
        $verified++
    }
    if ($verified -eq 0) { throw 'SHA256SUMS.txt contains no payload entries.' }

    $dll = Join-Path $Directory 'QS3D.BricsCAD.V25.dll'
    if (-not (Test-Path -LiteralPath $dll -PathType Leaf)) { throw 'QS3D.BricsCAD.V25.dll is missing.' }
    if ($SignedRequired) {
        $signature = Get-AuthenticodeSignature -FilePath $dll
        if ($signature.Status -ne 'Valid') { throw "QS3D plugin signature is not valid: $($signature.Status)" }
    }

    $commandsPath = Join-Path $Directory 'COMMANDS.txt'
    if (-not (Test-Path -LiteralPath $commandsPath -PathType Leaf)) { throw 'COMMANDS.txt is missing.' }
    $commands = @(Get-Content -LiteralPath $commandsPath | ForEach-Object { $_.Trim() } | Where-Object { $_ } | Sort-Object -Unique)
    if ($commands.Count -eq 0 -or -not ($commands -contains 'QS3D')) { throw 'COMMANDS.txt does not contain the QS3D entry command.' }
    return $commands
}

if (Get-Process -Name bricscad -ErrorAction SilentlyContinue) {
    throw 'Close all BricsCAD processes before installing or upgrading QS3D.'
}

$package = (Resolve-Path -LiteralPath $PackageDirectory).Path
$commands = Assert-PackageIntegrity -Directory $package -SignedRequired:$RequireSigned
$targets = @(Get-RegistryTargets -RequestedVersions $VersionKeys -RequestedLanguages $LanguageKeys)

foreach ($target in $targets) {
    if ((Test-Path -LiteralPath $target.AppKey) -and -not $Force) {
        throw "QS3D DemandLoad registration already exists for $($target.Version)/$($target.Language). Use -Force for an intentional upgrade."
    }
}

$installFull = [IO.Path]::GetFullPath($InstallDirectory)
$parent = Split-Path -Parent $installFull
if ([string]::IsNullOrWhiteSpace($parent)) { throw 'InstallDirectory must have a parent directory.' }
$stage = Join-Path $parent ('.qs3d-stage-' + [Guid]::NewGuid().ToString('N'))
$backup = $null
$payload = @(
    'QS3D.BricsCAD.V25.dll',
    'QS3D.Core.dll',
    'COMMANDS.txt',
    'PACKAGE-METADATA.json',
    'README.txt',
    'SHA256SUMS.txt',
    'uninstall-v25-autoload.ps1'
)

try {
    if ($PSCmdlet.ShouldProcess($installFull, 'Install QS3D V25 payload')) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
        New-Item -ItemType Directory -Path $stage -Force | Out-Null
        foreach ($name in $payload) {
            $source = Join-Path $package $name
            if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing installer payload: $name" }
            Copy-Item -LiteralPath $source -Destination (Join-Path $stage $name) -Force
        }

        if (Test-Path -LiteralPath $installFull) {
            if (-not $Force) { throw "Install directory already exists: $installFull" }
            $backup = $installFull + '.backup-' + [Guid]::NewGuid().ToString('N')
            Move-Item -LiteralPath $installFull -Destination $backup
        }
        Move-Item -LiteralPath $stage -Destination $installFull
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
        }
    }

    if ($backup -and (Test-Path -LiteralPath $backup)) { Remove-Item -LiteralPath $backup -Recurse -Force }
    Write-Host "QS3D installed: $installFull"
    Write-Host "DemandLoad mode: $LoadMode"
    Write-Host "Registered targets: $($targets.Count)"
    Write-Host 'Security settings were not weakened. If company policy blocks an unsigned DLL, use a signed build or an administrator-approved trusted location.'
}
catch {
    if (Test-Path -LiteralPath $stage) { Remove-Item -LiteralPath $stage -Recurse -Force -ErrorAction SilentlyContinue }
    if ($backup -and (Test-Path -LiteralPath $backup) -and -not (Test-Path -LiteralPath $installFull)) {
        Move-Item -LiteralPath $backup -Destination $installFull -ErrorAction SilentlyContinue
    }
    throw
}
