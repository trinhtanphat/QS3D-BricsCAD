from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SelectionSyncCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

refresh_start = text.find("public static void Refresh(Document? document)")
stop_start = text.find("public static void Stop()", refresh_start)
release_start = text.find("private static void ReleaseRefresh(Document document, object attachmentToken)")
handler_start = text.find("private static void OnImpliedSelectionChanged", release_start)
if min(refresh_start, stop_start, release_start, handler_start) < 0:
    print("ERROR: cannot locate SelectionSync refresh-generation ownership methods")
    sys.exit(1)
if not (refresh_start < stop_start < release_start < handler_start):
    print("ERROR: SelectionSync refresh-generation method ordering is unexpected")
    sys.exit(1)

refresh = text[refresh_start:stop_start]
release = text[release_start:handler_start]

required = [
    "private static readonly Dictionary<Document, object> Refreshing",
    "Refreshing[document] = attachmentToken;",
    "ReleaseRefresh(document, attachmentToken);",
]
for needle in required:
    if needle not in text:
        print("ERROR: SelectionSync in-flight refresh ownership must be attachment-generation aware; missing", needle)
        sys.exit(1)

if "finally { Refreshing.Remove(document); }" in refresh:
    print("ERROR: stale refresh generation can unconditionally remove a newer generation's ownership")
    sys.exit(1)

for needle in [
    "Refreshing.TryGetValue(document, out var currentToken)",
    "ReferenceEquals(currentToken, attachmentToken)",
    "Refreshing.Remove(document);",
]:
    if needle not in release:
        print("ERROR: ReleaseRefresh must remove only the exact captured attachment generation; missing", needle)
        sys.exit(1)

claim = refresh.find("Refreshing[document] = attachmentToken;")
work = refresh.find("PaletteCoordinator.EnsureCreated();")
release_call = refresh.find("ReleaseRefresh(document, attachmentToken);")
if claim < 0 or work < 0 or release_call < 0 or not (claim < work < release_call):
    print("ERROR: SelectionSync refresh generation must be claimed before modeless/native work and released afterward")
    sys.exit(1)

for forbidden in ["Dispatcher.BeginInvoke", "Task.Run", "Thread", "DocumentLock", "StartTransaction"]:
    if forbidden in release:
        print("ERROR: refresh-generation release must remain synchronous bookkeeping only; found", forbidden)
        sys.exit(1)

print("PASS: SelectionSync refresh ownership cleanup is fenced to the exact attachment generation")
