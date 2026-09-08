from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamRebarCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

finalize_start = text.find("private static void FinalizeUi(Document document, string message)")
helper_start = text.find("private static bool IsActiveDocument(Document document)", finalize_start)
refresh_start = text.find("private static void TryRefreshProject(Document document)", helper_start)
status_start = text.find("private static void TrySetPaletteStatus(Document document, string message)", refresh_start)
report_start = text.find("private static void Report(Document document, string message)", status_start)
write_start = text.find("private static void TryWriteMessage", report_start)
if min(finalize_start, helper_start, refresh_start, status_start, report_start, write_start) < 0:
    print("ERROR: cannot locate Beam Rebar UI publication/affinity methods")
    sys.exit(1)
if not (finalize_start < helper_start < refresh_start < status_start < report_start < write_start):
    print("ERROR: Beam Rebar UI publication/affinity helper ordering is unexpected")
    sys.exit(1)

finalize = text[finalize_start:helper_start]
active_helper = text[helper_start:refresh_start]
refresh = text[refresh_start:status_start]
status = text[status_start:report_start]
report = text[report_start:write_start]
helper = text[helper_start:report_start]

required_helper = [
    "private static bool IsActiveDocument(Document document)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "private static void TryRefreshProject(Document document)",
    "private static void TrySetPaletteStatus(Document document, string message)",
]
missing = [needle for needle in required_helper if needle not in helper]
if missing:
    print("ERROR: Beam Rebar Workspace publication must be fenced to the exact source active document")
    for needle in missing:
        print("  missing:", needle)
    sys.exit(1)

for method_name, body, required in [
    ("FinalizeUi", finalize, ["TryRefreshProject(document);", "TrySetPaletteStatus(document, message);"]),
    ("Report", report, ["TrySetPaletteStatus(document, message);"]),
]:
    for needle in required:
        if needle not in body:
            print(f"ERROR: {method_name} must route Workspace publication through source-affinity helper: {needle}")
            sys.exit(1)

for body_name, body in [("FinalizeUi", finalize), ("Report", report)]:
    for forbidden in ["PaletteCoordinator.RefreshProject();", "PaletteCoordinator.SetStatus(message);"]:
        if forbidden in body:
            print(f"ERROR: {body_name} bypasses source-document affinity via {forbidden}")
            sys.exit(1)

if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in active_helper:
    print("ERROR: IsActiveDocument must use exact source-document/active-MDI identity")
    sys.exit(1)

for label, body, native_call in [
    ("TryRefreshProject", refresh, "PaletteCoordinator.RefreshProject();"),
    ("TrySetPaletteStatus", status, "PaletteCoordinator.SetStatus(message);")
]:
    affinity = body.find("IsActiveDocument(document)")
    call = body.find(native_call)
    if affinity < 0 or call < 0 or affinity > call:
        print(f"ERROR: {label} must reject stale source documents before process-wide Workspace publication")
        sys.exit(1)

for forbidden in [
    "ProjectContextCoordinator",
    "ExistingProjectMutationContext",
    "DocumentLock",
    "StartTransaction",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
]:
    if forbidden in helper:
        print("ERROR: Beam Rebar UI affinity helpers must remain presentation-only; found", forbidden)
        sys.exit(1)

print("PASS: Beam Rebar Workspace refresh/status are fenced to the exact source active document")
