#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs"
DOC = ROOT / "docs/UI-WORKSPACE-COMPACT-SHELL-2026-08-11.md"
errors = []

for path in (XAML, PARTIAL, DOC):
    if not path.is_file():
        errors.append("missing Workspace compact-shell dependency: " + str(path.relative_to(ROOT)))

if XAML.is_file():
    xaml = XAML.read_text(encoding="utf-8")
    try:
        ET.fromstring(xaml)
    except ET.ParseError as exc:
        errors.append("WorkspacePanel.xaml is not well-formed XML: " + str(exc))

    required_xaml = (
        'x:Name="ZoneCombo"',
        'SelectionChanged="OnZoneChanged"',
        'x:Name="FloorCombo"',
        'SelectionChanged="OnFloorChanged"',
        'x:Name="ModelTree"',
        'SelectedItemChanged="OnModelTreeSelectedItemChanged"',
        'Tag="Grid"',
        'Tag="Room"',
        'Tag="Beam"',
        'Tag="Slab"',
        'Tag="Column"',
        'Tag="StructuralWall"',
        'Tag="ArchitecturalWall"',
        'Tag="GlassWall"',
        'Tag="WallPier"',
        'Tag="WallOpening"',
        'Tag="Door"',
        'Tag="Stair"',
        'Tag="Foundation"',
        'Tag="Earthwork"',
        'Tag="CustomQuantity"',
        'x:Name="FamilySearch"',
        'TextChanged="OnFamilySearchChanged"',
        'x:Name="FamilyList"',
        'SelectionChanged="OnFamilySelectionChanged"',
        'Click="OnAddClick"',
        'Click="OnDeleteClick"',
        'Click="OnCaptureSelectedClick"',
        'Click="OnView3DClick"',
        'Click="OnWallJunctionsClick"',
        'Click="OnWallSnapPreviewClick"',
        'Click="OnWallSnapApplyClick"',
        'Click="OnAutoHostClick"',
        'x:Name="PropertySearch"',
        'TextChanged="OnPropertySearchChanged"',
        'Click="OnClearPropertySearchClick"',
        'x:Name="PropertyList"',
        'Click="OnResetPropertyClick"',
        'x:Name="SelectionCount"',
        'x:Name="InspectionList"',
        'Click="OnFocusSelectedClick"',
        'Click="OnIsolateSelectedClick"',
        'Click="OnUnisolateClick"',
        'Click="OnLocateSelectedClick"',
        'Click="OnTopViewClick"',
        'Click="OnQuantityClick"',
        'Click="OnHealthClick"',
        'Click="OnSaveClick"',
        'PHẠM VI LÀM VIỆC',
        'FAMILY / TYPE',
        'THUỘC TÍNH',
        'ĐỐI TƯỢNG ĐANG CHỌN',
        'VIEWPORT BRICSCAD',
    )
    for token in required_xaml:
        if token not in xaml:
            errors.append("Workspace XAML contract missing: " + token)

    for forbidden in ("<Viewport3D", "x:Class=\"QS3D.BricsCAD.V25.UI.RightPanel\""):
        if forbidden in xaml:
            errors.append("Workspace must not embed a parallel viewport/right-panel implementation: " + forbidden)

if PARTIAL.is_file():
    partial = PARTIAL.read_text(encoding="utf-8")
    required_partial = (
        "public partial class WorkspacePanel",
        "static WorkspacePanel()",
        "EventManager.RegisterClassHandler(",
        "ApplyCompactShellPresentation()",
        "_compactShellApplied",
        "UseLayoutRounding = true",
        "SnapsToDevicePixels = true",
        "TuneWorkspaceGrid()",
        "workspace.ColumnDefinitions[0].Width = new GridLength(165)",
        "workspace.ColumnDefinitions[2].Width = new GridLength(255)",
        "ZoneCombo.MinHeight = 25",
        "FloorCombo.MinHeight = 25",
        "FamilyList.MinHeight = 82",
        "PropertyList.MinHeight = 118",
        "InspectionList.MinHeight = 96",
        "TuneModelTreeDensity()",
        'AppendShortcutHint(FamilySearch, "Ctrl+F")',
        'AppendShortcutHint(FindButton("Lưu"), "Ctrl+S")',
        'AppendShortcutHint(FindButton("BQ"), "Ctrl+B")',
        'AppendShortcutHint(FindButton("Làm mới"), "F5")',
        '"PHẠM VI LÀM VIỆC"',
        '"FAMILY / TYPE"',
        '"THUỘC TÍNH"',
        '"ĐỐI TƯỢNG ĐANG CHỌN"',
    )
    for token in required_partial:
        if token not in partial:
            errors.append("Workspace compact presentation missing: " + token)

    for forbidden in (
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        "SemanticCaptureService",
        "Viewport3D",
        "OnAddClick(",
        "OnDeleteClick(",
        "OnQuantityClick(",
        "OnSaveClick(",
    ):
        if forbidden in partial:
            errors.append("Workspace compact presentation must remain presentation-only: " + forbidden)

if DOC.is_file():
    doc = DOC.read_text(encoding="utf-8")
    for token in (
        "1366×768",
        "BricsCAD viewport",
        "Zone / Tầng",
        "Family / Type",
        "Đối tượng đang chọn",
        "Ctrl+S",
        "Ctrl+F",
        "Ctrl+B",
        "F5",
        "LOCAL-012",
        "presentation-only",
    ):
        if token not in doc:
            errors.append("Workspace compact-shell documentation missing: " + token)

if errors:
    print("Workspace compact-shell preflight FAILED:")
    for error in errors:
        print("- " + error)
    sys.exit(1)

print("Workspace compact-shell preflight PASS: screenshot-inspired density is presentation-only, existing Workspace actions remain wired, and the BricsCAD viewport boundary is preserved.")
