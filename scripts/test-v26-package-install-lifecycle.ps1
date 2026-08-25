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
$packageDir = Join-Path $root 'dist\QS3D-BricsCAD-V26'
$zipPath = Join-Path $root 'dist\QS3D-BricsCAD-V26.zip'
if ([string]::IsNullOrWhiteSpace($env:LOCALAPPDATA)) { throw 'LOCALAPPDATA is required for disposable V26 qualification.' }
$qualificationRoot = [IO.Path]::GetFullPath((Join-Path $env:LOCALAPPDATA 'QS3D\Qualification'))
$installDir = Join-Path $qualificationRoot ('V26-' + [Guid]::NewGuid().ToString('N'))
$registryApp = "HKCU:\Software\Bricsys\BricsCAD\$VersionKey\$LanguageKey\Applications\QS3D"
$sentinelRoot = 'HKCU:\Software\QS3D\Qualification'
$sentinelName = 'V26PackageLifecycle-' + [Guid]::NewGuid().ToString('N')
$sentinelPath = Join-Path $sentinelRoot $sentinelName
$sentinelValue = [Guid]::NewGuid().ToString('N')
$originalV26Dir = $env:BRICSCAD_V26_DIR
$result = [ordered]@{
    schema = 2
    status = 'FAIL'
    sourceSha = ''
    productVersion = ''
    packageSha256 = ''
    hostMajor = 0
    buildSucceeded = $false
    packageIdentityValid = $false
    hashesValid = $false
    runtimeConfigPackaged = $false
    registrationCreated = $false
    registrationV26Only = $false
    registrationIdentityValid = $false
    installedPayloadValid = $false
    installedPayloadHashesMatch = $false
    runtimeConfigInstalled = $false
    uninstallRemovedRegistration = $false
    uninstallRemovedFiles = $false
    unrelatedV25RegistrationPreserved = $false
    unrelatedSentinelPreserved = $false
    cleanupComplete = $false
}

function Assert-Leaf([string]$Path, [string]$Label) {
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "$Label was not found." }
}

function Assert-DirectoryInside([string]$Candidate, [string]$Parent, [string]$Label) {
    $candidateFull = [IO.Path]::GetFullPath($Candidate)
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $candidateFull.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must stay inside the disposable QS3D qualification root."
    }
}

function Assert-CleanExactSource {
    $head = (& git -C $root rev-parse HEAD).Trim().ToLowerInvariant()
    if ($LASTEXITCODE -ne 0 -or $head -notmatch '^[0-9a-f]{40}$') { throw 'Could not resolve exact Git HEAD.' }
    $expected = $ExpectedSourceSha.Trim().ToLowerInvariant()
    if ($expected -notmatch '^[0-9a-f]{40}$' -or $head -ne $expected) { throw 'ExpectedSourceSha does not match exact Git HEAD.' }
    $dirty = @(& git -C $root status --porcelain)
    if ($LASTEXITCODE -ne 0 -or $dirty.Count -ne 0) { throw 'Qualification requires a completely clean working tree, including untracked files.' }
    $result.sourceSha = $head
}

function Assert-HostIdentity {
    $exe = Join-Path $BricsCadDir 'bricscad.exe'
    Assert-Leaf $exe 'BricsCAD executable'
    foreach ($assembly in @('BrxMgd.dll','TD_Mgd.dll','TD_MgdBrep.dll')) { Assert-Leaf (Join-Path $BricsCadDir $assembly) "BricsCAD V26 $assembly" }
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe).FileVersion
    if ([string]::IsNullOrWhiteSpace($version)) { throw 'BricsCAD file version is unreadable.' }
    $majorText = $version.Split('.')[0]
    $major = 0
    if (-not [int]::TryParse($majorText, [ref]$major) -or $major -ne 26) { throw 'Configured BricsCAD host is not major version 26.' }
    if ($VersionKey -notmatch '^V26(?:x64)?(?:\.|$)') { throw 'VersionKey must identify a V26 registry key.' }
    if ($LanguageKey -notmatch '^[A-Za-z]{2}_[A-Za-z]{2}$') { throw 'LanguageKey is not canonical.' }
    $result.hostMajor = $major
}

