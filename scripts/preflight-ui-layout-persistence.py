#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

store = ROOT / "src/QS3D.BricsCAD.V25/Services/UserUiLayoutStore.cs"
palette = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
splitter = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.LayoutPersistence.cs"
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml"

for path in (store, palette, splitter, workspace):
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
        "layout.WorkspacePaletteWidth = Clamp(layout.WorkspacePaletteWidth, 560, 1600);",
        "layout.ModelColumnWidth = Clamp(layout.ModelColumnWidth, 135d, 500d, 160d);",
        "layout.FamilyColumnWidth = Clamp(layout.FamilyColumnWidth, 220d, 700d, 245d);",
        "layout.FamilyTopHeight = Clamp(layout.FamilyTopHeight, 160d, 900d, 250d);",
        "layout.RoomTopHeight = Clamp(layout.RoomTopHeight, 135d, 900d, 218d);",
        "Normalize(next);",
        "File.Replace(temp, path, backup, true);",
        "catch (IOException)",
        "catch (UnauthorizedAccessException)",
        "TryDelete(temp!)",
    ):
        if needle not in text: errors.append("UserUiLayoutStore missing fail-safe/atomic/upgraded-layout contract: " + needle)
    for forbidden in (".qsdb", "ProjectContextCoordinator", "ProjectState", "project.Metadata"):
        if forbidden in text: errors.append("per-user UI layout must not mutate project/QSDB state: " + forbidden)

if workspace.is_file():
    text = workspace.read_text(encoding="utf-8")
    for needle in (
        'MinWidth="560" MinHeight="540"',
        '<ColumnDefinition Width="160" MinWidth="135"/>',
        '<ColumnDefinition Width="245" MinWidth="220"/>',
        '<RowDefinition Height="250" MinHeight="160"/>',
        '<RowDefinition Height="218" MinHeight="135"/>',
    ):
        if needle not in text: errors.append("WorkspacePanel upgraded layout contract missing: " + needle)

if palette.is_file():
    text = palette.read_text(encoding="utf-8")
    for needle in (
        "using WpfSize = System.Windows.Size;",
        "var layout = UserUiLayoutStore.Get();",
        "_workspace.DeviceIndependentSize = new WpfSize(layout.WorkspacePaletteWidth, layout.WorkspacePaletteHeight);",
        "_right.DeviceIndependentSize = new WpfSize(layout.RightPaletteWidth, layout.RightPaletteHeight);",
        "PersistPaletteLayout();",
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

print("QS3D per-user UI layout persistence preflight")
if errors:
    for error in errors: print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: upgraded Workspace palette/splitter defaults and clamps match the XAML minimums, persist per user outside QSDB, write atomically/best-effort, restore with device-independent palette size, and save splitters only on DragCompleted.")
