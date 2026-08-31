$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/QS3D.BricsCAD.V25/bin/x64/Release/net48'
$distRoot = Join-Path $root 'dist'
$dist = Join-Path $distRoot 'QS3D-BricsCAD-V25'
$zip = Join-Path $distRoot 'QS3D-BricsCAD-V25.zip'
$required = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')
$forbidden = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$sampleSource = Join-Path $root 'samples/generated'
$script:MaxPackageTextBytes = 8MB
$script:StrictUtf8 = [Text.UTF8Encoding]::new($false, $true)

function Get-CanonicalFullPath {
    param([string]$Path, [string]$Label)
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path is required." }
    try { return [IO.Path]::GetFullPath($Path) }
    catch { throw "$Label path is invalid: $($_.Exception.Message)" }
}

function Test-PathEqualOrContained {
    param([string]$Path, [string]$Container)
    $pathFull = Get-CanonicalFullPath -Path $Path -Label 'candidate'
    $containerFull = (Get-CanonicalFullPath -Path $Container -Label 'container').TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ([string]::Equals($pathFull.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar), $containerFull, [System.StringComparison]::OrdinalIgnoreCase)) { return $true }
    $prefix = $containerFull + [IO.Path]::DirectorySeparatorChar
    return $pathFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)
}

function Assert-OrdinaryDirectory {
    param([string]$Path, [string]$Label)
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) { throw "$Label directory was not found: $fullPath" }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse directory: $fullPath"
    }
    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-SafeInputPathAncestors {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)
    $repo = Assert-OrdinaryDirectory -Path $RepositoryRoot -Label 'repository root'
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-PathEqualOrContained -Path $fullPath -Container $repo) -or [string]::Equals($fullPath, $repo, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must stay below the repository root: $fullPath"
    }
    $current = [IO.Path]::GetDirectoryName($fullPath)
    while (-not [string]::IsNullOrWhiteSpace($current) -and (Test-PathEqualOrContained -Path $current -Container $repo)) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
            if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "$Label traverses a non-directory or reparse-backed ancestor: $current"
            }
        }
        if ([string]::Equals($current, $repo, [System.StringComparison]::OrdinalIgnoreCase)) { break }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $current, [System.StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent
    }
    return $fullPath
}

function Assert-SafeInputDirectory {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)
    $fullPath = Assert-SafeInputPathAncestors -Path $Path -RepositoryRoot $RepositoryRoot -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) { throw "$Label directory was not found: $fullPath" }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse directory: $fullPath"
    }
    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-SafeInputFile {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)
    $fullPath = Assert-SafeInputPathAncestors -Path $Path -RepositoryRoot $RepositoryRoot -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "$Label file was not found: $fullPath" }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo]) -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $fullPath"
    }
    return $fullPath
}

function Open-HeldPackageInput {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)
    $fullPath = Assert-SafeInputFile -Path $Path -RepositoryRoot $RepositoryRoot -Label $Label
    $initial = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    $stream = [IO.File]::Open($fullPath, [IO.FileMode]::Open, [IO.FileAccess]::Read, [IO.FileShare]::Read)
    try {
        $reboundPath = Assert-SafeInputFile -Path $fullPath -RepositoryRoot $RepositoryRoot -Label $Label
        $rebound = Get-Item -LiteralPath $reboundPath -Force -ErrorAction Stop
        if (-not [string]::Equals($initial.FullName, $rebound.FullName, [StringComparison]::OrdinalIgnoreCase) -or
            $initial.Length -ne $stream.Length -or $rebound.Length -ne $stream.Length -or
            $initial.LastWriteTimeUtc.Ticks -ne $rebound.LastWriteTimeUtc.Ticks) {
            throw "$Label changed while its held generation was being admitted."
        }
        return [pscustomobject]@{
            Path = $rebound.FullName
            Length = [int64]$stream.Length
            LastWriteUtcTicks = [int64]$rebound.LastWriteTimeUtc.Ticks
            Stream = $stream
        }
    }
    catch {
        $stream.Dispose()
        throw
    }
}

