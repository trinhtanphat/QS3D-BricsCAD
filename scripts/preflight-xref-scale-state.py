#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
CATALOG = ROOT / "src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs"
VM = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/RightPanelViewModel.cs"
PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.XrefScale.cs"
XAML = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml"
CORE = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs"
LOCK_PARTIAL = ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.XrefLock.cs"
errors = []

for path in (CATALOG, VM, PARTIAL, XAML, CORE, LOCK_PARTIAL):
    if not path.is_file():
        errors.append("missing Xref scale source: " + str(path.relative_to(ROOT)))

if CATALOG.is_file():
    text = CATALOG.read_text(encoding="utf-8")
    required = (
        "public bool HasScale { get; set; }",
        "public bool MixedScale { get; set; }",
        "public double ScaleX { get; set; } = 1d;",
        "public double ScaleY { get; set; } = 1d;",
        "public double ScaleZ { get; set; } = 1d;",
        'public string ScaleText { get; set; } = "—";',
        "private const double ScaleTolerance = 1e-9;",
        "document.Database.CurrentSpaceId",
        "var scale = reference.ScaleFactors;",
        "snapshot.ScaleX = scale.X;",
        "snapshot.ScaleY = scale.Y;",
        "snapshot.ScaleZ = scale.Z;",
        "snapshot.MixedScale = true;",
        "private static bool SameScale(double left, double right)",
        "ScaleTolerance * magnitude",
        'snapshot.MixedScale\n                            ? "Hỗn hợp"',
        "private static string FormatScale(double x, double y, double z)",
        'if (SameScale(x, 1d)) return "1:1";',
        '? "1:" + FormatScaleNumber(1d / x)',
        ': FormatScaleNumber(x) + ":1";',
        'return "X/Y/Z " + FormatScaleNumber(x) + "/" + FormatScaleNumber(y) + "/" + FormatScaleNumber(z);',
        'value.ToString("0.######", CultureInfo.InvariantCulture)',
    )
    for needle in required:
        if needle not in text:
            errors.append("DrawingCatalogReader missing current-space Xref scale contract: " + needle)

    current_space_pos = text.find("document.Database.CurrentSpaceId")
    block_ref_pos = text.find("as BlockReference", current_space_pos)
    count_pos = text.find("snapshot.InstanceCount = checked", block_ref_pos)
    scale_pos = text.find("reference.ScaleFactors", count_pos)
    mixed_pos = text.find("snapshot.MixedScale = true;", scale_pos)
    finalize_pos = text.find("snapshot.ScaleText =", mixed_pos)
    if min(current_space_pos, block_ref_pos, count_pos, scale_pos, mixed_pos, finalize_pos) < 0 or not (
        current_space_pos < block_ref_pos < count_pos < scale_pos < mixed_pos < finalize_pos
    ):
        errors.append("scale capture must stay in the existing current-space reference scan before final scale formatting")

    method_start = text.find("public static IReadOnlyList<DrawingReferenceSnapshot> ReadReferences")
    method_end = text.find("private static bool SameScale", method_start)
    body = text[method_start:method_end]
    for forbidden in (
        "LockDocument",
        "OpenMode.ForWrite",
        "SetSystemVariable",
        "SendStringToExecute",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        ".qsdb",
    ):
        if forbidden in body:
            errors.append("Xref scale catalog must remain read-only: " + forbidden)

if VM.is_file():
    text = VM.read_text(encoding="utf-8")
    required = (
        "public sealed class DrawingItemViewModel : INotifyPropertyChanged",
        'private string _scaleText = "—";',
        "public string ScaleText",
        'var normalized = string.IsNullOrWhiteSpace(value) ? "—" : value;',
        "new PropertyChangedEventArgs(nameof(ScaleText))",
        "public event PropertyChangedEventHandler? PropertyChanged;",
        "public string Kind { get; set; } = \"DWG\";",
        "public string InstanceText { get; set; } = \"—\";",
    )
    for needle in required:
        if needle not in text:
            errors.append("DrawingItemViewModel missing notifying scale/preserved row contract: " + needle)

