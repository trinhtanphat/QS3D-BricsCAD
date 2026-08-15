#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileRuntimeProbeCommands.cs"
MARKER_PROBE = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcilePostUndoMarkerProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-source-reconcile.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
SOURCE_COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileCommands.cs"
SOURCE_SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
RUNBOOK = ROOT / "docs/SOURCE-RECONCILE-GENERATED-OUTPUTS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local004-source-reconcile-runtime.md"
DISCRIMINATOR_CLAIM = ROOT / "docs/agent-work-claims/2026-08-14-gpt56sol-issue1005-post-undo-marker-discriminator.md"
SUCCESSOR_CLAIM = ROOT / "docs/agent-work-claims/2026-08-15-codex-local004-post-undo-marker-classification.md"
errors = []

for path in (COMMAND, MARKER_PROBE, RUNNER, HELPER, SOURCE_COMMAND, SOURCE_SERVICE, RUNBOOK, INBOX, CLAIM, DISCRIMINATOR_CLAIM, SUCCESSOR_CLAIM):
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
        '"success_reconcile_count=3"',
        'SourceReconcileUndoCoordinator.CaptureSanitizedState(context.Document, context.Project)',
        'afterUndo.CompareMarkerTo(beforeUndo)',
        'final_selection_class=',
        'final_owner_match_class=',
        'final_generated_state=',
        'final_project_state=',
        'final_revision_state=',
        'final_native_marker_state=',
        'final_history_before_state=',
        'final_history_after_state=',
        'final_history_entry_before_class=',
        'final_history_entry_after_class=',
        'FileMode.CreateNew',
        'File.Move(temp, path)',
    )
    for token in required:
        if token not in text:
            errors.append("LOCAL-004 probe command missing contract token: " + token)
    if text.count('"success_reconcile_count=3"') != 2:
        errors.append("LOCAL-004 session and cold-reopen markers must report exactly three successful reconciles")
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

