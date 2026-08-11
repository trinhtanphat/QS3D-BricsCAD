#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ViewAids.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
CORE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
COMPACT = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs"
errors = []

for path in (PARTIAL, XAML, CORE, COMPACT):
    if not path.is_file():
        errors.append("missing Workspace viewport-aid source: " + str(path.relative_to(ROOT)))

if PARTIAL.is_file():
    text = PARTIAL.read_text(encoding="utf-8")
    required = (
        "private const int ObjectSnapSuppressedBit = 16384;",
        "private const int ObjectSnapModeMask = ObjectSnapSuppressedBit - 1;",
        "private static readonly bool ViewAidClassHandlerRegistered = RegisterViewportAidClassHandler();",
        "_ = ViewAidClassHandlerRegistered;",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "panel.EnsureViewportAidControls();",
        "panel.RefreshViewportAidState();",
        "if (_viewportAidsApplied) return;",
        "Grid.GetRow(border) == 2",
        "DockPanel.GetDock(stack) == Dock.Right",
        '"Vuông góc"',
        '"Bắt điểm"',
        "viewportStatus.Children.Add(_orthoModeCheck);",
        "viewportStatus.Children.Add(_objectSnapCheck);",
        "viewportStatus.MouseEnter += OnViewportAidBarMouseEnter;",
        'BcadApplication.SetSystemVariable("ORTHOMODE", (short)(enabled ? 1 : 0));',
        'var current = ReadSystemVariableInt("OSMODE");',
        "var configuredModes = current & ObjectSnapModeMask;",
        "if (enable && configuredModes == 0)",
        "? configuredModes",
        ": configuredModes | ObjectSnapSuppressedBit;",
        'BcadApplication.SetSystemVariable("OSMODE", checked((short)next));',
        'var orthoMode = ReadSystemVariableInt("ORTHOMODE");',
        'var osMode = ReadSystemVariableInt("OSMODE");',
        "var snapsSuppressed = (osMode & ObjectSnapSuppressedBit) != 0;",
        "_objectSnapCheck.IsChecked = configuredModes != 0 && !snapsSuppressed;",
        "BcadApplication.GetSystemVariable(name)",
        "QS3D sẽ không tự chọn preset thay bạn",
    )
    for needle in required:
        if needle not in text:
            errors.append("WorkspacePanel.ViewAids missing native drafting-aid contract: " + needle)

    ensure_pos = text.find("private void EnsureViewportAidControls()")
    idempotent_pos = text.find("if (_viewportAidsApplied) return;", ensure_pos)
    footer_pos = text.find("Grid.GetRow(border) == 2", idempotent_pos)
    dock_pos = text.find("DockPanel.GetDock(stack) == Dock.Right", footer_pos)
    add_ortho_pos = text.find("viewportStatus.Children.Add(_orthoModeCheck);", dock_pos)
    add_snap_pos = text.find("viewportStatus.Children.Add(_objectSnapCheck);", add_ortho_pos)
    applied_pos = text.find("_viewportAidsApplied = true;", add_snap_pos)
    if min(ensure_pos, idempotent_pos, footer_pos, dock_pos, add_ortho_pos, add_snap_pos, applied_pos) < 0 or not (
        ensure_pos < idempotent_pos < footer_pos < dock_pos < add_ortho_pos < add_snap_pos < applied_pos
    ):
        errors.append("viewport-aid injection must be idempotent and target only the existing right-docked footer status area")

    snap_handler = text.find("private void OnObjectSnapCheckClick")
    read_pos = text.find('ReadSystemVariableInt("OSMODE")', snap_handler)
    mask_pos = text.find("current & ObjectSnapModeMask", read_pos)
    zero_pos = text.find("if (enable && configuredModes == 0)", mask_pos)
    preserve_on_pos = text.find("? configuredModes", zero_pos)
    preserve_off_pos = text.find(": configuredModes | ObjectSnapSuppressedBit;", preserve_on_pos)
    write_pos = text.find('SetSystemVariable("OSMODE"', preserve_off_pos)
    if min(snap_handler, read_pos, mask_pos, zero_pos, preserve_on_pos, preserve_off_pos, write_pos) < 0 or not (
        snap_handler < read_pos < mask_pos < zero_pos < preserve_on_pos < preserve_off_pos < write_pos
    ):
        errors.append("OSMODE toggle must read current bits, fail closed on zero configured modes, preserve lower bits and only add/remove suppression bit")

    for forbidden in (
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "ProjectState",
        ".qsdb",
        "SendStringToExecute",
        'SetSystemVariable("OSMODE", (short)4135)',
        'SetSystemVariable("OSMODE", 4135)',
    ):
        if forbidden in text:
            errors.append("Workspace viewport aids must remain native CAD-state-only and must not invent snap presets: " + forbidden)

if XAML.is_file():
    text = XAML.read_text(encoding="utf-8")
    preserved = (
        'Grid.Row="2" Background="{StaticResource Bg1Brush}"',
        'Content="Mô hình"',
        'Click="OnViewModel3DClick"',
        'Content="BQ" Click="OnQuantityClick"',
        'Content="Kiểm tra" Click="OnHealthClick"',
        'DockPanel.Dock="Right" Orientation="Horizontal"',
        'Text="VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK"',
    )
    for needle in preserved:
        if needle not in text:
            errors.append("existing Workspace footer contract disappeared: " + needle)

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    for needle in (
        'private void OnViewModel3DClick(object sender, RoutedEventArgs e) => Send("QS3DVIEW3D");',
        'private void OnQuantityClick(object sender, RoutedEventArgs e) => Send("QS3DBQ");',
        'private void OnHealthClick(object sender, RoutedEventArgs e) => Send("QS3DHEALTH");',
    ):
        if needle not in text:
            errors.append("existing Workspace footer handler disappeared: " + needle)

if COMPACT.is_file():
    text = COMPACT.read_text(encoding="utf-8")
    for needle in (
        "private static void OnCompactShellLoaded",
        "TuneResponsiveHeader();",
        "TuneModelSectionHeaderCollision();",
    ):
        if needle not in text:
            errors.append("completed Workspace compact/header behavior must remain intact: " + needle)

print("QS3D Workspace native viewport-aid preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Workspace injects idempotent native ORTHOMODE/entity-snap controls, preserves configured OSMODE bits with the 16384 suppression flag, fails closed when no snap modes exist, refreshes on load/pointer-enter, and leaves QS3D semantic state plus existing footer/compact behavior untouched.")
