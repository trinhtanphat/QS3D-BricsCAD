#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COMMAND = ROOT / "src/QS3D.BricsCAD.V25/SidecarRevisionProbeCommands.cs"
RUNNER = ROOT / "scripts/test-bricscad-v25-sidecar-revision.ps1"
DOC = ROOT / "docs/COMMANDS.md"
INBOX = ROOT / "docs/LOCAL-AGENT-INBOX.md"
errors = []

for path in (COMMAND, RUNNER, DOC, INBOX):
    if not path.is_file():
        errors.append("missing sidecar runtime qualification file: " + str(path.relative_to(ROOT)))

if COMMAND.is_file():
    text = COMMAND.read_text(encoding="utf-8")
    for token in (
        '[CommandMethod("QS3DSIDECARREVISIONPROBE", CommandFlags.Modal)]',
        'if (string.IsNullOrWhiteSpace(rawResult))',
        'EndsWith(".reference-copy.dwg"',
        'ProjectContextCoordinator.GetProjectPath(document)',
        'ProjectContextCoordinator.Save(document)',
        'TestBackupAppearance(document, project, sidecar, baseline, progress)',
        'TestPrimaryReplacement(document, project, sidecar, nonce, baseline, progress)',
        'TestPrimaryRemoval(document, project, sidecar, nonce, baseline, progress)',
        'ProjectContextCoordinator.TryGetReadOnly(document, out _)',
        'ExistingProjectMutationContext.Require(document, "Sidecar revision probe")',
        'InterchangeConfirmationGuard.RequireFresh(document, project, reviewedVersion',
        'baseline.EnsureUnchanged(project)',
        'ProjectStateSnapshot.CreateDetachedCopy(project)',
        'Store.Save(detached, path)',
        '"sr-" + nonce.Substring(0, 8) + "-"',
        'progress.Stage = "baseline_snapshot_save"',
        'root.SetAttributeValue("updatedUtc", "<normalized-by-sidecar-probe>")',
        'SHA256.Create()',
        'path + ".lock"',
        '"error_code=" + errorCode',
        '"stage=" + SafeFailureStage(stage)',
        '"failure_kind=" + failureKind',
        'if (exception is InvalidDataException) return "invalid_data";',
        'var candidate = stage ?? string.Empty;',
        'FailureStages.Contains(candidate) ? candidate : "unknown"',
    ):
        if token not in text:
            errors.append("SidecarRevisionProbeCommands missing runtime/privacy token: " + token)
    for forbidden in (
        '"project_id="', '"drawing_path="', '"drawing_fingerprint="', '"handle="',
        '"error_message="', 'ex.Message', 'OpenMode.ForWrite', 'AppendEntity(', '.Erase()',
        'Store.Save(project, path)'
    ):
        if forbidden in text:
            errors.append("sidecar revision marker/CAD boundary contains forbidden token: " + forbidden)

if RUNNER.is_file():
    text = RUNNER.read_text(encoding="utf-8")
    for token in (
        '[switch]$ConfirmSyntheticFixture',
        '"QS3D-Sample.dwg"',
        '"generated"',
        'git -C $repoRoot status --porcelain',
        '"sidecar-revision.reference-copy.dwg"',
        '"QS3DSIDECARREVISIONPROBE"',
        'Get-FileHash -LiteralPath $drawingCopy -Algorithm SHA256',
        'Get-FileHash -LiteralPath $FixtureDwg -Algorithm SHA256',
        'Restore-EnvironmentValue -Name $name',
        'Stop-LaunchedProcess -Process $process',
        '$Process.WaitForExit()',
        'Private sidecar revision cleanup refuses directory targets.',
        'BricsCAD sidecar revision probe returned sanitized failure stage',
        '$allowedFailureKinds = @("invalid_data", "unauthorized", "io", "xml", "argument", "invalid_operation", "other")',
        'Remove-PrivateProbeArtifacts -ArtifactDir $ArtifactDir',
        '$scratchPrefix = "sr-" + $Nonce.Substring(0, 8) + "-"',
        '($sidecarPath + ".bak")',
        '($sidecarPath + ".lock")',
        '($sidecarPath + "." + $Nonce + ".original")',
        '($sidecarPath + "." + $Nonce + ".replacement")',
        '($sidecarPath + "." + $Nonce + ".removed")',
        '$drawingLockPath = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl")',
        '$drawingLock2Path = [IO.Path]::ChangeExtension($DrawingCopy, ".dwl2")',
        'if ($privatePaths.Count -ne 9)',
        '[IO.Path]::ChangeExtension($DrawingCopy, ".qsdb")',
        'Get-Process -Name "bricscad"',
        'cleanupVerified = $true',
        'warmCacheRevisionMatrix = $true',
    ):
        if token not in text:
            errors.append("sidecar revision runner missing exact-SHA/cleanup token: " + token)
    cleanup = text.find('Stop-LaunchedProcess -Process $process')
    metadata = text.find('[ordered]@{', cleanup)
    if cleanup < 0 or metadata < 0 or cleanup >= metadata:
        errors.append("sidecar revision runner must verify process/private-artifact cleanup before publishing PASS metadata")
    marker = text.find('$marker = Read-Marker')
    hash_after = text.find('$drawingHashAfter =', marker)
    stop_after_marker = text.find('Stop-LaunchedProcess -Process $process', marker)
    if min(marker, hash_after, stop_after_marker) < 0 or not marker < stop_after_marker < hash_after:
        errors.append("sidecar revision runner must finalize the exact BricsCAD process before hashing the disposable DWG")

if DOC.is_file() and "QS3DSIDECARREVISIONPROBE" not in DOC.read_text(encoding="utf-8"):
    errors.append("COMMANDS.md does not identify the automation-only sidecar revision probe")

if INBOX.is_file():
    text = INBOX.read_text(encoding="utf-8")
    for token in ("test-bricscad-v25-sidecar-revision.ps1", "warm-cache", ".qsdb/.bak"):
        if token not in text:
            errors.append("LOCAL-001 missing sidecar revision runtime scenario token: " + token)

print("QS3D V25 sidecar revision runtime preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: automation-only V25 probe is exact-SHA, disposable-copy scoped, privacy-safe and covers warm-cache primary/backup revision rejection without DWG writes.")