if PARTIAL.is_file():
    text = PARTIAL.read_text(encoding="utf-8")
    required = (
        "private static readonly bool XrefScaleClassHandlerRegistered = RegisterXrefScaleClassHandler();",
        "_ = XrefScaleClassHandlerRegistered;",
        "FrameworkElement.LoadedEvent",
        "_viewModel.Drawings.CollectionChanged += OnDrawingScaleCollectionChanged;",
        "if (_xrefScaleHooked) return;",
        "if (_xrefScaleRefreshQueued) return;",
        "Dispatcher.BeginInvoke(",
        "DispatcherPriority.Loaded,",
        "ApplyXrefScaleState();",
        "DrawingCatalogReader.ReadReferences(document)",
        "GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)",
        'row.ScaleText = row.IsXref ? "—" : "1:1";',
        'row.ScaleText = "1:1";',
        "byName.TryGetValue(row.Name, out var snapshot)",
        "? snapshot.ScaleText",
        ': "—";',
    )
    for needle in required:
        if needle not in text:
            errors.append("RightPanel.XrefScale missing idempotent enrichment contract: " + needle)

    hook_pos = text.find("_viewModel.Drawings.CollectionChanged += OnDrawingScaleCollectionChanged;")
    queue_pos = text.find("private void QueueXrefScaleRefresh()")
    dispatcher_pos = text.find("Dispatcher.BeginInvoke(", queue_pos)
    apply_pos = text.find("ApplyXrefScaleState();", dispatcher_pos)
    read_pos = text.find("DrawingCatalogReader.ReadReferences(document)", apply_pos)
    set_pos = text.find("row.ScaleText", read_pos)
    if min(hook_pos, queue_pos, dispatcher_pos, apply_pos, read_pos, set_pos) < 0 or not (
        hook_pos < queue_pos < dispatcher_pos < apply_pos < read_pos < set_pos
    ):
        errors.append("scale enrichment must subscribe once, coalesce through Dispatcher, then read catalog state and update row properties")

    for forbidden in (
        "RefreshDrawingsOnly(",
        "ProjectContextCoordinator",
        "ExistingProjectMutationContext",
        ".qsdb",
        "SendStringToExecute",
        "SetSystemVariable",
        "OpenMode.ForWrite",
        "ScaleFactors =",
    ):
        if forbidden in text:
            errors.append("RightPanel scale enrichment must remain read-only/non-recursive: " + forbidden)

if XAML.is_file():
    text = XAML.read_text(encoding="utf-8")
    required = (
        'GridViewColumn Header="Tên"',
        'GridViewColumn Header="Khóa"',
        'GridViewColumn Header="SL"',
        'DisplayMemberBinding="{Binding InstanceText}"',
        'GridViewColumn Header="Tỉ lệ"',
        'DisplayMemberBinding="{Binding ScaleText}"',
        'Content="+ Thêm"',
        'Click="OnReloadXrefClick"',
        'Click="OnMoveDrawingClick"',
        'Click="OnLockXrefClick"',
        'Click="OnUnlockXrefClick"',
        'Click="OnZoomWindowClick"',
        'Click="OnDeleteDrawingClick"',
    )
    for needle in required:
        if needle not in text:
            errors.append("RightPanel.xaml missing scale/preserved Xref action contract: " + needle)
    if 'GridViewColumn Header="Loại"' in text:
        errors.append("drawing manager must use the screenshot-style Tỉ lệ display column instead of the redundant Loại column")

if CORE.is_file():
    text = CORE.read_text(encoding="utf-8")
    required = (
        "private void RefreshDrawingsOnly()",
        "DrawingCatalogReader.ReadReferences(doc)",
        "InstanceText = item.InstanceCount.ToString(CultureInfo.InvariantCulture)",
        "private DrawingItemViewModel? SelectedXref()",
        "XrefService.SelectInstances(doc, item.Name)",
        "XrefService.Reload(doc, item.Name)",
        "XrefService.Detach(doc, item.Name)",
    )
    for needle in required:
        if needle not in text:
            errors.append("existing RightPanel drawing refresh/Xref safety action disappeared: " + needle)

if LOCK_PARTIAL.is_file():
    text = LOCK_PARTIAL.read_text(encoding="utf-8")
    for needle in (
        "OnLockXrefClick",
        "OnUnlockXrefClick",
        "XrefService.SetInstanceLayersLocked(document, item.Name, locked)",
        "RefreshAfterXrefMutation(status);",
    ):
        if needle not in text:
            errors.append("completed Xref native lock lane disappeared: " + needle)

print("QS3D Xref scale-state preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: drawing manager reports read-only current-space Xref scale state with mixed/non-uniform handling, notifying row enrichment, screenshot-style Tỉ lệ display, and all prior Xref/layer actions preserved.")