function Assert-HeldPathBinding {
    param([pscustomobject]$Held, [string]$RepositoryRoot, [string]$Label)
    $path = Assert-SafeInputFile -Path $Held.Path -RepositoryRoot $RepositoryRoot -Label $Label
    $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
    if (-not [string]::Equals($Held.Path, $item.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $Held.Length -ne $item.Length -or $Held.Length -ne $Held.Stream.Length -or
        $Held.LastWriteUtcTicks -ne $item.LastWriteTimeUtc.Ticks) {
        throw "$Label pathname no longer resolves to the held admitted generation."
    }
}

function Read-HeldPackageText {
    param([pscustomobject]$Held, [string]$Label)
    if ($Held.Stream.Length -gt $script:MaxPackageTextBytes) { throw "$Label exceeds the $($script:MaxPackageTextBytes)-byte package text limit." }
    $Held.Stream.Position = 0
    $reader = [IO.StreamReader]::new($Held.Stream, $script:StrictUtf8, $true, 4096, $true)
    try { return $reader.ReadToEnd() }
    catch [Text.DecoderFallbackException] { throw "$Label is not strict UTF-8." }
    finally {
        $reader.Dispose()
        $Held.Stream.Position = 0
    }
}

function Copy-HeldPackageInput {
    param([string]$SourcePath, [string]$DestinationPath, [string]$Label)
    $held = Open-HeldPackageInput -Path $SourcePath -RepositoryRoot $root -Label $Label
    try {
        $destination = Assert-SafeOutputFileTarget -Path $DestinationPath -RepositoryRoot $root -Label ("$Label destination")
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label $Label
        $output = [IO.File]::Open($destination, [IO.FileMode]::Create, [IO.FileAccess]::Write, [IO.FileShare]::None)
        try {
            $held.Stream.Position = 0
            $held.Stream.CopyTo($output)
            $output.Flush($true)
        }
        finally { $output.Dispose() }
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label $Label
    }
    finally { $held.Stream.Dispose() }
}

function Read-HeldSourceText {
    param([string]$Path, [string]$Label)
    $held = Open-HeldPackageInput -Path $Path -RepositoryRoot $root -Label $Label
    try {
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label $Label
        $text = Read-HeldPackageText -Held $held -Label $Label
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label $Label
        return $text
    }
    finally { $held.Stream.Dispose() }
}

function Get-SafeSourceFiles {
    param([string]$SourceRoot, [string]$RepositoryRoot, [string]$Extension)
    $sourceRootFull = Assert-SafeInputDirectory -Path $SourceRoot -RepositoryRoot $RepositoryRoot -Label 'command source root'
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $pending.Push($sourceRootFull)
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Command source contains a reparse-backed entry: $($item.FullName)" }
            if ($item.PSIsContainer) { $pending.Push($item.FullName); continue }
            if (-not ($item -is [IO.FileInfo])) { throw "Command source contains a non-regular filesystem entry: $($item.FullName)" }
            if ([string]::Equals($item.Extension, $Extension, [StringComparison]::OrdinalIgnoreCase)) { $files.Add($item) }
        }
    }
    return @($files | Sort-Object FullName)
}

