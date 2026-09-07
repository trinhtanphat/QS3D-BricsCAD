#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SELECTION = ROOT / "src/QS3D.BricsCAD.V25/SelectionSyncCoordinator.cs"
PALETTE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
errors = []


def read(path):
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")

selection = read(SELECTION)
palette = read(PALETTE)

for needle in [
    "PaletteCoordinator.SetStatusForDocument(document, \"Selection sync lỗi. Vui lòng thử lại.\")",
]:
    if needle not in selection:
        errors.append("selection-sync catch must publish status through the exact source Document: " + needle)

for needle in [
    "public static void SetStatusForDocument(Document? sourceDocument, string status)",
    "ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument)",
    "SetStatus(status);",
]:
    if needle not in palette:
        errors.append("palette document-affinity status fence missing token: " + needle)

catch_start = selection.find("catch (Exception)")
finally_start = selection.find("finally", catch_start if catch_start >= 0 else 0)
catch_body = selection[catch_start:finally_start if finally_start >= 0 else len(selection)] if catch_start >= 0 else ""
if catch_body and "PaletteCoordinator.SetStatus(" in catch_body:
    errors.append("selection-sync exception path must not publish through the unbound process-wide SetStatus API")

status_start = palette.find("public static void SetStatusForDocument")
status_end = palette.find("public static void RefreshProject", status_start if status_start >= 0 else 0)
status_body = palette[status_start:status_end if status_end >= 0 else len(palette)] if status_start >= 0 else ""
if status_body:
    guard = status_body.find("ReferenceEquals(sourceDocument, Application.DocumentManager.MdiActiveDocument)")
    publish = status_body.find("SetStatus(status);")
    if guard < 0 or publish < 0 or guard > publish:
        errors.append("source-document identity must be checked before status publication")
    for forbidden in ["GetOrCreate", "DocumentLock", "StartTransaction", "SetImpliedSelection", "SendStringToExecute"]:
        if forbidden in status_body:
            errors.append("status affinity fence must remain presentation-only: " + forbidden)

print("QS3D selection-sync status document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: selection-sync errors retain exact source-document affinity and fail closed before stale cross-DWG status publication.")
