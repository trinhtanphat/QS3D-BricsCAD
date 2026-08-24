[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$BricsCadDir,
    [Parameter(Mandatory = $true)][string]$VersionKey,
    [Parameter(Mandatory = $true)][string]$LanguageKey,
    [Parameter(Mandatory = $true)][string]$ExpectedSourceSha,
    [string]$ArtifactDir = (Join-Path $PSScriptRoot '..\artifacts\local-v26-package-install-lifecycle'),
    [switch]$ConfirmDisposableInstall
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactFull = [IO.Path]::GetFullPath($ArtifactDir)
$installDir = Join-Path $env:LOCALAPPDATA ('QS3D\Qualification\V26-' + [Guid]::NewGuid().ToString('N'))
$packageDir = Join-Path $root 'dist\QS3D-BricsCAD-V26'
$zipPath = Join-Path $root 'dist\QS3D-BricsCAD-V26.zip'
$registryApp = "HKCU:\Software\Bricsys\BricsCAD\$VersionKey\$LanguageKey\Applications\QS3D"
$sentinelName = 'QS3DQualificationSentinel'
$sentinelValue = [Guid]::NewGuid().ToString('N')
$sentinelPath = "HKCU:\Software\QS3D\Qualification\$sentinelName"
$result = [ordered]@{
    schema = 1
    status = 'FAIL'
    sourceSha = ''
    productVersion = ''
    packageSha256 = ''
    hostMajor = 0
    packageIdentityValid = $false
    hashesValid = $false
    registrationCreated = $false
    registrationV26Only = $false
    installedPayloadValid = $false
    uninstallRemovedRegistration = $false
    uninstallRemovedFiles = $false
    unrelatedSentinelPreserved = $false
    cleanupComplete = $false
}

function Assert-Leaf([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label was not found." }
}

function Assert-CleanExactSource {
    $head = (& git -C $root rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') { throw 'Could not resolve exact Git HEAD.' }
    $expected = $ExpectedSourceSha.Trim().ToLowerInvariant()
    if ($expected -notmatch '^[0-9a-f]{40}$' -or $head -ne $expected) { throw 'ExpectedSourceSha does not match exact Git HEAD.' }
    $dirty = @(& git -C $root status --porcelain --untracked-files=no)
    if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'Qualification requires a clean tracked working tree.' }
    $result.sourceSha = $head
}

function Assert-HostIdentity {
    $exe = Join-Path $BricsCadDir 'bricscad.exe'
    Assert-Leaf $exe 'BricsCAD executable'
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    if ([string]::IsNullOrWhiteSpace($version)) { throw 'BricsCAD file version is unreadable.' }
    $major = [int]($version.Split('.')[0])
    if ($major -ne 26) { throw 'Configured BricsCAD host is not major version 26.' }
    if ($VersionKey -notmatch '^V26') { throw 'VersionKey must identify a V26 registry key.' }
    if ($LanguageKey -notmatch '^[A-Za-z]{2}_[A-Za-z]{2}$') { throw 'LanguageKey is not canonical.' }
    $result.hostMajor = $major
}

function Assert-Package {
    & (Join-Path $root 'scripts\package-v26.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'V26 package creation failed.' }
    Assert-Leaf (Join-Path $packageDir 'PACKAGE-METADATA.json') 'V26 package metadata'
    Assert-Leaf (Join-Path $packageDir 'SHA256SUMS.txt') 'V26 package hash manifest'
    Assert-Leaf (Join-Path $packageDir 'install-v26-autoload.ps1') 'Generated V26 installer'
    Assert-Leaf (Join-Path $packageDir 'uninstall-v26-autoload.ps1') 'Generated V26 uninstaller'
    Assert-Leaf $zipPath 'V26 package ZIP'

    $metadata = Get-Content -LiteralPath (Join-Path $packageDir 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.framework -ne 'net8.0-windows') {
        throw 'Generated package metadata does not identify QS3D BricsCAD V26 x64/net8.0-windows.'
    }
    $result.productVersion = [string]$metadata.productVersion
    if ([string]::IsNullOrWhiteSpace($result.productVersion)) { throw 'Generated package productVersion is missing.' }
    $result.packageIdentityValid = $true

    $manifest = @(Get-Content -LiteralPath (Join-Path $packageDir 'SHA256SUMS.txt'))
    if ($manifest.Count -eq 0) { throw 'V26 hash manifest is empty.' }
    foreach ($line in $manifest) {
        if ($line -notmatch '^([0-9A-F]{64})  (.+)$') { throw 'V26 hash manifest contains a malformed entry.' }
        $relative = $Matches[2]
        if ($relative.Contains('..') -or [IO.Path]::IsPathRooted($relative)) { throw 'V26 hash manifest contains an unsafe relative path.' }
        $payload = Join-Path $packageDir $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)
        Assert-Leaf $payload 'Hashed V26 payload'
        if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToUpperInvariant() -ne $Matches[1]) { throw 'V26 package hash verification failed.' }
    }
    $result.hashesValid = $true
    $result.packageSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
}

function Assert-InstalledPayload {
    foreach ($name in @('QS3D.BricsCAD.V26.dll','QS3D.BricsCAD.V26.runtimeconfig.json','QS3D.Core.dll','PACKAGE-METADATA.json','SHA256SUMS.txt')) {
        Assert-Leaf (Join-Path $installDir $name) "Installed $name"
    }
    $metadata = Get-Content -LiteralPath (Join-Path $installDir 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
    if ([string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.productVersion -ne $result.productVersion) {
        throw 'Installed V26 payload identity differs from the generated package.'
    }
    $result.installedPayloadValid = $true
}

try {
    if (-not $ConfirmDisposableInstall) { throw 'Pass -ConfirmDisposableInstall to acknowledge disposable qualification registration/files.' }
    if (-not $IsWindows) { throw 'V26 package install lifecycle requires Windows.' }
    Assert-CleanExactSource
    Assert-HostIdentity
    if (Test-Path -LiteralPath $registryApp) { throw 'Refusing qualification because QS3D is already registered in the selected V26 profile.' }
    if (Test-Path -LiteralPath $installDir) { throw 'Disposable install directory unexpectedly already exists.' }

    New-Item -ItemType Directory -Path (Split-Path -Parent $sentinelPath) -Force | Out-Null
    New-Item -ItemType Directory -Path $sentinelPath -Force | Out-Null
    New-ItemProperty -LiteralPath $sentinelPath -Name Value -Value $sentinelValue -PropertyType String -Force | Out-Null

    Assert-Package
    $installer = Join-Path $packageDir 'install-v26-autoload.ps1'
    & $installer -PackageDirectory $packageDir -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Confirm:$false
    if (-not (Test-Path -LiteralPath $registryApp)) { throw 'V26 DemandLoad registration was not created.' }
    $result.registrationCreated = $true
    if (Test-Path -LiteralPath ($registryApp -replace '\\V26', '\\V25')) { throw 'Qualification observed a cross-major V25 registration.' }
    $result.registrationV26Only = $true
    Assert-InstalledPayload

    $uninstaller = Join-Path $packageDir 'uninstall-v26-autoload.ps1'
    & $uninstaller -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -Confirm:$false
    $result.uninstallRemovedRegistration = -not (Test-Path -LiteralPath $registryApp)
    $result.uninstallRemovedFiles = -not (Test-Path -LiteralPath $installDir)
    if (-not $result.uninstallRemovedRegistration -or -not $result.uninstallRemovedFiles) { throw 'V26 uninstall left owned registration or files behind.' }

    $sentinel = Get-ItemPropertyValue -LiteralPath $sentinelPath -Name Value
    $result.unrelatedSentinelPreserved = [string]$sentinel -eq $sentinelValue
    if (-not $result.unrelatedSentinelPreserved) { throw 'V26 package lifecycle changed unrelated sentinel state.' }
    $result.status = 'PASS'
}
finally {
    Remove-Item -LiteralPath $registryApp -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction SilentlyContinue
    Remove-Item -LiteralPath $sentinelPath -Recurse -Force -ErrorAction SilentlyContinue
    $result.cleanupComplete = (-not (Test-Path -LiteralPath $registryApp)) -and (-not (Test-Path -LiteralPath $installDir)) -and (-not (Test-Path -LiteralPath $sentinelPath))
    New-Item -ItemType Directory -Path $artifactFull -Force | Out-Null
    $evidence = Join-Path $artifactFull 'v26-package-install-lifecycle.json'
    $result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $evidence -Encoding UTF8
    Write-Host ("QS3D_V26_PACKAGE_INSTALL_LIFECYCLE status={0} source={1} cleanup={2}" -f $result.status, $result.sourceSha, $result.cleanupComplete)
}

if ($result.status -ne 'PASS' -or -not $result.cleanupComplete) { exit 1 }
