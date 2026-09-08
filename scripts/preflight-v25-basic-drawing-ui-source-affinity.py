from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BasicDrawingCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

finalize_start = text.find("private static void FinalizeSuccess(Document document, ObjectId id, BasicDrawingContext context, string primitiveLabel)")
active_start = text.find("private static bool IsActiveDocument(Document document)", finalize_start)
status_start = text.find("private static void TrySetPaletteStatus(Document document, string message)", active_start)
report_start = text.find("private static void Report(Document document, string message)", status_start)
enum_start = text.find("private enum BasicPrimitiveKind", report_start)

if min(finalize_start, active_start, status_start, report_start, enum_start) < 0:
    print("ERROR: cannot locate Basic Drawing UI source-affinity methods")
    sys.exit(1)
if not (finalize_start < active_start < status_start < report_start < enum_start):
    print("ERROR: Basic Drawing UI source-affinity method ordering is unexpected")
    sys.exit(1)

finalize = text[finalize_start:active_start]
active = text[active_start:status_start]
status = text[status_start:report_start]
report = text[report_start:enum_start]
helper = text[active_start:report_start]

required = [
    (active, "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)"),
    (status, "IsActiveDocument(document)"),
    (status, "PaletteCoordinator.SetStatus(message);"),
    (report, "TrySetPaletteStatus(document, message);"),
    (report, "document.Editor.WriteMessage"),
]
for body, needle in required:
    if needle not in body:
        print("ERROR: Basic Drawing Workspace status must remain bound to the exact source active document; missing", needle)
        sys.exit(1)

if "PaletteCoordinator.SetStatus(message);" in report:
    print("ERROR: Report bypasses source-document affinity for process-wide Workspace status")
    sys.exit(1)

if status.find("IsActiveDocument(document)") > status.find("PaletteCoordinator.SetStatus(message);"):
    print("ERROR: Basic Drawing status must reject stale source documents before Workspace publication")
    sys.exit(1)

for forbidden in [
    "ProjectContextCoordinator",
    "ExistingProjectMutationContext",
    "LockDocument",
    "StartTransaction",
    "SendStringToExecute",
    "Dispatcher.BeginInvoke",
]:
    if forbidden in helper:
        print("ERROR: Basic Drawing UI affinity helper must remain presentation-only; found", forbidden)
        sys.exit(1)

if "AppendEntity(" not in text or "transaction.Commit();" not in text:
    print("ERROR: Basic Drawing native commit boundary is no longer recognizable")
    sys.exit(1)
if "Report(document, status + \" \" + UiSyncWarning);" not in finalize or "Report(document, status);" not in finalize:
    print("ERROR: Basic Drawing post-commit reporting contract changed unexpectedly")
    sys.exit(1)

print("PASS: Basic Drawing Workspace status is fenced to the exact source active document")
