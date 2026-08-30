#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
CASES = {
    "Project Properties": (
        ROOT / "src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs",
        "ProjectPropertiesWindow",
    ),
    "Geometry Extensions": (
        ROOT / "src/QS3D.BricsCAD.V25/GeometryExtensionsCommands.cs",
        "GeometryExtensionsWindow",
    ),
}
errors = []

for label, (path, window_type) in CASES.items():
    if not path.is_file():
        errors.append(f"missing {label} command source: {path.relative_to(ROOT)}")
        continue

    source = path.read_text(encoding="utf-8")
    required = [
        f"private static {window_type}? _published;",
        "var previous = _published;",
        "if (previous.IsLoaded)",
        "previous.Activate();",
        "if (ReferenceEquals(_published, previous))",
        "_published = null;",
        f"window = new {window_type}();",
        "var published = window;",
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
            errors.append(f"{label} missing host-global publication contract: {needle}")

    capture = source.find("var previous = _published;")
    reuse = source.find("if (previous.IsLoaded)", capture)
    stale_release = source.find("if (ReferenceEquals(_published, previous))", reuse)
    construct = source.find(f"window = new {window_type}();", stale_release)
    closed = source.find("window.Closed += (_, __) =>", construct)
    show = source.find("Application.ShowModelessWindow(IntPtr.Zero, window, true);", closed)
    loaded = source.find("if (!window.IsLoaded)", show)
    publish = source.find("_published = published;", loaded)
    local_release = source.find("window = null;", publish)
    if min(capture, reuse, stale_release, construct, closed, show, loaded, publish, local_release) < 0:
        errors.append(f"{label} publication ordering tokens are incomplete")
    elif not (capture < reuse < stale_release < construct < closed < show < loaded < publish < local_release):
        errors.append(
            f"{label} must reuse loaded owner, release stale owner, construct, bind Closed, show, confirm Loaded, then publish"
        )

    if source.find("_published = published;", show, loaded) >= 0:
        errors.append(f"{label} publishes before Loaded admission")

geometry_code = ROOT / "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml.cs"
if not geometry_code.is_file():
    errors.append("missing GeometryExtensionsWindow code-behind")
else:
    geometry = geometry_code.read_text(encoding="utf-8")
    for needle in (
        "Application.DocumentManager.MdiActiveDocument",
        "document.SendStringToExecute(normalizedCommand + \" \", true, false, false);",
    ):
        if needle not in geometry:
            errors.append("Geometry Extensions must retain click-time active-document dispatch: " + needle)

properties_window = ROOT / "src/QS3D.BricsCAD.V25/UI/ProjectPropertiesWindow.cs"
if not properties_window.is_file():
    errors.append("missing ProjectPropertiesWindow")
else:
    properties = properties_window.read_text(encoding="utf-8")
    if "(Chưa xây dựng — Thuộc tính dự án)" not in properties:
        errors.append("Project Properties must retain the bounded BLT3D placeholder")
    for forbidden in ("ProjectState", "ProjectContextCoordinator", "ExistingProjectMutationContext"):
        if forbidden in properties:
            errors.append("Project Properties host-global placeholder must remain read-only: " + forbidden)

if errors:
    for error in errors:
        print("ERROR:", error)
    raise SystemExit(f"FAILED with {len(errors)} host-global utility publication error(s).")

print("PASS: Project Properties and Geometry Extensions publish exactly one Loaded host-global utility window while preserving product-specific semantics")