if MARKER_PROBE.is_file():
    text = MARKER_PROBE.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DSRTMARKERBEFOREFINAL", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTMARKERAFTERFINAL", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTMARKERAFTERUNDO", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTMARKERAFTERREDO", CommandFlags.Modal)',
        'CommandMethod("QS3DSRTMARKERPUBLISH", CommandFlags.Modal)',
        'SourceReconcileUndoCoordinator.CaptureSanitizedState(context.Document, context.Project)',
        'ClassifyMarker(current, state.PreFinal, postFinal)',
        'current.CompareMarkerTo(before)',
        'current.CompareMarkerTo(after)',
        '"post_undo_marker_class="',
        '"post_redo_marker_class="',
        '"BEFORE"',
        '"AFTER"',
        '"OTHER_OR_INVALID"',
        '"UNCHANGED"',
        '"MISSING_OR_INVALID"',
        'FileMode.CreateNew',
        'File.Replace(temp, path, null)',
        'QS3D_SOURCE_RECONCILE_MARKER_RESULT',
        'source-reconcile-post-undo-marker.txt',
        'QS3D_SOURCE_RECONCILE_POST_UNDO_MARKER_V1',
        'QS3D_SOURCE_RECONCILE_NONCE',
        'yield return "qualification_boundary=" + Boundary',
    )
    for token in required:
        if token not in text:
            errors.append("LOCAL-004 post-Undo marker discriminator missing contract token: " + token)
    lower = text.lower()
    for forbidden in (
        'native_revision=', 'revision_token=', 'project_id=', 'drawing_path=', 'handle=',
        '.message', '.stacktrace', '.innerexception', 'sourcereconcileundocoordinator.readrevision',
    ):
        if forbidden in lower:
            errors.append("LOCAL-004 post-Undo marker discriminator leaks/bypasses private marker state: " + forbidden)
    for forbidden in (
        'post_undo_marker_vs_pre_final_state',
        'post_undo_marker_vs_post_final_state',
        'post_redo_marker_vs_pre_final_state',
        'post_redo_marker_vs_post_final_state',
    ):
        if forbidden in text:
            errors.append("LOCAL-004 post-Undo marker discriminator retains an ambiguous comparison field: " + forbidden)
    marker_lines_start = text.find('private static IEnumerable<string> MarkerLines')
    marker_lines_end = text.find('private static void WriteNew', marker_lines_start)
    marker_lines = text[marker_lines_start:marker_lines_end].lower()
    for forbidden in (
        'revision=', 'id=', 'handle=', 'path=', 'message=', 'count=',
        'project=', 'document=', 'source=', 'generated=',
    ):
        if forbidden in marker_lines:
            errors.append("LOCAL-004 post-Undo marker diagnostic exposes a forbidden field: " + forbidden)

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
        'QS3D_SOURCE_RECONCILE_MARKER_RESULT',
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
        '"QS3DSRTMARKERBEFOREFINAL"',
        '"QS3DSRTMARKERAFTERFINAL"',
        '"QS3DSRTMARKERAFTERUNDO"',
        '"QS3DSRTMARKERAFTERREDO"',
        '"QS3DSRTMARKERPUBLISH"',
        '"_.UNDO", "_Mark"',
        '"_.UNDO", "_Back"',
        '"_.UNDO", "_Begin"',
        '"_.UNDO", "_End", "_.UNDO", "1", "_.REDO"',
        'Require-Qs3dValue -Marker $phaseMarker -Key "success_reconcile_count" -Expected "3"',
        '"QS3DSAVE", "_.QSAVE"',
        'QS3D_SOURCE_RECONCILE_RUNTIME_V1',
        'LOCAL_004_ONLY',
        'NATIVE_UNDO_SEMANTIC_DIVERGENCE',
        'Require-FinalReconcileDiagnostics',
        'Require-PostUndoMarkerDiagnostics',
        'source-reconcile-post-undo-marker.txt',
        'QS3D_SOURCE_RECONCILE_POST_UNDO_MARKER_V1',
        'post_undo_marker_class',
        'post_redo_marker_class',
        '@("BEFORE", "AFTER", "OTHER_OR_INVALID")',
        'post_undo_marker_diagnostic = $markerDiagnostic',
        'if (@($Marker.Keys).Count -ne $expectedKeys.Count)',
        'Require-PostUndoMarkerDiagnostics -Marker $markerDiagnostic -Nonce $nonce -RequireRedo',
        '@("BOTH_SOURCES", "LINE_ONLY", "POLY_ONLY", "OTHER_OR_MISSING")',
        '@("REMOVED_ALL", "RETAINED_ALL", "PARTIAL")',
        '@("NONE", "SYNCED", "MARKER_MISMATCH", "DESYNCHRONIZED")',
        '@("ONE", "MULTIPLE")',
        'phase_marker = $phaseMarker',
        'function Find-Qs3dHandoffProcess',
        'function Wait-Qs3dHandoffProcess',
        'function Stop-Qs3dLateHandoffProcesses',
        'ParentProcessId = " + $LauncherId',
        '[string]::Equals($candidatePath, $ExpectedExecutable, [StringComparison]::OrdinalIgnoreCase)',
        'Wait-Qs3dMarkerOrFailure -Process ([ref]$processOne)',
        'Wait-Qs3dMarkerOrFailure -Process ([ref]$processTwo)',
        'Stop-Qs3dLateHandoffProcesses -LauncherIds @($launcherOneId, $launcherTwoId)',
        'Stop-Qs3dLaunchedProcess -Process $processOne',
        'Stop-Qs3dLaunchedProcess -Process $processTwo',
        'Remove-ExactFile -Path $drawingA',
        'Remove-ExactFile -Path $drawingB',
        'private_state_cleanup_verified',
        'drawing_restore_verified',
        'launcher_handoffs',
    )
    for token in required:
        if token not in text:
            errors.append("LOCAL-004 runner missing contract token: " + token)
    prefix_ordered = (
        '"QS3DSRTPREPARE"', '"QS3DSYNCSOURCE"', '"QS3DSRTAFTERSYNC1"',
        '"QS3DSRTPREPAREROLLBACK"', '"INSUNITS", "6"', '"QS3DSRTCHECKROLLBACK"',
        '"QS3DSRTPREPGENERATED"', '"QS3DSRTCHECKGENERATED"',
        '"QS3DSRTPREPAMBIGUOUS"', '"QS3DSRTCHECKAMBIGUOUS"',
        '"_.OPEN"', '"QS3DSRTSEEDB"', '"QS3DSRTCHECKB"',
        '"QS3DSRTMARKERBEFOREFINAL"',
    )
    positions = [text.find(token) for token in prefix_ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("LOCAL-004 runner pre-Undo state machine is not in canonical order")

    compact = "".join(text.split())
    explicit_cycles = (
        '"QS3DSRTMARKERBEFOREFINAL","QS3DSRTSELECTSOURCES","_.UNDO","_Mark",'
        '"QS3DSYNCSOURCE","QS3DSRTAFTERFINALSYNC","QS3DSRTMARKERAFTERFINAL",'
        '"_.UNDO","_Back","QS3DSRTMARKERAFTERUNDO","QS3DSRTCHECKUNDO",'
        '"QS3DSRTSELECTSOURCES","_.UNDO","_Begin","QS3DSYNCSOURCE",'
        '"QS3DSRTAFTERFINALSYNC","QS3DSRTMARKERAFTERFINAL","_.UNDO","_End",'
        '"_.UNDO","1","_.REDO","QS3DSRTMARKERAFTERREDO","QS3DSRTCHECKREDO"'
    )
    if explicit_cycles not in compact:
        errors.append("LOCAL-004 runner must isolate Undo with Mark/Back and Redo with direct grouped UNDO/REDO")
    if (
        text.count('"QS3DSYNCSOURCE"') != 7
        or text.count('"QS3DSRTSELECTSOURCES"') != 2
        or text.count('"QS3DSRTAFTERFINALSYNC"') != 2
        or text.count('"QS3DSRTMARKERAFTERFINAL"') != 2
        or text.count('"_.UNDO"') != 5
        or text.count('"_.U"') != 0
        or text.count('"_.REDO"') != 1
        or compact.count('"_.UNDO","1"') != 1
    ):
        errors.append("LOCAL-004 runner explicit Undo/Redo command cardinality drifted")

    cycles_end = text.find('"QS3DSRTCHECKREDO"', text.find('"_.REDO"'))
    suffix_ordered = (
        '"QS3DSRTFINALREBUILD"', '"QS3DSRTSESSION1"', '"QS3DSRTMARKERPUBLISH"', '"QS3DSAVE"', '"_.QSAVE"',
    )
    suffix_positions = [text.find(token, cycles_end) for token in suffix_ordered]
    if cycles_end < 0 or any(position < 0 for position in suffix_positions) or suffix_positions != sorted(suffix_positions):
        errors.append("LOCAL-004 runner post-Redo state machine is not in canonical order")
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
    for token in ('LOCAL-004', 'Status: `ACTIVE`', 'QS3DSYNCSOURCE', 'Undo/Redo', 'INSUNITS', 'post-Undo marker discriminator split'):
        if token not in text:
            errors.append("LOCAL-004 claim missing coordination token: " + token)

if DISCRIMINATOR_CLAIM.is_file():
    text = DISCRIMINATOR_CLAIM.read_text(encoding="utf-8")
    for token in ('LOCAL-004', 'gpt56sol-source-reconcile-desync-agent', 'post-Undo marker discriminator'):
        if token not in text:
            errors.append("LOCAL-004 discriminator claim missing coordination token: " + token)

if SUCCESSOR_CLAIM.is_file():
    text = SUCCESSOR_CLAIM.read_text(encoding="utf-8")
    for token in ('LOCAL-004', 'Status: `COMPLETED`', 'BEFORE', 'AFTER', 'OTHER_OR_INVALID', 'no production fix is implemented'):
        if token not in text:
            errors.append("LOCAL-004 successor discriminator claim missing coordination token: " + token)

print("QS3D LOCAL-004 Source Reconcile runtime-probe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: additive LOCAL-004 automation preserves production authoring/reconcile/rebuild/Undo/Save boundaries, captures only BEFORE/AFTER/OTHER_OR_INVALID post-Undo/post-Redo marker classes in a dedicated exact-key diagnostic, covers success plus generated/ambiguous/multi-DWG refusal and post-invalidation unit-mismatch rollback, and enforces exact-SHA/privacy/cleanup without manufacturing licensed runtime evidence.")
