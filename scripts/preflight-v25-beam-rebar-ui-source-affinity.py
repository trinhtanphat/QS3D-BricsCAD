from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "BeamRebarCommands.cs"
text = SOURCE.read_text(encoding="utf-8")


def block(signature: str, next_signature: str) -> str:
    start = text.find(signature)
    end = text.find(next_signature, start + len(signature)) if start >= 0 else -1
    if start < 0 or end <= start:
        print("ERROR: cannot locate Beam Rebar generation-affinity method:", signature)
        sys.exit(1)
    return text[start:end]


finalize = block(
    "private static void FinalizeUi(Document document, IntPtr nativeDatabaseIdentity, string message)",
    "private static IntPtr GetNativeDatabaseIdentity(Document document)",
)
generation = block(
    "private static bool IsActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
    "private static void RequireActiveDocumentGeneration(Document document, IntPtr nativeDatabaseIdentity)",
)
refresh = block(
    "private static void RefreshModelTree(Document document, IntPtr nativeDatabaseIdentity)",
    "private static void TrySetPaletteStatus(Document document, IntPtr nativeDatabaseIdentity, string message)",
)
status = block(
    "private static void TrySetPaletteStatus(Document document, IntPtr nativeDatabaseIdentity, string message)",
    "private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)",
)
report = block(
    "private static void Report(Document document, IntPtr nativeDatabaseIdentity, string message)",
    "private static void TryWriteMessage(Document document, IntPtr nativeDatabaseIdentity, string message)",
)

required_generation = [
    "nativeDatabaseIdentity == IntPtr.Zero",
    "ReferenceEquals(document, Application.DocumentManager.MdiActiveDocument)",
    "document.Database.UnmanagedObject == nativeDatabaseIdentity",
]
for needle in required_generation:
    if needle not in generation:
        print("ERROR: Beam Rebar document-generation fence missing:", needle)
        sys.exit(1)

for name, body, native_call in [
    ("RefreshModelTree", refresh, "PaletteCoordinator.RefreshProject();"),
    ("TrySetPaletteStatus", status, "PaletteCoordinator.SetStatus(message);")
]:
    guard = body.find("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)")
    call = body.find(native_call)
    if guard < 0 or call < 0 or guard > call:
        print(f"ERROR: {name} must reject stale native database generations before Workspace publication")
        sys.exit(1)

ordered = [
    "RefreshModelTree(document, nativeDatabaseIdentity);",
    "document.Editor.Regen();",
    "TrySetPaletteStatus(document, nativeDatabaseIdentity, message);",
    'document.Editor.WriteMessage("\\nQS3D " + message);',
]
pos = [finalize.find(token) for token in ordered]
if min(pos) < 0 or pos != sorted(pos):
    print("ERROR: Beam Rebar finalization must preserve refresh/regen/status/output ordering")
    sys.exit(1)
if finalize.count("IsActiveDocumentGeneration(document, nativeDatabaseIdentity)") < 4:
    print("ERROR: Beam Rebar finalization must revalidate the exact native generation between UI stages")
    sys.exit(1)
if "TrySetPaletteStatus(document, nativeDatabaseIdentity, message);" not in report or "TryWriteMessage(document, nativeDatabaseIdentity" not in report:
    print("ERROR: Beam Rebar reporting must retain exact generation affinity")
    sys.exit(1)

for body in (refresh, status, report):
    for forbidden in ("DocumentLock", "StartTransaction", "SendStringToExecute", "Dispatcher.BeginInvoke"):
        if forbidden in body:
            print("ERROR: Beam Rebar UI affinity helpers must remain presentation-only; found", forbidden)
            sys.exit(1)

print("PASS: Beam Rebar Workspace publication is fenced to exact Document + native Database generation")
