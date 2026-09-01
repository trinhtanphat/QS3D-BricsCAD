#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/Rebar3DHubCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing Rebar3DHubCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private static Rebar3DHubWindow? _pending;",
        "private static Rebar3DHubWindow? _published;",
        "var pending = _pending;",
        'CloseOwnerBeforeReplacement(pending, "pending");',
        "var published = _published;",
        "if (published.IsLoaded)",
        'CloseOwnerBeforeReplacement(published, "published");',
        "candidate = window;",
        "if (ReferenceEquals(_pending, window)) _pending = null;",
        "if (ReferenceEquals(_published, window)) _published = null;",
        "_pending = window;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, window))",
        "_pending = null;",
        "_published = window;",
        "candidate = null;",
        "CloseOwnerBeforeReplacement(Rebar3DHubWindow window, string state)",
        "if (window.IsLoaded || ReferenceEquals(_pending, window) || ReferenceEquals(_published, window))",
        'var message = "QS3DREBARHUB không thể mở Rebar 3D Hub (" + ex.GetType().Name + ").";',
    )
    for needle in required:
        if needle not in text:
            errors.append("Rebar Hub publication contract missing: " + needle)

    forbidden = (
        "private static Rebar3DHubWindow? _window;",
        '"\\nQS3DREBARHUB lỗi: " + ex.Message',
        'PaletteCoordinator.SetStatus("QS3DREBARHUB lỗi: " + ex.Message)',
        "TryCloseUnpublishedWindow",
    )
    for needle in forbidden:
        if needle in text:
            errors.append("Rebar Hub publication contract retains unsafe legacy pattern: " + needle)

    try:
        pending_read = text.index("var pending = _pending;")
        published_read = text.index("var published = _published;", pending_read)
        construct = text.index("var window = new Rebar3DHubWindow();", published_read)
        pending_assign = text.index("_pending = window;", construct)
        show = text.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending_assign)
        loaded = text.index("if (!window.IsLoaded)", show)
        exact = text.index("if (!ReferenceEquals(_pending, window))", loaded)
        clear = text.index("_pending = null;", exact)
        publish = text.index("_published = window;", clear)
        transfer = text.index("candidate = null;", publish)
        if not (pending_read < published_read < construct < pending_assign < show < loaded < exact < clear < publish < transfer):
            errors.append("Rebar Hub publication ownership steps must remain ordered pending-drain -> construct -> pending -> show -> loaded/proof -> publish")
    except ValueError as exc:
        errors.append("Rebar Hub publication ordering marker missing: " + str(exc))

print("QS3D Rebar 3D Hub publication preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: Rebar 3D Hub retains exact pending ownership through host publication and fails closed before duplicate construction.")
