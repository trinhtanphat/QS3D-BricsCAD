$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/QS3D.BricsCAD.V25/bin/x64/Release/net48'
$distRoot = Join-Path $root 'dist'
$dist = Join-Path $distRoot 'QS3D-BricsCAD-V25'
$zip = Join-Path $distRoot 'QS3D-BricsCAD-V25.zip'
$required = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')
$forbidden = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')
$sampleSource = Join-Path $root 'samples/generated'

function Read-ProjectProductVersion {
    param([string]$ProjectPath)
    if (-not (Test-Path -LiteralPath $ProjectPath -PathType Leaf)) { throw "Project file was not found: $ProjectPath" }
    [xml]$project = Get-Content -LiteralPath $ProjectPath -Raw
    $versions = @($project.Project.PropertyGroup | ForEach-Object { [string]$_.Version } | Where-Object { -not [string]::IsNullOrWhiteSpace($_) })
    if ($versions.Count -ne 1) { throw "Project must declare exactly one Version value: $ProjectPath" }
    return $versions[0].Trim()
}

function Convert-ToStrictSemVerText {
    param([string]$Value, [string]$Label)

    if ([string]::IsNullOrWhiteSpace($Value)) { throw "$Label is missing." }
    $text = $Value.Trim()
    $match = [regex]::Match(
        $text,
        '^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(?:-([0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*))?(?:\+[0-9A-Za-z-]+(?:\.[0-9A-Za-z-]+)*)?$',
        [Text.RegularExpressions.RegexOptions]::CultureInvariant)
    if (-not $match.Success) { throw "$Label is not strict SemVer: $text" }

    if ($match.Groups[4].Success) {
        foreach ($identifier in $match.Groups[4].Value.Split('.')) {
            if ($identifier -match '^[0-9]+$' -and $identifier.Length -gt 1 -and $identifier[0] -eq '0') {
                throw "$Label has a numeric prerelease identifier with a leading zero: $text"
            }
        }
    }
    return $text
}

$pluginProject = Join-Path $root 'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj'
$coreProject = Join-Path $root 'src/QS3D.Core/QS3D.Core.csproj'
$productVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $pluginProject) -Label 'QS3D plugin product version'
$coreProductVersion = Convert-ToStrictSemVerText -Value (Read-ProjectProductVersion -ProjectPath $coreProject) -Label 'QS3D Core product version'
if (-not [string]::Equals($productVersion, $coreProductVersion, [StringComparison]::Ordinal)) {
    throw "QS3D plugin/Core product versions differ: plugin=$productVersion core=$coreProductVersion"
}
if (-not [string]::IsNullOrWhiteSpace($env:RELEASE_TAG)) {
    $expectedTag = 'v' + $productVersion
    if (-not [string]::Equals($env:RELEASE_TAG.Trim(), $expectedTag, [StringComparison]::Ordinal)) {
        throw "RELEASE_TAG must exactly match the source product version. Expected $expectedTag, got $env:RELEASE_TAG."
    }
}

if (-not (Test-Path $source)) { throw "V25 Release output was not found: $source" }
New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $dist -Force | Out-Null

foreach ($name in $required) {
    $path = Join-Path $source $name
    if (-not (Test-Path $path)) { throw "Missing build artifact: $path" }
    Copy-Item $path (Join-Path $dist $name)
}

foreach ($script in @('install-v25-autoload.ps1', 'uninstall-v25-autoload.ps1', 'update-v25.ps1')) {
    $scriptPath = Join-Path $PSScriptRoot $script
    if (-not (Test-Path -LiteralPath $scriptPath -PathType Leaf)) { throw "Missing release script: $scriptPath" }
    Copy-Item -LiteralPath $scriptPath -Destination (Join-Path $dist $script)
}

if (-not (Test-Path -LiteralPath $sampleSource -PathType Container)) { throw "Synthetic sample folder was not found: $sampleSource" }
$sampleDestination = Join-Path $dist 'Samples'
New-Item -ItemType Directory -Path $sampleDestination -Force | Out-Null
foreach ($sampleName in @('README.md', 'QS3D-Sample.dxf', 'QS3D-Sample.qsdb', 'QS3D-Quantity-Template.xlsx', 'QS3D-Architecture.qstemplate')) {
    $samplePath = Join-Path $sampleSource $sampleName
    if (-not (Test-Path -LiteralPath $samplePath -PathType Leaf)) { throw "Missing synthetic sample artifact: $samplePath" }
    Copy-Item -LiteralPath $samplePath -Destination (Join-Path $sampleDestination $sampleName)
}
$sampleDwg = Join-Path $sampleSource 'QS3D-Sample.dwg'
if (Test-Path -LiteralPath $sampleDwg -PathType Leaf) { Copy-Item -LiteralPath $sampleDwg -Destination (Join-Path $sampleDestination 'QS3D-Sample.dwg') }

$commands = @()
Get-ChildItem (Join-Path $root 'src/QS3D.BricsCAD.V25') -Recurse -Filter '*.cs' | ForEach-Object {
    $text = Get-Content $_.FullName -Raw
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

Recommended install:
1. Close BricsCAD.
2. Run install-v25-autoload.ps1 from this extracted package.
3. Default mode is OnCommand DemandLoad. Start BricsCAD and run QS3D or QS3DDOMAIN.
4. Run QS3DRUNTIMECHECK to confirm V25/x64/package consistency on the customer machine.
5. For an intentional upgrade over an existing QS3D registration, rerun the installer with -Force.
6. For production, require the expected Authenticode publisher with -RequireSigned -ExpectedSignerThumbprint <40-hex-thumbprint>.

Secure update:
- Run update-v25.ps1 with an HTTPS manifest and the expected publisher thumbprint.
- The updater blocks downgrades, verifies the ZIP SHA-256, internal SHA256SUMS.txt and the Authenticode publisher before calling the atomic installer.

Manual fallback:
- Start BricsCAD V25, run NETLOAD, select QS3D.BricsCAD.V25.dll, then run QS3D.

Security:
- The installer verifies SHA256SUMS.txt before copying files.
- It does not disable or weaken BricsCAD security settings.
- This package intentionally excludes BricsCAD runtime assemblies.
- Samples/ contains only repository-owned synthetic DXF/DWG/QSDB/XLSX/template fixtures.

Native Solid3d and DemandLoad behavior still require the real licensed V25 runtime gate before release qualification.
"@ | Set-Content -Path (Join-Path $dist 'README.txt') -Encoding UTF8

foreach ($name in $forbidden) {
    if (Get-ChildItem $dist -Recurse -Filter $name -ErrorAction SilentlyContinue) {
        throw "Proprietary BricsCAD assembly must not be packaged: $name"
    }
}

$distFull = [IO.Path]::GetFullPath($dist).TrimEnd([IO.Path]::DirectorySeparatorChar, [IO.Path]::AltDirectorySeparatorChar)
$hashLines = Get-ChildItem $dist -Recurse -File | Sort-Object FullName | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    $relativePath = $_.FullName.Substring($distFull.Length + 1).Replace([IO.Path]::DirectorySeparatorChar, '/')
    "$hash  $relativePath"
}
if (-not $hashLines) { throw 'No package files were available for hashing.' }
$hashLines | Set-Content -Path (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$dist/*" -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host "Package ready: $zip"
Write-Host "Product version: $productVersion"
Write-Host "Assembly version: $($assemblyVersion.ToString())"
Write-Host "Commands: $($commands.Count)"
Write-Host "Plugin signature: $($signature.Status)"
Write-Host "SHA256: $zipHash"
