#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

store = ROOT / "src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs"
palette = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
splitter = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs"
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
runtime = ROOT / "scripts/test-wpf-palettes-runtime.ps1"
right_panel = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"

for path in (store, palette, splitter, workspace, right_panel, runtime):
    if not path.is_file(): errors.append("missing UI layout persistence file: " + str(path.relative_to(ROOT)))

if store.is_file():
    text = store.read_text(encoding="utf-8")
    for needle in (
        "Environment.SpecialFolder.LocalApplicationData",
        'Path.Combine(root, "QS3D", "BricsCAD-V25", "ui-layout-v1.txt")',
        "MaxFileBytes = 16 * 1024",
        "internal const int WorkspacePaletteMinWidth = 460;",
        "internal const int WorkspacePaletteMinHeight = 420;",
        "internal const int RightPaletteMinWidth = 255;",
        "internal const int RightPaletteMinHeight = 480;",
        "public int WorkspacePaletteWidth { get; set; } = 640;",
        "public double ModelColumnWidth { get; set; } = 160d;",
        "public double FamilyColumnWidth { get; set; } = 245d;",
        "public double FamilyTopHeight { get; set; } = 250d;",
        "public double RoomTopHeight { get; set; } = 218d;",
        "layout.WorkspacePaletteWidth = Clamp(layout.WorkspacePaletteWidth, WorkspacePaletteMinWidth, 1600);",
        "layout.WorkspacePaletteHeight = Clamp(layout.WorkspacePaletteHeight, WorkspacePaletteMinHeight, 2000);",
        "layout.RightPaletteWidth = Clamp(layout.RightPaletteWidth, RightPaletteMinWidth, 1200);",
        "layout.RightPaletteHeight = Clamp(layout.RightPaletteHeight, RightPaletteMinHeight, 2000);",
        "layout.ModelColumnWidth = Clamp(layout.ModelColumnWidth, 135d, 500d, 160d);",
        "layout.FamilyColumnWidth = Clamp(layout.FamilyColumnWidth, 220d, 700d, 245d);",
        "layout.FamilyTopHeight = Clamp(layout.FamilyTopHeight, 160d, 900d, 250d);",
        "layout.RoomTopHeight = Clamp(layout.RoomTopHeight, 135d, 900d, 218d);",
        "Normalize(next);",
        "if (Equivalent(_current, next)) return;",
        "private static bool Equivalent(UserUiLayout left, UserUiLayout right)",
        "File.Replace(temp, path, backup, true);",
        "catch (IOException)",
        "catch (UnauthorizedAccessException)",
        "TryDelete(temp!)",
    ):
        if needle not in text: errors.append("UserUiLayoutStore missing fail-safe/atomic/upgraded-layout/minimum/no-op contract: " + needle)
    for forbidden in (".qsdb", "ProjectContextCoordinator", "ProjectState", "project.Metadata"):
        if forbidden in text: errors.append("per-user UI layout must not mutate project/QSDB state: " + forbidden)

if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    for needle in (
        'MinWidth="0" MinHeight="0"',
        '<Grid MinWidth="560" Background="{StaticResource Bg0Brush}">',
        'HorizontalScrollBarVisibility="Auto"',
        'VerticalScrollBarVisibility="Disabled"',
        'PanningMode="HorizontalOnly"',
        'Width="{Binding ViewportWidth, RelativeSource={RelativeSource AncestorType={x:Type ScrollViewer}}}"',
        '<ColumnDefinition Width="160" MinWidth="135"/>',
        '<ColumnDefinition Width="245" MinWidth="220"/>',
        '<RowDefinition Height="250" MinHeight="160"/>',
        '<RowDefinition Height="218" MinHeight="135"/>',
    ):
        if needle not in text: errors.append("WorkspacePanel upgraded layout contract missing: " + needle)
    if 'MinWidth="560" MinHeight="540"' in text:
        errors.append("WorkspacePanel must not force its 560-DIP content width onto the compact palette host")
    try:
        root = ET.parse(workspace).getroot()
        wpf = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
        if root.attrib.get("MinWidth") != "0" or root.attrib.get("MinHeight") != "0":
            errors.append("WorkspacePanel host surface must remain shrinkable to the PaletteSet minimum")
        scroller = root.find(wpf + "UserControl.Template/" + wpf + "ControlTemplate/" + wpf + "ScrollViewer")
        if scroller is None:
            errors.append("WorkspacePanel missing the explicit compact-host horizontal overflow viewport")
        else:
            expected = {
                "HorizontalScrollBarVisibility": "Auto",
                "VerticalScrollBarVisibility": "Disabled",
                "CanContentScroll": "False",
                "PanningMode": "HorizontalOnly",
            }
            for key, value in expected.items():
                if scroller.attrib.get(key) != value:
                    errors.append("WorkspacePanel overflow viewport must set " + key + "=" + value)
            presenter = scroller.find(wpf + "ContentPresenter")
            if presenter is None:
                errors.append("WorkspacePanel overflow viewport must present the original root content")
            else:
                if presenter.attrib.get("MinWidth") != "560":
                    errors.append("WorkspacePanel overflow presenter must retain the 560-DIP design floor")
                width_binding = presenter.attrib.get("Width", "")
                if "ViewportWidth" not in width_binding or "AncestorType={x:Type ScrollViewer}" not in width_binding:
                    errors.append("WorkspacePanel overflow presenter must follow the live viewport width above 560 DIP")
        content_grid = root.find(wpf + "Grid")
        if content_grid is None or content_grid.attrib.get("MinWidth") != "560":
            errors.append("WorkspacePanel three-column content must retain its 560-DIP design width inside overflow")
    except ET.ParseError as exc:
        errors.append("WorkspacePanel.xaml is not well-formed: " + str(exc))

