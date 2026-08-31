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
    schema = 3
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

function Assert-NoReparseAncestors {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $cursor = Get-Item -LiteralPath $Path -Force -ErrorAction Stop
    while ($null -ne $cursor) {
        if (($cursor.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label is reparse-backed: $($cursor.FullName)"
        }
        $parent = $cursor.Parent
        if ($null -eq $parent) { break }
        $cursor = $parent
    }
}

function Resolve-OrdinaryNonReparseDirectory {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $full = [IO.Path]::GetFullPath($Path)
    $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or -not ($item -is [IO.DirectoryInfo])) { throw "$Label is not an ordinary directory." }
    Assert-NoReparseAncestors -Path $item.FullName -Label $Label
    return $item
}

function Resolve-OrdinaryNonReparseFile {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $full = [IO.Path]::GetFullPath($Path)
    $item = Get-Item -LiteralPath $full -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo])) { throw "$Label is not an ordinary file." }
    Assert-NoReparseAncestors -Path $item.FullName -Label $Label
    return $item
}

function Get-StreamingSha256 {
    param([Parameter(Mandatory = $true)][IO.FileInfo]$File, [Parameter(Mandatory = $true)][string]$Label)
    $stream = [IO.File]::Open($File.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash($stream))).Replace('-', '').ToUpperInvariant()
    }
    catch { throw "$Label SHA-256 could not be read safely: $($_.Exception.Message)" }
    finally { $sha.Dispose(); $stream.Dispose() }
}

function Get-StableFileState {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $first = Resolve-OrdinaryNonReparseFile -Path $Path -Label $Label
    $firstLength = [long]$first.Length
    $firstTicks = [long]$first.LastWriteTimeUtc.Ticks
    $firstHash = Get-StreamingSha256 -File $first -Label $Label
    $second = Resolve-OrdinaryNonReparseFile -Path $first.FullName -Label $Label
    $secondHash = Get-StreamingSha256 -File $second -Label $Label
    if ($firstLength -ne [long]$second.Length -or $firstTicks -ne [long]$second.LastWriteTimeUtc.Ticks -or -not [string]::Equals($firstHash, $secondHash, [StringComparison]::Ordinal)) {
        throw "$Label changed while its stable input state was being captured."
    }
    return [pscustomobject]@{ Path = $second.FullName; Length = [long]$second.Length; LastWriteUtcTicks = [long]$second.LastWriteTimeUtc.Ticks; Sha256 = $secondHash }
}

function Assert-StableFileState {
    param([Parameter(Mandatory = $true)]$Expected, [Parameter(Mandatory = $true)][string]$Label)
    $current = Resolve-OrdinaryNonReparseFile -Path ([string]$Expected.Path) -Label $Label
    $currentHash = Get-StreamingSha256 -File $current -Label $Label
    if ([long]$Expected.Length -ne [long]$current.Length -or [long]$Expected.LastWriteUtcTicks -ne [long]$current.LastWriteTimeUtc.Ticks -or -not [string]::Equals([string]$Expected.Sha256, $currentHash, [StringComparison]::Ordinal)) {
        throw "$Label changed after its admitted input generation was captured."
    }
    return $current
}