function Get-RegistryStateDigest([string]$MajorPattern) {
    $rootKey = 'HKCU:\Software\Bricsys\BricsCAD'
    if (-not (Test-Path -LiteralPath $rootKey)) { return 'MISSING' }
    $lines = [Collections.Generic.List[string]]::new()
    foreach ($version in @(Get-ChildItem -LiteralPath $rootKey | Where-Object { $_.PSChildName -match $MajorPattern } | Sort-Object PSChildName)) {
        foreach ($language in @(Get-ChildItem -LiteralPath $version.PSPath | Where-Object { $_.PSChildName -match '^[A-Za-z]{2}_[A-Za-z]{2}$' } | Sort-Object PSChildName)) {
            $app = Join-Path $language.PSPath 'Applications\QS3D'
            if (-not (Test-Path -LiteralPath $app)) { continue }
            $key = Get-Item -LiteralPath $app
            try {
                $lines.Add($version.PSChildName + '/' + $language.PSChildName)
                foreach ($name in @($key.GetValueNames() | Sort-Object)) {
                    $lines.Add($name + '=' + [string]$key.GetValue($name, ''))
                }
            }
            finally { $key.Close() }
            $commands = Join-Path $app 'Commands'
            if (Test-Path -LiteralPath $commands) {
                $commandKey = Get-Item -LiteralPath $commands
                try {
                    foreach ($name in @($commandKey.GetValueNames() | Sort-Object)) {
                        $lines.Add('command:' + $name + '=' + [string]$commandKey.GetValue($name, ''))
                    }
                }
                finally { $commandKey.Close() }
            }
        }
    }
    $bytes = [Text.Encoding]::UTF8.GetBytes(($lines -join "`n"))
    $sha = [Security.Cryptography.SHA256]::Create()
    try { return ([BitConverter]::ToString($sha.ComputeHash($bytes))).Replace('-', '') }
    finally { $sha.Dispose() }
}

function Invoke-V26Build {
    $env:BRICSCAD_V26_DIR = [IO.Path]::GetFullPath($BricsCadDir)
    & dotnet build (Join-Path $root 'src\QS3D.BricsCAD.V26\QS3D.BricsCAD.V26.csproj') -c Release '-p:Platform=x64'
    if ($LASTEXITCODE -ne 0) { throw 'V26 Release build failed.' }
    $result.buildSucceeded = $true
}

