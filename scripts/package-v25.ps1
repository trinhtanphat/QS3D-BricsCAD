$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'src/QS3D.BricsCAD.V25/bin/Release/net48'
$dist = Join-Path $root 'dist/QS3D-BricsCAD-V25'
$required = @('QS3D.BricsCAD.V25.dll','QS3D.Core.dll')
if (-not (Test-Path $source)) { throw "V25 Release output was not found: $source" }
Remove-Item $dist -Recurse -Force -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $dist -Force | Out-Null
foreach ($name in $required) { $path = Join-Path $source $name; if (-not (Test-Path $path)) { throw "Missing build artifact: $path" }; Copy-Item $path (Join-Path $dist $name) }
foreach ($forbidden in @('BrxMgd.dll','TD_Mgd.dll','TD_MgdBrep.dll')) { if (Get-ChildItem $dist -Recurse -Filter $forbidden -ErrorAction SilentlyContinue) { throw "Proprietary BricsCAD assembly must not be packaged: $forbidden" } }
@"
QS3D for BricsCAD V25

1. Start BricsCAD V25 x64.
2. Run NETLOAD.
3. Select QS3D.BricsCAD.V25.dll from this folder.
4. Run QS3D.

Do not redistribute BricsCAD runtime assemblies with this package.
"@ | Set-Content -Path (Join-Path $dist 'README.txt') -Encoding UTF8
$zip = Join-Path $root 'dist/QS3D-BricsCAD-V25.zip'
Remove-Item $zip -Force -ErrorAction SilentlyContinue
Compress-Archive -Path "$dist/*" -DestinationPath $zip -CompressionLevel Optimal
Write-Host "Package ready: $zip"
