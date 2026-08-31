#!/usr/bin/env python3
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
errors = []

properties_path = ROOT / "src/QS3D.BricsCAD.V25/ProjectPropertiesCommands.cs"
if not properties_path.is_file():
    errors.append("missing Project Properties command source")
else:
    source = properties_path.read_text(encoding="utf-8")
    for needle in (
        "private static ProjectPropertiesWindow? _published;", "var previous = _published;", "if (previous.IsLoaded)",
        "previous.Activate();", "if (ReferenceEquals(_published, previous))", "_published = null;",
        "window = new ProjectPropertiesWindow();", "var published = window;", "window.Closed += (_, __) =>",
        "if (ReferenceEquals(_published, published)) _published = null;", "Application.ShowModelessWindow(IntPtr.Zero, window, true);",
        "if (!window.IsLoaded)", "host show returned without a loaded window.", "_published = published;", "window = null;",
        "if (window != null)", "try { window.Close(); } catch { }"):
        if needle not in source:
            errors.append("Project Properties missing host-global publication contract: " + needle)
    published_owner = source.find("_published = published;")
    clear_window = source.find("window = null;", published_owner) if published_owner >= 0 else -1
    positions = [source.find(token) for token in (
        "var previous = _published;", "if (previous.IsLoaded)", "if (ReferenceEquals(_published, previous))",
        "window = new ProjectPropertiesWindow();", "window.Closed += (_, __) =>",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);", "if (!window.IsLoaded)",
        "_published = published;")]
    positions.append(clear_window)
    if min(positions) < 0 or positions != sorted(positions):
        errors.append("Project Properties must reuse loaded owner, release stale owner, construct, bind Closed, show, confirm Loaded, then publish")

geometry_path = ROOT / "src/QS3D.BricsCAD.V25/GeometryExtensionsCommands.cs"
if not geometry_path.is_file():
    errors.append("missing Geometry Extensions command source")
else:
    source = geometry_path.read_text(encoding="utf-8")
    for needle in (
        "private static GeometryExtensionsWindow? _published;", "private static GeometryExtensionsWindow? _pending;",
        "var pending = _pending;", "if (pending != null && !TryClosePendingWindow(pending))",
        "var previous = _published;", "if (previous.IsLoaded)", "previous.Activate();", "ReleasePublishedWindow(previous);",
        "candidate = new GeometryExtensionsWindow();", "_pending = window;", "window.Closed += (_, __) => ReleaseWindow(window);",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);", "if (!window.IsLoaded)", "_published = window;",
        "ReleasePendingWindow(window);", "candidate = null;", "finally", "TryClosePendingWindow(candidate);",
        "if (!ReferenceEquals(_pending, window)) return true;", "if (ReferenceEquals(_published, window))",
        "if (window.IsLoaded) return false;"):
        if needle not in source:
            errors.append("Geometry Extensions missing pending-owner host-global publication contract: " + needle)
    release_pending = source.find("ReleasePendingWindow(window);")
    clear_candidate = source.find("candidate = null;", release_pending) if release_pending >= 0 else -1
    positions = [source.find(token) for token in (
        "var pending = _pending;", "if (pending != null && !TryClosePendingWindow(pending))", "var previous = _published;",
        "candidate = new GeometryExtensionsWindow();", "_pending = window;", "window.Closed += (_, __) => ReleaseWindow(window);",
        "Application.ShowModelessWindow(IntPtr.Zero, window, true);", "if (!window.IsLoaded)", "_published = window;",
        "ReleasePendingWindow(window);")]
    positions.extend([clear_candidate, source.find("finally"), source.find("TryClosePendingWindow(candidate);")])
    if min(positions) < 0 or positions != sorted(positions):
        errors.append("Geometry Extensions must drain pending failure before construct and transfer ownership only after Loaded admission")
    if source.find("_published = window;", source.find("Application.ShowModelessWindow"), source.find("if (!window.IsLoaded)")) >= 0:
        errors.append("Geometry Extensions publishes before Loaded admission")

geometry_code = ROOT / "src/QS3D.BricsCAD.V25/UI/GeometryExtensionsWindow.xaml.cs"
if not geometry_code.is_file():
    errors.append("missing GeometryExtensionsWindow code-behind")
else:
    geometry = geometry_code.read_text(encoding="utf-8")
    for needle in ("Application.DocumentManager.MdiActiveDocument", "document.SendStringToExecute(normalizedCommand + \" \", true, false, false);"):
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
print("PASS: Project Properties retains its original host-global publication contract and Geometry Extensions uses pending-owned failure-clean publication")
