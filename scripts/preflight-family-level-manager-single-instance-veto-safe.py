#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    "Family": ROOT / "src/QS3D.BricsCAD.V25/FamilyManagerCommands.cs",
    "Level": ROOT / "src/QS3D.BricsCAD.V25/FloorLevelCommands.cs",
}
errors = []

for label, path in CASES.items():
    if not path.is_file():
        errors.append(f"missing {label} manager command source: {path.relative_to(ROOT)}")
        continue

    source = path.read_text(encoding="utf-8")
    required = [
        "private static PublishedManager? _published;",
        "private readonly WeakReference<Document> _document;",
        "database.UnmanagedObject == IntPtr.Zero",
        "NativeDatabaseIdentity = database.UnmanagedObject;",
        "_document = new WeakReference<Document>(document);",
        "database.UnmanagedObject == NativeDatabaseIdentity",
        "_document.TryGetTarget(out var ownedDocument)",
        "ReferenceEquals(ownedDocument, document)",
        "ExistingProjectMutationContext.TryGet(document, out _);",
        "var previous = _published;",
        "if (previous.Window.IsLoaded)",
        "if (previous.Matches(document) && previous.MatchesManagedWrapper(document))",
        "previous.Window.Activate();",
        "previous.Window.Close();",
        "if (ReferenceEquals(_published, previous))",
        "_published = null;",
        "var publishedWindow = candidate;",
        "var published = new PublishedManager(publishedWindow, document);",
        "publishedWindow.Closed += (_, __) =>",
        "if (ReferenceEquals(_published, published)) _published = null;",
        "Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);",
        "_published = published;",
        "candidate = null;",
        "if (candidate != null)",
        "try { candidate.Close(); } catch { }",
    ]
    for needle in required:
        if needle not in source:
            errors.append(f"{label} manager missing lifecycle contract: {needle}")

    forbidden = [
        "if (previous.Matches(document))\n",
        "_published = null;\n                        try { previous.Window.Close();",
        "try { previous.Window.Close(); } catch { }",
    ]
    for needle in forbidden:
        if needle in source:
            errors.append(f"{label} manager contains unsafe publication shortcut: {needle.strip()}")

    warm = source.find("ExistingProjectMutationContext.TryGet(document, out _);")
    capture = source.find("var previous = _published;")
    reuse = source.find("if (previous.Matches(document) && previous.MatchesManagedWrapper(document))")
    close = source.find("previous.Window.Close();")
    retained = source.find("if (ReferenceEquals(_published, previous))", close)
    construct = source.find("candidate = new ")
    show = source.find("Application.ShowModelessWindow(IntPtr.Zero, publishedWindow, true);")
    publish = source.find("_published = published;", show)
    if min(warm, capture, reuse, close, retained, construct, show, publish) < 0:
        errors.append(f"{label} manager ordering tokens are incomplete")
    elif not (warm < capture < reuse < close < retained < construct < show < publish):
        errors.append(f"{label} manager must warm-bind, arbitrate/reuse, terminal-close, construct, show, then publish in fail-closed order")

print("QS3D Family/Level manager single-instance veto-safe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print("PASS: Family and Level managers preserve exact native+managed-wrapper affinity, terminal close arbitration, veto safety, publication-after-show and instance-safe Closed release.")
