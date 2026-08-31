[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/QS3D.BricsCAD.V26/bin/x64/Release/net8.0-windows'
$distRoot = Join-Path $root 'dist'
$dist = Join-Path $distRoot 'QS3D-BricsCAD-V26'
$zip = Join-Path $distRoot 'QS3D-BricsCAD-V26.zip'
$required = @('QS3D.BricsCAD.V26.dll', 'QS3D.BricsCAD.V26.runtimeconfig.json', 'QS3D.Core.dll')
$forbidden = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$sampleSource = Join-Path $root 'samples/generated'
$generator = Join-Path $PSScriptRoot 'new-v26-script-from-v25.ps1'
$script:MaxPackageTextBytes = 8MB
$script:StrictUtf8 = [Text.UTF8Encoding]::new($false, $true)

function Get-CanonicalFullPath {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    if ([string]::IsNullOrWhiteSpace($Path)) { throw "$Label path is required." }
    try { return [IO.Path]::GetFullPath($Path) }
    catch { throw "$Label path is invalid: $($_.Exception.Message)" }
}

function Test-PathEqualOrContained {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Container)
    $pathFull = Get-CanonicalFullPath -Path $Path -Label 'candidate'
    $containerFull = (Get-CanonicalFullPath -Path $Container -Label 'container').TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
    if ([string]::Equals($pathFull.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar), $containerFull, [StringComparison]::OrdinalIgnoreCase)) { return $true }
    return $pathFull.StartsWith($containerFull + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)
}

function Assert-OrdinaryDirectory {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) { throw "$Label directory was not found: $fullPath" }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse directory: $fullPath"
    }
    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-SafeInputPathAncestors {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $repo = Assert-OrdinaryDirectory -Path $RepositoryRoot -Label 'repository root'
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-PathEqualOrContained -Path $fullPath -Container $repo) -or [string]::Equals($fullPath, $repo, [StringComparison]::OrdinalIgnoreCase)) {
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
        if ([string]::Equals($current, $repo, [StringComparison]::OrdinalIgnoreCase)) { break }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $current, [StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent
    }
    return $fullPath
}

function Assert-SafeInputDirectory {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $fullPath = Assert-SafeInputPathAncestors -Path $Path -RepositoryRoot $RepositoryRoot -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Container)) { throw "$Label directory was not found: $fullPath" }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse directory: $fullPath"
    }
    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-SafeInputFile {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $fullPath = Assert-SafeInputPathAncestors -Path $Path -RepositoryRoot $RepositoryRoot -Label $Label
    if (-not (Test-Path -LiteralPath $fullPath -PathType Leaf)) { throw "$Label file was not found: $fullPath" }
    $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
    if ($item.PSIsContainer -or -not ($item -is [IO.FileInfo]) -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$Label must be an ordinary non-reparse file: $fullPath"
    }
    return $fullPath
}

function Open-HeldPackageInput {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )
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
    param(
        [Parameter(Mandatory = $true)][pscustomobject]$Held,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $path = Assert-SafeInputFile -Path $Held.Path -RepositoryRoot $RepositoryRoot -Label $Label
    $item = Get-Item -LiteralPath $path -Force -ErrorAction Stop
    if (-not [string]::Equals($Held.Path, $item.FullName, [StringComparison]::OrdinalIgnoreCase) -or
        $Held.Length -ne $item.Length -or $Held.Length -ne $Held.Stream.Length -or
        $Held.LastWriteUtcTicks -ne $item.LastWriteTimeUtc.Ticks) {
        throw "$Label pathname no longer resolves to the held admitted generation."
    }
}

function Read-HeldPackageText {
    param(
        [Parameter(Mandatory = $true)][pscustomobject]$Held,
        [Parameter(Mandatory = $true)][string]$Label
    )
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

function Invoke-WithHeldPackageInput {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$Label,
        [Parameter(Mandatory = $true)][scriptblock]$Action
    )
    $held = Open-HeldPackageInput -Path $Path -RepositoryRoot $root -Label $Label
    try {
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label $Label
        $result = & $Action $held.Path
        if (-not $?) { throw "$Label consumer failed." }
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label $Label
        return $result
    }
    finally { $held.Stream.Dispose() }
}