function Get-PackageManifest {
    $manifestPath = Join-Path $packageDir 'SHA256SUMS.txt'
    $entries = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    $packageRoot = [IO.Path]::GetFullPath($packageDir).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    foreach ($line in @(Get-Content -LiteralPath $manifestPath)) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-F]{64})  ([^\\:]+)$') { throw 'V26 hash manifest contains a malformed or non-canonical entry.' }
        $relative = $Matches[2]
        $segments = @($relative.Split('/'))
        if ($relative -eq 'SHA256SUMS.txt' -or [IO.Path]::IsPathRooted($relative) -or @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) {
            throw 'V26 hash manifest contains an unsafe relative path.'
        }
        if (-not $entries.TryAdd($relative, $Matches[1])) { throw 'V26 hash manifest contains a duplicate payload entry.' }
        $payload = [IO.Path]::GetFullPath((Join-Path $packageDir $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
        if (-not $payload.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 hash manifest payload escaped package root.' }
        Assert-Leaf $payload 'Hashed V26 payload'
        if ((Get-FileHash -LiteralPath $payload -Algorithm SHA256).Hash.ToUpperInvariant() -ne $Matches[1]) { throw 'V26 package hash verification failed.' }
    }
    if ($entries.Count -eq 0) { throw 'V26 hash manifest is empty.' }
    $actual = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @(Get-ChildItem -LiteralPath $packageDir -Recurse -File)) {
        if ([string]::Equals($file.Name, 'SHA256SUMS.txt', [StringComparison]::OrdinalIgnoreCase) -and [string]::Equals($file.DirectoryName, $packageDir, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $full = [IO.Path]::GetFullPath($file.FullName)
        if (-not $full.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 package payload escaped package root.' }
        $relative = $full.Substring($packageRoot.Length).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')
        if (-not $actual.Add($relative) -or -not $entries.ContainsKey($relative)) { throw 'V26 package contains an unhashed or case-colliding payload.' }
    }
    if ($actual.Count -ne $entries.Count) { throw 'V26 hash manifest does not cover the package exactly.' }
    foreach ($relative in $entries.Keys) { if (-not $actual.Contains($relative)) { throw 'V26 hash manifest references a non-package payload.' } }
    return $entries
}

function Assert-Package {
    & (Join-Path $root 'scripts\package-v26.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'V26 package creation failed.' }
    foreach ($name in @('PACKAGE-METADATA.json','SHA256SUMS.txt','install-v26-autoload.ps1','uninstall-v26-autoload.ps1','update-v26.ps1','QS3D.BricsCAD.V26.runtimeconfig.json')) {
        Assert-Leaf (Join-Path $packageDir $name) "V26 package $name"
    }
    Assert-Leaf $zipPath 'V26 package ZIP'

    $metadata = Get-Content -LiteralPath (Join-Path $packageDir 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.framework -ne 'net8.0-windows') {
        throw 'Generated package metadata does not identify QS3D BricsCAD V26 x64/net8.0-windows.'
    }
    $result.productVersion = [string]$metadata.productVersion
    if ([string]::IsNullOrWhiteSpace($result.productVersion)) { throw 'Generated package productVersion is missing.' }
    $result.packageIdentityValid = $true

    $manifest = Get-PackageManifest
    $result.hashesValid = $true
    $result.runtimeConfigPackaged = $manifest.ContainsKey('QS3D.BricsCAD.V26.runtimeconfig.json')
    if (-not $result.runtimeConfigPackaged) { throw 'V26 runtimeconfig is not covered by package hashes.' }
    $result.packageSha256 = (Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToUpperInvariant()
    return $manifest
}

function Assert-RegistrationIdentity {
    if (-not (Test-Path -LiteralPath $registryApp)) { throw 'V26 DemandLoad registration was not created.' }
    $key = Get-Item -LiteralPath $registryApp
    try {
        $loader = [string]$key.GetValue('Loader', '')
        $loadCtrls = [int]$key.GetValue('LoadCtrls', -1)
        $description = [string]$key.GetValue('Description', '')
    }
    finally { $key.Close() }
    $expectedLoader = [IO.Path]::GetFullPath((Join-Path $installDir 'QS3D.BricsCAD.V26.dll'))
    if (-not [string]::Equals([IO.Path]::GetFullPath($loader), $expectedLoader, [StringComparison]::OrdinalIgnoreCase) -or $loadCtrls -ne 4 -or $description -ne 'QS3D for BricsCAD V26') {
        throw 'V26 DemandLoad registration identity is incorrect.'
    }
    $commandsPath = Join-Path $registryApp 'Commands'
    if (-not (Test-Path -LiteralPath $commandsPath)) { throw 'V26 DemandLoad Commands registration is missing.' }
    $commands = Get-Item -LiteralPath $commandsPath
    try { if (-not ($commands.GetValueNames() -contains 'QS3D')) { throw 'V26 DemandLoad registration is missing the QS3D entry command.' } }
    finally { $commands.Close() }
    $result.registrationCreated = $true
    $result.registrationIdentityValid = $true
}

function Get-RuntimeFrameworkNames($RuntimeConfig) {
    if ($null -eq $RuntimeConfig) { throw 'Installed V26 runtimeconfig is empty.' }
    $runtimeOptionsProperty = $RuntimeConfig.PSObject.Properties['runtimeOptions']
    if ($null -eq $runtimeOptionsProperty -or $null -eq $runtimeOptionsProperty.Value) { throw 'Installed V26 runtimeconfig is missing runtimeOptions.' }
    $frameworksProperty = $runtimeOptionsProperty.Value.PSObject.Properties['frameworks']
    if ($null -eq $frameworksProperty -or $null -eq $frameworksProperty.Value) { throw 'Installed V26 runtimeconfig is missing runtimeOptions.frameworks.' }
    if (-not ($frameworksProperty.Value -is [System.Array])) { throw 'Installed V26 runtimeconfig runtimeOptions.frameworks must be an array.' }
    $frameworks = @($frameworksProperty.Value)
    if ($frameworks.Count -eq 0) { throw 'Installed V26 runtimeconfig runtimeOptions.frameworks is empty.' }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($framework in $frameworks) {
        if ($null -eq $framework) { throw 'Installed V26 runtimeconfig contains a null framework entry.' }
        $nameProperty = $framework.PSObject.Properties['name']
        if ($null -eq $nameProperty) { throw 'Installed V26 runtimeconfig contains a framework without a name.' }
        $name = [string]$nameProperty.Value
        if ([string]::IsNullOrWhiteSpace($name)) { throw 'Installed V26 runtimeconfig contains an empty framework name.' }
        if (-not $names.Add($name)) { throw 'Installed V26 runtimeconfig contains a duplicate framework name.' }
    }
    return @($names)
}

function Assert-InstalledPayload($Manifest) {
    $expected = @(
        'QS3D.BricsCAD.V26.dll',
        'QS3D.BricsCAD.V26.runtimeconfig.json',
        'QS3D.Core.dll',
        'COMMANDS.txt',
        'PACKAGE-METADATA.json',
        'README.txt',
        'SHA256SUMS.txt',
        'uninstall-v26-autoload.ps1',
        'update-v26.ps1'
    )
    $actual = @(Get-ChildItem -LiteralPath $installDir -File | Select-Object -ExpandProperty Name | Sort-Object)
    $expectedSorted = @($expected | Sort-Object)
    if (($actual -join "`n") -cne ($expectedSorted -join "`n")) { throw 'Installed V26 payload file set differs from the canonical installer contract.' }
    foreach ($name in $expected) {
        $installed = Join-Path $installDir $name
        Assert-Leaf $installed "Installed $name"
        if ($name -eq 'SHA256SUMS.txt') { continue }
        if (-not $Manifest.ContainsKey($name)) { throw "Installed V26 payload is not covered by package manifest: $name" }
        if ((Get-FileHash -LiteralPath $installed -Algorithm SHA256).Hash.ToUpperInvariant() -ne $Manifest[$name]) { throw "Installed V26 payload hash differs from package: $name" }
    }
    $metadata = Get-Content -LiteralPath (Join-Path $installDir 'PACKAGE-METADATA.json') -Raw | ConvertFrom-Json
    if ([string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.framework -ne 'net8.0-windows' -or [string]$metadata.productVersion -ne $result.productVersion) {
        throw 'Installed V26 payload identity differs from the generated package.'
    }
    $runtime = Get-Content -LiteralPath (Join-Path $installDir 'QS3D.BricsCAD.V26.runtimeconfig.json') -Raw | ConvertFrom-Json
    $frameworkNames = @(Get-RuntimeFrameworkNames $runtime)
    if (-not ($frameworkNames -contains 'Microsoft.WindowsDesktop.App')) { throw 'Installed V26 runtimeconfig does not target Microsoft.WindowsDesktop.App.' }
    $result.installedPayloadValid = $true
    $result.installedPayloadHashesMatch = $true
    $result.runtimeConfigInstalled = $true
}

try {
    if (-not $ConfirmDisposableInstall) { throw 'Pass -ConfirmDisposableInstall to acknowledge disposable qualification registration/files.' }
    if ([Environment]::OSVersion.Platform -ne [PlatformID]::Win32NT) { throw 'V26 package install lifecycle requires Windows.' }
    Assert-DirectoryInside $installDir $qualificationRoot 'InstallDirectory'
    if (Get-Process -Name bricscad -ErrorAction SilentlyContinue) { throw 'Close all BricsCAD processes before V26 package lifecycle qualification.' }
    Assert-CleanExactSource
    Assert-HostIdentity
    if (Test-Path -LiteralPath $registryApp) { throw 'Refusing qualification because QS3D is already registered in the selected V26 profile.' }
    if (Test-Path -LiteralPath $installDir) { throw 'Disposable install directory unexpectedly already exists.' }

    $v25Before = Get-RegistryStateDigest '^V25(?:\.|$)'
    New-Item -ItemType Directory -Path $sentinelPath -Force | Out-Null
    New-ItemProperty -LiteralPath $sentinelPath -Name Value -Value $sentinelValue -PropertyType String -Force | Out-Null

    Invoke-V26Build
    $manifest = Assert-Package
    $installer = Join-Path $packageDir 'install-v26-autoload.ps1'
    & $installer -PackageDirectory $packageDir -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Confirm:$false
    Assert-RegistrationIdentity
    $v25AfterInstall = Get-RegistryStateDigest '^V25(?:\.|$)'
    $result.registrationV26Only = [string]::Equals($v25Before, $v25AfterInstall, [StringComparison]::Ordinal)
    if (-not $result.registrationV26Only) { throw 'V26 install changed unrelated V25 QS3D registration state.' }
    Assert-InstalledPayload $manifest

    $sentinel = Get-ItemPropertyValue -LiteralPath $sentinelPath -Name Value
    $result.unrelatedSentinelPreserved = [string]$sentinel -eq $sentinelValue
    if (-not $result.unrelatedSentinelPreserved) { throw 'V26 package install changed unrelated sentinel state.' }

    $uninstaller = Join-Path $packageDir 'uninstall-v26-autoload.ps1'
    & $uninstaller -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -Confirm:$false
    $result.uninstallRemovedRegistration = -not (Test-Path -LiteralPath $registryApp)
    $result.uninstallRemovedFiles = -not (Test-Path -LiteralPath $installDir)
    if (-not $result.uninstallRemovedRegistration -or -not $result.uninstallRemovedFiles) { throw 'V26 uninstall left owned registration or files behind.' }

    $v25AfterUninstall = Get-RegistryStateDigest '^V25(?:\.|$)'
    $result.unrelatedV25RegistrationPreserved = [string]::Equals($v25Before, $v25AfterUninstall, [StringComparison]::Ordinal)
    if (-not $result.unrelatedV25RegistrationPreserved) { throw 'V26 uninstall changed unrelated V25 QS3D registration state.' }
    $sentinel = Get-ItemPropertyValue -LiteralPath $sentinelPath -Name Value
    $result.unrelatedSentinelPreserved = $result.unrelatedSentinelPreserved -and ([string]$sentinel -eq $sentinelValue)
    if (-not $result.unrelatedSentinelPreserved) { throw 'V26 package lifecycle changed unrelated sentinel state.' }
    $result.status = 'PASS'
}
finally {
    $env:BRICSCAD_V26_DIR = $originalV26Dir
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