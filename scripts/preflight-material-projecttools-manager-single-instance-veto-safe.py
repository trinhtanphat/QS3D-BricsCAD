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


def check_pending_first(source, label, constructor):
    require(
        source,
        label,
        "private static PublishedManager? _pending;",
        "private static PublishedManager? _published;",
        "private readonly WeakReference<Document> _document;",
        "database.UnmanagedObject == IntPtr.Zero",
        "NativeDatabaseIdentity = database.UnmanagedObject;",
        "_document = new WeakReference<Document>(document);",
        "database.UnmanagedObject == NativeDatabaseIdentity",
        "_document.TryGetTarget(out var ownedDocument)",
        "ReferenceEquals(ownedDocument, document)",
        "var pending = _pending;",
        "pending.Matches(document) && pending.MatchesManagedWrapper(document)",
        "var previous = _published;",
        "if (previous.Window.IsLoaded)",
        "if (previous.Matches(document) && previous.MatchesManagedWrapper(document))",
        "previous.Window.Close();",
        "if (ReferenceEquals(_published, previous))",
        constructor,
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
        "candidate = null;",
        "window = null;",
        "try { window.Close(); } catch { }",
    )
    ordered(
        source,
        f"{label} pending-first publication",
        "var pending = _pending;",
        "var previous = _published;",
        constructor,
        "candidate = new PublishedManager(window, document);",
        "var reserved = candidate;",
        "_pending = reserved;",
        "window.Closed += (_, __) =>",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)",
        "if (!ReferenceEquals(_pending, reserved))",
        "_pending = null;",
        "_published = reserved;",
        "candidate = null;",
        "window = null;",
    )
    for forbidden in (
        "_published = reserved;\n                Application.ShowModelessWindow",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);\n                _pending = reserved;",
        "try { previous.Window.Close(); } catch { }",
    ):
        if forbidden in source:
            errors.append(f"{label} contains unsafe publication/close shortcut: {forbidden.strip()}")


if not MATERIAL.is_file():
    errors.append(f"missing Material manager command source: {MATERIAL.relative_to(ROOT)}")
else:
    material = MATERIAL.read_text(encoding="utf-8")
    check_pending_first(material, "Material manager", "window = new MaterialCatalogWindow(document, project);")
    require(
        material,
        "Material manager",
        "ExistingProjectMutationContext.TryGet(document, out var project)",
        "new MaterialCatalogWindow(document, project)",
        "if (candidate != null && ReferenceEquals(_pending, candidate))",
        "QS3DMATERIALS không thể mở Material Catalog an toàn; trạng thái hiện tại được giữ nguyên.",
    )
    if "ex.Message" in material:
        errors.append("Material manager must not expose caught host exception details")

if not PROJECT_TOOLS.is_file():
    errors.append(f"missing ProjectTools manager command source: {PROJECT_TOOLS.relative_to(ROOT)}")
else:
    project_tools = PROJECT_TOOLS.read_text(encoding="utf-8")
    check_pending_first(project_tools, "ProjectTools manager", "window = new ProjectToolsWindow(document);")
    require(
        project_tools,
        "ProjectTools manager",
        "new ProjectToolsWindow(document)",
        "ClosePendingOnAffinityDrift(reserved);",
        "ClosePendingAfterFailure(candidate, window);",
        "QS3DPROJECTTOOLS không thể mở Project Tools an toàn; trạng thái hiện tại được giữ nguyên.",
    )

print("QS3D Material/Project Tools manager single-instance veto-safe preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    raise SystemExit(1)

print(
    "PASS: Material Catalog and Project Tools both reserve exact pending ownership before host show, retain native+managed-wrapper affinity and terminal close/veto arbitration, require Loaded + exact-owner admission before publication, and release only the exact pending/published generation."
)