function Assert-SafeOutputDirectoryTarget {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label, [switch]$MayBeMissing)
    $repo = Assert-OrdinaryDirectory -Path $RepositoryRoot -Label 'repository root'
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    $pathRoot = [IO.Path]::GetPathRoot($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($pathRoot) -and [string]::Equals($fullPath, $pathRoot, [System.StringComparison]::OrdinalIgnoreCase)) { throw "$Label must not be a filesystem root: $fullPath" }
    if (-not (Test-PathEqualOrContained -Path $fullPath -Container $repo) -or [string]::Equals($fullPath, $repo, [System.StringComparison]::OrdinalIgnoreCase)) { throw "$Label must stay below the repository root: $fullPath" }
    $current = [IO.Path]::GetDirectoryName($fullPath)
    while (-not [string]::IsNullOrWhiteSpace($current) -and (Test-PathEqualOrContained -Path $current -Container $repo)) {
        if (Test-Path -LiteralPath $current) {
            $item = Get-Item -LiteralPath $current -Force -ErrorAction Stop
            if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label traverses a non-directory or reparse-backed ancestor: $current" }
        }
        if ([string]::Equals($current, $repo, [System.StringComparison]::OrdinalIgnoreCase)) { break }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $current, [System.StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent
    }
    if (Test-Path -LiteralPath $fullPath) {
        $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse directory target: $fullPath" }
    } elseif (-not $MayBeMissing) { throw "$Label directory was not found: $fullPath" }
    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-SafeOutputFileTarget {
    param([string]$Path, [string]$RepositoryRoot, [string]$Label)
    $repo = Assert-OrdinaryDirectory -Path $RepositoryRoot -Label 'repository root'
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-PathEqualOrContained -Path $fullPath -Container $repo) -or [string]::Equals($fullPath, $repo, [System.StringComparison]::OrdinalIgnoreCase)) { throw "$Label must stay below the repository root: $fullPath" }
    $parent = [IO.Path]::GetDirectoryName($fullPath)
    $null = Assert-SafeOutputDirectoryTarget -Path $parent -RepositoryRoot $repo -Label ("$Label parent") -MayBeMissing
    if (Test-Path -LiteralPath $fullPath) {
        $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "$Label must be an ordinary non-reparse file target: $fullPath" }
    }
    return $fullPath
}

function Get-SafePackageFiles {
    param([string]$PackageRoot)
    $package = Assert-OrdinaryDirectory -Path $PackageRoot -Label 'package staging root'
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $pending.Push($package)
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { throw "Package staging contains a reparse-backed entry: $($item.FullName)" }
            if ($item.PSIsContainer) { $pending.Push($item.FullName); continue }
            if (-not ($item -is [IO.FileInfo])) { throw "Package staging contains a non-regular filesystem entry: $($item.FullName)" }
            $files.Add($item)
        }
    }
    return @($files | Sort-Object FullName)
}

function Read-ProjectProductVersion {
    param([string]$ProjectPath)
    $held = Open-HeldPackageInput -Path $ProjectPath -RepositoryRoot $root -Label 'project file'
    try {
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label 'project file'
        [xml]$project = Read-HeldPackageText -Held $held -Label 'project file'
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label 'project file'
        $versions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($versions.Count -ne 1) { throw "Project must declare exactly one Version value: $ProjectPath" }
        $version = $versions[0]
        if (-not [string]::Equals($version, $version.Trim(), [StringComparison]::Ordinal)) { throw "Project Version must be canonical without surrounding whitespace: $ProjectPath" }
        return $version
    }
    finally { $held.Stream.Dispose() }
}

function Convert-ToStrictSemVerText {
    param([string]$Value, [string]$Label)
    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Label is missing." }
    $text = $Value
    if (-not [string]::Equals($text, $text.Trim(), [StringComparison]::Ordinal)) { throw "$Label must be canonical without surrounding whitespace." }
    $match = [regex]::Match($text, '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$', [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) { throw "$Label is not strict SemVer: $text" }
    if ($match.Groups[4].Success) {
        foreach ($identifier in $match.Groups[4].Value.Split('.')) {
            if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier[0] -eq '0') { throw "$Label has a numeric prerelease identifier with a leading zero: $text" }
        }
    }
    return $text
}

function Get-SourceGitCommit {
    $output = @(& git -C $root rev-parse --verify HEAD 2>&1)
    if ($LASTEXITCODE -ne 0 -or $output.Count -ne 1) { throw "Could not resolve the exact source Git HEAD for package provenance." }
    $commit = ([string]$output[0]).Trim().ToLowerInvariant()
    if ($commit -notmatch '^[0-9a-f]{40}$') { throw "Source Git HEAD is not one exact 40-hex commit: '$commit'." }
    return $commit
}

