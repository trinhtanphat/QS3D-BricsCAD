#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

if hasattr(sys.stdout, "reconfigure"):
    sys.stdout.reconfigure(errors="backslashreplace")

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
errors = []

files = {
    "workspace": UI / "WorkspacePanel.xaml",
    "right": UI / "RightPanel.xaml",
    "theme": UI / "Theme.xaml",
    "hub": UI / "DomainHubWindow.xaml",
    "family": UI / "FamilyManagerWindow.xaml",
    "door_schedule": UI / "DoorOpeningScheduleWindow.xaml",
    "project_tools": UI / "ProjectToolsWindow.xaml",
    "schedule_hub": UI / "ScheduleHubWindow.xaml",
    "quantity": UI / "QuantitySummaryWindow.xaml",
    "rebar_hub": UI / "Rebar3DHubWindow.xaml",
    "rebar_schedule": UI / "RebarScheduleWindow.xaml",
    "rebar_mesh": UI / "RebarMeshSetupWindow.xaml",
    "curtain": UI / "CurtainWallWindow.xaml",
    "floor": UI / "FloorLevelWindow.xaml",
    "zone": UI / "ZoneManagerWindow.xaml",
    "material": UI / "MaterialCatalogWindow.xaml",
    "recognition": UI / "RecognitionWindow.xaml",
    "revision": UI / "RevisionWindow.xaml",
    "health": UI / "ModelHealthWindow.xaml",
    "audit": UI / "AuditLogWindow.xaml",
    "room_finish": UI / "RoomFinishScheduleWindow.xaml",
    "geometry": UI / "GeometryExtensionsWindow.xaml",
}

for label, path in files.items():
    if not path.is_file():
        errors.append("missing premium UI file: " + str(path.relative_to(ROOT)))
        continue
    try:
        ET.parse(path)
    except ET.ParseError as exc:
        errors.append(str(path.relative_to(ROOT)) + " is not well-formed XAML/XML: " + str(exc))

for path in sorted(UI.glob("*Window.xaml")):
    text = path.read_text(encoding="utf-8")
    if 'ResourceDictionary Source="Theme.xaml"' not in text:
        errors.append(str(path.relative_to(ROOT)) + " does not merge Theme.xaml")
    for needle in (
        'Background="#17191C"',
        'Foreground="Black"',
        'Foreground="#000000"',
        'Foreground="#FF000000"',
    ):
        if needle in text:
            errors.append(str(path.relative_to(ROOT)) + " contains legacy/dark-host-risk styling: " + needle)

workspace = files["workspace"]
if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    required = (
        'x:Key="WorkspaceCard"', 'x:Key="WorkspaceBadge"', 'x:Key="WorkspaceToolbarBand"',
        'Text="BIM WORKSPACE"', 'Text="PHẠM VI LÀM VIỆC"', 'Text="Tìm Family / Type"',
        'Text="ĐỐI TƯỢNG ĐANG CHỌN"', 'Text="CAD + SEMANTIC"',
        'Foreground="{StaticResource LuxuryBrush}"',
        'Click="OnWallJunctionsClick"', 'Click="OnWallSnapPreviewClick"',
        'Click="OnWallSnapApplyClick"', 'Click="OnAutoHostClick"',
        'Click="OnFocusSelectedClick"', 'Click="OnIsolateSelectedClick"', 'Click="OnUnisolateClick"',
        'ItemsSource="{Binding PropertyScopes}"',
        'SelectedItem="{Binding SelectedPropertyScope, Mode=TwoWay, UpdateSourceTrigger=PropertyChanged}"',
        'ToolTip="Reset override về giá trị Family"',
        'Text="VIEWPORT BRICSCAD • PAN • ZOOM • ORBIT • PICK"',
    )
    for needle in required:
        if needle not in text:
            errors.append("WorkspacePanel.xaml missing premium/workflow contract: " + needle)

