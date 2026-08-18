#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ViewAids.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
CORE = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
COMPACT = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs"
LAYOUT = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.ReferencePaletteLayout.cs"
V26_PROJECT = ROOT / "src/QS3D.BricsCAD.V26/QS3D.BricsCAD.V26.csproj"
errors = []

for path in (PARTIAL, XAML, CORE, COMPACT, LAYOUT, V26_PROJECT):
    if not path.is_file():
        errors.append("missing Workspace viewport-aid source: " + str(path.relative_to(ROOT)))

if PARTIAL.is_file():
    text = PARTIAL.read_text(encoding="utf-8")
    required = (
        "private const int ObjectSnapEndpointBit = 1;",
        "private const int ObjectSnapMidpointBit = 2;",
        "private const int ObjectSnapCenterBit = 4;",
        "private const int ObjectSnapNearestBit = 512;",
        "private const int ObjectSnapSuppressedBit = 16384;",
        "private const int ObjectSnapModeMask = ObjectSnapSuppressedBit - 1;",
        'private const string ViewportAidPanelTag = "QS3D_REFERENCE_VIEWPORT_AIDS";',
        'private const string LightBackgroundColor = "RGB:250,250,250";',
        'private const string ContrastBackgroundColor = "RGB:0,0,0";',
        "private static readonly bool ViewAidClassHandlerRegistered = RegisterViewportAidClassHandler();",
        "_ = ViewAidClassHandlerRegistered;",
        "EventManager.RegisterClassHandler(",
        "FrameworkElement.LoadedEvent",
        "panel.EnsureViewportAidControls();",
        "panel.RefreshViewportAidState();",
        "if (_viewportAidsApplied) return;",
        "var root = WorkspaceContentRoot;",
        "if (root == null) return;",
        "Grid.GetRow(border) == 2",
        "string.Equals(stack.Tag as string, ViewportAidPanelTag, StringComparison.Ordinal)",
        "DockPanel.SetDock(viewportStatus, Dock.Right);",
        "footerDock.Children.Insert(contextIndex, viewportStatus);",
        "footerDock.Children.Add(viewportStatus);",
        '"Nền sáng"',
        '"Tương phản"',
        '"Vuông góc"',
        '"Bắt điểm"',
        '"Điểm cuối"',
        '"Trung điểm"',
        '"Tâm"',
        '"Trên cạnh"',
        "viewportStatus.Children.Add(_lightBackgroundButton);",
        "viewportStatus.Children.Add(_contrastBackgroundButton);",
        "viewportStatus.Children.Add(_orthoModeButton);",
        "viewportStatus.Children.Add(_objectSnapButton);",
        "viewportStatus.Children.Add(_objectSnapMenuButton);",
        'ReadSystemVariableString("BKGCOLOR")',
        'BcadApplication.SetSystemVariable("BKGCOLOR", next);',
        'ReadSystemVariableInt("ORTHOMODE")',
        'BcadApplication.SetSystemVariable("ORTHOMODE", (short)(enabled ? 1 : 0));',
        'var current = ReadSystemVariableInt("OSMODE");',
        "var configuredModes = current & ObjectSnapModeMask;",
        "var suppression = current & ObjectSnapSuppressedBit;",
        "? configuredModes | bit",
        ": configuredModes & ~bit;",
        "var next = suppression | configuredModes;",
        'BcadApplication.SetSystemVariable("OSMODE", checked((short)next));',
        "SetObjectSnapMenuState(osMode & ObjectSnapModeMask);",
        "_endpointSnapItem.IsChecked = (configuredModes & ObjectSnapEndpointBit) != 0;",
        "_midpointSnapItem.IsChecked = (configuredModes & ObjectSnapMidpointBit) != 0;",
        "_centerSnapItem.IsChecked = (configuredModes & ObjectSnapCenterBit) != 0;",
        "_nearestSnapItem.IsChecked = (configuredModes & ObjectSnapNearestBit) != 0;",
        "BcadApplication.GetSystemVariable(name)",
    )
    for needle in required:
        if needle not in text:
            errors.append("WorkspacePanel.ViewAids missing complete reference-footer contract: " + needle)

    if "Content is Grid root" in text:
        errors.append(
            "viewport aids must bind the named WorkspaceContentRoot inside WorkspaceOverflow, not assume UserControl.Content is a Grid"
        )

    ensure_pos = text.find("private void EnsureViewportAidControls()")
    idempotent_pos = text.find("if (_viewportAidsApplied) return;", ensure_pos)
    named_root_pos = text.find("var root = WorkspaceContentRoot;", idempotent_pos)
    footer_pos = text.find("Grid.GetRow(border) == 2", named_root_pos)
    tag_pos = text.find("ViewportAidPanelTag", footer_pos)
    dock_pos = text.find("DockPanel.SetDock(viewportStatus, Dock.Right);", tag_pos)
    add_light_pos = text.find("viewportStatus.Children.Add(_lightBackgroundButton);", dock_pos)
    add_contrast_pos = text.find("viewportStatus.Children.Add(_contrastBackgroundButton);", add_light_pos)
    add_ortho_pos = text.find("viewportStatus.Children.Add(_orthoModeButton);", add_contrast_pos)
    add_snap_pos = text.find("viewportStatus.Children.Add(_objectSnapButton);", add_ortho_pos)
    add_menu_pos = text.find("viewportStatus.Children.Add(_objectSnapMenuButton);", add_snap_pos)
    applied_pos = text.find("_viewportAidsApplied = true;", add_menu_pos)
    if min(
        ensure_pos,
        idempotent_pos,
        named_root_pos,
        footer_pos,
        tag_pos,
        dock_pos,
        add_light_pos,
        add_contrast_pos,
        add_ortho_pos,
        add_snap_pos,
        add_menu_pos,
        applied_pos,
    ) < 0 or not (
        ensure_pos
        < idempotent_pos
        < named_root_pos
        < footer_pos
        < tag_pos
        < dock_pos
        < add_light_pos
        < add_contrast_pos
        < add_ortho_pos
        < add_snap_pos
        < add_menu_pos
        < applied_pos
    ):
        errors.append(
            "viewport-aid injection must be idempotent, bind WorkspaceContentRoot, live in its own right-docked footer panel, "
            "and expose the complete reference control order"
        )

    if "DockPanel.GetDock(stack) == Dock.Right" in text:
        errors.append(
            "viewport aids must not attach to the legacy right-docked LIVE SEMANTIC stack because "
            "ReferencePaletteLayout collapses that stack"
        )

    background_handler = text.find("private void ToggleViewportBackgroundPreset")
    read_background = text.find('ReadSystemVariableString("BKGCOLOR")', background_handler)
    restore_background = text.find("restoreColor = current;", read_background)
    write_background = text.find('SetSystemVariable("BKGCOLOR", next);', restore_background)
    if min(background_handler, read_background, restore_background, write_background) < 0 or not (
        background_handler < read_background < restore_background < write_background
    ):
        errors.append("BKGCOLOR presets must read current host state, retain a restore value, and then write the preset")

    global_snap_handler = text.find("private void OnObjectSnapButtonClick")
    read_pos = text.find('ReadSystemVariableInt("OSMODE")', global_snap_handler)
    mask_pos = text.find("current & ObjectSnapModeMask", read_pos)
    enabled_pos = text.find("var currentlyEnabled", mask_pos)
    zero_pos = text.find("if (enable && configuredModes == 0)", enabled_pos)
    preserve_on_pos = text.find("? configuredModes", zero_pos)
    preserve_off_pos = text.find(": configuredModes | ObjectSnapSuppressedBit;", preserve_on_pos)
    write_pos = text.find('SetSystemVariable("OSMODE"', preserve_off_pos)
    if min(
        global_snap_handler,
        read_pos,
        mask_pos,
        enabled_pos,
        zero_pos,
        preserve_on_pos,
        preserve_off_pos,
        write_pos,
    ) < 0 or not (
        global_snap_handler
        < read_pos
        < mask_pos
        < enabled_pos
        < zero_pos
        < preserve_on_pos
        < preserve_off_pos
        < write_pos
    ):
        errors.append(
            "global OSMODE toggle must read current bits, fail closed on zero configured modes, "
            "preserve lower bits and only add/remove the suppression bit"
        )

    mode_handler = text.find("private void OnObjectSnapModeClick")
    mode_read = text.find('ReadSystemVariableInt("OSMODE")', mode_handler)
    suppression_pos = text.find("current & ObjectSnapSuppressedBit", mode_read)
    configured_pos = text.find("current & ObjectSnapModeMask", suppression_pos)
    set_pos = text.find("? configuredModes | bit", configured_pos)
    clear_pos = text.find(": configuredModes & ~bit;", set_pos)
    combine_pos = text.find("var next = suppression | configuredModes;", clear_pos)
    mode_write = text.find('SetSystemVariable("OSMODE"', combine_pos)
    if min(
        mode_handler,
        mode_read,
        suppression_pos,
        configured_pos,
        set_pos,
        clear_pos,
        combine_pos,
        mode_write,
    ) < 0 or not (
        mode_handler
        < mode_read
        < suppression_pos
        < configured_pos
        < set_pos
        < clear_pos
        < combine_pos
        < mode_write
    ):
        errors.append(
            "per-mode OSNAP menu must preserve suppression and unrelated configured bits while toggling one requested bit"
        )

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
            errors.append(
                "Workspace viewport aids must remain native CAD-state-only and must not invent snap presets: "
                + forbidden
            )

