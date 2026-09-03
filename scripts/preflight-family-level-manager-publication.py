#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    "family": (ROOT / "src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs", "FamilyManagerWindow", "Family Manager"),
    "level": (ROOT / "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs", "FloorLevelWindow", "Level Manager"),
}

errors = []
for key, (path, window_type, label) in CASES.items():
    if not path.is_file():
        errors.append(f"{key}: missing command source")
        continue
    source = path.read_text(encoding="utf-8")
    required = [
        "private static PublishedManager? _pending;",
        "private static PublishedManager? _published;",
        'CloseOwnerBeforeReplacement(pending, "pending")',
        "previous.Matches(document)",
        "previous.MatchesManagedWrapper(document)",
        'CloseOwnerBeforeReplacement(previous, "published")',
        f"var window = new {window_type}(document);",
        "var owner = new PublishedManager(window, document);",
        "if (ReferenceEquals(_pending, owner)) _pending = null;",
        "if (ReferenceEquals(_published, owner)) _published = null;",
        "_pending = owner;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, owner))",
        "_published = owner;",
        "candidate != null && ReferenceEquals(_pending, candidate)",
        "candidate.Window.Close();",
        'string.Equals(state, "published", StringComparison.Ordinal)',
        "owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)",
        "ex.GetType().Name",
    ]
    for marker in required:
        if marker not in source:
            errors.append(f"{key}: publication contract missing: {marker}")
    if "ex.Message" in source:
        errors.append(f"{key}: user-visible failure paths must not expose raw exception messages")

    try:
        drain = source.index('CloseOwnerBeforeReplacement(pending, "pending")')
        prior = source.index("var previous = _published;")
        construct = source.index(f"var window = new {window_type}(document);")
        owner = source.index("var owner = new PublishedManager(window, document);", construct)
        closed = source.index("window.Closed +=", owner)
        pending = source.index("_pending = owner;", closed)
        show = source.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", pending)
        loaded = source.index("if (!window.IsLoaded)", show)
        exact = source.index("if (!ReferenceEquals(_pending, owner))", loaded)
        clear = source.index("_pending = null;", exact)
        publish = source.index("_published = owner;", clear)
        release = source.index("candidate = null;", publish)
        if not (drain < prior < construct < owner < closed < pending < show < loaded < exact < clear < publish < release):
            errors.append(f"{key}: pending/publication transfer ordering is unsafe")
    except ValueError as exc:
        errors.append(f"{key}: publication ordering marker missing: {exc}")

    try:
        helper = source.index("private static void CloseOwnerBeforeReplacement")
        stale = source.index('string.Equals(state, "published", StringComparison.Ordinal)', helper)
        close = source.index("owner.Window.Close();", helper)
        terminal = source.index("owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)", close)
        if not (helper < stale < close < terminal):
            errors.append(f"{key}: cleanup must reserve stale repair for published owners and prove terminal close")
    except ValueError as exc:
        errors.append(f"{key}: cleanup ordering marker missing: {exc}")

if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Family and Level managers retain pending ownership through host publication, fail closed on non-terminal cleanup, publish only loaded exact owners, isolate stale callbacks, and redact raw host failures.")
