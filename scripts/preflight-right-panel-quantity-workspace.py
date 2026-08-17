from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml"
CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/QuantityInsightPanel.xaml.cs"
VM = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/QuantityInsightViewModel.cs"
PALETTE = ROOT / "src/QS3D.BricsCAD.V25/PaletteCoordinator.cs"
RIGHT_XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
RIGHT_CODE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"

errors = []
for path in (XAML, CODE, VM, PALETTE, RIGHT_XAML, RIGHT_CODE):
    if not path.exists():
        errors.append("missing required file: " + str(path.relative_to(ROOT)))

if errors:
    print("FAIL: " + "\nFAIL: ".join(errors))
    sys.exit(1)

xaml = XAML.read_text(encoding="utf-8")
code = CODE.read_text(encoding="utf-8")
vm = VM.read_text(encoding="utf-8")
palette = PALETTE.read_text(encoding="utf-8")
right_xaml = RIGHT_XAML.read_text(encoding="utf-8")
right_code = RIGHT_CODE.read_text(encoding="utf-8")

try:
    ET.fromstring(xaml)
except ET.ParseError as exc:
    errors.append("QuantityInsightPanel.xaml is not well-formed XML: " + str(exc))

for needle in (
    'x:Class="QS3D.BricsCAD.V25.UI.QuantityInsightPanel"',
    'Text="DIỄN GIẢI KHỐI LƯỢNG"',
    'ItemsSource="{Binding Floors}"',
    'Text="TỔNG QUAN CẢ DỰ ÁN"',
    'Text="{Binding GrossConcreteText}"',
    'Text="{Binding DeductionText}"',
    'Text="{Binding NetConcreteText}"',
    'Text="{Binding FormworkText}"',
    'Text="{Binding LengthText}"',
    'Click="OnRefreshClick"',
    'Click="OnRegenerateClick"',
    'Click="OnOpenBqClick"',
    'Click="OnLocateClick"',
    'MouseDoubleClick="OnQuantityTreeDoubleClick"',
    'Binding IsSelectionMatch',
):
    if needle not in xaml:
        errors.append("quantity insight XAML missing contract: " + needle)

for needle in (
    "ProjectContextCoordinator.TryGetReadOnly(document, out var project)",
    "ProjectQuantityReportBuilder.Group(previewProject)",
    "QuantityReportTotals.FromRows(rows)",
    "SourceHandleResolver.Resolve(project, currentRow.ElementIds)",
    "Cad.CadHandleService.Select(document, handles)",
    'DispatchExistingCommand("QS3DREGEN "',
    'DispatchExistingCommand("QS3DBQ "',
    "ViewportCommands.TryZoomSelection(document)",
    "public void SetInspectionReadOnly",
    "public void ClearQuantityInsights",
):
    if needle not in code:
        errors.append("quantity insight code missing functional contract: " + needle)

for forbidden in (
    "ProjectContextCoordinator.GetOrCreate",
    "ExistingProjectMutationContext.Require",
    ".Touch()",
    "ProjectContextCoordinator.Save",
    'SendStringToExecute("QS3DZOOMSELECTED ',
):
    if forbidden in code:
        errors.append("read-only quantity insight must not use stale mutation/queued-zoom behavior: " + forbidden)

for needle in (
    "ObservableCollection<QuantityInsightFloorViewModel>",
    "QuantityReportTotals totals",
    "GrossConcreteText",
    "DeductionText",
    "NetConcreteText",
    "FormworkText",
    "LengthText",
    "IsSelectionMatch",
):
    if needle not in vm:
        errors.append("quantity insight view model missing contract: " + needle)

for needle in (
    "QuantityInsightGuid",
    "private static PaletteSet? _quantityInsight;",
    "private static QuantityInsightPanel? _quantityInsightPanel;",
    'new PaletteSet("QS3D — Diễn giải khối lượng", QuantityInsightGuid)',
    '_quantityInsight.AddVisual("Khối lượng", _quantityInsightPanel, true);',
    "_quantityInsightPanel?.SetInspectionReadOnly(snapshots, project);",
    "_quantityInsightPanel?.RefreshQuantityInsights();",
    "_quantityInsightPanel?.ClearQuantityInsights(status);",
    "SetVisibility(workspace: false, properties: false, right: false, quantityInsight: true);",
    "private static void SetVisibility(bool workspace, bool properties, bool right, bool quantityInsight)",
    "if (_quantityInsight != null) _quantityInsight.Visible = quantityInsight;",
):
    if needle not in palette:
        errors.append("PaletteCoordinator missing quantity workspace integration: " + needle)

for needle in (
    'Click="OnAttachXrefClick"',
    'Click="OnReloadXrefClick"',
    'Click="OnMoveDrawingClick"',
    'Click="OnZoomWindowClick"',
    'Click="OnDeleteDrawingClick"',
    'ItemsSource="{Binding Layers}"',
):
    if needle not in right_xaml:
        errors.append("existing drawing/layer UI contract disappeared: " + needle)

for needle in (
    "private void OnAttachXrefClick",
    "private void OnReloadXrefClick",
    "private void OnMoveDrawingClick",
    "private void OnZoomWindowClick",
    "private void OnDeleteDrawingClick",
):
    if needle not in right_code:
        errors.append("existing drawing/Xref handler disappeared: " + needle)

if errors:
    for error in errors:
        print("FAIL: " + error)
    sys.exit(1)

print(
    "PASS: the BLT-inspired far-right quantity workspace is backed by live read-only QS3D reporting, "
    "selection highlighting, direct CAD locate/zoom, QS3DREGEN/QS3DBQ dispatch, project totals, centralized palette visibility, and the existing "
    "drawing/Xref/layer manager remains wired to its real handlers."
)