function Copy-HeldPackageInput {
    param(
        [Parameter(Mandatory = $true)][string]$SourcePath,
        [Parameter(Mandatory = $true)][string]$DestinationPath,
        [Parameter(Mandatory = $true)][string]$Label
    )
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

function Open-HeldStagedManagedFile {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    return Open-HeldPackageInput -Path $Path -RepositoryRoot $root -Label $Label
}

function Get-SafeSourceFiles {
    param(
        [Parameter(Mandatory = $true)][string]$SourceRoot,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Extension
    )
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
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label,
        [switch]$MayBeMissing
    )
    $repo = Assert-OrdinaryDirectory -Path $RepositoryRoot -Label 'repository root'
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    $pathRoot = [IO.Path]::GetPathRoot($fullPath)
    if (-not [string]::IsNullOrWhiteSpace($pathRoot) -and [string]::Equals($fullPath, $pathRoot, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must not be a filesystem root: $fullPath"
    }
    if (-not (Test-PathEqualOrContained -Path $fullPath -Container $repo) -or [string]::Equals($fullPath, $repo, [StringComparison]::OrdinalIgnoreCase)) {
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
        if ([string]::Equals($current, $repo, [StringComparison]::OrdinalIgnoreCase)) { break }
        $parent = [IO.Path]::GetDirectoryName($current)
        if ([string]::IsNullOrWhiteSpace($parent) -or [string]::Equals($parent, $current, [StringComparison]::OrdinalIgnoreCase)) { break }
        $current = $parent
    }
    if (Test-Path -LiteralPath $fullPath) {
        $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        if (-not $item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must be an ordinary non-reparse directory target: $fullPath"
        }
    } elseif (-not $MayBeMissing) {
        throw "$Label directory was not found: $fullPath"
    }
    return $fullPath.TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
}

function Assert-SafeOutputFileTarget {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RepositoryRoot,
        [Parameter(Mandatory = $true)][string]$Label
    )
    $repo = Assert-OrdinaryDirectory -Path $RepositoryRoot -Label 'repository root'
    $fullPath = Get-CanonicalFullPath -Path $Path -Label $Label
    if (-not (Test-PathEqualOrContained -Path $fullPath -Container $repo) -or [string]::Equals($fullPath, $repo, [StringComparison]::OrdinalIgnoreCase)) {
        throw "$Label must stay below the repository root: $fullPath"
    }
    $parent = [IO.Path]::GetDirectoryName($fullPath)
    $null = Assert-SafeOutputDirectoryTarget -Path $parent -RepositoryRoot $repo -Label ("$Label parent") -MayBeMissing
    if (Test-Path -LiteralPath $fullPath) {
        $item = Get-Item -LiteralPath $fullPath -Force -ErrorAction Stop
        if ($item.PSIsContainer -or ($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
            throw "$Label must be an ordinary non-reparse file target: $fullPath"
        }
    }
    return $fullPath
}

function Get-SafePackageFiles {
    param([Parameter(Mandatory = $true)][string]$PackageRoot)
    $package = Assert-OrdinaryDirectory -Path $PackageRoot -Label 'package staging root'
    $pending = New-Object 'System.Collections.Generic.Stack[string]'
    $files = New-Object 'System.Collections.Generic.List[System.IO.FileInfo]'
    $pending.Push($package)
    while ($pending.Count -gt 0) {
        $directory = $pending.Pop()
        foreach ($item in Get-ChildItem -LiteralPath $directory -Force -ErrorAction Stop) {
            if (($item.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) {
                throw "Package staging contains a reparse-backed entry: $($item.FullName)"
            }
            if ($item.PSIsContainer) {
                $pending.Push($item.FullName)
                continue
            }
            if (-not ($item -is [IO.FileInfo])) {
                throw "Package staging contains a non-regular filesystem entry: $($item.FullName)"
            }
            $files.Add($item)
        }
    }
    return @($files | Sort-Object FullName)
}

function Read-ProjectProductVersion {
    param([Parameter(Mandatory = $true)][string]$ProjectPath)
    $held = Open-HeldPackageInput -Path $ProjectPath -RepositoryRoot $root -Label 'project file'
    try {
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label 'project file'
        [xml]$project = Read-HeldPackageText -Held $held -Label 'project file'
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label 'project file'
        $versions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
        if ($versions.Count -ne 1) { throw "Project must declare exactly one Version value: $ProjectPath" }
        $value = [string]$versions[0]
        if (-not [string]::Equals($value, $value.Trim(), [StringComparison]::Ordinal)) { throw "Project Version must not contain leading or trailing whitespace: $ProjectPath" }
        return $value
    }
    finally { $held.Stream.Dispose() }
}

function Convert-ToStrictSemVerText {
    param([Parameter(Mandatory = $true)][string]$Value, [Parameter(Mandatory = $true)][string]$Label)
    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Label is missing." }
    $text = [string]$Value
    if (-not [string]::Equals($text, $text.Trim(), [StringComparison]::Ordinal)) { throw "$Label must not contain leading or trailing whitespace." }
    $match = [regex]::Match($text, '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$', [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) { throw "$Label is not strict SemVer: $text" }
    if ($match.Groups[4].Success) {
        foreach ($identifier in $match.Groups[4].Value.Split('.')) {
            if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier[0] -eq '0') { throw "$Label has a numeric prerelease identifier with a leading zero: $text" }
        }
    }
    return $text
}

function Read-ManagedProductVersion {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][string]$Label)
    try { $value = [string][Diagnostics.FileVersionInfo]::GetVersionInfo($Path).ProductVersion }
    catch { throw "$Label product version is unreadable: $($_.Exception.Message)" }
    return Convert-ToStrictSemVerText -Value $value -Label ("$Label product version")
}

