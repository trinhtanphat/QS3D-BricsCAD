#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
WORKSPACE_XAML = UI / "WorkspacePanel.xaml"
WORKSPACE_CODE = UI / "WorkspacePanel.xaml.cs"
DEDICATED = UI / "WorkspacePanel.DedicatedPropertiesPalette.cs"
PALETTE = ROOT / "src" / "QS3D.BricsCAD.V25" / "PaletteCoordinator.cs"
DUPLICATE_VISUALS = (
    UI / "PropertiesPanel.xaml",
    UI / "PropertiesPanel.xaml.cs",
    UI / "PropertiesPanel.DarkHostTheme.cs",
)
errors = []


def read(path: Path) -> str:
    if not path.is_file():
        errors.append("missing source: " + str(path.relative_to(ROOT)))
        return ""
    return path.read_text(encoding="utf-8")


workspace_xaml = read(WORKSPACE_XAML)
workspace_code = read(WORKSPACE_CODE)
dedicated = read(DEDICATED)
palette = read(PALETTE)

for path in DUPLICATE_VISUALS:
    if path.exists():
        errors.append("duplicate QS3D Properties visual authority is forbidden: " + str(path.relative_to(ROOT)))

for token in (
    'x:Name="PropertyList"',
    'ItemsSource="{Binding Properties}"',
    'ItemsSource="{Binding PropertyScopes}"',
    'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
    'Click="OnResetPropertyClick"',
):
    if token not in workspace_xaml:
        errors.append("authoritative Workspace Properties editor missing: " + token)

for token in ("OnResetPropertyClick", "button.CommandParameter is PropertyRowViewModel row", "row.ResetValue();"):
    if token not in workspace_code:
        errors.append("authoritative Properties write-back/reset handler missing: " + token)

for token in (
    "CreatePropertiesPaletteVisual",
    "SetDedicatedPropertiesPaletteActive(bool active)",
    "ownerGrid.Children.Remove(region);",
    "host.Children.Add(region);",
    "host.Children.Remove(region);",
    "ownerGrid.Children.Add(region);",
    "new Binding(nameof(DataContext))",
):
    if token not in dedicated:
        errors.append("single-editor optional reparenting contract missing: " + token)

for token in (
    "var propertiesVisible = IsPropertiesVisible;",
    "_workspacePanel?.SetDedicatedPropertiesPaletteActive(propertiesVisible);",
    "SetVisibility(workspaceVisible, propertiesVisible, rightVisible, quantityVisible);",
):
    if token not in palette:
        errors.append("single-editor reset-host contract missing: " + token)

bim_start = palette.find("public static bool ShowBimWorkspace()")
bim_end = palette.find("public static void ShowDrawingManagement()", bim_start)
bim = palette[bim_start:bim_end]
if "SetDedicatedPropertiesPaletteActive(false)" not in bim:
    errors.append("default BIM must host the authoritative editor in Workspace")
if "SetDedicatedPropertiesPaletteActive(true)" in bim:
    errors.append("default BIM must not create the separated Properties presentation")

if "new WorkspaceViewModel" in dedicated:
    errors.append("dedicated Properties host must not create a second WorkspaceViewModel")
if "new PropertiesPanel" in palette or "PropertiesPanel(" in palette:
    errors.append("PaletteCoordinator must not instantiate a second PropertiesPanel visual")

print("QS3D Properties single-editor authority preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: one authoritative editable QS3D Properties editor remains; default BIM keeps it embedded under Family, while the existing dedicated palette can reparent that same visual only when explicitly visible.")