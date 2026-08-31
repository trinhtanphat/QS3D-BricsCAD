from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src" / "QS3D.BricsCAD.V25" / "WallQuantityCommands.cs"
text = SOURCE.read_text(encoding="utf-8")

required = [
    "private static WallQuantityWindow? _window;",
    "private static Document? _document;",
    "private static IntPtr _nativeDatabaseIdentity;",
    "GetNativeDatabaseIdentity(document)",
    "PreparePublishedWindow(document, nativeDatabaseIdentity)",
    "ReferenceEquals(_document, requestedDocument)",
    "published.Close();",
    "if (published.IsLoaded)",
    "window.Closed += (_, __) => ReleasePublishedWindow(window);",
    "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
    "if (!window.IsLoaded) return;",
    "_window = window;",
    "_document = document;",
    "_nativeDatabaseIdentity = nativeDatabaseIdentity;",
    "if (!ReferenceEquals(_window, window)) return;",
    "database.UnmanagedObject",
    "if (identity == IntPtr.Zero)",
]

missing = [token for token in required if token not in text]
if missing:
    raise SystemExit("Wall Quantity publication preflight failed; missing: " + ", ".join(missing))

forbidden = [
    "ShowModelessWindow(IntPtr.Zero, new WallQuantityWindow(document)",
    "_window = null;\n            _document = null;\n            _nativeDatabaseIdentity = IntPtr.Zero;\n        }\n\n        [CommandMethod",
]
for token in forbidden:
    if token in text:
        raise SystemExit("Wall Quantity publication preflight failed; forbidden source shape: " + token)

show_pos = text.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
loaded_pos = text.index("if (!window.IsLoaded) return;", show_pos)
publish_pos = text.index("_window = window;", loaded_pos)
if not show_pos < loaded_pos < publish_pos:
    raise SystemExit("Wall Quantity publication preflight failed; candidate must show, confirm Loaded, then publish")

close_pos = text.index("published.Close();")
post_close_loaded_pos = text.index("if (published.IsLoaded)", close_pos)
release_pos = text.index("ReleasePublishedWindow(published);", post_close_loaded_pos)
if not close_pos < post_close_loaded_pos < release_pos:
    raise SystemExit("Wall Quantity publication preflight failed; replacement must terminal-close before release")

print("PASS Wall Quantity single-instance document-safe publication lifecycle")
