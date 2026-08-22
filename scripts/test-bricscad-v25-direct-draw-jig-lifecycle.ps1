param(
    [string]$RepoRoot = (Split-Path -Parent $PSScriptRoot),
    [string]$BricsCadV25Dir = $env:BRICSCAD_V25_DIR
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Run([string]$File, [string[]]$ArgumentList) {
    & $File @ArgumentList
    if ($LASTEXITCODE -ne 0) { throw "$File failed with exit code $LASTEXITCODE" }
}

$RepoRoot = (Resolve-Path $RepoRoot).Path
$head = (& git -C $RepoRoot rev-parse HEAD).Trim()
if (-not $head) { throw 'Cannot resolve exact repository HEAD.' }

$dirty = (& git -C $RepoRoot status --porcelain=v1 --untracked-files=all) -join "`n"
if ($dirty) { throw "Repository must be clean before LOCAL-008 P02 qualification.`n$dirty" }

Run 'python' @((Join-Path $RepoRoot 'scripts/preflight-direct-draw-jig-runtime-probe.py'))

if (-not $BricsCadV25Dir) {
    throw 'BRICSCAD_V25_DIR is required. Point it to the licensed BricsCAD V25 installation directory.'
}
if (-not (Test-Path (Join-Path $BricsCadV25Dir 'BrxMgd.dll'))) {
    throw "BrxMgd.dll not found under BRICSCAD_V25_DIR: $BricsCadV25Dir"
}

Run 'dotnet' @(
    'build', (Join-Path $RepoRoot 'src/QS3D.BricsCAD.V25/QS3D.BricsCAD.V25.csproj'),
    '-c', 'Release',
    "-p:BRICSCAD_V25_DIR=$BricsCadV25Dir"
)

Write-Host ''
Write-Host 'LOCAL-008 P02 SOURCE_READY / PENDING_LOCAL'
Write-Host "Exact SHA: $head"
Write-Host 'Licensed runtime steps:'
Write-Host '  1. NETLOAD the Release QS3D.BricsCAD.V25.dll built from the exact SHA above.'
Write-Host '  2. Start from a disposable DWG and record DB object count / visible model state before the probe.'
Write-Host '  3. Run QS3DPROBEDIRECTDRAWJIG.'
Write-Host '  4. Move the cursor before each click and visually confirm the full profile strip follows the cursor.'
Write-Host '  5. Click at least three successive endpoints in one command invocation.'
Write-Host '  6. Repeat once terminating with Enter and once terminating with ESC.'
Write-Host '  7. Confirm no LINE/POLYLINE/SOLID/XData/QS3D ownership residue is created by the probe.'
Write-Host '  8. Confirm marker starts QS3D_DIRECT_DRAW_JIG_RUNTIME_V1, coordinate_model=EDITOR_UCS_TO_JIG_WCS_UCS_PLANE and persistent_writes=0.'
Write-Host '  9. Repeat under a rotated or translated UCS. Confirm the first point and every subsequent jig point remain anchored to the picked cursor points; switching active documents must fail closed.'
Write-Host ' 10. Switch back to WCS and repeat to confirm the same cursor-following geometry without preview residue.'
Write-Host ''
Write-Host 'Do NOT publish LOCAL_PASS for #74 from source/build alone. Attach the sanitized exact-SHA runtime observation to issue #3530.'