if XAML.is_file():
    text = XAML.read_text(encoding="utf-8")
    preserved = (
        '<ScrollViewer x:Name="WorkspaceOverflow"',
        '<Grid x:Name="WorkspaceContentRoot"',
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

if LAYOUT.is_file():
    text = LAYOUT.read_text(encoding="utf-8")
    for needle in (
        "private void ApplyReferenceFooter(Grid root)",
        "legacyStatus.Visibility = Visibility.Collapsed;",
        "RenderReferenceFooterContext();",
        '_footerContextText.Text = "Tầng " + floor + "   ·   Cao độ " + elevation;',
    ):
        if needle not in text:
            errors.append("reference footer layout/context contract disappeared: " + needle)

if V26_PROJECT.is_file():
    text = V26_PROJECT.read_text(encoding="utf-8")
    if r'<Compile Include="..\QS3D.BricsCAD.V25\**\*.cs"' not in text:
        errors.append("V26 must continue linking V25 shared adapter sources so Workspace viewport aids stay in parity")

SUPPRESSION = 16384
MASK = SUPPRESSION - 1

def toggle_mode(current, bit, enabled):
    suppression = current & SUPPRESSION
    configured = current & MASK
    configured = (configured | bit) if enabled else (configured & ~bit)
    return suppression | configured

sample = SUPPRESSION | 1 | 32 | 4096
if toggle_mode(sample, 2, True) != (SUPPRESSION | 1 | 2 | 32 | 4096):
    errors.append("OSNAP add-mode contract simulation failed")
if toggle_mode(sample, 1, False) != (SUPPRESSION | 32 | 4096):
    errors.append("OSNAP remove-mode contract simulation failed")
if toggle_mode(4 | 512, 4, False) != 512:
    errors.append("OSNAP mode removal must retain unrelated configured modes")

print("QS3D Workspace complete reference viewport-aid preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: Workspace keeps model/BQ/floor context, binds the real named WorkspaceContentRoot, surfaces a dedicated visible right footer with "
    "light/contrast/ortho/entity-snap controls, exposes the requested four OSNAP modes, preserves "
    "native OSMODE bits/suppression state, and shares the implementation with V26."
)
