$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/QS3D.BricsCAD.V25/bin/Release/net48'
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

foreach ($name in $forbidden) {
    if (Get-ChildItem $dist -Recurse -Filter $name -ErrorAction SilentlyContinue) {
        throw "Proprietary BricsCAD assembly must not be packaged: $name"
    }
}

@"
QS3D for BricsCAD V25 x64

1. Start BricsCAD V25.
2. Run NETLOAD.
3. Select QS3D.BricsCAD.V25.dll.
4. Run QS3D or QS3DDOMAIN.

This package intentionally excludes BricsCAD runtime assemblies.
Native Solid3d paths require the real V25 runtime gate before release qualification.
"@ | Set-Content -Path (Join-Path $dist 'README.txt') -Encoding UTF8

$hashLines = foreach ($name in $required) {
    $artifact = Join-Path $dist $name
    $hash = (Get-FileHash $artifact -Algorithm SHA256).Hash
    "$hash  $name"
}
$hashLines | Set-Content -Path (Join-Path $dist 'SHA256SUMS.txt') -Encoding ASCII

Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$dist/*" -DestinationPath $zip -CompressionLevel Optimal
$zipHash = (Get-FileHash $zip -Algorithm SHA256).Hash
Write-Host "Package ready: $zip"
Write-Host "SHA256: $zipHash"
