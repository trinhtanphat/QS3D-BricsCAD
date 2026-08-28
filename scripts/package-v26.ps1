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
    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) { throw "Project file was not found: $ProjectPath" }
    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $versions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($versions.Count -ne 1) { throw "Project must declare exactly one Version value: $ProjectPath" }
    $value = [string]$versions[0]
    if (-not [string]::Equals($value, $value.Trim(), [StringComparison]::Ordinal)) { throw "Project Version must not contain leading or trailing whitespace: $ProjectPath" }
    return $value
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
    if (-not (Test-Path -LiteralPath $Path -PathType Leaf)) { throw "V26 command source was not found: $Path" }
    $text = Get-Content -LiteralPath $Path -Raw
    [regex]::Matches($text, '\[CommandMethod\("([^\"]+)"') | ForEach-Object { $script:commands += $_.Groups[1].Value.ToUpperInvariant() }
}

$pluginProject = Join-Path $root 'src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj'
$coreProject = Join-Path $root 'src/QS3D.Core/QS3D.Core.csproj'
$productVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $pluginProject) -Label 'QS3D V26 plugin product version'
$coreProductVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $coreProject) -Label 'QS3D Core product version'
if (-not [string]::Equals($productVersion, $coreProductVersion, [StringComparison]::Ordinal)) { throw "QS3D V26 plugin/Core product versions differ: plugin=$productVersion core=$coreProductVersion" }
if (-not [string]::IsNullOrEmpty($env:RELEASE_TAG)) {
    $expectedTag = 'v' + $productVersion
    if (-not [string]::Equals($env:RELEASE_TAG, $expectedTag, [StringComparison]::Ordinal)) { throw "RELEASE_TAG must exactly match the V26 source product version. Expected $expectedTag, got $env:RELEASE_TAG." }
}

if (-not (Test-Path -LiteralPath $source -PathType Container)) { throw "V26 Release output was not found: $source" }
if (-not (Test-Path -LiteralPath $generator -PathType Leaf)) { throw "V26 script transformer was not found: $generator" }
$root = Assert-OrdinaryDirectory -Path $root -Label 'repository root'
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
    $path = Join-Path $source $name
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw "Missing V26 build artifact: $path" }
    Copy-Item -LiteralPath $path -Destination (Join-Path $dist $name)
}

$generatedScripts = [ordered]@{'install-v25-autoload.ps1'='install-v26-autoload.ps1';'uninstall-v25-autoload.ps1'='uninstall-v26-autoload.ps1';'update-v25.ps1'='update-v26.ps1'}
foreach ($sourceScript in $generatedScripts.Keys) {
    $output = Join-Path $dist $generatedScripts[$sourceScript]
    & $generator -SourceScript $sourceScript -OutputPath $output
    if (-not $?) { throw "Failed to generate V26 release script from $sourceScript" }
    $generatedText = Get-Content -LiteralPath $output -Raw
    if ($generatedText -match '(?i)v25') { throw "Generated V26 release script leaked a V25 token: $output" }
}

if (-not (Test-Path -LiteralPath $sampleSource -PathType Container)) { throw "Synthetic sample folder was not found: $sampleSource" }
$sampleDestination = Join-Path $dist 'Samples'
New-Item -ItemType Directory -Path $sampleDestination -Force | Out-Null
foreach ($sampleName in @('README.md','QS3D-Sample.dxf','QS3D-Sample.qsdb','QS3D-Quantity-Template.xlsx','QS3D-Architecture.qstemplate')) {
    $samplePath = Join-Path $sampleSource $sampleName
    if (-not (Test-Path -LiteralPath $samplePath -PathType Leaf)) { throw "Missing synthetic sample artifact: $samplePath" }
    Copy-Item -LiteralPath $samplePath -Destination (Join-Path $sampleDestination $sampleName)
}
$sampleDwg = Join-Path $sampleSource 'QS3D-Sample.dwg'
if (Test-Path -LiteralPath $sampleDwg -PathType Leaf) { Copy-Item -LiteralPath $sampleDwg -Destination (Join-Path $sampleDestination 'QS3D-Sample.dwg') }

$commands = @()
$v25Root = Join-Path $root 'src/QS3D.BricsCAD.V25'
Get-ChildItem $v25Root -Recurse -Filter '*.cs' | Where-Object { $_.Name -ne 'PluginEntry.cs' -and -not $_.FullName.StartsWith((Join-Path $v25Root 'Updates') + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase) } | ForEach-Object { Add-CommandMethodsFromSource -Path $_.FullName }
foreach ($linkedUpdateSource in @('SemanticReleaseVersion.cs','UpdateBootstrapper.cs','UpdateCenterWindow.cs','UpdateCoordinator.cs','UpdatePreferences.cs','UpdateSettingsCommands.cs')) { Add-CommandMethodsFromSource -Path (Join-Path $v25Root ('Updates/' + $linkedUpdateSource)) }
Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V26') -Recurse -Filter '*.cs' | ForEach-Object { Add-CommandMethodsFromSource -Path $_.FullName }
$commands = @($commands | Sort-Object -Unique)
if ($commands.Count -eq 0 -or -not ($commands -contains 'QS3D')) { throw 'No QS3D CommandMethod entries were discovered for V26.' }
foreach ($requiredCommand in @('QS3DUPDATE','QSUPDATE','QS3DVER','QSVER')) { if (-not ($commands -contains $requiredCommand)) { throw "Required V26 command was not discovered from compiled source: $requiredCommand" } }
$commands | Set-Content -LiteralPath (Join-Path $dist 'COMMANDS.txt') -Encoding ASCII

$pluginPath = Join-Path $dist 'QS3D.BricsCAD.V26.dll'
$corePath = Join-Path $dist 'QS3D.Core.dll'
$signature = Get-AuthenticodeSignature -FilePath $pluginPath
try { $assemblyVersion = [Reflection.AssemblyName]::GetAssemblyName($pluginPath).Version } catch { throw "Could not read QS3D V26 plugin assembly version: $($_.Exception.Message)" }
if (-not $assemblyVersion) { throw 'Could not read QS3D V26 plugin assembly version.' }
Assert-ManagedIdentity -Path $pluginPath -ExpectedAssemblyVersion $assemblyVersion -ExpectedProductVersion $productVersion -Label 'QS3D.BricsCAD.V26.dll'
Assert-ManagedIdentity -Path $corePath -ExpectedAssemblyVersion $assemblyVersion -ExpectedProductVersion $productVersion -Label 'QS3D.Core.dll'

$metadata = [ordered]@{product='QS3D';target='BricsCAD V26 x64';framework='net8.0-windows';productVersion=$productVersion;version=$assemblyVersion.ToString();generatedUtc=[DateTime]::UtcNow.ToString('o');commandCount=$commands.Count;defaultLoadMode='OnCommand';autoloadMethod='BricsCAD Registry DemandLoad';pluginSignatureStatus=$signature.Status.ToString();pluginSignerThumbprint=if($signature.SignerCertificate){$signature.SignerCertificate.Thumbprint}else{''};securityPolicy='Installer/updater never weaken BricsCAD security settings.'}
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

foreach ($name in $forbidden) { if (Get-ChildItem -LiteralPath $dist -Recurse -Filter $name -ErrorAction SilentlyContinue) { throw "Proprietary BricsCAD assembly must not be packaged: $name" } }

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