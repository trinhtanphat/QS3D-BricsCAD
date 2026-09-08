from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private static bool IsActiveDocument(Document document)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "private static void RefreshProjectForDocument(Document document)",
    "private static void SetPaletteStatusForDocument(Document document, string message)",
    "private static void TrySetPaletteStatusForDocument(Document document, string message)",
    "RefreshProjectForDocument(document);",
    "SetPaletteStatusForDocument(document, message);",
    "TrySetPaletteStatusForDocument(document, message);",
]
missing = [needle for needle in required if needle not in text]
if missing:
    print("ERROR: Beam Stirrup process-wide palette refresh/status must be fenced to the exact source document")
    for needle in missing:
        print("  missing:", needle)
    sys.exit(1)


def method_body(signature: str) -> str:
    start = text.find(signature)
    if start < 0:
        return ""
    end = text.find("private static", start + len(signature))
    if end < 0:
        end = len(text)
    return text[start:end]


is_active_body = method_body("private static bool IsActiveDocument(Document document)")
if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in is_active_body:
    print("ERROR: Beam Stirrup affinity helper must use exact Document reference identity")
    sys.exit(1)

refresh_body = method_body("private static void RefreshProjectForDocument(Document document)")
set_status_body = method_body("private static void SetPaletteStatusForDocument(Document document, string message)")
try_status_body = method_body("private static void TrySetPaletteStatusForDocument(Document document, string message)")

for name, body, mutation in [
    ("RefreshProjectForDocument", refresh_body, "PaletteCoordinator.RefreshProject();"),
    ("SetPaletteStatusForDocument", set_status_body, "PaletteCoordinator.SetStatus(message);"),
]:
    guard = body.find("IsActiveDocument(document)")
    mutate = body.find(mutation)
    if guard < 0 or mutate < 0 or guard > mutate:
        print(f"ERROR: {name} must fail closed on source-document affinity before process-wide palette mutation")
        sys.exit(1)

if "try" not in try_status_body or "SetPaletteStatusForDocument(document, message);" not in try_status_body or "catch" not in try_status_body:
    print("ERROR: Health/Report status helper must preserve best-effort exception containment while delegating to the affinity fence")
    sys.exit(1)

# Direct process-wide palette mutations are centralized behind the exact-source
# helpers. Native editor output remains tied to the captured source Document.
refresh_start = text.find("private static void RefreshProjectForDocument(Document document)")
refresh_end = text.find("private static", refresh_start + 1)
if refresh_end < 0:
    refresh_end = len(text)
set_start = text.find("private static void SetPaletteStatusForDocument(Document document, string message)")
set_end = text.find("private static", set_start + 1)
if set_end < 0:
    set_end = len(text)
outside_refresh = text[:refresh_start] + text[refresh_end:]
outside_status = text[:set_start] + text[set_end:]
if "PaletteCoordinator.RefreshProject();" in outside_refresh:
    print("ERROR: Beam Stirrup contains an unfenced direct project-palette refresh")
    sys.exit(1)
if "PaletteCoordinator.SetStatus(" in outside_status:
    print("ERROR: Beam Stirrup contains an unfenced direct palette status publication")
    sys.exit(1)

finalize_body = method_body("private static void FinalizeUi(Document document, string message)")
refresh_call = finalize_body.find("RefreshProjectForDocument(document);")
regen = finalize_body.find("document.Editor.Regen();")
status_call = finalize_body.find("SetPaletteStatusForDocument(document, message);")
write = finalize_body.find("document.Editor.WriteMessage")
if min(refresh_call, regen, status_call, write) < 0 or not (refresh_call < regen < status_call < write):
    print("ERROR: Beam Stirrup finalization must preserve refresh/regen/status/editor ordering and non-best-effort status semantics")
    sys.exit(1)
if "TrySetPaletteStatusForDocument(document, message);" in finalize_body:
    print("ERROR: FinalizeUi must not swallow status exceptions that previously entered its UI-sync warning path")
    sys.exit(1)

health_start = text.find("public void BeamStirrupHealth()")
health_end = text.find("private static List<ProjectElement>", health_start)
health_body = text[health_start:health_end] if health_start >= 0 and health_end >= 0 else ""
if "TrySetPaletteStatusForDocument(document, message);" not in health_body:
    print("ERROR: Beam Stirrup Health must publish palette status through the exact-source best-effort helper")
    sys.exit(1)

report_body = method_body("private static void Report(Document document, string message)")
if "TrySetPaletteStatusForDocument(document, message);" not in report_body or "TryWriteMessage(document," not in report_body:
    print("ERROR: Beam Stirrup Report must route status through source-document affinity and retain exact-source editor output")
    sys.exit(1)

for forbidden in [
    "DocumentLock",
    "StartTransaction",
    "ProjectContextCoordinator.Save(",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
]:
    if forbidden in refresh_body or forbidden in set_status_body or forbidden in try_status_body:
        print("ERROR: Beam Stirrup palette affinity helpers must remain presentation-only:", forbidden)
        sys.exit(1)

print("PASS: Beam Stirrup palette refresh/status publication is fenced to the exact active source document")
