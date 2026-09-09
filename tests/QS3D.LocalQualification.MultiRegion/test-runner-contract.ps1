$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$runner = Join-Path $PSScriptRoot 'Run-Local005NativeMultiRegion.ps1'
$probe = Join-Path $PSScriptRoot 'Local005NativeMultiRegionProbeCommands.cs'
$project = Join-Path $PSScriptRoot 'QS3D.LocalQualification.MultiRegion.csproj'
foreach ($path in @($runner,$probe,$project)) { if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw ('FAIL: missing LOCAL-005 harness file: ' + (Split-Path $path -Leaf)) } }

$runnerText = Get-Content -LiteralPath $runner -Raw
$probeText = Get-Content -LiteralPath $probe -Raw
$projectText = Get-Content -LiteralPath $project -Raw

$orderedCommands = @('QL005DUMPEX','QL005VERIFY','QS3DMULTIREBARHEALTH','QS3DSAVE','_.QSAVE','QL005SAVED')
$cursor = -1
foreach ($command in $orderedCommands) {
    $needle = $command + '\n'
    $next = $probeText.IndexOf($needle, $cursor + 1, [StringComparison]::Ordinal)
    if ($next -lt 0) { throw ('FAIL: queued production tail missing member ' + $command) }
    if ($next -le $cursor) { throw 'FAIL: LOCAL-005 queued production tail is not ordered.' }
    $cursor = $next
}
if ($runnerText.IndexOf("Invoke-HostPhase 'run' @('QL005SETUP') @('setup','run','saved')", [StringComparison]::Ordinal) -lt 0) { throw 'FAIL: run phase must leave production dispatch to the setup-owned queued tail.' }
$setPickfirst = $probeText.IndexOf('context.Document.Editor.SetImpliedSelection(ids);', [StringComparison]::Ordinal)
$realCommand = $probeText.IndexOf('context.Document.Editor.Command("QS3DSLABREBAR3DMULTI");', [StringComparison]::Ordinal)
$queuedTail = $probeText.IndexOf('QueueProductionTail(context.Document, sourceLayer);', [StringComparison]::Ordinal)
if ($setPickfirst -lt 0 -or $realCommand -le $setPickfirst -or $queuedTail -le $realCommand) { throw 'FAIL: LOCAL-005 must execute real production synchronously after PICKFIRST and before queued verification.' }
if ($probeText.IndexOf('(command \"QS3DSLABREBAR3DMULTI\")', [StringComparison]::Ordinal) -ge 0 -or $probeText.IndexOf('\"QS3DSLABREBAR3DMULTI\\n\" +', [StringComparison]::Ordinal) -ge 0) { throw 'FAIL: production command must not be dispatched through queued AutoLISP/text.' }
foreach ($queueToken in @('QueueProductionTail(context.Document, sourceLayer);','SendStringToExecute(','QL005DUMPEX\n','QL005VERIFY\n')) {
    if ($probeText.IndexOf($queueToken, [StringComparison]::Ordinal) -lt 0) { throw ('FAIL: LOCAL-005 queued production tail missing ' + $queueToken) }
}
if ($runnerText.IndexOf("Invoke-HostPhase 'reopen'", [StringComparison]::Ordinal) -lt 0 -or
    $runnerText.IndexOf("'QL005REOPEN'", [StringComparison]::Ordinal) -lt 0) { throw 'FAIL: cold reopen phase missing.' }
if ($runnerText.IndexOf('Remove-Item -LiteralPath $privateRoot -Recurse -Force', [StringComparison]::Ordinal) -lt 0) { throw 'FAIL: exact private cleanup missing.' }
if ($runnerText.IndexOf("'LOCAL_PASS_BOUNDED'", [StringComparison]::Ordinal) -lt 0 -or
    $runnerText.IndexOf("'PENDING_LOCAL'", [StringComparison]::Ordinal) -ge 0) { throw 'FAIL: runner status contract is ambiguous.' }

# Cleanup is destructive to the nonce profile/private allocation. It must be
# gated on a process-wide zero-BricsCAD proof, matching the established LOCAL
# native runner pattern. A timeout/failure must retain private evidence instead
# of restoring/deleting state while an owned host may still be using it.
foreach ($requiredCleanupGuard in @(
    '$zeroHosts = $false',
    '$zeroHosts = $true',
    'if ($zeroHosts) {',
    'profile_restore_skipped_host_active',
    'private_cleanup_skipped_host_active')) {
    if ($runnerText.IndexOf($requiredCleanupGuard, [StringComparison]::Ordinal) -lt 0) {
        throw ('FAIL: LOCAL-005 cleanup is not fail-closed on zero-host proof: ' + $requiredCleanupGuard)
    }
}
$zeroProof = $runnerText.IndexOf('$zeroHosts = $true', [StringComparison]::Ordinal)
$profileRestore = $runnerText.IndexOf('Restore-Qs3dV25ProfileSandbox', [StringComparison]::Ordinal)
$privateDelete = $runnerText.IndexOf('Remove-Item -LiteralPath $privateRoot -Recurse -Force', [StringComparison]::Ordinal)
if ($zeroProof -lt 0 -or $profileRestore -le $zeroProof -or $privateDelete -le $zeroProof) {
    throw 'FAIL: destructive cleanup appears before zero-host proof.'
}

