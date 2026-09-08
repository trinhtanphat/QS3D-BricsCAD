from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "SelectionSyncCoordinator.cs"
text = SOURCE.read_text(encoding="utf-8")

attach_start = text.find("public static void Attach(Document? document)")
detach_start = text.find("public static void Detach(Document? document)", attach_start)
refresh_start = text.find("public static void Refresh(Document? document)", detach_start)
stop_start = text.find("public static void Stop()", refresh_start)
rollback_start = text.find("private static void RollbackAttachment(Document document, bool subscribed, object? attachmentToken)", stop_start)
release_start = text.find("private static void ReleaseRefresh(Document document, object attachmentToken)", rollback_start)
handler_start = text.find("private static void OnImpliedSelectionChanged", release_start)
if min(attach_start, detach_start, refresh_start, stop_start, rollback_start, release_start, handler_start) < 0:
    print("ERROR: cannot locate SelectionSync generation-ownership methods")
    sys.exit(1)
if not (attach_start < detach_start < refresh_start < stop_start < rollback_start < release_start < handler_start):
    print("ERROR: SelectionSync generation-ownership method ordering is unexpected")
    sys.exit(1)

attach = text[attach_start:detach_start]
refresh = text[refresh_start:stop_start]
rollback = text[rollback_start:release_start]
release = text[release_start:handler_start]

required = [
    "private static readonly Dictionary<Document, object> Refreshing",
    "Refreshing[document] = attachmentToken;",
    "ReleaseRefresh(document, attachmentToken);",
    "object? attachmentToken = null;",
    "RollbackAttachment(document, subscribed, attachmentToken);",
]
for needle in required:
    if needle not in text:
        print("ERROR: SelectionSync attachment/refresh ownership must be generation aware; missing", needle)
        sys.exit(1)

if "finally { Refreshing.Remove(document); }" in refresh:
    print("ERROR: stale refresh generation can unconditionally remove a newer generation's ownership")
    sys.exit(1)
if "Refreshing.Remove(document);" in attach or "AttachmentTokens.Remove(document);" in attach or "Attached.Remove(document);" in attach:
    print("ERROR: Attach catch must not blindly roll back a newer reattachment generation")
    sys.exit(1)

for needle in [
    "AttachmentTokens.TryGetValue(document, out var currentToken)",
    "!ReferenceEquals(currentToken, attachmentToken)",
    "return;",
    "document.ImpliedSelectionChanged -= OnImpliedSelectionChanged",
    "RemovePending(document);",
    "AttachmentTokens.Remove(document);",
    "Attached.Remove(document);",
]:
    if needle not in rollback:
        print("ERROR: RollbackAttachment must preserve a newer generation and clean only the failed owner; missing", needle)
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

for label, body in [("RollbackAttachment", rollback), ("ReleaseRefresh", release)]:
    for forbidden in ["Dispatcher.BeginInvoke", "Task.Run", "Thread", "DocumentLock", "StartTransaction"]:
        if forbidden in body:
            print(f"ERROR: {label} must remain synchronous bookkeeping only; found {forbidden}")
            sys.exit(1)

print("PASS: SelectionSync attach rollback and refresh cleanup are fenced to exact attachment generations")