function Assert-ManagedIdentity {
    param([Parameter(Mandatory = $true)][string]$Path, [Parameter(Mandatory = $true)][Version]$ExpectedAssemblyVersion, [Parameter(Mandatory = $true)][string]$ExpectedProductVersion, [Parameter(Mandatory = $true)][string]$Label)
    try { $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($Path).Version }
    catch { throw "$Label assembly version is unreadable: $($_.Exception.Message)" }
    if (-not $assemblyVersion -or $assemblyVersion -ne $ExpectedAssemblyVersion) { throw "$Label assembly version $assemblyVersion does not match expected $ExpectedAssemblyVersion." }
    $productVersion = Read-ManagedProductVersion -Path $Path -Label $Label
    if (-not [string]::Equals($productVersion, $ExpectedProductVersion, [StringComparison]::Ordinal)) { throw "$Label product version $productVersion does not match expected $ExpectedProductVersion." }
}

function Add-CommandMethodsFromSource {
    param([Parameter(Mandatory = $true)][string]$Path)
    $held = Open-HeldPackageInput -Path $Path -RepositoryRoot $root -Label 'V26 command source'
    try {
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label 'V26 command source'
        $text = Read-HeldPackageText -Held $held -Label 'V26 command source'
        Assert-HeldPathBinding -Held $held -RepositoryRoot $root -Label 'V26 command source'
        [regex]::Matches($text, '\[CommandMethod\("([^\"]+)"') | ForEach-Object { $script:commands += $_.Groups[1].Value.ToUpperInvariant() }
    }
    finally { $held.Stream.Dispose() }
}

$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'
$pluginProject = Assert-SafeInputFile -Path (Join-Path $root 'src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj') -RepositoryRoot $root -Label 'V26 plugin project'
$coreProject = Assert-SafeInputFile -Path (Join-Path $root 'src/QS3D.Core/QS3D.Core.csproj') -RepositoryRoot $root -Label 'Core project'
$productVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $pluginProject) -Label 'QS3D V26 plugin product version'
$coreProductVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $coreProject) -Label 'QS3D Core product version'
if (-not [string]::Equals($productVersion, $coreProductVersion, [StringComparison]::Ordinal)) { throw "QS3D V26 plugin/Core product versions differ: plugin=$productVersion core=$coreProductVersion" }
if (-not [string]::IsNullOrEmpty($env:RELEASE_TAG)) {
    $expectedTag = 'v' + $productVersion
    if (-not [string]::Equals($env:RELEASE_TAG, $expectedTag, [StringComparison]::Ordinal)) { throw "RELEASE_TAG must exactly match the V26 source product version. Expected $expectedTag, got $env:RELEASE_TAG." }
}

