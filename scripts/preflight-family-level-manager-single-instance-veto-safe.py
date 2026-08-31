#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    "Family": (ROOT / "src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs", "FamilyManagerWindow"),
    "Level": (ROOT / "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs", "FloorLevelWindow"),
}
errors = []

for label, (path, window_type) in CASES.items():
    if not path.is_file():
        errors.append(f"missing {label} manager command source: {path.relative_to(ROOT)}")
        continue

    source = path.read_text(encoding="utf-8")
    required = [
        "private static PublishedManager? _pending;",
        "private static PublishedManager? _published;",
        "private readonly WeakReference<Document> _document;",
        "database.UnmanagedObject == IntPtr.Zero",
        "NativeDatabaseIdentity = database.UnmanagedObject;",
        "_document = new WeakReference<Document>(document);",
        "database.UnmanagedObject == NativeDatabaseIdentity",
        "_document.TryGetTarget(out var ownedDocument)",
        "ReferenceEquals(ownedDocument, document)",
        "ExistingProjectMutationContext.TryGet(document, out _);",
        "var pending = _pending;",
        'CloseOwnerBeforeReplacement(pending, "pending");',
        "var previous = _published;",
        "previous.Window.IsLoaded &&",
        "previous.Matches(document) &&",
        "previous.MatchesManagedWrapper(document)",
        "previous.Window.Activate();",
        'CloseOwnerBeforeReplacement(previous, "published");',
        f"var window = new {window_type}(document);",
        "var owner = new PublishedManager(window, document);",
        "window.Closed += (_, __) =>",
        "if (ReferenceEquals(_pending, owner)) _pending = null;",
        "if (ReferenceEquals(_published, owner)) _published = null;",
        "_pending = owner;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, owner))",
        "_pending = null;",
        "_published = owner;",
        "candidate = null;",
        "candidate != null && ReferenceEquals(_pending, candidate)",
        "candidate.Window.Close();",
        'string.Equals(state, "published", StringComparison.Ordinal)',
        "owner.Window.Close();",
        "owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)",
        "ex.GetType().Name",
    ]
    for needle in required:
        if needle not in source:
            errors.append(f"{label} manager missing lifecycle contract: {needle}")

    forbidden = [
        "var publishedWindow = candidate;",
        "Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);",
        "try { previous.Window.Close(); } catch { }",
        "ex.Message",
    ]
    for needle in forbidden:
        if needle in source:
            errors.append(f"{label} manager contains unsafe publication shortcut: {needle}")

    try:
        warm = source.index("ExistingProjectMutationContext.TryGet(document, out _);")
        pending_read = source.index("var pending = _pending;", warm)
        pending_close = source.index('CloseOwnerBeforeReplacement(pending, "pending");', pending_read)
        published_read = source.index("var previous = _published;", pending_close)
        reuse = source.index("previous.MatchesManagedWrapper(document)", published_read)
        published_close = source.index('CloseOwnerBeforeReplacement(previous, "published");', reuse)
        construct = source.index(f"var window = new {window_type}(document);", published_close)
        owner = source.index("var owner = new PublishedManager(window, document);", construct)
        closed = source.index("window.Closed +=", owner)
        own_pending = source.index("_pending = owner;", closed)
        show = source.index("Application.ShowModelessWindow(IntPtr.Zero, window, true);", own_pending)
        loaded = source.index("if (!window.IsLoaded)", show)
        exact = source.index("if (!ReferenceEquals(_pending, owner))", loaded)
        clear = source.index("_pending = null;", exact)
        publish = source.index("_published = owner;", clear)
        release = source.index("candidate = null;", publish)
        if not (warm < pending_read < pending_close < published_read < reuse < published_close < construct < owner < closed < own_pending < show < loaded < exact < clear < publish < release):
            errors.append(f"{label} manager must drain pending, arbitrate published owner, construct exact candidate, own before host show, prove loaded/exact ownership, then publish")
    except ValueError as exc:
        errors.append(f"{label} manager ordering token missing: {exc}")

    try:
        helper = source.index("private static void CloseOwnerBeforeReplacement")
        stale = source.index('string.Equals(state, "published", StringComparison.Ordinal)', helper)
        close = source.index("owner.Window.Close();", stale)
        terminal = source.index("owner.Window.IsLoaded || ReferenceEquals(_pending, owner) || ReferenceEquals(_published, owner)", close)
        if not (helper < stale < close < terminal):
            errors.append(f"{label} manager cleanup must stale-repair only published owners and prove terminal close")
    except ValueError as exc:
        errors.append(f"{label} manager cleanup ordering token missing: {exc}")

print("QS3D Family/Level manager single-instance veto-safe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Family and Level managers preserve exact native+managed-wrapper affinity, pending-first ownership, terminal close arbitration, loaded/exact publication proof, stale-callback isolation, and redacted host failures.")
