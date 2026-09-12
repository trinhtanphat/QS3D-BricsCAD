from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src/QS3D.BricsCAD.V25/ReferenceSearchCommands.cs"
WINDOW = ROOT / "src/QS3D.BricsCAD.V25/UI/ReferenceSearchWindow.xaml.cs"
commands = COMMANDS.read_text(encoding="utf-8-sig")
window = WINDOW.read_text(encoding="utf-8-sig")

required_commands = (
    "private static PublishedWindow? _pending;",
    "private static PublishedWindow? _published;",
    "GetNativeDatabaseIdentity(document)",
    "PreparePublishedWindow(document, nativeDatabaseIdentity)",
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded)",
    "if (!ReferenceEquals(_pending, owner))",
    "_pending = null;",
    "_published = owner;",
    "owner = null;",
    "try { owner.Window.Close(); } catch { }",
    "if (!owner.Window.IsLoaded) ReleaseOwnedWindow(owner);",
    "if (published.NativeDatabaseIdentity == requestedNativeDatabaseIdentity &&",
    "ReferenceEquals(published.Document, requestedDocument)",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
)
missing = [token for token in required_commands if token not in commands]
if missing:
    raise SystemExit("Reference Search publication preflight failed; missing generation-bound command contract: " + ", ".join(missing))

required_window = (
    "private readonly Document _document;",
    "private readonly IntPtr _nativeDatabaseIdentity;",
    "_nativeDatabaseIdentity = GetNativeDatabaseIdentity(_document);",
    "internal bool IsBoundTo(Document document, IntPtr nativeDatabaseIdentity)",
    "ReferenceEquals(_document, document)",
    "DocumentBoundWindowLifetime.Attach(this, _document);",
    "var active = Application.DocumentManager.MdiActiveDocument;",
    "if (!ReferenceEquals(active, _document))",
    "var activeIdentity = GetNativeDatabaseIdentity(active);",
    "if (activeIdentity != _nativeDatabaseIdentity)",
    "UseShellExecute = true",
    "safe=active",
    "private const int MaxQueryLength = 512;",
)
missing = [token for token in required_window if token not in window]
if missing:
    raise SystemExit("Reference Search publication preflight failed; missing window contract: " + ", ".join(missing))

for token in (
    "private static ReferenceSearchWindow? _window;",
    "ShowModelessWindow(IntPtr.Zero, new ReferenceSearchWindow(document)",
    "Process.Start(url)",
):
    if token in commands or token in window:
        raise SystemExit("Reference Search publication preflight failed; forbidden legacy source shape: " + token)

prefix = (
    "owner = new PublishedWindow(window, document, nativeDatabaseIdentity);",
    "var releaseOwner = owner;",
    "window.Closed += (_, __) => ReleaseOwnedWindow(releaseOwner);",
    "_pending = owner;",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
)
pos = [commands.find(token) for token in prefix]
if min(pos) < 0 or pos != sorted(pos) or len(set(pos)) != len(pos):
    raise SystemExit("Reference Search candidate must be generation-owned and pending-rooted before host show")

show = commands.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
post = commands.find("if (!IsActiveDocumentGeneration(document, nativeDatabaseIdentity))", show)
loaded = commands.find("if (!window.IsLoaded)", post)
exact = commands.find("if (!ReferenceEquals(_pending, owner))", loaded)
clear_pending = commands.find("_pending = null;", exact)
publish = commands.find("_published = owner;", clear_pending)
release_local = commands.find("owner = null;", publish)
if min(show, post, loaded, exact, clear_pending, publish, release_local) < 0 or not (
    show < post < loaded < exact < clear_pending < publish < release_local
):
    raise SystemExit("Reference Search must revalidate generation and Loaded/exact-owner before publication")
active_ref_pos = window.index("if (!ReferenceEquals(active, _document))")
native_read_pos = window.index("var activeIdentity = GetNativeDatabaseIdentity(active);", active_ref_pos)
native_guard_pos = window.index("if (activeIdentity != _nativeDatabaseIdentity)", native_read_pos)
if not active_ref_pos < native_read_pos < native_guard_pos:
    raise SystemExit("Reference Search browser launch must preserve wrapper and native-database affinity")

print("PASS Reference Search generation-bound, pending-first, wrapper-safe publication lifecycle")