$source = Assert-SafeInputDirectory -Path $source -RepositoryRoot $root -Label 'V26 Release output'
$generator = Assert-SafeInputFile -Path $generator -RepositoryRoot $root -Label 'V26 script transformer'
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
    $path = Assert-SafeInputFile -Path (Join-Path $source $name) -RepositoryRoot $root -Label ("V26 build artifact $name")
    Copy-HeldPackageInput -SourcePath $path -DestinationPath (Join-Path $dist $name) -Label ("V26 build artifact $name")
}

$generatedScripts = [ordered]@{
    'install-v25-autoload.ps1' = 'install-v26-autoload.ps1'
    'uninstall-v25-autoload.ps1' = 'uninstall-v26-autoload.ps1'
    'update-v25.ps1' = 'update-v26.ps1'
}
Invoke-WithHeldPackageInput -Path $generator -Label 'V26 script transformer' -Action {
    param($heldGeneratorPath)
    foreach ($sourceScript in $generatedScripts.Keys) {
        $sourceScriptPath = Assert-SafeInputFile -Path (Join-Path $PSScriptRoot $sourceScript) -RepositoryRoot $root -Label ("V26 generator input $sourceScript")
        $output = Join-Path $dist $generatedScripts[$sourceScript]
        Invoke-WithHeldPackageInput -Path $sourceScriptPath -Label ("V26 generator input $sourceScript") -Action {
            param($heldSourcePath)
            & $heldGeneratorPath -SourceScript ([IO.Path]::GetFileName($heldSourcePath)) -OutputPath $output
            if (-not $?) { throw "Failed to generate V26 release script from $sourceScript" }
        } | Out-Null
        $generatedText = Get-Content -LiteralPath $output -Raw
        if ($generatedText -match '(?i)v25') { throw "Generated V26 release script leaked a V25 token: $output" }
    }
} | Out-Null

$sampleDestination = Join-Path $dist 'Samples'
New-Item -ItemType Directory -Path $sampleDestination -Force | Out-Null
foreach ($sampleName in @('README.md','QS3D-Sample.dxf','QS3D-Sample.qsdb','QS3D-Quantity-Template.xlsx','QS3D-Architecture.qstemplate')) {
    $samplePath = Assert-SafeInputFile -Path (Join-Path $sampleSource $sampleName) -RepositoryRoot $root -Label ("synthetic sample $sampleName")
    Copy-HeldPackageInput -SourcePath $samplePath -DestinationPath (Join-Path $sampleDestination $sampleName) -Label ("synthetic sample $sampleName")
}
$sampleDwg = Join-Path $sampleSource 'QS3D-Sample.dwg'
if (Test-Path -LiteralPath $sampleDwg) {
    $sampleDwg = Assert-SafeInputFile -Path $sampleDwg -RepositoryRoot $root -Label 'synthetic sample QS3D-Sample.dwg'
    Copy-HeldPackageInput -SourcePath $sampleDwg -DestinationPath (Join-Path $sampleDestination 'QS3D-Sample.dwg') -Label 'synthetic sample QS3D-Sample.dwg'
}

