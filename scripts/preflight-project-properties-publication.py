#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs"
errors = []

if not SOURCE.is_file():
    errors.append("missing ProjectPropertiesCommands.cs")
else:
    text = SOURCE.read_text(encoding="utf-8")
    required = (
        "private static ProjectPropertiesWindow? _pending;",
        "private static ProjectPropertiesWindow? _published;",
        "var pending = _pending;",
        'CloseOwnerBeforeReplacement(pending, "pending");',
        "var published = _published;",
        "if (published.IsLoaded)",
        'CloseOwnerBeforeReplacement(published, "published");',
        "var window = new ProjectPropertiesWindow();",
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
        "CloseOwnerBeforeReplacement(ProjectPropertiesWindow window, string state)",
        "if (window.IsLoaded || ReferenceEquals(_pending, window) || ReferenceEquals(_published, window))",
        "ex.GetType().Name",
    )
    for token in required:
        if token not in text:
            errors.append("Project Properties publication contract missing: " + token)

    for token in (
        "ProjectPropertiesWindow? window = null;",
        "var previous = _published;",
        "var published = window;",
        '"QS3DPROJECTPROPERTIES lỗi: " + ex.Message',
    ):
        if token in text:
            errors.append("Project Properties retains unsafe legacy publication/error pattern: " + token)

    try:
        pending_read = text.index("var pending = _pending;")
        pending_drain = text.index('CloseOwnerBeforeReplacement(pending, "pending");', pending_read)
        published_read = text.index("var published = _published;", pending_drain)
        construct = text.index("var window = new ProjectPropertiesWindow();", published_read)
        pending_assign = text.index("_pending = window;", construct)
        host_show = text.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending_assign)
        loaded = text.index("if (!window.IsLoaded)", host_show)
        exact = text.index("if (!ReferenceEquals(_pending, window))", loaded)
        clear = text.index("_pending = null;", exact)
        publish = text.index("_published = window;", clear)
        transfer = text.index("candidate = null;", publish)
        if not (pending_read < pending_drain < published_read < construct < pending_assign < host_show < loaded < exact < clear < publish < transfer):
            errors.append("Project Properties publication ordering is not pending-first/exact-owner")
    except ValueError as exc:
        errors.append("Project Properties publication ordering marker missing: " + str(exc))

print("QS3D Project Properties publication preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print(f"FAILED with {len(errors)} error(s).")
    sys.exit(1)
print("PASS: Project Properties retains exact pending ownership through host publication and refuses duplicate replacement until cleanup is terminal.")
