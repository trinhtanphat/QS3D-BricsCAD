from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private static bool IsActiveDocument(Document document)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "private static void RefreshProjectForDocument(Document document)",
    "private static void TrySetPaletteStatus(Document document, string message)",
    "RefreshProjectForDocument(document);",
    "TrySetPaletteStatus(document, message);",
]
missing = [needle for needle in required if needle not in text]
if missing:
    print("ERROR: Beam Stirrup process-wide palette refresh/status must be fenced to the exact source document")
    for needle in missing:
        print("  missing:", needle)
    sys.exit(1)

is_active_start = text.find("private static bool IsActiveDocument(Document document)")
is_active_end = text.find("private static", is_active_start + 1)
if is_active_end < 0:
    is_active_end = len(text)
is_active_body = text[is_active_start:is_active_end]
if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in is_active_body:
    print("ERROR: Beam Stirrup affinity helper must use exact Document reference identity")
    sys.exit(1)

refresh_start = text.find("private static void RefreshProjectForDocument(Document document)")
status_start = text.find("private static void TrySetPaletteStatus(Document document, string message)")
if refresh_start < 0 or status_start < 0:
    print("ERROR: cannot locate Beam Stirrup document-affine palette helpers")
    sys.exit(1)

refresh_end = text.find("private static", refresh_start + 1)
if refresh_end < 0:
    refresh_end = len(text)
refresh_body = text[refresh_start:refresh_end]
status_end = text.find("private static", status_start + 1)
if status_end < 0:
    status_end = len(text)
status_body = text[status_start:status_end]

for name, body, mutation in [
    ("RefreshProjectForDocument", refresh_body, "PaletteCoordinator.RefreshProject();"),
    ("TrySetPaletteStatus", status_body, "PaletteCoordinator.SetStatus(message);"),
]:
    guard = body.find("IsActiveDocument(document)")
    mutate = body.find(mutation)
    if guard < 0 or mutate < 0 or guard > mutate:
        print(f"ERROR: {name} must fail closed on source-document affinity before process-wide palette mutation")
        sys.exit(1)

# Every process-wide palette mutation in this command must live behind one of the
# two source-document-affine helpers. Native editor output remains tied to the
# captured source Document and is intentionally not redirected to current MDI.
outside_refresh = text[:refresh_start] + text[refresh_end:]
outside_status = text[:status_start] + text[status_end:]
if "PaletteCoordinator.RefreshProject();" in outside_refresh:
    print("ERROR: Beam Stirrup contains an unfenced direct project-palette refresh")
    sys.exit(1)
if "PaletteCoordinator.SetStatus(" in outside_status:
    print("ERROR: Beam Stirrup contains an unfenced direct palette status publication")
    sys.exit(1)

finalize_start = text.find("private static void FinalizeUi(Document document, string message)")
status_helper_start = text.find("private static void TrySetPaletteStatus", finalize_start)
if finalize_start < 0 or status_helper_start < 0:
    print("ERROR: cannot locate Beam Stirrup FinalizeUi/status helper boundary")
    sys.exit(1)
finalize_body = text[finalize_start:status_helper_start]
refresh_call = finalize_body.find("RefreshProjectForDocument(document);")
regen = finalize_body.find("document.Editor.Regen();")
status_call = finalize_body.find("TrySetPaletteStatus(document, message);")
write = finalize_body.find("document.Editor.WriteMessage")
if min(refresh_call, regen, status_call, write) < 0 or not (refresh_call < regen < status_call < write):
    print("ERROR: Beam Stirrup finalization must preserve refresh/regen/status/editor ordering while fencing palette work")
    sys.exit(1)

report_start = text.find("private static void Report(Document document, string message)")
write_helper_start = text.find("private static void TryWriteMessage", report_start)
if report_start < 0 or write_helper_start < 0:
    print("ERROR: cannot locate Beam Stirrup Report/Write helper boundary")
    sys.exit(1)
report_body = text[report_start:write_helper_start]
if "TrySetPaletteStatus(document, message);" not in report_body or "TryWriteMessage(document," not in report_body:
    print("ERROR: Beam Stirrup Report must route status through source-document affinity and retain exact-source editor output")
    sys.exit(1)

for forbidden in [
    "DocumentLock",
    "StartTransaction",
    "ProjectContextCoordinator.Save(",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
]:
    if forbidden in refresh_body or forbidden in status_body:
        print("ERROR: Beam Stirrup palette affinity helpers must remain presentation-only:", forbidden)
        sys.exit(1)

print("PASS: Beam Stirrup palette refresh/status publication is fenced to the exact active source document")