$commands = @()
$v25Root = Assert-SafeInputDirectory -Path (Join-Path $root 'src/QS3D.BricsCAD.V25') -RepositoryRoot $root -Label 'V25 linked command source root'
Get-SafeSourceFiles -SourceRoot $v25Root -RepositoryRoot $root -Extension '.cs' | Where-Object { $_.Name -ne 'PluginEntry.cs' -and -not $_.FullName.StartsWith((Join-Path $v25Root 'Updates') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) } | ForEach-Object { Add-CommandMethodsFromSource -Path $_.FullName }
foreach ($linkedUpdateSource in @('SemanticReleaseVersion.cs','UpdateBootstrapper.cs','UpdateCenterWindow.cs','UpdateCoordinator.cs','UpdatePreferences.cs','UpdateSettingsCommands.cs')) { Add-CommandMethodsFromSource -Path (Join-Path $v25Root ('Updates/' + $linkedUpdateSource)) }
Get-SafeSourceFiles -SourceRoot (Join-Path $root 'src/QS3D.BricsCAD.V26') -RepositoryRoot $root -Extension '.cs' | ForEach-Object { Add-CommandMethodsFromSource -Path $_.FullName }
$commands = @($commands | Sort-Object -Unique)
if ($commands.Count -eq 0 -or -not ($commands -contains 'QS3D')) { throw 'No QS3D CommandMethod entries were discovered for V26.' }
foreach ($requiredCommand in @('QS3DUPDATE','QSUPDATE','QS3DVER','QSVER')) { if (-not ($commands -contains $requiredCommand)) { throw "Required V26 command was not discovered from compiled source: $requiredCommand" } }
$commands | Set-Content -LiteralPath (Join-Path $dist 'COMMANDS.txt') -Encoding ASCII

$pluginPath = Join-Path $dist 'QS3D.BricsCAD.V26.dll'
$corePath = Join-Path $dist 'QS3D.Core.dll'
$heldPlugin = $null
$heldCore = $null
try {
    $heldPlugin = Open-HeldStagedManagedFile -Path $pluginPath -Label 'QS3D.BricsCAD.V26.dll'
    $heldCore = Open-HeldStagedManagedFile -Path $corePath -Label 'QS3D.Core.dll'

    Assert-HeldPathBinding -Held $heldPlugin -RepositoryRoot $root -Label 'QS3D.BricsCAD.V26.dll'
    $signature = Get-AuthenticodeSignature -FilePath $heldPlugin.Path
    Assert-HeldPathBinding -Held $heldPlugin -RepositoryRoot $root -Label 'QS3D.BricsCAD.V26.dll'
    try { $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($heldPlugin.Path).Version } catch { throw "Could not read QS3D V26 plugin assembly version: $($_.Exception.Message)" }
    if (-not $assemblyVersion) { throw 'Could not read QS3D V26 plugin assembly version.' }
    Assert-ManagedIdentity -Path $heldPlugin.Path -ExpectedAssemblyVersion $assemblyVersion -ExpectedProductVersion $productVersion -Label 'QS3D.BricsCAD.V26.dll'
    Assert-HeldPathBinding -Held $heldPlugin -RepositoryRoot $root -Label 'QS3D.BricsCAD.V26.dll'

    Assert-HeldPathBinding -Held $heldCore -RepositoryRoot $root -Label 'QS3D.Core.dll'
    try { $coreAssemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($heldCore.Path).Version } catch { throw "Could not read QS3D Core assembly version: $($_.Exception.Message)" }
    if (-not $coreAssemblyVersion -or $coreAssemblyVersion -ne $assemblyVersion) { throw "QS3D Core assembly version $coreAssemblyVersion does not match expected $assemblyVersion." }
    Assert-ManagedIdentity -Path $heldCore.Path -ExpectedAssemblyVersion $assemblyVersion -ExpectedProductVersion $productVersion -Label 'QS3D.Core.dll'
    Assert-HeldPathBinding -Held $heldCore -RepositoryRoot $root -Label 'QS3D.Core.dll'
}
finally {
    if ($null -ne $heldCore) { $heldCore.Stream.Dispose() }
    if ($null -ne $heldPlugin) { $heldPlugin.Stream.Dispose() }
}

$metadata = [ordered]@{
    product = 'QS3D'
    target = 'BricsCAD V26 x64'
    framework = 'net8.0-windows'
    productVersion = $productVersion
    version = $assemblyVersion.ToString()
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    commandCount = $commands.Count
    defaultLoadMode = 'OnCommand'
    autoloadMethod = 'BricsCAD Registry DemandLoad'
    pluginSignatureStatus = $signature.Status.ToString()
    pluginSignerThumbprint = if ($signature.SignerCertificate) { $signature.SignerCertificate.Thumbprint } else { '' }
    securityPolicy = 'Installer/updater never weaken BricsCAD security settings.'
}
$metadata | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $dist 'PACKAGE-METADATA.json') -Encoding UTF8

