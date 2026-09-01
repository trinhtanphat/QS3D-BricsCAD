#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
MATERIAL = ROOT / "src/QS3D.BricsCAD.V25/MaterialCatalogCommands.cs"
PROJECT_TOOLS = ROOT / "src/QS3D.BricsCAD.V25/ProjectToolsCommands.cs"
errors = []


def require(source, label, *needles):
    for needle in needles:
        if needle not in source:
            errors.append(f"{label} missing lifecycle contract: {needle}")


def ordered(source, label, *needles):
    position = 0
    for needle in needles:
        found = source.find(needle, position)
        if found < 0:
            errors.append(f"{label} ordering token missing/late: {needle}")
            return
        position = found + len(needle)


if not MATERIAL.is_file():
    errors.append(f"missing Material manager command source: {MATERIAL.relative_to(ROOT)}")
else:
    material = MATERIAL.read_text(encoding="utf-8")
    require(
        material,
        "Material manager",
        "private static PublishedManager? _pending;",
        "private static PublishedManager? _published;",
        "private readonly WeakReference<Document> _document;",
        "database.UnmanagedObject == IntPtr.Zero",
        "NativeDatabaseIdentity = database.UnmanagedObject;",
        "_document = new WeakReference<Document>(document);",
        "database.UnmanagedObject == NativeDatabaseIdentity",
        "_document.TryGetTarget(out var ownedDocument)",
        "ReferenceEquals(ownedDocument, document)",
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "new MaterialCatalogWindow(document, project)",
        "var pending = _pending;",
        "pending.Matches(document) && pending.MatchesManagedWrapper(document)",
        "var previous = _published;",
        "if (previous.Window.IsLoaded)",
        "if (previous.Matches(document) && previous.MatchesManagedWrapper(document))",
        "previous.Window.Close();",
        "if (ReferenceEquals(_published, previous))",
        "candidate = new PublishedManager(window, document);",
        "var reserved = candidate;",
        "_pending = reserved;",
        "window.Closed += (_, __) =>",
        "if (ReferenceEquals(_pending, reserved)) _pending = null;",
        "if (ReferenceEquals(_published, reserved)) _published = null;",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, reserved))",
        "_pending = null;",
        "_published = reserved;",
        "if (candidate != null && ReferenceEquals(_pending, candidate))",
        "try { window.Close(); } catch { }",
        "QS3DMATERIALS không thể mở Material Catalog an toàn; trạng thái hiện tại được giữ nguyên.",
    )
    ordered(
        material,
        "Material manager pending-first publication",
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "var pending = _pending;",
        "var previous = _published;",
        "window = new MaterialCatalogWindow(document, project);",
        "candidate = new PublishedManager(window, document);",
        "var reserved = candidate;",
        "_pending = reserved;",
        "window.Closed += (_, __) =>",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, reserved))",
        "_pending = null;",
        "_published = reserved;",
    )
    for forbidden in (
        "ex.Message",
        "_published = reserved;\n                Application.ShowModelessWindow",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);\n                _pending = reserved;",
    ):
        if forbidden in material:
            errors.append(f"Material manager contains unsafe publication/reporting shortcut: {forbidden.strip()}")

if not PROJECT_TOOLS.is_file():
    errors.append(f"missing ProjectTools manager command source: {PROJECT_TOOLS.relative_to(ROOT)}")
else:
    project_tools = PROJECT_TOOLS.read_text(encoding="utf-8")
    require(
        project_tools,
        "ProjectTools manager",
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
        "new ProjectToolsWindow(document)",
    )
    ordered(
        project_tools,
        "ProjectTools manager published lifecycle",
        "var previous = _published;",
        "if (previous.Matches(document) && previous.MatchesManagedWrapper(document))",
        "previous.Window.Close();",
        "if (ReferenceEquals(_published, previous))",
        "window = new ProjectToolsWindow(document);",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "_published = published;",
    )
    for forbidden in (
        "if (previous.Matches(document))\n",
        "_published = null;\n                        try { previous.Window.Close();",
        "try { previous.Window.Close(); } catch { }",
        "_published = published;\n                Application.ShowModelessWindow",
    ):
        if forbidden in project_tools:
            errors.append(f"ProjectTools manager contains unsafe publication shortcut: {forbidden.strip()}")

print("QS3D Material/Project Tools manager single-instance veto-safe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print(
    "PASS: Material Catalog uses pending-first exact-owner publication with redacted failure handling, while Project Tools retains exact native+managed-wrapper affinity, terminal close arbitration, veto safety, loaded host-show admission and instance-safe Closed release."
)
