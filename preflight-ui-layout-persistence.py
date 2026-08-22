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

for path in (store, palette, splitter, workspace, runtime):
    if not path.is_file(): errors.append("missing UI layout persistence file: " + str(path.relative_to(ROOT)))

if store.is_file():
    text = store.read_text(encoding="utf-8")
    for needle in (
        "Environment.SpecialFolder.LocalApplicationData",
        'Path.Combine(root, "QS3D", "BricsCAD-V25", "ui-layout-v1.txt")',
        "MaxFileBytes = 16 * 1024",
        "public int WorkspacePaletteWidth { get; set; } = 640;",
        "public double ModelColumnWidth { get; set; } = 160d;",
        "public double FamilyColumnWidth { get; set; } = 245d;",
        "public double FamilyTopHeight { get; set; } = 250d;",
        "public double RoomTopHeight { get; set; } = 218d;",
        "layout.WorkspacePaletteWidth = Clamp(layout.WorkspacePaletteWidth, 460, 1600);",
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
        if needle not in text: errors.append("UserUiLayoutStore missing fail-safe/atomic/upgraded-layout/no-op contract: " + needle)
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

if palette.is_file():
    text = palette.read_text(encoding="utf-8")
    for needle in (
        "using WpfSize = System.Windows.Size;",
        "var layout = UserUiLayoutStore.Get();",
        "_workspace.DeviceIndependentSize = new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight);",
        "_right.DeviceIndependentSize = new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight);",
        "PersistPaletteLayout();",
        "MinimumSize = new DrawingSize(460, 420)",
        "UserUiLayoutStore.Update(layout =>",
    ):
        if needle not in text: errors.append("PaletteCoordinator missing per-user dimension persistence: " + needle)
    if "_workspace.Size =" in text or "_right.Size =" in text:
        errors.append("PaletteCoordinator must not regress to obsolete PaletteSet.Size for layout persistence")

if splitter.is_file():
    text = splitter.read_text(encoding="utf-8")
    for needle in (
        "static WorkspacePanel()",
        "EventManager.RegisterClassHandler",
        "FrameworkElement.LoadedEvent",
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
<<<<<<< HEAD
print("PASS: the Workspace host restores down to 460x420 while its 560-DIP three-column content overflows horizontally without clipping; splitter defaults persist per user outside QSDB and save only on DragCompleted.")
=======
print("PASS: upgraded Workspace palette/splitter defaults and clamps match the XAML minimums, persist per user outside QSDB, skip identical no-op writes, write atomically/best-effort, restore with device-independent palette size, and save splitters only on DragCompleted.")
>>>>>>> origin/main
