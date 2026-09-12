#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "RoomFinishScheduleWindowCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

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
    "if (ReferenceEquals(_pending, owner)) _pending = null;",
    "if (ReferenceEquals(_published, owner)) _published = null;",
    "ReferenceEquals(Application.DocumentManager.MdiActiveDocument, document)",
    "database.UnmanagedObject == nativeDatabaseIdentity",
]
missing = [needle for needle in required if needle not in text]
if missing:
    raise SystemExit("Room Finish Schedule publication guard missing behavioral contract tokens: " + "; ".join(missing))

show = text.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
post_show_generation = text.index("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show)
loaded = text.index("if (!window.IsLoaded)", post_show_generation)
exact_owner = text.index("if (!ReferenceEquals(_pending, owner))", loaded)
clear_pending = text.index("_pending = null;", exact_owner)
publish = text.index("_published = owner;", clear_pending)
if not (show < post_show_generation < loaded < exact_owner < clear_pending < publish):
    raise SystemExit("Room Finish Schedule must revalidate generation and exact pending owner before publication")

pending_assign = text.index("_pending = owner;")
if pending_assign > show:
    raise SystemExit("Room Finish Schedule must root pending ownership before host publication")

close_call = text.index("try { owner.Window.Close(); } catch { return false; }")
close_check = text.index("if (owner.Window.IsLoaded) return false;", close_call)
release_after_close = text.index("ReleaseOwnedWindow(owner);", close_check)
if not (close_call < close_check < release_after_close):
    raise SystemExit("Room Finish Schedule replacement must retain ownership until terminal close")

for forbidden in [
    "private static RoomFinishScheduleWindow? _window;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(owner);",
    "new RoomFinishScheduleWindow(document), true",
]:
    if forbidden in text:
        raise SystemExit("Room Finish Schedule publication guard found superseded/unsafe topology: " + forbidden)

print("PASS Room Finish Schedule generation-bound pending-first modeless publication lifecycle")