function Read-BoundedStrictUtf8State {
    param([Parameter(Mandatory = $true)]$State, [Parameter(Mandatory = $true)][string]$Label, [int]$MaxBytes = 1048576)
    if ([long]$State.Length -gt $MaxBytes) { throw "$Label exceeds the bounded UTF-8 read limit." }
    $file = Assert-StableFileState -Expected $State -Label $Label
    $utf8 = [Text.UTF8Encoding]::new($false, $true)
    $stream = [IO.File]::Open($file.FullName, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    $reader = [IO.StreamReader]::new($stream, $utf8, $true)
    try { $text = $reader.ReadToEnd() }
    catch { throw "$Label is not strict UTF-8: $($_.Exception.Message)" }
    finally { $reader.Dispose(); $stream.Dispose() }
    $null = Assert-StableFileState -Expected $State -Label $Label
    return $text
}

function Get-SafeFiles {
    param([Parameter(Mandatory = $true)][string]$Root, [Parameter(Mandatory = $true)][string]$Label)
    $rootDirectory = Resolve-OrdinaryNonReparseDirectory -Path $Root -Label $Label
    $pending = [Collections.Generic.Stack[string]]::new()
    $files = [Collections.Generic.List[IO.FileInfo]]::new()
    $pending.Push($rootDirectory.FullName)
    while ($pending.Count -gt 0) {
        $directory = Resolve-OrdinaryNonReparseDirectory -Path $pending.Pop() -Label "$Label directory"
        foreach ($item in @(Get-ChildItem -LiteralPath $directory.FullName -Force -ErrorAction Stop)) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label contains a reparse-backed entry: $($item.FullName)" }
            if ($item.PSIsContainer) { $pending.Push($item.FullName); continue }
            if (-not ($item -is [IO.FileInfo])) { throw "$Label contains a non-regular filesystem entry: $($item.FullName)" }
            $files.Add((Resolve-OrdinaryNonReparseFile -Path $item.FullName -Label "$Label file"))
        }
    }
    return @($files | Sort-Object FullName)
}

