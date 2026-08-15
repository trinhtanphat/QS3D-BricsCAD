#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoLifecycleProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-source-reconcile-undo-lifecycle.ps1"
HELPER = ROOT / "scripts/bricscad-runner-window-interop.ps1"
CLAIM = ROOT / "docs/agent-work-claims/2026-08-13-codex-local004-source-reconcile-runtime.md"
COORDINATOR = ROOT / "src/QS3D.BricsCAD.V25/SourceReconcileUndoCoordinator.cs"
SERVICE = ROOT / "src/QS3D.BricsCAD.V25/Services/SourceReconcileService.cs"
errors = []

for path in (COMMAND, RUNNER, HELPER, CLAIM, COORDINATOR, SERVICE):
    if not path.is_file():
        errors.append("missing Source Reconcile Undo lifecycle surface: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    required = (
        'CommandMethod("QS3DSRULPREPARE", CommandFlags.Modal)',
        'CommandMethod("QS3DSRULMUTATE", CommandFlags.Modal)',
        'CommandMethod("QS3DSRULCHECKUNDO", CommandFlags.Modal)',
        'Schema = "QS3D_SOURCE_UNDO_LIFECYCLE_V1"',
        'Boundary = "LOCAL_004_DIAGNOSTIC_ONLY"',
        'ObjectOnly', 'DbEnableObject', 'DbStartObject', 'DbEnableDbStartObject',
        'database.DisableUndoRecording(false)',
        'database.StartUndoRecord()',
        'state.DatabaseRecordingAtEntry = BooleanClass(database.UndoRecording)',
        'state.DatabaseRecordingAfterEnable = BooleanClass(database.UndoRecording)',
        'state.DatabaseRecordingAfterStart = BooleanClass(database.UndoRecording)',
        'carrier.DisableUndoRecording(false)',
        'carrier.UpgradeOpen()',
        'WriteMarker(carrier, AfterToken)',
        'state.SentinelId = modelSpace.AppendEntity(sentinel)',
        'transaction.AddNewlyCreatedDBObject(sentinel, true)',
        'transaction.Commit()',
        'existing_after_undo=',
        'topology_after_undo=',
        'production_local004_qualified=false',
        'FileMode.CreateNew',
        'File.Move(temp, path)',
    )
    for token in required:
        if token not in text:
            errors.append("Undo lifecycle command missing contract token: " + token)

    mutate_start = text.find('public void Mutate()')
    mutate_end = text.find('[CommandMethod("QS3DSRULCHECKUNDO"', mutate_start)
    mutate = text[mutate_start:mutate_end]
    ordered = (
        'state.DatabaseRecordingAtEntry = BooleanClass(database.UndoRecording)',
        'database.DisableUndoRecording(false)',
        'database.StartUndoRecord()',
        'using (var transaction = database.TransactionManager.StartTransaction())',
        'carrier.DisableUndoRecording(false)',
        'carrier.UpgradeOpen()',
        'WriteMarker(carrier, AfterToken)',
        'state.SentinelId = modelSpace.AppendEntity(sentinel)',
        'transaction.AddNewlyCreatedDBObject(sentinel, true)',
        'transaction.Commit()',
    )
    positions = [mutate.find(token) for token in ordered]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Undo lifecycle mutation order drifted")

    for forbidden in (
        'SourceReconcileService.',
        'SourceReconcileUndoCoordinator.',
        'ProjectContextCoordinator.',
        'GeneratedDependentGeometryInvalidator.',
        '.Message', '.StackTrace', '.InnerException',
        'document.SendStringToExecute',
    ):
        if forbidden in text:
            errors.append("Undo lifecycle command crosses its diagnostic boundary: " + forbidden)

    result_start = text.find('WriteNew(RequiredResultPath()')
    result_end = text.find('});', result_start)
    result = text[result_start:result_end].lower()
    for forbidden in (
        'handle=', 'id=', 'path=', 'revision=', 'message=', 'count=',
        'project=', 'document=', 'drawing=', 'source=', 'generated=',
    ):
        if forbidden in result:
            errors.append("Undo lifecycle result leaks a forbidden field: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    required = (
        '[switch]$ConfirmDisposableCopies',
        'samples\\generated\\QS3D-Sample.dwg',
        'ArtifactDir must stay outside the repository.',
        'requires a clean exact-SHA worktree.',
        '$expectedRevision = "+" + $gitHead',
        'ProductVersion',
        'Get-Process -Name "bricscad"',
        'Start-Process -FilePath $bricscadExe',
        '-WindowStyle Hidden',
        'function Find-Qs3dHandoffProcess',
        'function Wait-Qs3dHandoffProcess',
        'function Stop-Qs3dLaunchedProcess',
        'ParentProcessId = " + $LauncherId',
        '$variants = @("OBJECT_ONLY", "DB_ENABLE_OBJECT", "DB_START_OBJECT", "DB_ENABLE_DB_START_OBJECT")',
        'source-undo-lifecycle-probe-copy.dwg',
        'source-undo-lifecycle-result.txt',
        'source-undo-lifecycle.private.scr',
        '"QS3DSRULPREPARE", "QS3DSRULMUTATE"',
        '"_.UNDO", "1", "QS3DSRULCHECKUNDO"',
        '"_.CLOSE", "_N"',
        '"_.QUIT", "_N"',
        'QS3D_SOURCE_UNDO_MATRIX_RESULT',
        'QS3D_SOURCE_UNDO_MATRIX_NONCE',
        'QS3D_SOURCE_UNDO_MATRIX_VARIANT',
        'QS3D_SOURCE_UNDO_MATRIX_DWG',
        'Require-Qs3dPassMarker',
        '@("ON", "OFF", "NOT_RUN")',
        '@("BEFORE", "AFTER", "OTHER_OR_INVALID")',
        '@("UNDONE", "PRESENT", "OTHER_OR_INVALID")',
        'production_local004_qualified = $false',
        'process_cleanup_verified',
        'script_cleanup_verified',
        'private_state_cleanup_verified',
        'drawing_cleanup_verified',
        '$metadataVariants = @($results | ForEach-Object { $_ })',
        '$metadataError = $_',
        'variants = $metadataVariants',
        'LOCAL-004 Undo lifecycle metadata publication failed.',
    )
    for token in required:
        if token not in text:
            errors.append("Undo lifecycle runner missing contract token: " + token)

    if 'variants = @($results)' in text:
        errors.append("Undo lifecycle runner regressed to Windows PowerShell 5.1-incompatible generic-list array materialization")

    sequence = (
        '"QS3DSRULPREPARE", "QS3DSRULMUTATE"',
        '"_.UNDO", "1", "QS3DSRULCHECKUNDO"',
        '"_.CLOSE", "_N"',
        '"_.QUIT", "_N"',
    )
    positions = [text.find(token) for token in sequence]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Undo lifecycle runner command order drifted")

    metadata_start = text.find('$metadata = [ordered]@{')
    metadata_end = text.find('$metadata | ConvertTo-Json', metadata_start)
    metadata = text[metadata_start:metadata_end].lower()
    for forbidden in ('profile =', 'drawing_path', 'plugin_path', 'artifact_path', 'handle', 'project_id'):
        if forbidden in metadata:
            errors.append("Undo lifecycle metadata contains a private/identity field: " + forbidden)

    materialize_position = text.find('$metadataVariants = @($results | ForEach-Object { $_ })')
    metadata_build_position = text.find('$metadata = [ordered]@{')
    metadata_write_position = text.find('$metadata | ConvertTo-Json', metadata_build_position)
    metadata_catch_position = text.find('$metadataError = $_', metadata_write_position)
    if min(materialize_position, metadata_build_position, metadata_write_position, metadata_catch_position) < 0 or not (
        materialize_position < metadata_build_position < metadata_write_position < metadata_catch_position
    ):
        errors.append("Undo lifecycle runner metadata materialization/publication error boundary drifted")

    error_priority = (
        'if ($null -ne $cleanupError) { throw "LOCAL-004 Undo lifecycle cleanup failed." }',
        'if ($null -ne $qualificationError) { throw $qualificationError }',
        'if ($null -ne $metadataError) { throw "LOCAL-004 Undo lifecycle metadata publication failed." }',
    )
    positions = [text.find(token) for token in error_priority]
    if any(position < 0 for position in positions) or positions != sorted(positions):
        errors.append("Undo lifecycle runner must preserve cleanup then qualification then metadata error precedence")

    for forbidden in ('Get-Process -Name "*"', 'Process.GetProcesses', 'SendKeys', 'SetForegroundWindow'):
        if forbidden in text:
            errors.append("Undo lifecycle runner contains a broad process/window action: " + forbidden)

