#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
errors = []

store = ROOT / "src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs"
palette = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
splitter = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs"
compact = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.CompactShell.cs"
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"
runtime = ROOT / "scripts/test-wpf-palettes-runtime.ps1"
right_panel = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"

for path in (store, palette, splitter, compact, workspace, right_panel, runtime):
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
        'x:Name="WorkspaceOverflow"',
        'x:Name="WorkspaceContentRoot"',
        'Width="{Binding ViewportWidth, ElementName=WorkspaceOverflow}"',
        'MinWidth="560"',
        'HorizontalScrollBarVisibility="Auto"',
        'VerticalScrollBarVisibility="Disabled"',
        'PanningMode="HorizontalOnly"',
        'HorizontalContentAlignment="Stretch"',
        '<ColumnDefinition Width="160" MinWidth="135"/>',
        '<ColumnDefinition Width="245" MinWidth="220"/>',
        '<RowDefinition Height="250" MinHeight="160"/>',
        '<RowDefinition Height="218" MinHeight="135"/>',
    ):
        if needle not in text: errors.append("WorkspacePanel upgraded layout contract missing: " + needle)
    if 'MinWidth="560" MinHeight="540"' in text:
        errors.append("WorkspacePanel must not force its 560-DIP content width onto the compact palette host")
    if "<UserControl.Template>" in text:
        errors.append("WorkspacePanel must not replace the UserControl template just to provide compact horizontal overflow")
    if 'Width="{Binding ViewportWidth, RelativeSource={RelativeSource AncestorType={x:Type ScrollViewer}}}"' in text:
        errors.append("WorkspacePanel must not bind content Width to ScrollViewer.ViewportWidth; normal content composition owns overflow")
    try:
        root = ET.parse(workspace).getroot()
        wpf = "{http://schemas.microsoft.com/winfx/2006/xaml/presentation}"
        xaml = "{http://schemas.microsoft.com/winfx/2006/xaml}"
        if root.attrib.get("MinWidth") != "0" or root.attrib.get("MinHeight") != "0":
            errors.append("WorkspacePanel host surface must remain shrinkable to the PaletteSet minimum")
        if root.find(wpf + "UserControl.Template") is not None:
            errors.append("WorkspacePanel host-safe overflow must use normal content, not a custom UserControl template")
        scroller = root.find(wpf + "ScrollViewer")
        if scroller is None:
            errors.append("WorkspacePanel missing the explicit compact-host horizontal overflow viewport")
        else:
            expected = {
                "HorizontalScrollBarVisibility": "Auto",
                "VerticalScrollBarVisibility": "Disabled",
                "CanContentScroll": "False",
                "PanningMode": "HorizontalOnly",
                "HorizontalContentAlignment": "Stretch",
                "VerticalContentAlignment": "Stretch",
            }
            for key, value in expected.items():
                if scroller.attrib.get(key) != value:
                    errors.append("WorkspacePanel overflow viewport must set " + key + "=" + value)
            if scroller.attrib.get(xaml + "Name") != "WorkspaceOverflow":
                errors.append("WorkspacePanel overflow viewport must expose the stable WorkspaceOverflow name")
            content_grid = scroller.find(wpf + "Grid")
            if content_grid is None:
                errors.append("WorkspacePanel overflow viewport must contain the three-column Grid directly")
            else:
                if content_grid.attrib.get(xaml + "Name") != "WorkspaceContentRoot":
                    errors.append("WorkspacePanel design surface must expose WorkspaceContentRoot for layout persistence")
                if content_grid.attrib.get("MinWidth") != "560":
                    errors.append("WorkspacePanel three-column content must retain its 560-DIP design width inside overflow")
                if content_grid.attrib.get("Width") != "{Binding ViewportWidth, ElementName=WorkspaceOverflow}":
                    errors.append("WorkspacePanel content must follow the live viewport width while retaining its compact minimum")
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
        "var root = WorkspaceContentRoot;",
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
    if "Content is Grid" in text:
        errors.append("Workspace splitter persistence must target WorkspaceContentRoot after host-safe ScrollViewer composition")
    if "SizeChanged" in text or "LayoutUpdated" in text:
        errors.append("Workspace splitter persistence must save on DragCompleted, not high-frequency layout/size events")

    base = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
    base_text = base.read_text(encoding="utf-8") if base.is_file() else ""
    if "AttachLayoutPersistence();" not in base_text:
        errors.append("Workspace canonical constructor must attach splitter persistence exactly once after InitializeComponent")

if compact.is_file():
    text = compact.read_text(encoding="utf-8")
    for needle in (
        "var root = WorkspaceContentRoot;",
        "root.MinWidth = 0;",
        "WorkspaceOverflow.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;",
        "WorkspaceOverflow.ScrollToHorizontalOffset(0);",
        "root.RowDefinitions[0].Height = new GridLength(40);",
        "root.RowDefinitions[2].Height = new GridLength(30);",
        "var primaryColumn = workspace.ColumnDefinitions[0];",
        "primaryColumn.MinWidth = 0;",
        "primaryColumn.MaxWidth = double.PositiveInfinity;",
        "primaryColumn.Width = new GridLength(1, GridUnitType.Star);",
        "for (var index = 1; index < workspace.ColumnDefinitions.Count; index++)",
        "retiredColumn.MinWidth = 0;",
        "retiredColumn.MaxWidth = 0;",
        "retiredColumn.Width = new GridLength(0);",
        "if (Grid.GetColumn(child) > 0)",
        "child.Visibility = Visibility.Collapsed;",
    ):
        if needle not in text:
            errors.append("Workspace compact shell missing retired-pane presentation contract: " + needle)

    for forbidden in (
        "familyAndProperties.RowDefinitions[0].Height =",
        "familyAndProperties.RowDefinitions[0].MinHeight =",
        "roomAndSelection.RowDefinitions[0].Height =",
        "roomAndSelection.RowDefinitions[0].MinHeight =",
    ):
        if forbidden in text:
            errors.append(
                "Workspace compact shell must not mutate retired pane row dimensions; retirement is enforced at the workspace-column boundary: " + forbidden
            )

if runtime.is_file():
    text = runtime.read_text(encoding="utf-8")
    for needle in (
        "new(460d, 420d)",
        "FindName('WorkspaceOverflow')",
        "FindName('WorkspaceContentRoot')",
        "ComputedHorizontalScrollBarVisibility",
        "ComputedVerticalScrollBarVisibility",
        "$dataContextMarker = [object]::new()",
        "ReferenceEquals($contentRoot.DataContext, $dataContextMarker)",
        "@('FamilySearch', 'PropertySearch')",
        "$focusTarget.IsTabStop",
    ):
        if needle not in text:
            errors.append("offline WPF palette smoke missing host-safe compact overflow/content/focus assertion: " + needle)

print("QS3D per-user UI layout persistence preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: centralized palette minimums remain persisted atomically/best-effort, while the compact presentation layer permanently retires the obsolete Workspace dashboard columns after legacy widths are restored.")
