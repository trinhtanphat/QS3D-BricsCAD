from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "ReferenceSearchCommands.cs"
WINDOW = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "ReferenceSearchWindow.xaml.cs"
commands = COMMANDS.read_text(encoding="utf-8")
window = WINDOW.read_text(encoding="utf-8")

required_commands = [
    "private static ReferenceSearchWindow? _window;",
    "GetNativeDatabaseIdentity(document)",
    "PreparePublishedWindow(document, nativeDatabaseIdentity)",
    "published.IsBoundTo(requestedDocument, requestedNativeDatabaseIdentity)",
    "published.Close();",
    "if (published.IsLoaded)",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "if (!ReferenceEquals(_window, window)) return;",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
]
missing = [token for token in required_commands if token not in commands]
if missing:
    raise SystemExit("Reference Search publication preflight failed; missing command contract: " + ", ".join(missing))

required_window = [
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
]
missing = [token for token in required_window if token not in window]
if missing:
    raise SystemExit("Reference Search publication preflight failed; missing window contract: " + ", ".join(missing))

forbidden = [
    "ShowModelessWindow(IntPtr.Zero, new ReferenceSearchWindow(document)",
    "Process.Start(url)",
]
for token in forbidden:
    if token in commands or token in window:
        raise SystemExit("Reference Search publication preflight failed; forbidden source shape: " + token)

show_pos = commands.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
loaded_pos = commands.index("if (!window.IsLoaded) return;", show_pos)
publish_pos = commands.index("_window = window;", loaded_pos)
if not show_pos < loaded_pos < publish_pos:
    raise SystemExit("Reference Search publication preflight failed; candidate must show, confirm Loaded, then publish")

close_pos = commands.index("published.Close();")
post_close_loaded_pos = commands.index("if (published.IsLoaded)", close_pos)
release_pos = commands.index("ReleasePublishedWindow(published);", post_close_loaded_pos)
if not close_pos < post_close_loaded_pos < release_pos:
    raise SystemExit("Reference Search publication preflight failed; replacement must terminal-close before release")

active_ref_pos = window.index("if (!ReferenceEquals(active, _document))")
native_read_pos = window.index("var activeIdentity = GetNativeDatabaseIdentity(active);", active_ref_pos)
native_guard_pos = window.index("if (activeIdentity != _nativeDatabaseIdentity)", native_read_pos)
if not active_ref_pos < native_read_pos < native_guard_pos:
    raise SystemExit("Reference Search publication preflight failed; browser launch must preserve wrapper and native-database affinity")

print("PASS Reference Search single-instance wrapper-safe publication lifecycle")