foreach ($required in @(
    '[CommandMethod("QL005SETUP"',
    '[CommandMethod("QL005VERIFY"',
    '[CommandMethod("QL005SAVED"',
    '[CommandMethod("QL005REOPEN"',
    'synthetic_two_disjoint_regions',
    'synthetic_one_hole',
    'GeneratedSlabMeshMultiRegionSourceManifest',
    'GeneratedSlabMeshMultiRegionGeneratedManifest',
    'GeneratedSlabMeshMultiRegionTopologyFingerprint',
    'QS3D_REBAR',
    'QS3D_REBAR_REGION',
    'bar_intrudes_hole_interior',
    'runtime_health_no_errors',
    'cold_reopen_project_bind')) {
    if ($probeText.IndexOf($required, [StringComparison]::Ordinal) -lt 0) { throw ('FAIL: probe contract missing ' + $required) }
}
if ($runnerText.IndexOf("'QL005ARMEX'", [StringComparison]::Ordinal) -ge 0) { throw 'FAIL: diagnostic arming command must not sit between PICKFIRST setup and production.' }
if ($runnerText.IndexOf("'_.SELECT'", [StringComparison]::Ordinal) -ge 0 -or $runnerText.IndexOf("'PICKFIRST'", [StringComparison]::Ordinal) -ge 0) {
    throw 'FAIL: script-level selection commands must not interpose between setup and production.'
}
foreach ($fixtureToken in @('QS3D_LOCAL005_SOURCE_','polyline.Layer = sourceLayer')) {
    if ($probeText.IndexOf($fixtureToken, [StringComparison]::Ordinal) -lt 0) { throw ('FAIL: LOCAL-005 synthetic source isolation missing ' + $fixtureToken) }
}
foreach ($selectionHookToken in @('CommandWillStart','OnProductionCommandWillStart','SetImpliedSelection(ids)','QS3DSLABREBAR3DMULTI')) {
    if ($probeText.IndexOf($selectionHookToken, [StringComparison]::Ordinal) -lt 0) { throw ('FAIL: LOCAL-005 command-start selection hook missing ' + $selectionHookToken) }
}
foreach ($diagnosticToken in @('ArmProductionExceptionDiagnostic(context);','[CommandMethod("QL005DUMPEX"','FirstChanceException','local005-production-exception.private.txt','selection_hook_observed=','selection_hook_count=','project_readonly_available=','generated_handles_present=')) {
    if ($probeText.IndexOf($diagnosticToken, [StringComparison]::Ordinal) -lt 0) { throw ('FAIL: production exception diagnostic contract missing ' + $diagnosticToken) }
}
if ($probeText.IndexOf('SlabFoundationMultiRegionMeshSolidBuilder', [StringComparison]::Ordinal) -ge 0) {
    throw 'FAIL: test probe must not call the internal builder directly.'
}
foreach ($demandLoadToken in @(
    '$demandLoadRegistryPath',
    '$demandLoadOriginalControls',
    '$demandLoadIsolatedControls',
    'Set-Qs3dDemandLoadControls',
    'Restore-Qs3dDemandLoadControls',
    'demandload_restore_skipped_host_active')) {
    if ($runnerText.IndexOf($demandLoadToken, [StringComparison]::Ordinal) -lt 0) {
        throw ('FAIL: LOCAL-005 startup DemandLoad isolation contract missing ' + $demandLoadToken)
    }
}
$demandLoadSet = $runnerText.IndexOf('Set-Qs3dDemandLoadControls', [StringComparison]::Ordinal)
$hostStart = $runnerText.IndexOf('Start-Process -FilePath $bricscadExe', [StringComparison]::Ordinal)
$demandLoadRestore = $runnerText.LastIndexOf('Restore-Qs3dDemandLoadControls', [StringComparison]::Ordinal)
if ($demandLoadSet -lt 0 -or $hostStart -le $demandLoadSet -or $demandLoadRestore -le $zeroProof) {
    throw 'FAIL: LOCAL-005 DemandLoad isolation/restore ordering is unsafe.'
}
if ($probeText.IndexOf('using System.Diagnostics;', [StringComparison]::Ordinal) -lt 0) { throw 'FAIL: probe host identity dependency is not explicit.' }
if ($probeText.IndexOf('catch(System.Exception error)', [StringComparison]::Ordinal) -lt 0 -or
    $probeText.IndexOf('catch(Exception error)', [StringComparison]::Ordinal) -ge 0) {
    throw 'FAIL: probe exception type must be explicitly System.Exception to avoid Teigha ambiguity.'
}
if ($probeText.IndexOf('var value=raw!;', [StringComparison]::Ordinal) -lt 0) {
    throw 'FAIL: RequiredPath nullable flow must be explicitly narrowed before Trim/GetFullPath.'
}
foreach ($reference in @('QS3D.Core','QS3D.BricsCAD.V25','BrxMgd','TD_Mgd')) {
    if ($projectText.IndexOf('<Reference Include="' + $reference + '">', [StringComparison]::Ordinal) -lt 0) { throw ('FAIL: project missing reference ' + $reference) }
}

'PASS LOCAL-005 runner contract'