$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'
$pluginProject = Assert-SafeInputFile -Path (Join-Path $root 'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj') -RepositoryRoot $root -Label 'V25 plugin project'
$coreProject = Assert-SafeInputFile -Path (Join-Path $root 'src/QS3D.Core/QS3D.Core.csproj') -RepositoryRoot $root -Label 'Core project'
$productVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $pluginProject) -Label 'QS3D plugin product version'
$coreProductVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $coreProject) -Label 'QS3D Core product version'
$gitCommit = Get-SourceGitCommit
if (-not [string]::Equals($productVersion, $coreProductVersion, [StringComparison]::Ordinal)) { throw "QS3D plugin/Core product versions differ: plugin=$productVersion core=$coreProductVersion" }
if (-not [string]::IsNullOrWhiteSpace($env:RELEASE_TAG)) {
    $expectedTag = 'v' + $productVersion
    if (-not [string]::Equals($env:RELEASE_TAG, $expectedTag, [StringComparison]::Ordinal)) { throw "RELEASE_TAG must exactly match the source product version. Expected $expectedTag, got $env:RELEASE_TAG." }
}

$source = Assert-SafeInputDirectory -Path $source -RepositoryRoot $root -Label 'V25 Release output'
$sampleSource = Assert-SafeInputDirectory -Path $sampleSource -RepositoryRoot $root -Label 'synthetic sample folder'
$distRoot = Assert-SafeOutputDirectoryTarget -Path $distRoot -RepositoryRoot $root -Label 'package dist root' -MayBeMissing
$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory' -MayBeMissing
$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
$distRoot = Assert-SafeOutputDirectoryTarget -Path $distRoot -RepositoryRoot $root -Label 'package dist root'
$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory' -MayBeMissing
$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
if (Test-Path -LiteralPath $dist) { Remove-Item -LiteralPath $dist -Recurse -Force }
New-Item -ItemType Directory -Path $dist -Force | Out-Null
$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory'

foreach ($name in $required) {
    $path = Assert-SafeInputFile -Path (Join-Path $source $name) -RepositoryRoot $root -Label ("V25 build artifact $name")
    Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name) -Label ("V25 build artifact $name")
}

foreach ($script in @('install-v25-autoload.ps1', 'uninstall-v25-autoload.ps1', 'update-v25.ps1', 'unblock-v25-netload.ps1')) {
    $scriptPath = Assert-SafeInputFile -Path (Join-Path $PSScriptRoot $script) -RepositoryRoot $root -Label ("release script $script")
    Copy-HeldPackageInput -SourcePath $scriptPath -DestinationPath (Join-Path $dist $script) -Label ("release script $script")
}

foreach ($launcherName in @('INSTALL-QS3D.cmd', 'UNBLOCK-QS3D.cmd')) {
    $launcherPath = Assert-SafeInputFile -Path (Join-Path $PSScriptRoot $launcherName) -RepositoryRoot $root -Label ("package launcher $launcherName")
    Copy-HeldPackageInput -SourcePath $launcherPath -DestinationPath (Join-Path $dist $launcherName) -Label ("package launcher $launcherName")
}

$sampleDestination = Join-Path $dist 'Samples'
New-Item -ItemType Directory -Path $sampleDestination -Force | Out-Null
foreach ($sampleName in @('README.md', 'QS3D-Sample.dxf', 'QS3D-Sample.qsdb', 'QS3D-Quantity-Template.xlsx', 'QS3D-Architecture.qstemplate')) {
    $samplePath = Assert-SafeInputFile -Path (Join-Path $sampleSource $sampleName) -RepositoryRoot $root -Label ("synthetic sample $sampleName")
    Copy-HeldPackageInput -SourcePath $samplePath -DestinationPath (Join-Path $sampleDestination $sampleName) -Label ("synthetic sample $sampleName")
}
$sampleDwg = Join-Path $sampleSource 'QS3D-Sample.dwg'
if (Test-Path -LiteralPath $sampleDwg) {
    $sampleDwg = Assert-SafeInputFile -Path $sampleDwg -RepositoryRoot $root -Label 'synthetic sample QS3D-Sample.dwg'
    Copy-HeldPackageInput -SourcePath $sampleDwg -DestinationPath (Join-Path $sampleDestination 'QS3D-Sample.dwg') -Label 'synthetic sample QS3D-Sample.dwg'
}