if right_panel.is_file():
    text = right_panel.read_text(encoding="utf-8")
    if 'MinWidth="255" MinHeight="480"' not in text:
        errors.append("RightPanel minimum layout contract missing: MinWidth=255 MinHeight=480")

if palette.is_file():
    text = palette.read_text(encoding="utf-8")
    for needle in (
        "using WpfSize = System.Windows.Size;",
        "var layout = UserUiLayoutStore.Get();",
        "MinimumSize = new DrawingSize(UserUiLayoutStore.WorkspacePaletteMinWidth, UserUiLayoutStore.WorkspacePaletteMinHeight)",
        "MinimumSize = new DrawingSize(UserUiLayoutStore.RightPaletteMinWidth, UserUiLayoutStore.RightPaletteMinHeight)",
        "_workspace.DeviceIndependentSize = new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight);",
        "_right.DeviceIndependentSize = new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight);",
        "PersistPaletteLayout();",
        "UserUiLayoutStore.Update(layout =>",
    ):
        if needle not in text: errors.append("PaletteCoordinator missing per-user dimension/minimum persistence: " + needle)
    for stale in (
        "MinimumSize = new DrawingSize(460, 420)",
        "MinimumSize = new DrawingSize(255, 420)",
    ):
        if stale in text: errors.append("PaletteCoordinator retains stale palette minimum: " + stale)
    if "_workspace.Size =" in text or "_right.Size =" in text:
        errors.append("PaletteCoordinator must not regress to obsolete PaletteSet.Size for layout persistence")

if splitter.is_file():
    text = splitter.read_text(encoding="utf-8")
    for needle in (
        "AttachLayoutPersistence",
        "Grid.GetRow(x) == 1",
        "Grid.GetColumn(x) == 2",
        "Grid.GetColumn(x) == 4",
        "GridSplitter",
        "DragCompleted += OnLayoutSplitterDragCompleted",
        "ColumnDefinitions[0].ActualWidth",
        "ColumnDefinitions[2].ActualWidth",
        "RowDefinitions[0].ActualHeight",
        "UserUiLayoutStore.Update(layout =>",
    ):
        if needle not in text: errors.append("Workspace splitter persistence missing contract: " + needle)
    if "SizeChanged" in text or "LayoutUpdated" in text:
        errors.append("Workspace splitter persistence must save on DragCompleted, not high-frequency layout/size events")

    base = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
    base_text = base.read_text(encoding="utf-8") if base.is_file() else ""
    if "AttachLayoutPersistence();" not in base_text:
        errors.append("Workspace canonical constructor must attach splitter persistence exactly once after InitializeComponent")

if runtime.is_file():
    text = runtime.read_text(encoding="utf-8")
    for needle in (
        "new(460d, 420d)",
        "ComputedHorizontalScrollBarVisibility",
        "ComputedVerticalScrollBarVisibility",
        "$dataContextMarker = [object]::new()",
        "ReferenceEquals($compact.Content.DataContext, $dataContextMarker)",
        "@('FamilySearch', 'PropertySearch')",
        "$focusTarget.IsTabStop",
    ):
        if needle not in text:
            errors.append("offline WPF palette smoke missing compact overflow/content/focus assertion: " + needle)

print("QS3D per-user UI layout persistence preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: centralized palette minimums preserve a compact 460x420 Workspace host while its 560-DIP content overflows horizontally; RightPanel keeps its 255x480 floor, identical writes are skipped, and layout persistence remains atomic/best-effort.")
