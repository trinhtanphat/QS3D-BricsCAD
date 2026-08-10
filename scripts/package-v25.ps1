$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/QS3D.BricsCAD.V25/bin/x64/Release/net48'
$distRoot = Join-Path $root 'dist'
$dist = Join-Path $distRoot 'QS3D-BricsCAD-V25'
$zip = Join-Path $distRoot 'QS3D-BricsCAD-V25.zip'
$required = @('QS3D.BricsCAD.V25.dll', 'QS3D.Core.dll')
$forbidden = @('BrxMgd.dll', 'TD_Mgd.dll', 'TD_MgdBrep.dll')

if (-not (Test-Path $source)) { throw "V25 Release output was not found: $source" }
New-Item -ItemType Directory -Path $distRoot -Force | Out-Null
Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $dist -Force | Out-Null

foreach ($name in $required) {
    $path = Join-Path $source $name
    if (-not (Test-Path $path)) { throw "Missing build artifact: $path" }
    Copy-Item $path (Join-Path $dist $name)
}

Copy-Item (Join-Path $PSScriptRoot 'install-v25-autoload.ps1') (Join-Path $dist 'install-v25-autoload.ps1')
Copy-Item (Join-Path $PSScriptRoot 'uninstall-v25-autoload.ps1') (Join-Path $dist 'uninstall-v25-autoload.ps1')

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
$metadata = [ordered]@{
    product = 'QS3D'
    target = 'BricsCAD V25 x64'
    generatedUtc = [DateTime]::UtcNow.ToString('o')
    commandCount = $commands.Count
    defaultLoadMode = 'OnCommand'
    autoloadMethod = 'BricsCAD Registry DemandLoad'
    pluginSignatureStatus = $signature.Status.ToString()
    securityPolicy = 'Installer never weakens BricsCAD security settings.'
}
$metadata | ConvertTo-Json | Set-Content -Path (Join-Path $dist 'PACKAGE-METADATA.json') -Encoding UTF8

@"
QS3D for BricsCAD V25 x64

Recommended install:
1. Close BricsCAD.
2. Run install-v25-autoload.ps1 from this extracted package.
3. Default mode is OnCommand DemandLoad. Start BricsCAD and run QS3D or QS3DDOMAIN.
4. For an intentional upgrade over an existing QS3D registration, rerun the installer with -Force.
5. To require an Authenticode-signed plugin, use -RequireSigned.

Manual fallback:
- Start BricsCAD V25, run NETLOAD, select QS3D.BricsCAD.V25.dll, then run QS3D.

Security:
- The installer verifies SHA256SUMS.txt before copying files.
- It does not disable or weaken BricsCAD security settings.
- This package intentionally excludes BricsCAD runtime assemblies.

Native Solid3d and DemandLoad behavior still require the real licensed V25 runtime gate before release qualification.
"@ | Set-Content -Path (Join-Path $dist 'README.txt') -Encoding UTF8

foreach ($name in $forbidden) {
    if (Get-ChildItem $dist -Recurse -Filter $name -ErrorAction SilentlyContinue) {
        throw "Proprietary BricsCAD assembly must not be packaged: $name"
    }
}

$hashLines = Get-ChildItem $dist -File | Where-Object { $_.Name -ne 'SHA256SUMS.txt' } | Sort-Object Name | ForEach-Object {
    $hash = (Get-FileHash $_.FullName -Algorithm SHA256).Hash
    "$hash  $($_.Name)"
}
if (-not $hashLines) { throw 'No package files were available for hashing.' }
$hashLines | Set-Content -Path (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$dist/*" -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host "Package ready: $zip"
Write-Host "Commands: $($commands.Count)"
Write-Host "Plugin signature: $($signature.Status)"
Write-Host "SHA256: $zipHash"
