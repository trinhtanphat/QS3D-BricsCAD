#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
P0 = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawCommands.cs"
REPEATED = ROOT / "src/QS3D.BricsCAD.V25/DirectDrawRepeatedCommands.cs"
REPORTER = ROOT / "src/QS3D.BricsCAD.V25/Services/DirectDrawUiFailureReporter.cs"
errors = []

def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")

p0 = read(P0)
repeated = read(REPEATED)
reporter = read(REPORTER)

# P0 must delegate all process-wide status publication to the established source-fenced reporter.
if "DirectDrawUiFailureReporter.ReportOperationFailure(document, operation);" not in p0:
    errors.append("DirectDrawCommands.Guard must use source-fenced ReportOperationFailure")
if "DirectDrawUiFailureReporter.ReportPostCommitSuccess(document, status);" not in p0:
    errors.append("DirectDrawCommands.FinalizeUi must use source-fenced post-commit success publication")
if "DirectDrawUiFailureReporter.ReportPostCommitWarning(document);" not in p0:
    errors.append("DirectDrawCommands.FinalizeUi failure must use stable source-fenced post-commit warning")
if "private static void TrySetPaletteStatus" in p0:
    errors.append("DirectDrawCommands must not retain an unfenced local palette-status helper")

# P0 post-commit Workspace refresh is process-wide too. Prove the captured source Document is
# still active before that refresh, while leaving the irreversible native/semantic commit intact.
finalize_start = p0.find("private static void FinalizeUi(Document document")
finalize_end = p0.find("private static void EnsureActive", finalize_start if finalize_start >= 0 else 0)
finalize_body = p0[finalize_start:finalize_end if finalize_end >= 0 else len(p0)] if finalize_start >= 0 else ""
if finalize_start < 0:
    errors.append("missing DirectDrawCommands.FinalizeUi")
else:
    post_commit_fence = finalize_body.find('EnsureActive(document, "Direct Draw post-commit UI refresh");')
    refresh_project = finalize_body.find("PaletteCoordinator.RefreshProject();")
    if post_commit_fence < 0:
        errors.append("FinalizeUi must exact-document-fence process-wide post-commit Workspace refresh")
    if refresh_project < 0:
        errors.append("FinalizeUi must retain project refresh while source Document is active")
    if post_commit_fence >= 0 and refresh_project >= 0 and post_commit_fence > refresh_project:
        errors.append("FinalizeUi active-document fence must execute before PaletteCoordinator.RefreshProject")

# Repeated authoring has document-deactivation awareness, so every arbitrary final/partial status
# must preserve that same source affinity at the shared presentation boundary.
report_start = repeated.find("private static void Report(Document document, string message)")
report_body = repeated[report_start:] if report_start >= 0 else ""
if report_start < 0:
    errors.append("missing DirectDrawRepeatedCommands.Report(Document,string)")
else:
    if "DirectDrawUiFailureReporter.ReportMessage(document, message);" not in report_body:
        errors.append("Repeated Direct Draw Report must delegate to source-fenced shared reporter")
    if "PaletteCoordinator.SetStatus(message)" in report_body:
        errors.append("Repeated Direct Draw must not publish process-wide palette status directly")

# Shared arbitrary-message reporting must keep exact source Editor output and fail closed before
# touching the process-wide Workspace palette.
method_start = reporter.find("internal static void ReportMessage(Document document, string message)")
method_end = reporter.find("private static void TryWriteEditor", method_start if method_start >= 0 else 0)
method_body = reporter[method_start:method_end if method_end >= 0 else len(reporter)] if method_start >= 0 else ""
if method_start < 0:
    errors.append("DirectDrawUiFailureReporter must expose ReportMessage for repeated source-bound status")
else:
    if "TryWriteEditor(document, message);" not in method_body:
        errors.append("ReportMessage must preserve best-effort Editor reporting to the source Document")
    if "TrySetPaletteForCurrentDocument(document, message);" not in method_body:
        errors.append("ReportMessage must route palette status through the exact-document fence")
    for forbidden in [
        "ProjectContextCoordinator",
        "GetOrCreate",
        "DocumentLock",
        "StartTransaction",
        "SetImpliedSelection",
        "SendStringToExecute",
        "Dispatcher.BeginInvoke",
    ]:
        if forbidden in method_body:
            errors.append("ReportMessage must remain synchronous presentation-only: " + forbidden)

fence_start = reporter.find("private static void TrySetPaletteForCurrentDocument(Document document, string message)")
fence_body = reporter[fence_start:] if fence_start >= 0 else ""
identity = fence_body.find("ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)")
publish = fence_body.find("PaletteCoordinator.SetStatus(message)")
if fence_start < 0 or identity < 0 or publish < 0 or identity > publish:
    errors.append("shared reporter must exact-reference-fence source Document before PaletteCoordinator.SetStatus")

# Preserve the native/semantic authoring safety fences that this presentation-only fix must not weaken.
for required in [
    "EnsureActive(document, operation);",
    "ProjectStateSnapshot.Capture(project)",
    "EraseDirectDrawCad(document, project, createdElement, sourceId, generatedHandles)",
]:
    if required not in p0:
        errors.append("P0 authoring invariant missing: " + required)
for required in [
    "using var lifecycleGuard = new RepeatedDocumentLifecycleGuard(document);",
    "DocumentToBeDeactivated += OnDocumentToBeDeactivated",
    "DocumentToBeDeactivated -= OnDocumentToBeDeactivated",
    "RollbackWholeCommand(",
]:
    if required not in repeated:
        errors.append("Repeated authoring invariant missing: " + required)

print("QS3D V25 Direct Draw P0/repeated status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: P0/repeated Direct Draw preserves source-document Editor reporting and suppresses stale process-wide Workspace status/refresh after MDI switches.")