function Assert-DirectoryInside([string]$Candidate, [string]$Parent, [string]$Label) {
    $candidateFull = [IO.Path]::GetFullPath($Candidate)
    $parentFull = [IO.Path]::GetFullPath($Parent).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    if (-not $candidateFull.StartsWith($parentFull, [StringComparison]::OrdinalIgnoreCase)) { throw "$Label must stay inside the disposable QS3D qualification root." }
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
    $host = Resolve-OrdinaryNonReparseDirectory -Path $BricsCadDir -Label 'BricsCAD V26 directory'
    $exe = Resolve-OrdinaryNonReparseFile -Path (Join-Path $host.FullName 'bricscad.exe') -Label 'BricsCAD executable'
    foreach ($assembly in @('BrxMgd.dll','TD_Mgd.dll','TD_MgdBrep.dll')) { $null = Resolve-OrdinaryNonReparseFile -Path (Join-Path $host.FullName $assembly) -Label "BricsCAD V26 $assembly" }
    $version = [Diagnostics.FileVersionInfo]::GetVersionInfo($exe.FullName).FileVersion
    if ([string]::IsNullOrWhiteSpace($version)) { throw 'BricsCAD file version is unreadable.' }
    $major = 0
    if (-not [int]::TryParse($version.Split('.')[0], [ref]$major) -or $major -ne 26) { throw 'Configured BricsCAD host is not major version 26.' }
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
            try { $lines.Add($version.PSChildName + '/' + $language.PSChildName); foreach ($name in @($key.GetValueNames() | Sort-Object)) { $lines.Add($name + '=' + [string]$key.GetValue($name, '')) } }
            finally { $key.Close() }
            $commands = Join-Path $app 'Commands'
            if (Test-Path -LiteralPath $commands) {
                $commandKey = Get-Item -LiteralPath $commands
                try { foreach ($name in @($commandKey.GetValueNames() | Sort-Object)) { $lines.Add('command:' + $name + '=' + [string]$commandKey.GetValue($name, '')) } }
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
    $package = Resolve-OrdinaryNonReparseDirectory -Path $packageDir -Label 'V26 package root'
    $manifestState = Get-StableFileState -Path (Join-Path $package.FullName 'SHA256SUMS.txt') -Label 'V26 hash manifest'
    $manifestText = Read-BoundedStrictUtf8State -State $manifestState -Label 'V26 hash manifest'
    $entries = [Collections.Generic.Dictionary[string,string]]::new([StringComparer]::OrdinalIgnoreCase)
    $states = [Collections.Generic.Dictionary[string,object]]::new([StringComparer]::OrdinalIgnoreCase)
    $packageRoot = $package.FullName.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar) + [IO.Path]::DirectorySeparatorChar
    foreach ($line in @($manifestText -split "\r?\n")) {
        if ([string]::IsNullOrWhiteSpace($line)) { continue }
        if ($line -notmatch '^([0-9A-F]{64})  ([^\\:]+)$') { throw 'V26 hash manifest contains a malformed or non-canonical entry.' }
        $relative = $Matches[2]
        $segments = @($relative.Split('/'))
        if ($relative -eq 'SHA256SUMS.txt' -or [IO.Path]::IsPathRooted($relative) -or @($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) { throw 'V26 hash manifest contains an unsafe relative path.' }
        if (-not $entries.TryAdd($relative, $Matches[1])) { throw 'V26 hash manifest contains a duplicate payload entry.' }
        $payloadPath = [IO.Path]::GetFullPath((Join-Path $package.FullName $relative.Replace('/', [IO.Path]::DirectorySeparatorChar)))
        if (-not $payloadPath.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 hash manifest payload escaped package root.' }
        $state = Get-StableFileState -Path $payloadPath -Label ("Hashed V26 payload " + $relative)
        if (-not [string]::Equals([string]$state.Sha256, $Matches[1], [StringComparison]::Ordinal)) { throw 'V26 package hash verification failed.' }
        $states.Add($relative, $state)
    }
    if ($entries.Count -eq 0) { throw 'V26 hash manifest is empty.' }
    $actual = [Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($file in @(Get-SafeFiles -Root $package.FullName -Label 'V26 package root')) {
        if ([string]::Equals($file.Name, 'SHA256SUMS.txt', [StringComparison]::OrdinalIgnoreCase) -and [string]::Equals($file.DirectoryName, $package.FullName, [StringComparison]::OrdinalIgnoreCase)) { continue }
        $full = $file.FullName
        if (-not $full.StartsWith($packageRoot, [StringComparison]::OrdinalIgnoreCase)) { throw 'V26 package payload escaped package root.' }
        $relative = $full.Substring($packageRoot.Length).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')
        if (-not $actual.Add($relative) -or -not $entries.ContainsKey($relative)) { throw 'V26 package contains an unhashed or case-colliding payload.' }
    }
    if ($actual.Count -ne $entries.Count) { throw 'V26 hash manifest does not cover the package exactly.' }
    foreach ($relative in $entries.Keys) { if (-not $actual.Contains($relative)) { throw 'V26 hash manifest references a non-package payload.' } }
    $null = Assert-StableFileState -Expected $manifestState -Label 'V26 hash manifest'
    return [pscustomobject]@{ Entries = $entries; States = $states; ManifestState = $manifestState }
}

function Assert-Package {
    & (Join-Path $root 'scripts\package-v26.ps1')
    if ($LASTEXITCODE -ne 0) { throw 'V26 package creation failed.' }
    $package = Resolve-OrdinaryNonReparseDirectory -Path $packageDir -Label 'V26 package root'
    $zipState = Get-StableFileState -Path $zipPath -Label 'V26 package ZIP'
    $metadataState = Get-StableFileState -Path (Join-Path $package.FullName 'PACKAGE-METADATA.json') -Label 'V26 package metadata'
    $metadataText = Read-BoundedStrictUtf8State -State $metadataState -Label 'V26 package metadata'
    try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Generated V26 package metadata is invalid JSON: $($_.Exception.Message)" }
    if ([string]$metadata.product -ne 'QS3D' -or [string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.framework -ne 'net8.0-windows') { throw 'Generated package metadata does not identify QS3D BricsCAD V26 x64/net8.0-windows.' }
    $result.productVersion = [string]$metadata.productVersion
    if ([string]::IsNullOrWhiteSpace($result.productVersion)) { throw 'Generated package productVersion is missing.' }
    $result.packageIdentityValid = $true
    $manifest = Get-PackageManifest
    $result.hashesValid = $true
    $result.runtimeConfigPackaged = $manifest.Entries.ContainsKey('QS3D.BricsCAD.V26.runtimeconfig.json')
    if (-not $result.runtimeConfigPackaged) { throw 'V26 runtimeconfig is not covered by package hashes.' }
    $result.packageSha256 = [string]$zipState.Sha256
    $null = Assert-StableFileState -Expected $metadataState -Label 'V26 package metadata'
    $null = Assert-StableFileState -Expected $zipState -Label 'V26 package ZIP'
    return [pscustomobject]@{ Manifest = $manifest; ZipState = $zipState; MetadataState = $metadataState }
}

function Assert-PackageStates($PackageEvidence) {
    $null = Resolve-OrdinaryNonReparseDirectory -Path $packageDir -Label 'V26 package root'
    $null = Assert-StableFileState -Expected $PackageEvidence.MetadataState -Label 'V26 package metadata'
    $null = Assert-StableFileState -Expected $PackageEvidence.ZipState -Label 'V26 package ZIP'
    $null = Assert-StableFileState -Expected $PackageEvidence.Manifest.ManifestState -Label 'V26 hash manifest'
    foreach ($relative in $PackageEvidence.Manifest.States.Keys) { $null = Assert-StableFileState -Expected $PackageEvidence.Manifest.States[$relative] -Label ("V26 package payload " + $relative) }
}

function Assert-RegistrationIdentity {
    if (-not (Test-Path -LiteralPath $registryApp)) { throw 'V26 DemandLoad registration was not created.' }
    $key = Get-Item -LiteralPath $registryApp
    try { $loader = [string]$key.GetValue('Loader', ''); $loadCtrls = [int]$key.GetValue('LoadCtrls', -1); $description = [string]$key.GetValue('Description', '') }
    finally { $key.Close() }
    $expectedLoader = [IO.Path]::GetFullPath((Join-Path $installDir 'QS3D.BricsCAD.V26.dll'))
    if (-not [string]::Equals([IO.Path]::GetFullPath($loader), $expectedLoader, [StringComparison]::OrdinalIgnoreCase) -or $loadCtrls -ne 4 -or $description -ne 'QS3D for BricsCAD V26') { throw 'V26 DemandLoad registration identity is incorrect.' }
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
    if ($null -eq $frameworksProperty -or $null -eq $frameworksProperty.Value -or -not ($frameworksProperty.Value -is [System.Array])) { throw 'Installed V26 runtimeconfig runtimeOptions.frameworks must be an array.' }
    $frameworks = @($frameworksProperty.Value)
    if ($frameworks.Count -eq 0) { throw 'Installed V26 runtimeconfig runtimeOptions.frameworks is empty.' }
    $names = [Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
    foreach ($framework in $frameworks) {
        if ($null -eq $framework) { throw 'Installed V26 runtimeconfig contains a null framework entry.' }
        $nameProperty = $framework.PSObject.Properties['name']
        if ($null -eq $nameProperty) { throw 'Installed V26 runtimeconfig contains a framework without a name.' }
        $name = [string]$nameProperty.Value
        if ([string]::IsNullOrWhiteSpace($name) -or -not $names.Add($name)) { throw 'Installed V26 runtimeconfig contains an empty or duplicate framework name.' }
    }
    return @($names)
}

function Assert-InstalledPayload($Manifest) {
    $install = Resolve-OrdinaryNonReparseDirectory -Path $installDir -Label 'Installed V26 package root'
    $expected = @('QS3D.BricsCAD.V26.dll','QS3D.BricsCAD.V26.runtimeconfig.json','QS3D.Core.dll','COMMANDS.txt','PACKAGE-METADATA.json','README.txt','SHA256SUMS.txt','uninstall-v26-autoload.ps1','update-v26.ps1')
    $files = @(Get-SafeFiles -Root $install.FullName -Label 'Installed V26 package root')
    $actual = @($files | ForEach-Object { $_.Name } | Sort-Object)
    $expectedSorted = @($expected | Sort-Object)
    if (($actual -join "`n") -cne ($expectedSorted -join "`n")) { throw 'Installed V26 payload file set differs from the canonical installer contract.' }
    $installedStates = @{}
    foreach ($name in $expected) {
        $state = Get-StableFileState -Path (Join-Path $install.FullName $name) -Label ("Installed " + $name)
        $installedStates[$name] = $state
        if ($name -eq 'SHA256SUMS.txt') { continue }
        if (-not $Manifest.Entries.ContainsKey($name)) { throw "Installed V26 payload is not covered by package manifest: $name" }
        if (-not [string]::Equals([string]$state.Sha256, [string]$Manifest.Entries[$name], [StringComparison]::Ordinal)) { throw "Installed V26 payload hash differs from package: $name" }
    }
    $metadataText = Read-BoundedStrictUtf8State -State $installedStates['PACKAGE-METADATA.json'] -Label 'Installed V26 package metadata'
    try { $metadata = $metadataText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Installed V26 package metadata is invalid JSON: $($_.Exception.Message)" }
    if ([string]$metadata.target -ne 'BricsCAD V26 x64' -or [string]$metadata.framework -ne 'net8.0-windows' -or [string]$metadata.productVersion -ne $result.productVersion) { throw 'Installed V26 payload identity differs from the generated package.' }
    $runtimeText = Read-BoundedStrictUtf8State -State $installedStates['QS3D.BricsCAD.V26.runtimeconfig.json'] -Label 'Installed V26 runtimeconfig'
    try { $runtime = $runtimeText | ConvertFrom-Json -ErrorAction Stop }
    catch { throw "Installed V26 runtimeconfig is invalid JSON: $($_.Exception.Message)" }
    $frameworkNames = @(Get-RuntimeFrameworkNames $runtime)
    if (-not ($frameworkNames -contains 'Microsoft.WindowsDesktop.App')) { throw 'Installed V26 runtimeconfig does not target Microsoft.WindowsDesktop.App.' }
    foreach ($name in $installedStates.Keys) { $null = Assert-StableFileState -Expected $installedStates[$name] -Label ("Installed " + $name) }
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
    $packageEvidence = Assert-Package
    Assert-PackageStates $packageEvidence
    $installerState = $packageEvidence.Manifest.States['install-v26-autoload.ps1']
    $installer = (Assert-StableFileState -Expected $installerState -Label 'V26 installer').FullName
    & $installer -PackageDirectory $packageDir -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -LoadMode OnCommand -Confirm:$false
    Assert-PackageStates $packageEvidence
    Assert-RegistrationIdentity
    $v25AfterInstall = Get-RegistryStateDigest '^V25(?:\.|$)'
    $result.registrationV26Only = [string]::Equals($v25Before, $v25AfterInstall, [StringComparison]::Ordinal)
    if (-not $result.registrationV26Only) { throw 'V26 install changed unrelated V25 QS3D registration state.' }
    Assert-InstalledPayload $packageEvidence.Manifest

    $sentinel = Get-ItemPropertyValue -LiteralPath $sentinelPath -Name Value
    $result.unrelatedSentinelPreserved = [string]$sentinel -eq $sentinelValue
    if (-not $result.unrelatedSentinelPreserved) { throw 'V26 package install changed unrelated sentinel state.' }

    $uninstallerState = $packageEvidence.Manifest.States['uninstall-v26-autoload.ps1']
    $uninstaller = (Assert-StableFileState -Expected $uninstallerState -Label 'V26 uninstaller').FullName
    & $uninstaller -InstallDirectory $installDir -VersionKeys @($VersionKey) -LanguageKeys @($LanguageKey) -Confirm:$false
    Assert-PackageStates $packageEvidence
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
    if (Test-Path -LiteralPath $installDir) {
        try { $null = Resolve-OrdinaryNonReparseDirectory -Path $installDir -Label 'Disposable V26 install cleanup root'; Remove-Item -LiteralPath $installDir -Recurse -Force -ErrorAction Stop }
        catch { Write-Warning ("Refused unsafe disposable install cleanup: " + $_.Exception.Message) }
    }
    Remove-Item -LiteralPath $sentinelPath -Recurse -Force -ErrorAction SilentlyContinue
    $result.cleanupComplete = (-not (Test-Path -LiteralPath $registryApp)) -and (-not (Test-Path -LiteralPath $installDir)) -and (-not (Test-Path -LiteralPath $sentinelPath))
    New-Item -ItemType Directory -Path $artifactFull -Force | Out-Null
    $evidence = Join-Path $artifactFull 'v26-package-install-lifecycle.json'
    $result | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $evidence -Encoding UTF8
    Write-Host ("QS3D_V26_PACKAGE_INSTALL_LIFECYCLE status={0} source={1} cleanup={2}" -f $result.status, $result.sourceSha, $result.cleanupComplete)
}

if ($result.status -ne 'PASS' -or -not $result.cleanupComplete) { exit 1 }
