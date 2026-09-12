from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamStirrupCommands.cs"
text = SOURCE.read_text(encoding="utf-8")


def method_body(signature: str, next_signature: str) -> str:
    start = text.find(signature)
    end = text.find(next_signature, start + len(signature)) if start >= 0 else -1
    if start < 0 or end <= start:
        print("ERROR: Beam Stirrup affinity method missing:", signature)
        sys.exit(1)
    return text[start:end]


generation = method_body(
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
)
refresh = method_body(
    "private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)",
    "private static void SetPaletteStatusForDocument(Document document, IntPtr nativeDatabaseIdentity, string message)",
)
status = method_body(
    "private static void SetPaletteStatusForDocument(Document document, IntPtr nativeDatabaseIdentity, string message)",
    "private static void TrySetPaletteStatusForDocument(Document document, string message)",
)
health_status = method_body(
    "private static void TrySetPaletteStatusForDocument(Document document, string message)",
    "private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)",
)
finalize = method_body(
    "private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)",
    "private static IntPtr GetNativeDatabaseIdentity(Document document)",
)
report = method_body(
    "private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)",
    "private static void ReportHealth(Document document, string message)",
)

for token in [
    "nativeDatabaseIdentity == IntPtr.Zero",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
]:
    if token not in generation:
        print("ERROR: Beam Stirrup exact document-generation fence missing:", token)
        sys.exit(1)

for name, body, mutation in [
    ("RefreshModelTree", refresh, "PaletteCoordinator.RefreshProject();"),
    ("SetPaletteStatusForDocument", status, "PaletteCoordinator.SetStatus(message);")
]:
    guard = body.find("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)")
    call = body.find(mutation)
    if guard < 0 or call < 0 or guard > call:
        print(f"ERROR: {name} must fail closed on stale native database generation before palette mutation")
        sys.exit(1)

ordered = [
    "RefreshModelTree(document, nativeDatabaseIdentity);",
    "document.Editor.Regen();",
    "SetPaletteStatusForDocument(document, nativeDatabaseIdentity, message);",
    'document.Editor.WriteMessage("\\nQS3D " + message);',
]
pos = [finalize.find(token) for token in ordered]
if min(pos) < 0 or pos != sorted(pos):
    print("ERROR: Beam Stirrup finalization must preserve refresh/regen/status/output ordering")
    sys.exit(1)
if finalize.count("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)") < 4:
    print("ERROR: Beam Stirrup finalization must revalidate exact generation between UI stages")
    sys.exit(1)

for token in [
    "try",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "PaletteCoordinator.SetStatus(message);",
    "catch { }",
]:
    if token not in health_status:
        print("ERROR: Beam Stirrup Health status helper must remain fail-soft and exact-document-affine:", token)
        sys.exit(1)

if "IsActiveDocumentGeneration(document, nativeDatabaseIdentity)" not in report or "TryWriteMessage(document, nativeDatabaseIdentity" not in report:
    print("ERROR: Beam Stirrup mutation reporting must retain exact document-generation affinity")
    sys.exit(1)

health_start = text.find("public void BeamStirrupHealth()")
health_end = text.find("private static List<ProjectElement>", health_start)
health = text[health_start:health_end] if health_start >= 0 and health_end > health_start else ""
if "TrySetPaletteStatusForDocument(document, message);" not in health:
    print("ERROR: Beam Stirrup Health must publish status through the best-effort exact-document helper")
    sys.exit(1)

for body in (refresh, status, health_status):
    for forbidden in ("DocumentLock", "StartTransaction", "ProjectContextCoordinator.Save(", "SendStringToExecute", "Dispatcher.BeginInvoke"):
        if forbidden in body:
            print("ERROR: Beam Stirrup palette affinity helpers must remain presentation-only:", forbidden)
            sys.exit(1)

print("PASS: Beam Stirrup UI publication is fenced to exact native generation; Health remains exact-document fail-soft")
