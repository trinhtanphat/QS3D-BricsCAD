from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private static bool IsActiveDocument(Document document)",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "private static void RevealWorkspaceForDocument(Document document)",
    "private static void SetWorkspaceStatusForDocument(Document document, string message)",
    "RevealWorkspaceForDocument(document);",
    "SetWorkspaceStatusForDocument(document, message);",
]
missing = [needle for needle in required if needle not in text]
if missing:
    print("ERROR: Beam Stirrup process-wide Workspace UI must be fenced to the exact source document")
    for needle in missing:
        print("  missing:", needle)
    sys.exit(1)

is_active_start = text.find("private static bool IsActiveDocument(Document document)")
is_active_end = text.find("}", is_active_start)
is_active_body = text[is_active_start:is_active_end + 1] if is_active_start >= 0 and is_active_end >= 0 else ""
if "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)" not in is_active_body:
    print("ERROR: Beam Stirrup affinity helper must use exact Document reference identity")
    sys.exit(1)

reveal_start = text.find("private static void RevealWorkspaceForDocument(Document document)")
status_start = text.find("private static void SetWorkspaceStatusForDocument(Document document, string message)")
if reveal_start < 0 or status_start < 0:
    print("ERROR: cannot locate Beam Stirrup document-affine Workspace helpers")
    sys.exit(1)

reveal_end = text.find("private static", reveal_start + 1)
if reveal_end < 0:
    reveal_end = len(text)
reveal_body = text[reveal_start:reveal_end]
status_end = text.find("private static", status_start + 1)
if status_end < 0:
    status_end = len(text)
status_body = text[status_start:status_end]

for name, body, mutation in [
    ("RevealWorkspaceForDocument", reveal_body, "WorkspaceAutoRevealService.EnsureShown();"),
    ("SetWorkspaceStatusForDocument", status_body, "PaletteCoordinator.SetStatus(message);"),
]:
    guard = body.find("IsActiveDocument(document)")
    mutate = body.find(mutation)
    if guard < 0 or mutate < 0 or guard > mutate:
        print(f"ERROR: {name} must fail closed on source-document affinity before process-wide UI mutation")
        sys.exit(1)

# No raw process-wide mutations may remain outside the two affinity helpers.
outside_reveal = text[:reveal_start] + text[reveal_end:]
outside_status = text[:status_start] + text[status_end:]
if "WorkspaceAutoRevealService.EnsureShown();" in outside_reveal:
    print("ERROR: Beam Stirrup contains an unfenced direct Workspace reveal outside the affinity helper")
    sys.exit(1)
if "PaletteCoordinator.SetStatus(" in outside_status:
    print("ERROR: Beam Stirrup contains an unfenced direct Workspace status publication outside the affinity helper")
    sys.exit(1)

report_start = text.find("private static void Report(Document document, string message, bool isError)")
finalize_start = text.find("private static void FinalizeUi(Document document, string? message)")
if report_start < 0 or finalize_start < 0:
    print("ERROR: cannot locate Beam Stirrup Report/FinalizeUi methods")
    sys.exit(1)
report_body = text[report_start:finalize_start]

status_publish = report_body.find("SetWorkspaceStatusForDocument(document, message);")
error_gate = report_body.find("if (!isError || !IsActiveDocument(document))")
message_box = report_body.find("MessageBox.Show(")
if min(status_publish, error_gate, message_box) < 0 or not (status_publish < error_gate < message_box):
    print("ERROR: Beam Stirrup stale error presentation must revalidate the source document after status publication")
    sys.exit(1)

for forbidden in [
    "DocumentLock",
    "StartTransaction",
    "ProjectContextCoordinator.Save(",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
]:
    if forbidden in reveal_body or forbidden in status_body:
        print("ERROR: Beam Stirrup UI affinity helpers must remain presentation-only:", forbidden)
        sys.exit(1)

print("PASS: Beam Stirrup Workspace/status/error publication is fenced to the exact active source document")