if CLAIM.is_file():
    text = CLAIM.read_text(encoding="utf-8")
    for token in (
        'database Undo lifecycle diagnostic matrix',
        'SourceReconcileUndoLifecycleProbeCommands.cs',
        'test-bricscad-v25-source-reconcile-undo-lifecycle.ps1',
        'preflight-source-reconcile-undo-lifecycle-probe.py',
        '`OBJECT_ONLY`', '`DB_ENABLE_OBJECT`', '`DB_START_OBJECT`', '`DB_ENABLE_DB_START_OBJECT`',
        'current operator-owned BricsCAD process is out of',
        'scope; execution waits for the mandatory zero-process boundary',
    ):
        if token not in text:
            errors.append("LOCAL-004 claim missing Undo lifecycle coordination token: " + token)

for path in (COORDINATOR, SERVICE):
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for forbidden in ('StartUndoRecord()', 'database.DisableUndoRecording(false)', 'Database.DisableUndoRecording(false)'):
        if forbidden in text:
            errors.append("production Source Reconcile unexpectedly uses diagnostic database Undo lifecycle API: " + forbidden)

print("QS3D LOCAL-004 Source Reconcile Undo lifecycle diagnostic preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: four fresh-process database/object Undo variants are statically bound to an existing XData mutation plus topology sentinel, close the synthetic DWG explicitly without saving before host quit, materialize generic result metadata through the Windows PowerShell 5.1-safe pipeline path without masking qualification errors, preserve sanitized evidence/cleanup guards, and leave production Source Reconcile database Undo lifecycle untouched.")