right = files["right"]
if right.is_file():
    text = right.read_text(encoding="utf-8")
    required = (
        'x:Key="RightBadge"', 'x:Key="RightToolbarBand"',
        'Drawings.Count, StringFormat={}{0} bản vẽ', 'Text="{Binding LayerCountText}"',
        'Text="Xref / Drawing"', 'Text="Hiện / Ẩn / Khóa / Màu native"', 'Text="Tìm lớp"',
        'Background="{Binding ColorBrush}"', 'IsChecked="{Binding IsLocked}"',
        'Click="OnLockLayersClick"', 'Click="OnUnlockLayersClick"',
        'ToolTip="Màu layer native"', 'ToolTip="{Binding Status}"',
    )
    for needle in required:
        if needle not in text:
            errors.append("RightPanel.xaml missing premium/live-state contract: " + needle)

checks = {
    "hub": (
        'x:Key="HubSectionCard"', 'Text="WORKFLOW HUB"', 'Text="PROFESSIONAL CAD WORKSPACE"',
        'Tag="QS3DDRAWWALL"', 'Tag="QS3DDRAWDOOR"', 'Tag="QS3DSECTIONBOX"',
        'Tag="QS3DREBARHEALTHALL"', 'Tag="QS3DRELEASECHECK"', 'x:Name="StatusText"',
    ),
    "family": (
        'x:Key="ManagerCard"', 'Text="PARAMETRIC"', 'x:Name="ReferenceCountText"',
        'Style="{StaticResource DangerButton}"', 'Click="OnAssignClick"', 'x:Name="StatusText"',
    ),
    "door_schedule": (
        'x:Key="MetricCard"', 'Text="LIVE BIM DATA"', 'Text="Tìm trong schedule"',
        'x:Name="GroupCountText"', 'x:Name="AreaText"', 'x:Name="ScheduleGrid"',
        'Click="OnExportClick"', 'Text="READ-ONLY SCHEDULE • EXPORT XLSX"',
    ),
    "project_tools": (
        'Text="PROJECT CONTROL"', 'x:Key="ProjectCard"', 'x:Key="ProjectMetric"',
        'Tag="QS3DLEVELS"', 'Tag="QS3DZONES"', 'Tag="QS3DFAMILIES"', 'Tag="QS3DMATERIALS"',
        'Tag="QS3DHEALTHALL"', 'Text="PROJECT-SAFE • DWG CONTEXT LOCK"',
    ),
    "schedule_hub": (
        'Text="QUANTITY HUB"', 'x:Key="ScheduleCard"', 'x:Key="ScheduleMetric"',
        'Tag="QS3DBQ"', 'Tag="QS3DFINISHSCHEDULE"', 'Tag="QS3DDOORSCHEDULE"',
        'Tag="QS3DREBARHUB"', 'Text="SCHEDULE-SAFE • DWG CONTEXT LOCK"',
    ),
    "quantity": (
        'Text="BQ REVIEW"', 'x:Name="FloorCombo"', 'x:Name="SearchBox"',
        'x:Name="CategoryList"', 'x:Name="QuantityGrid"', 'x:Name="TotalsText"',
        'Checked="OnColumnVisibilityChanged"', 'Unchecked="OnColumnVisibilityChanged"',
        'Text="DETAIL: CLICK → 3D • SUMMARY: DOUBLE-CLICK → LOCATE • EXPORT XLSX"',
    ),
    "rebar_hub": (
        'Text="REBAR WORKFLOW"', 'x:Key="RebarCard"', 'Tag="QS3DREBAR3D"',
        'Tag="QS3DSLABREBAR3D"', 'Tag="QS3DFOUNDATIONREBAR3D"', 'Tag="QS3DREBARHEALTHALL"',
        'Text="EXPLICIT REBAR INPUTS • NATIVE 3D"',
    ),
    "rebar_schedule": (
        'Text="BBS REVIEW"', 'x:Name="Grid"', 'x:Name="Totals"',
        'Click="OnLocateClick"', 'Click="OnExportClick"', 'MouseDoubleClick="OnGridDoubleClick"',
    ),
    "rebar_mesh": (
        'Text="EXPLICIT INPUT"', 'x:Name="Direction1Text"', 'x:Name="Direction2Text"',
        'x:Name="CoverText"', 'x:Name="FacesCombo"', 'x:Name="ValidationText"',
        'Click="OnSave"', 'Click="OnCancel"',
    ),
    "curtain": (
        'Text="CURTAIN SYSTEM"', 'x:Name="FamilyCombo"', 'x:Name="WallCountText"',
        'Tag="QS3DCURTAINFRAMES3D"', 'Tag="QS3DCURTAINFRAMEHEALTH"',
        'Tag="QS3DCUTOPENINGSCURVED"', 'Text="CURVE FRAME = V25 GATE"',
    ),
    "floor": (
        'Text="SEMANTIC LEVEL"', 'x:Name="FloorList"', 'x:Name="ActiveFloorText"',
        'Click="OnActivateClick"', 'Click="OnAssignClick"', 'Style="{StaticResource DangerButton}"',
        'Text="NO CAD MOVE • STALE ON LEVEL CHANGE"',
    ),
    "zone": (
        'Text="SEMANTIC SCOPE"', 'x:Name="ZoneList"', 'x:Name="ActiveZoneText"',
        'Click="OnActivateClick"', 'Click="OnAssignClick"', 'Style="{StaticResource DangerButton}"',
        'Text="SEMANTIC SCOPE ONLY • NO CAD MOVE"',
    ),
    "material": (
        'Text="PROJECT MATERIALS"', 'x:Name="MaterialList"', 'x:Name="ReferencedText"',
        'x:Name="TargetCombo"', 'Click="OnApplyClick"', 'Style="{StaticResource DangerButton}"',
        'Text="AMBIGUOUS HANDLE = FAIL CLOSED"',
    ),
    "recognition": (
        'Text="REVIEW GATED"', 'x:Name="Grid"', 'Click="OnApplyClick"',
        'Click="OnApplyConfidentClick"', 'Text="LOW CONFIDENCE = REVIEW"',
    ),
    "revision": (
        'Text="SO SÁNH BẢN SỬA ĐỔI"', 'Text="SEMANTIC + QUANTITY"',
        'x:Name="Header"', 'x:Name="Grid"', 'x:Name="SemanticGrid"', 'x:Name="Totals"',
        'MouseDoubleClick="OnGridDoubleClick"', 'MouseDoubleClick="OnSemanticGridDoubleClick"',
        'Text="DOUBLE-CLICK ROW TO LOCATE"',
    ),
    "health": (
        'Text="HEALTH REVIEW"', 'x:Name="SummaryText"', 'x:Name="IssueGrid"',
        'Click="OnLocateClick"', 'MouseDoubleClick="OnGridDoubleClick"', 'Text="ISSUE → CAD LOCATE"',
    ),
    "audit": (
        'Text="AUDIT TRAIL"', 'Text="Tìm nhật ký"', 'x:Name="SearchBox"',
        'x:Name="Grid"', 'x:Name="Summary"', 'Text="MỚI NHẤT HIỂN THỊ TRƯỚC"',
    ),
    "room_finish": (
        'Text="ROOM FINISH"', 'x:Name="SearchBox"', 'x:Name="GroupCountText"',
        'x:Name="ScheduleGrid"', 'Click="OnExportClick"', 'Text="ROOM FINISH SCHEDULE • EXPORT XLSX"',
    ),
    "geometry": (
        'Text="REVIEW GATED"', 'x:Key="GeometryCard"', 'Tag="QS3DWALLJUNCTIONS"',
        'Tag="QS3DWALLSNAPPREVIEW"', 'Tag="QS3DWALLSNAPAPPLY"', 'Tag="QS3DCUTOPENINGSCURVED"',
        'Tag="QS3DREBARHEALTHALL"', 'Text="PREVIEW / FINGERPRINT / HEALTH GATES"',
    ),
}

for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(path.name + " missing premium/workflow contract: " + needle)

theme = files["theme"]
if theme.is_file():
    text = theme.read_text(encoding="utf-8")
    for needle in (
        'x:Key="PanelTitle"',
        '<Setter Property="Foreground" Value="{StaticResource TextBrush}"/>',
        'x:Key="AccentSoftBrush"', 'x:Key="LuxuryBrush"', 'x:Key="BorderFocusBrush"',
    ):
        if needle not in text:
            errors.append("Theme.xaml missing premium design-system contract: " + needle)

print("QS3D premium UI layout preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print(
    "PASS: all BricsCAD V25 modeless windows and core palettes use the shared premium CAD-first theme; "
    "critical workflow tags/handlers, live layer state and review/fail-closed UX contracts remain present."
)
