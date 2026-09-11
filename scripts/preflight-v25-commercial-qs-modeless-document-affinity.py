#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "CommercialQsCommands.cs"
text = SOURCE.read_text(encoding="utf-8")
errors = []

required = [
    "private static PublishedWindow? _pending;",
    "private static PublishedWindow? _published;",
    "private readonly WeakReference<Document> _document;",
    "NativeDatabaseIdentity = nativeDatabaseIdentity;",
    "ReferenceEquals(ownedDocument, document)",
    "nativeDatabaseIdentity == NativeDatabaseIdentity",
    "nativeDatabaseIdentity = GetNativeDatabaseIdentity(document);",
    "if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity)) return;",
    "var pending = _pending;",
    "if (pending != null && !TryCloseOwner(pending)) return;",
    "var published = _published;",
    "published.Window.IsLoaded && published.Matches(document, nativeDatabaseIdentity)",
    "if (!TryCloseOwner(published)) return;",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "try { owner.Window.Close(); } catch { return false; }",
    "if (owner.Window.IsLoaded) return false;",
    "ReleaseOwnedWindow(owner);",
    "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    "database.UnmanagedObject == nativeDatabaseIdentity",
]
for needle in required:
    if needle not in text:
        errors.append("missing contract: " + needle)

show = text.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
publish = text.find("_published = owner;", show)
if show >= 0 and publish >= 0:
    if text.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show, publish) < 0:
        errors.append("missing exact-generation fence after host show")
    if text.find("if (!ReferenceEquals(_pending, owner))", show, publish) < 0:
        errors.append("missing exact pending-owner fence before publication")

pending_assign = text.find("_pending = owner;")
if show >= 0 and pending_assign >= 0 and pending_assign > show:
    errors.append("pending ownership must be rooted before host publication")

for forbidden in [
    "private static CommercialQsWindow? _window;",
    "private static CommercialQsWindow? _unpublishedCandidate;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(owner);",
]:
    if forbidden in text:
        errors.append("superseded/unsafe topology remains: " + forbidden)

print("QS3D V25 Commercial QS modeless document-affinity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)
print("PASS: Commercial QS modeless publication is bound to the exact managed document/native database generation.")