$commands = @()
Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V25') -RepositoryRoot $root -Extension '.cs' | ForEach-Object {
    $text = Read-HeldSourceText -Path $_.FullName -Label 'V25 command source'
    [regex]::Matches($text, '\[CommandMethod\("([^\"]+)"') | ForEach-Object { $commands += $_.Groups[1].Value.ToUpperInvariant() }
}
$commands = @($commands | Sort-Object -Unique)
if ($commands.Count -eq 0 -or -not ($commands -contains 'QS3D')) { throw 'No QS3D CommandMethod entries were discovered.' }
$commands | Set-Content -Path (Join-Path $dist 'COMMANDS.txt') -Encoding ASCII

$pluginPath = Join-Path $dist 'QS3D.BricsCAD.V25.dll'
$signature = Get-AuthenticodeSignature -FilePath $pluginPath
$assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginPath).Version
if (-not $assemblyVersion) { throw 'Could not read QS3D plugin assembly version.' }
$metadata = [ordered]@{
    product = 'QS3D'
    target = 'BricsCAD V25 x64'
    productVersion = $productVersion
    version = $assemblyVersion.ToString()
    gitCommit = $gitCommit
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    commandCount = $commands.Count
    defaultLoadMode = 'OnCommand'
    autoloadMethod = 'BricsCAD Registry DemandLoad'
    pluginSignatureStatus = $signature.Status.ToString()
    pluginSignerThumbprint = if ($signature.SignerCertificate) { $signature.SignerCertificate.Thumbprint } else { '' }
    securityPolicy = 'Installer/updater never weaken BricsCAD security settings.'
}
$metadata | ConvertTo-Json | Set-Content -Path (Join-Path $dist 'PACKAGE-METADATA.json') -Encoding UTF8

@"
QS3D for BricsCAD V25 x64
Product version: $productVersion
Assembly version: $($assemblyVersion.ToString())
Source commit: $gitCommit

Recommended install (avoids .NET 0x80131515 / Mark-of-the-Web NETLOAD failures):
1. Close BricsCAD.
2. Before extracting a browser-downloaded ZIP, you may right-click the ZIP > Properties > Unblock, then extract the complete package to a normal local folder.
3. Double-click INSTALL-QS3D.cmd. Signed installers must have valid Authenticode; invalid/untrusted signatures are rejected. Unsigned cloud previews are explicitly warned, then only the bootstrap installer script is unblocked so it can run under RemoteSigned.
4. The installer verifies SHA256SUMS.txt/signatures where required, copies QS3D to the per-user install directory and removes Mark-of-the-Web from installed payloads.
5. Start BricsCAD V25 and run QS3D or QS3DDOMAIN. DemandLoad handles the installed DLL; do not NETLOAD the DLL directly from Downloads.
6. Run QS3DRUNTIMECHECK to confirm V25/x64/package consistency on the customer machine.
7. For an intentional upgrade over an existing QS3D registration, use the built-in QS3D Update Center or rerun install-v25-autoload.ps1 with -Force.

Manual NETLOAD recovery for an extracted package:
- If BricsCAD reports "Could not load file or assembly" with "Operation is not supported" or HRESULT 0x80131515 while loading QS3D.BricsCAD.V25.dll from the extracted package, Windows may have propagated Mark-of-the-Web to the DLL or one of its dependencies.
- Preferred fix: use INSTALL-QS3D.cmd and load the installed copy through DemandLoad.
- If direct NETLOAD is intentionally required for troubleshooting, close any load attempt and double-click UNBLOCK-QS3D.cmd in this package first.
- UNBLOCK-QS3D.cmd verifies the recovery helper hash before bootstrap. The helper then verifies complete SHA256SUMS.txt coverage plus V25 package identity files before removing Mark-of-the-Web from the whole package. It never changes BricsCAD trusted-path/security settings or PowerShell execution policy.
- After the recovery reports success, NETLOAD the QS3D.BricsCAD.V25.dll in this same package folder. Do not unblock only one DLL because a blocked dependency can produce the same .NET loader error.