@"
QS3D for BricsCAD V26 x64
Product version: $productVersion
Assembly version: $($assemblyVersion.ToString())
Managed target: net8.0-windows

Prerequisite:
- BricsCAD V26 x64.
- Microsoft .NET 8 Desktop Runtime x64.

Recommended install:
1. Close BricsCAD.
2. Run install-v26-autoload.ps1 from this extracted package.
3. Default mode is OnCommand DemandLoad. Start BricsCAD V26 and run QS3D or QS3DDOMAIN.
4. Run QS3DRUNTIMECHECK / the repository V26 runtime qualification before production rollout.
5. For an intentional upgrade over an existing verified QS3D V26 registration, rerun the installer with -Force.
6. For production, require the expected Authenticode publisher with -RequireSigned -ExpectedSignerThumbprint <40-hex-thumbprint>.

Secure manual update:
- Run update-v26.ps1 with the V26 HTTPS manifest and expected publisher thumbprint.
- The updater is generated from the current hardened V25 updater with host-major tokens transformed under a deterministic guard.
- It blocks cross-major V25 assets, downgrade/identity/hash/signature failures and calls only the V26 installer.

Manual fallback:
- Start BricsCAD V26, run NETLOAD, select QS3D.BricsCAD.V26.dll, then run QS3D.

Security:
- SHA256SUMS.txt covers every package file except itself.
- The installer/updater do not disable or weaken BricsCAD security settings.
- This package intentionally excludes BricsCAD runtime assemblies.
- Samples/ contains only repository-owned synthetic fixtures.

Licensed V26 NETLOAD/DemandLoad, signing, clean-machine install/update/uninstall and native runtime behavior remain required before a production release is qualified.
"@ | Set-Content -LiteralPath (Join-Path $dist 'README.txt') -Encoding UTF8

foreach ($name in $forbidden) { if (Get-SafePackageFiles -PackageRoot $dist | Where-Object { [string]::Equals($_.Name, $name, [StringComparison]::OrdinalIgnoreCase) }) { throw "Proprietary BricsCAD assembly must not be packaged: $name" } }

$distFull = [IO.Path]::GetFullPath($dist).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
$hashLines = Get-SafePackageFiles -PackageRoot $dist | ForEach-Object {
    $hash = (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToUpperInvariant()
    $relativePath = $_.FullName.Substring($distFull.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, '/').Replace([IO.Path]::AltDirectorySeparatorChar, '/')
    if ([string]::IsNullOrWhiteSpace($relativePath) -or [IO.Path]::IsPathRooted($relativePath) -or $relativePath.Contains(':') -or $relativePath.Contains('\')) { throw "Unsafe package-relative path while hashing: $relativePath" }
    $segments = @($relativePath.Split('/'))
    if (@($segments | Where-Object { [string]::IsNullOrWhiteSpace($_) -or $_ -eq '.' -or $_ -eq '..' }).Count -gt 0) { throw "Unsafe package-relative path while hashing: $relativePath" }
    "$hash  $relativePath"
}
if (-not $hashLines) { throw 'No V26 package files were available for hashing.' }
$hashLines | Set-Content -LiteralPath (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII

$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
if (Test-Path -LiteralPath $zip) { Remove-Item -LiteralPath $zip -Force }
$null = Get-SafePackageFiles -PackageRoot $dist
Compress-Archive -Path (Join-Path $dist '*') -DestinationPath $zip -CompressionLevel Optimal
$zip = Assert-SafeOutputFileTarget -Path $zip -RepositoryRoot $root -Label 'package ZIP'
$zipHash = (Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash.ToUpperInvariant()
Write-Host "V26 package ready: $zip"
Write-Host "Product version: $productVersion"
Write-Host "Assembly version: $($assemblyVersion.ToString())"
Write-Host "Commands: $($commands.Count)"
Write-Host "Plugin signature: $($signature.Status)"
Write-Host "SHA256: $zipHash"