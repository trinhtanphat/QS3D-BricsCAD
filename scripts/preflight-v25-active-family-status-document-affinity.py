#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ActiveFamilyQuickDrawCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing source: " + str(SOURCE.relative_to(ROOT)))
    source = ""
else:
    source = SOURCE.read_text(encoding="utf-8")

report_start = source.find("private static void Report(Document document, string message)")
report_body = source[report_start:] if report_start >= 0 else ""
if report_start < 0:
    errors.append("missing Active Family Report(Document,string) helper")
else:
    editor_write = report_body.find('document.Editor.WriteMessage("\\n" + message)')
    affinity = report_body.find("ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)")
    palette_write = report_body.find("PaletteCoordinator.SetStatus(message)")
    if editor_write < 0:
        errors.append("Report must preserve best-effort Editor reporting to the exact source Document")
    if affinity < 0:
        errors.append("Report must fence process-wide status publication to the exact active Document")
    if palette_write < 0:
        errors.append("Report must retain Workspace status publication while the source Document is active")
    if affinity >= 0 and palette_write >= 0 and affinity > palette_write:
        errors.append("active-document affinity fence must execute before PaletteCoordinator.SetStatus")

    for forbidden in [
        "ProjectContextCoordinator",
        "GetOrCreate",
        "DocumentLock",
        "StartTransaction",
        "SetImpliedSelection",
        "SendStringToExecute",
        "Dispatcher.BeginInvoke",
    ]:
        if forbidden in report_body:
            errors.append("Report must remain synchronous presentation-only: " + forbidden)

# The dispatcher already detects an A->B MDI switch before native dispatch. Keep that
# upstream fail-closed fence so the status helper cannot become the only affinity defense.
snapshot_start = source.find("private static ProjectFamily RequireCurrentDispatchSnapshot(")
snapshot_end = source.find("private static void Dispatch(", snapshot_start if snapshot_start >= 0 else 0)
snapshot_body = source[snapshot_start:snapshot_end if snapshot_end >= 0 else len(source)] if snapshot_start >= 0 else ""
if "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)" not in snapshot_body:
    errors.append("RequireCurrentDispatchSnapshot must keep exact active-document refusal before dispatch")

# Catch must report through the captured source Document, not recapture whatever document is
# active after a failure/document switch.
catch_start = source.find("catch (Exception)")
catch_end = source.find("}", catch_start if catch_start >= 0 else 0)
catch_body = source[catch_start:catch_end + 1] if catch_start >= 0 and catch_end >= 0 else ""
if "Report(document, operation +" not in catch_body:
    errors.append("catch path must report using the originally captured source Document")
if "MdiActiveDocument" in catch_body:
    errors.append("catch path must not recapture a different active Document")

print("QS3D V25 Active Family status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: Active Family Quick Draw keeps source-document Editor reporting and suppresses stale process-wide Workspace status after MDI switches.")
