#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileRuntimeProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-source-reconcile.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
SOURCE_COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileCommands.cs"
SOURCE_SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
RUNBOOK = ROOT / "docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local004-source-reconcile-runtime.md"
errors = []

for path in (COMMAND, RUNNER, HELPER, SOURCE_COMMAND, SOURCE_SERVICE, RUNBOOK, INBOX, CLAIM):
    if not path.is_file():
        errors.append("missing LOCAL-004 Source Reconcile file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DSRTPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTAFTERSYNC1", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTPREPAREROLLBACK", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTCHECKROLLBACK", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTPREPGENERATED", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTCHECKGENERATED", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTPREPAMBIGUOUS", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTCHECKAMBIGUOUS", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTSEEDB", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTCHECKB", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTAFTERFINALSYNC", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTCHECKUNDO", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTCHECKREDO", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTSESSION1", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTREOPEN", CommandFlags.Modal)',
        'Schema = "QS3D_SOURCE_RECONCILE_RUNTIME_V1"',
        'qualification_boundary=LOCAL_004_ONLY',
        'production_local004_qualified=',
        'EditSources(context.Document, owners, secondEdit: false)',
        'EditSources(context.Document, owners, secondEdit: true)',
        'ProjectStateSnapshot.Capture(context.Project)',
        'rollback.Restore(context.Project)',
        'documentB.CloseAndDiscard()',
        'Application.DocumentManager.MdiActiveDocument = state.DocumentA',
        'Path.GetFullPath(state.DocumentA.Name)',
        'Path.GetFullPath(context.Document.Name)',
        'state.DocumentBSourceHandle = CadHandleService.NormalizeHexHandle(id.Handle.ToString())',
        'EntityDigest(documentB, new[] { state.DocumentBSourceHandle })',
        'RequireSemanticMatchesSources(context.Document, owners)',
        'CadHandleService.GetLiveSolidHandles',
        'ProjectDigest(project)',
        'SourceDigest(document, owners)',
        'EntityDigest(document, generated)',
        'NATIVE_UNDO_SEMANTIC_DIVERGENCE',
        'FileMode.CreateNew',
        'File.Move(temp, path)',
    )
    for token in required:
        if token not in text:
            errors.append("LOCAL-004 probe command missing contract token: " + token)
    for forbidden in (
        'SourceReconcileService.ReconcileSelection',
        'GeneratedDependentGeometryInvalidator.Prepare',
        'WallSolidBuilder.BuildSelectedLineWalls',
        'PolylineWallSolidBuilder.BuildSelected',
        'ProjectContextCoordinator.Save(',
        'documentB.Editor.SelectImplied()',
        'ReferenceEquals(state.DocumentA, context.Document)',
    ):
        if forbidden in text:
            errors.append("LOCAL-004 probe bypasses a production command boundary: " + forbidden)
    marker_start = text.find('WriteMarkerAtomic(RequiredPath(ResultVariable')
    marker_end = text.find('});', marker_start)
    marker = text[marker_start:marker_end].lower()
    for forbidden in ('handle=', 'element_id=', 'project_id=', 'drawing_path=', 'profile=', 'source_digest=', 'native_digest='):
        if forbidden in marker:
            errors.append("LOCAL-004 result marker leaks an identity/private field: " + forbidden)
    failure_start = text.find('private static void TryWriteFailure')
    failure_end = text.find('private static void WriteMarkerAtomic', failure_start)
    failure = text[failure_start:failure_end]
    for forbidden in ('.Message', '.StackTrace', '.InnerException', '.GetType('):
        if forbidden in failure:
            errors.append("LOCAL-004 failure marker exposes exception detail: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopies',
        'samples\\generated\\QS3D-Sample.dwg',
        'ArtifactDir must stay outside the repository.',
        'LOCAL-004 qualification requires a clean exact-SHA worktree.',
        'ProductVersion',
        '$expectedAssemblyRevision = "+" + $gitHead',
        'Get-Process -Name "bricscad"',
        '$privateFiles.Count -ne 12',
        '($sidecar + ".bak")',
        '($sidecar + ".lock")',
        'source-a.source-reconcile-probe-copy.dwg',
        'source-b.source-reconcile-probe-copy.dwg',
        'QS3D_SOURCE_RECONCILE_RESULT',
        'QS3D_SOURCE_RECONCILE_PHASE_RESULT',
        'QS3D_SOURCE_RECONCILE_NONCE',
        'QS3D_SOURCE_RECONCILE_DWG_A',
        'QS3D_SOURCE_RECONCILE_DWG_B',
        '"QS3DDRAWWALL"',
        '"QS3DDRAWWALLADV"',
        '"QS3DSYNCSOURCE"',
        '"QS3DBUILD3D"',
        '"INSUNITS", "6", "QS3DSYNCSOURCE", "INSUNITS", "4"',
        '"QS3DSRTPREPGENERATED", "QS3DSYNCSOURCE", "QS3DSRTCHECKGENERATED"',
        '"QS3DSRTPREPAMBIGUOUS", "QS3DSYNCSOURCE", "QS3DSRTCHECKAMBIGUOUS"',
        '"_.OPEN", (\'"\' + $drawingB + \'"\')',
        '"_.UNDO", "1", "QS3DSRTCHECKUNDO", "_.REDO", "QS3DSRTCHECKREDO"',
        '"QS3DSAVE", "_.QSAVE"',
        'QS3D_SOURCE_RECONCILE_RUNTIME_V1',
        'LOCAL_004_ONLY',
        'NATIVE_UNDO_SEMANTIC_DIVERGENCE',
        'Stop-Qs3dLaunchedProcess -Process $processOne',
        'Stop-Qs3dLaunchedProcess -Process $processTwo',
        'Remove-ExactFile -Path $drawingA',
        'Remove-ExactFile -Path $drawingB',
        'private_state_cleanup_verified',
        'drawing_restore_verified',
    )
    for token in required:
        if token not in text:
            errors.append("LOCAL-004 runner missing contract token: " + token)
    ordered = (
        '"QS3DSRTPREPARE"', '"QS3DSYNCSOURCE"', '"QS3DSRTAFTERSYNC1"',
        '"QS3DSRTPREPAREROLLBACK"', '"INSUNITS", "6"', '"QS3DSRTCHECKROLLBACK"',
        '"QS3DSRTPREPGENERATED"', '"QS3DSRTCHECKGENERATED"',
        '"QS3DSRTPREPAMBIGUOUS"', '"QS3DSRTCHECKAMBIGUOUS"',
        '"_.OPEN"', '"QS3DSRTSEEDB"', '"QS3DSRTCHECKB"',
        '"QS3DSRTSELECTSOURCES"', '"QS3DSRTAFTERFINALSYNC"',
        '"_.UNDO"', '"QS3DSRTCHECKUNDO"', '"_.REDO"', '"QS3DSRTCHECKREDO"',
        '"QS3DSRTFINALREBUILD"', '"QS3DSRTSESSION1"', '"QS3DSAVE"', '"_.QSAVE"',
    )
    positions = [text.find(token) for token in ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("LOCAL-004 runner command state machine is not in canonical order")
    metadata_start = text.find('$metadata = [ordered]@{')
    metadata_end = text.find('$metadata | ConvertTo-Json', metadata_start)
    metadata = text[metadata_start:metadata_end].lower()
    for forbidden in ('profile =', 'drawing_path', 'plugin_path', 'artifact_path', 'handle'):
        if forbidden in metadata:
            errors.append("LOCAL-004 metadata contains a private/identity field: " + forbidden)
    for forbidden in ('Get-Process -Name "*"', 'Process.GetProcesses', 'SendKeys', 'SetForegroundWindow'):
        if forbidden in text:
            errors.append("LOCAL-004 runner contains a broad process/window action: " + forbidden)

if SOURCE_COMMAND.is_file() and SOURCE_SERVICE.is_file():
    command = SOURCE_COMMAND.read_text(encoding="utf-8")
    service = SOURCE_SERVICE.read_text(encoding="utf-8")
    for token in ('CommandMethod("QS3DSYNCSOURCE", CommandFlags.UsePickSet)', 'SourceReconcileService.ReconcileSelection(document)'):
        if token not in command:
            errors.append("production Source Reconcile command boundary drifted: " + token)
    ordered = (
        'ProjectStateSnapshot.Capture(project)',
        'GeneratedDependentGeometryInvalidator.Prepare',
        'CadUnitService.TryGetPolicy',
        'RefreshSourceDerivedState',
        'RegenerateAffectedToStable',
        'invalidation.CommitMetadata()',
        'transaction.Commit()',
        'rollback.Restore(project)',
    )
    positions = [service.find(token) for token in ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("production Source Reconcile rollback/invalidation ordering drifted")

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in ('LOCAL-004', 'Status: `ACTIVE`', 'QS3DSYNCSOURCE', 'Undo/Redo', 'INSUNITS'):
        if token not in text:
            errors.append("LOCAL-004 claim missing coordination token: " + token)

print("QS3D LOCAL-004 Source Reconcile runtime-probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-004 automation preserves production authoring/reconcile/rebuild/Undo/Save boundaries, covers success plus generated/ambiguous/multi-DWG refusal and post-invalidation unit-mismatch rollback, and enforces exact-SHA/privacy/cleanup without manufacturing licensed runtime evidence.")
