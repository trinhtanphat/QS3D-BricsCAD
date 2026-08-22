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
if ($dirty) { throw "Repository must be clean before LOCAL-004 P05 qualification.`n$dirty" }

Run 'python' @((Join-Path $RepoRoot 'scripts/preflight-source-reconcile-native-grip-runtime-probe.py'))

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
Write-Host 'LOCAL-004 P05 SOURCE_READY / PENDING_LOCAL'
Write-Host "Exact SHA: $head"
Write-Host 'Use a disposable clean DWG, WCS, millimetre drawing units, and the exact Release plugin above.'
Write-Host ''
Write-Host 'Licensed manual matrix (the probe itself never performs the grip edit):'
Write-Host '  1. NETLOAD exact-SHA QS3D.BricsCAD.V25.dll.'
Write-Host '  2. Run QS3DDRAWBEAM and create one Beam LINE from 0,0 to 5000,0 using 0.3 x 0.5 m defaults.'
Write-Host '  3. Run QS3DSRGRIPP05BASELINE. It must PASS and leave the authoritative source selected.'
Write-Host '  4. Use the real manual endpoint grip on the source LINE, drag toward 8000,0, then press ESC to cancel.'
Write-Host '  5. Run QS3DSRGRIPP05CANCELCHECK. Source/semantic/quantity/generated host must remain exactly 5 m baseline.'
Write-Host '  6. Run QS3DSRGRIPP05SELECT. Use the same real manual endpoint grip again and COMMIT endpoint at 8000,0.'
Write-Host '  7. Run QS3DSRGRIPP05EDITCHECK. Source must be 8 m while semantic/quantities/generated host remain 5 m pre-sync.'
Write-Host '  8. Run QS3DSRGRIPP05SELECT then production QS3DSYNCSOURCE.'
Write-Host '  9. Run QS3DSRGRIPP05SYNCCHECK. Semantic/quantities must be 8 m and the baseline generated host must be invalidated.'
Write-Host ' 10. Run QS3DSRGRIPP05SELECT then production QS3DBUILD3D, then QS3DSRGRIPP05FINAL.'
Write-Host ' 11. SAVE, close BricsCAD, cold reopen the DWG, NETLOAD the same exact-SHA plugin, run QS3DSRGRIPP05REOPEN.'
Write-Host ' 12. The REOPEN PASS must include prior_sequence_reasserted=false and proves persistence only; it does not re-prove the pre-restart sequence.'
Write-Host ' 13. Record all six sanitized PASS phase markers for baseline, cancel_check, edit_check, sync_check, final, and reopen in issue #3532.'
Write-Host ''
Write-Host 'A full P05 qualification requires all six sanitized PASS phase markers from the same exact candidate/run. REOPEN alone is never LOCAL_PASS evidence.'
Write-Host 'Do NOT publish LOCAL_PASS from source/build alone. The real manual endpoint grip + ESC/commit behavior is the licensed boundary.'
