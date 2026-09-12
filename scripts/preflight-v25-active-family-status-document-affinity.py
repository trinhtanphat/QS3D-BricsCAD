#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
errors = []
source = SOURCE.read_text(encoding="utf-8") if SOURCE.is_file() else ""
if not source:
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))

report_sig = "private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)"
report_start = source.find(report_sig)
report_body = source[report_start:] if report_start >= 0 else ""
if report_start < 0:
    errors.append("missing generation-aware Active Family Report helper")
else:
    editor_write = report_body.find('document.Editor.WriteMessage("\\n" + message)')
    palette_write = report_body.find("PaletteCoordinator.SetStatus(message)")
    if editor_write < 0:
        errors.append("Report must preserve best-effort Editor reporting to the exact source Document")
    if palette_write < 0:
        errors.append("Report must retain Workspace status publication")
    if report_body.count("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)") < 2:
        errors.append("Report must fence exact managed/native generation before and after Editor output")
    if editor_write >= 0 and palette_write >= 0 and editor_write > palette_write:
        errors.append("Editor source-document reporting must precede Workspace status publication")
    for forbidden in [
        "ProjectContextCoordinator", "GetOrCreate", "DocumentLock", "StartTransaction",
        "SetImpliedSelection", "SendStringToExecute", "Dispatcher.BeginInvoke",
    ]:
        if forbidden in report_body:
            errors.append("Report must remain synchronous presentation-only: " + forbidden)

snapshot_start = source.find("private static ProjectFamily RequireCurrentDispatchSnapshot(")
snapshot_end = source.find("private static void Dispatch(", snapshot_start if snapshot_start >= 0 else 0)
snapshot_body = source[snapshot_start:snapshot_end if snapshot_end >= 0 else len(source)] if snapshot_start >= 0 else ""
if "RequireActiveDocumentGeneration(document, nativeDatabaseIdentity, operation);" not in snapshot_body:
    errors.append("RequireCurrentDispatchSnapshot must keep exact active-document/native-generation refusal before dispatch")
if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in source:
    errors.append("generation authority must still prove exact active managed Document identity")

catch_start = source.find("catch (Exception)")
catch_end = source.find("}", catch_start if catch_start >= 0 else 0)
catch_body = source[catch_start:catch_end + 1] if catch_start >= 0 and catch_end >= 0 else ""
if "Report(document, nativeDatabaseIdentity, operation +" not in catch_body:
    errors.append("catch path must report through the captured source Document/native generation")
if "MdiActiveDocument" in catch_body:
    errors.append("catch path must not recapture a different active Document")

print("QS3D V25 Active Family status document-affinity preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Active Family Quick Draw keeps exact source-document/native-generation Editor reporting and suppresses stale Workspace status.")
