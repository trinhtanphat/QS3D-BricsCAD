#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
COMMANDS = ROOT / "src" / "QS3D.BricsCAD.V25" / "Commands.cs"
ACTIVATION = ROOT / "src" / "QS3D.BricsCAD.V25" / "Ribbon" / "BltBimWorkspaceActivationCoordinator.cs"
PROPERTIES_XAML = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "PropertiesPanel.xaml"
PROPERTIES_CODE = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "PropertiesPanel.xaml.cs"
LAYOUT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.Blt3dFiveZoneRuntimeLayout.cs"
COMPACT = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI" / "WorkspacePanel.CompactShell.cs"
LAYOUT_STORE = ROOT / "src" / "QS3D.BricsCAD.V25" / "Services" / "UserUiLayoutStore.cs"
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


palette = read(PALETTE)
commands = read(COMMANDS)
activation = read(ACTIVATION)
properties_xaml = read(PROPERTIES_XAML)
properties_code = read(PROPERTIES_CODE)
layout = read(LAYOUT)
compact = read(COMPACT)
layout_store = read(LAYOUT_STORE)

# Owner-facing QS3D activation must restore four distinct plugin PaletteSets around the real
# BricsCAD viewport. The dedicated QS3D Properties palette is not interchangeable with the
# host-native BricsCAD Properties palette or with the compatibility editor embedded in Workspace.
for token in (
    "private static readonly Guid PropertiesGuid",
    "private static PaletteSet? _properties;",
    "private static PropertiesPanel? _propertiesPanel;",
    "public static bool IsPropertiesVisible",
    "_propertiesPanel = new PropertiesPanel();",
    "_propertiesPanel.Attach(_workspacePanel);",
    'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)',
    '_properties.AddVisual("Thuộc tính", _propertiesPanel, true);',
    "public static void Show() => ShowBimWorkspace();",
    "public static void ShowWorkspace()",
    "SetVisibility(workspace: true, properties: false, right: false, quantityInsight: false);",
    "EnsureBimDockContract();",
    "SetVisibility(workspace: true, properties: true, right: true, quantityInsight: true);",
    "_workspace.Dock = DockSides.Left;",
    "_properties.Dock = DockSides.Left;",
    "_right.Dock = DockSides.Right;",
    "_quantityInsight.Dock = DockSides.Right;",
    "ReassertPersistedPaletteSizes();",
    "Mô hình + QS3D Properties tách riêng bên trái",
    "viewport BricsCAD native ở giữa",
):
    if token not in palette:
        errors.append("PaletteCoordinator runtime contract missing: " + token)

# Standalone management/quantity commands remain isolated and must not silently drag the whole
# BIM surface open. Safe Mode retains the two left QS3D-owned surfaces.
for token in (
    "SetVisibility(workspace: false, properties: false, right: true, quantityInsight: false);",
    "SetVisibility(workspace: false, properties: false, right: false, quantityInsight: true);",
    "SetVisibility(workspace: true, properties: true, right: false, quantityInsight: false);",
):
    if token not in palette:
        errors.append("isolated palette visibility contract missing: " + token)

if 'new PaletteSet("QS3D — Thuộc tính", PropertiesGuid)' not in palette:
    errors.append("QS3D Properties must be a dedicated plugin PaletteSet")
if 'SendStringToExecute("PROPERTIES' in palette or 'SendStringToExecute("PROPERTIESBAR' in palette:
    errors.append("native BricsCAD Properties must never be used as the QS3D Properties implementation")

for token in (
    '[CommandMethod("QS3D", CommandFlags.Modal)] public void ShowWorkspace() => PaletteCoordinator.Show();',
):
    if token not in commands:
        errors.append("explicit QS3D command must route through coordinated BIM activation")

for token in (
    'private const string BimTabId = "QS3D_BIM";',
    "PaletteCoordinator.ShowBimWorkspace();",
):
    if token not in activation:
        errors.append("BIM activation contract missing: " + token)

# The dedicated properties surface must share WorkspaceViewModel rather than copy or weaken the
# semantic edit policy. This keeps Family/Instance switching, two-way Apply callbacks, read-only
# source/identity fields, multi-selection state labels and override reset behavior identical.
for token in (
    'x:Class="QS3D.BricsCAD.V25.UI.PropertiesPanel"',
    'ItemsSource="{Binding Properties}"',
    'ItemsSource="{Binding PropertyScopes}"',
    'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'IsReadOnly="{Binding IsReadOnly}"',
    'IsEnabled="{Binding IsEditable}"',
    'Binding EditorKind',
    'Text="{Binding StateLabel}"',
    'IsEnabled="{Binding CanReset}"',
):
    if token not in properties_xaml:
        errors.append("dedicated QS3D Properties editor contract missing: " + token)

for token in (
    "public void Attach(WorkspacePanel workspace)",
    "DataContext = workspace.DataContext;",
    "workspace.DataContextChanged += OnWorkspaceDataContextChanged;",
    "workspace.DataContextChanged -= OnWorkspaceDataContextChanged;",
    "DataContext = e.NewValue;",
    "row.ResetValue();",
):
    if token not in properties_code:
        errors.append("dedicated QS3D Properties shared-viewmodel contract missing: " + token)

for token in (
    "public int PropertiesPaletteWidth { get; set; } = 320;",
    "public int PropertiesPaletteHeight { get; set; } = 720;",
    "internal const int PropertiesPaletteMinWidth = 260;",
    "internal const int PropertiesPaletteMinHeight = 360;",
    'Int(values, "PropertiesPaletteWidth", layout.PropertiesPaletteWidth)',
    'Int(values, "PropertiesPaletteHeight", layout.PropertiesPaletteHeight)',
    'builder.Append("PropertiesPaletteWidth=")',
    'builder.Append("PropertiesPaletteHeight=")',
    "layout.PropertiesPaletteWidth = Clamp(layout.PropertiesPaletteWidth, PropertiesPaletteMinWidth, 900);",
    "layout.PropertiesPaletteHeight = Clamp(layout.PropertiesPaletteHeight, PropertiesPaletteMinHeight, 2000);",
):
    if token not in layout_store:
        errors.append("QS3D Properties deterministic size persistence missing: " + token)

# WorkspacePanel remains a partial type and C# permits only one static constructor for the whole
# type. The existing CompactShell initializer still owns deterministic class-handler registration.
if "static WorkspacePanel()" not in compact:
    errors.append("WorkspacePanel deterministic type initializer missing from CompactShell")
if "static WorkspacePanel()" in layout:
    errors.append("BLT3D runtime layout must not declare a duplicate WorkspacePanel static constructor")

# The compatibility Workspace editor may remain as a mirror, but the layout must never fabricate a
# second viewport. The native BricsCAD modelspace remains the center of the five-zone host layout.
if "new Viewport" in layout or "Viewport3D" in layout:
    errors.append("runtime layout must not create a fake second 3D viewport")
if "public static void Show() => ShowWorkspace();" in palette:
    errors.append("regression: owner-facing QS3D activation must restore the coordinated BIM surface")

print("QS3D BLT3D runtime five-zone regression preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: explicit QS3D/BIM activation restores four distinct QS3D plugin palettes around native BricsCAD modelspace; QS3D Properties is a dedicated left plugin PaletteSet bound to the same semantic WorkspaceViewModel, deterministic dock/size fallback is reasserted, standalone management/quantity commands remain isolated, and remote CI does not substitute for interactive BricsCAD screenshot validation.")