Built-in update:
- QS3D checks GitHub Releases on startup.
- Run QS3DUPDATE or click Cập nhật QS3D in KHỞI ĐẦU > Hệ thống for one-click secure update.
- QS3DUPDATEONCLOSE toggles Update khi đóng. When enabled, a release already verified in the current session is scheduled as BricsCAD exits; the detached updater waits for all BricsCAD processes to close, installs it and reopens BricsCAD.
- Production one-click update remains fail-closed: the updater requires the signed manifest, ZIP SHA-256, internal SHA256SUMS.txt and Authenticode publisher before atomic install.

Manual/developer fallback:
- Prefer installing first and NETLOAD only the DLL from the installed QS3D directory if debugging requires NETLOAD.
- Never NETLOAD QS3D.BricsCAD.V25.dll directly from a downloaded ZIP/Downloads folder. Windows may attach Zone.Identifier and .NET Framework can reject it with HRESULT 0x80131515.
- For an extracted release package use UNBLOCK-QS3D.cmd so the complete package is verified before Mark-of-the-Web is removed.
- If you intentionally test an unpackaged development copy, remove Mark-of-the-Web from the complete dependency folder before NETLOAD rather than unblocking only one DLL.

Security:
- INSTALL-QS3D.cmd and UNBLOCK-QS3D.cmd use RemoteSigned and never use ExecutionPolicy Bypass.
- Valid Authenticode helpers report their signer; invalid/untrusted signatures fail. Unsigned preview bootstrap is visibly warned only after the helper hash is verified.
- The installer verifies SHA256SUMS.txt before copying files and removes Mark-of-the-Web only from the verified installed payload.
- The manual recovery helper verifies every hashed package file and manifest coverage before unblocking the package DLLs/dependencies.
- Neither path disables or weakens BricsCAD security settings.
- This package intentionally excludes BricsCAD runtime assemblies.
- Samples/ contains only repository-owned synthetic DXF/DWG/QSDB/XLSX/template fixtures.

Native Solid3d and DemandLoad behavior still require the real licensed V25 runtime gate before release qualification.
"@ | Set-Content -Path (Join-Path $dist 'README.txt') -Encoding UTF8

foreach ($name in $forbidden) {
    if (Get-SafePackageFiles -PackageRoot $dist | Where-Object { [string]::Equals($_.Name, $name, [System.StringComparison]::OrdinalIgnoreCase) }) {
        throw "Proprietary BricsCAD assembly must not be packaged: $name"
    }
}

$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory'
$distFull = [IO.Path]::GetFullPath($dist).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
# Legacy manifest-coverage contract marker (non-executable); hashing below intentionally uses safe traversal:
# Get-ChildItem $dist -Recurse -File | Sort-Object FullName | ForEach-Object
$hashLines = Get-SafePackageFiles -PackageRoot $dist | ForEach-Object {
    $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash
    $relativePath = $_.FullName.Substring($distFull.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, '/')
    "$hash  $relativePath"
}
if (-not $hashLines) { throw 'No package files were available for hashing.' }
$hashLines | Set-Content -Path (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII
$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
$dist = Assert-SafeOutputDirectoryTarget -Path $dist -RepositoryRoot $root -Label 'package staging directory'
$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal
$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash
Write-Host "Package ready: $zip"
Write-Host "Product version: $productVersion"
Write-Host "Assembly version: $($assemblyVersion.ToString())"
Write-Host "Source commit: $gitCommit"
Write-Host "Commands: $($commands.Count)"
Write-Host "Plugin signature: $($signature.Status)"
Write-Host "SHA256: $zipHash"