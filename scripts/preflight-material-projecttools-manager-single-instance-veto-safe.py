#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    "Material": ROOT / "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs",
    "ProjectTools": ROOT / "src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs",
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
        "var previous = _published;",
        "if (previous.Window.IsLoaded)",
        "if (previous.Matches(document) && previous.MatchesManagedWrapper(document))",
        "previous.Window.Activate();",
        "previous.Window.Close();",
        "if (ReferenceEquals(_published, previous))",
        "_published = null;",
        "var published = new PublishedManager(window, document);",
        "window.Closed += (_, __) =>",
        "if (ReferenceEquals(_published, published)) _published = null;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "host show returned without a loaded window.",
        "_published = published;",
        "window = null;",
        "if (window != null)",
        "try { window.Close(); } catch { }",
    ]
    for needle in required:
        if needle not in source:
            errors.append(f"{label} manager missing lifecycle contract: {needle}")

    forbidden = [
        "if (previous.Matches(document))\n",
        "_published = null;\n                        try { previous.Window.Close();",
        "try { previous.Window.Close(); } catch { }",
        "_published = published;\n                Application.ShowModelessWindow",
    ]
    for needle in forbidden:
        if needle in source:
            errors.append(f"{label} manager contains unsafe publication shortcut: {needle.strip()}")

    capture = source.find("var previous = _published;")
    reuse = source.find("if (previous.Matches(document) && previous.MatchesManagedWrapper(document))")
    close = source.find("previous.Window.Close();")
    retained = source.find("if (ReferenceEquals(_published, previous))", close)
    construct = source.find("window = new ")
    show = source.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);")
    loaded = source.find("if (!window.IsLoaded)", show)
    publish = source.find("_published = published;", loaded)
    if min(capture, reuse, close, retained, construct, show, loaded, publish) < 0:
        errors.append(f"{label} manager ordering tokens are incomplete")
    elif not (capture < reuse < close < retained < construct < show < loaded < publish):
        errors.append(
            f"{label} manager must arbitrate/reuse, terminal-close, construct, show, confirm Loaded, then publish in fail-closed order"
        )

material = CASES["Material"].read_text(encoding="utf-8") if CASES["Material"].is_file() else ""
project_tools = CASES["ProjectTools"].read_text(encoding="utf-8") if CASES["ProjectTools"].is_file() else ""

material_project = material.find("ExistingProjectMutationContext.TryGet(document, out var project)")
material_capture = material.find("var previous = _published;")
if material_project < 0 or material_capture < 0 or material_project > material_capture:
    errors.append("Material manager must preserve the existing-project requirement before reuse/publication arbitration")
if "new MaterialCatalogWindow(document, project)" not in material:
    errors.append("Material manager must preserve the exact bound project passed to its wrapper-bound window")

if "new ProjectToolsWindow(document)" not in project_tools:
    errors.append("Project Tools manager must preserve explicit source-Document construction")

print("QS3D Material/Project Tools manager single-instance veto-safe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print(
    "PASS: Material Catalog and Project Tools retain exact native+managed-wrapper affinity, "
    "terminal close arbitration, veto safety, loaded host-show admission and instance-safe Closed release."
)
