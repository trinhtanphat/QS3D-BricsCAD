#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "catalog": ROOT / "src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs",
    "service": ROOT / "src/QS3D.BricsCAD.V25/Cad/LayerVisibilityService.cs",
    "vm": ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/RightPanelViewModel.cs",
    "xaml": ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml",
    "code": ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs",
}

for path in files.values():
    if not path.is_file():
        errors.append("missing RightPanel layer-fidelity file: " + str(path.relative_to(ROOT)))

checks = {
    "catalog": [
        "public bool IsLocked", "public byte Red", "public byte Green", "public byte Blue",
        "IsLocked = layer.IsLocked", "Red = color.Red", "Green = color.Green", "Blue = color.Blue",
    ],
    "service": [
        "SetVisible", "SetLocked", "layer.IsLocked == locked", "layer.IsLocked = locked",
        "document.LockDocument()", "StartTransaction()", "document.Editor.Regen()",
    ],
    "vm": [
        "public Brush ColorBrush", "public bool IsLocked", "PropertyChangedEventArgs(nameof(IsLocked))",
    ],
    "xaml": [
        'Content="Khóa"', 'Click="OnLockLayersClick"', 'Content="Mở khóa"', 'Click="OnUnlockLayersClick"',
        'IsChecked="{Binding IsLocked}"', 'Background="{Binding ColorBrush}"',
        'Checked="OnLayerChecked"', 'Unchecked="OnLayerUnchecked"',
    ],
    "code": [
        "private bool _refreshingLayers", "_refreshingLayers = true", "_refreshingLayers = false",
        "if (_refreshingLayers) return", "Color.FromRgb(item.Red, item.Green, item.Blue)", "brush.Freeze()",
        "private void ReloadLayers()", "foreach (var item in DrawingCatalogReader.ReadLayers(doc))",
        "CollectionViewSource.GetDefaultView(_viewModel.Layers)",
        "private void ApplyLayerFilter()", "SetSelectedLayerLocks", "LayerVisibilityService.SetLocked", "ReloadLayers();",
    ],
}

for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing layer-fidelity token: " + needle)

if files["xaml"].is_file():
    xaml = files["xaml"].read_text(encoding="utf-8")
    if 'Background="{StaticResource AccentBrush}" Opacity="0.75"' in xaml:
        errors.append("RightPanel still uses the old accent placeholder as the layer color swatch")

if files["code"].is_file():
    code = files["code"].read_text(encoding="utf-8")
    refresh_start = code.find("private void ReloadLayers()")
    filter_start = code.find("private void ApplyLayerFilter()", refresh_start)
    checkbox_start = code.find("private void SetLayerFromCheckBox")
    bulk_visibility_start = code.find("private void SetSelectedLayers", checkbox_start)
    bulk_lock_start = code.find("private void SetSelectedLayerLocks", bulk_visibility_start)
    next_method = code.find("private void OnAttachXrefClick", bulk_lock_start)
    if min(refresh_start, filter_start, checkbox_start, bulk_visibility_start, bulk_lock_start, next_method) < 0:
        errors.append("RightPanel live reload/filter/checkbox/bulk layer methods are missing")
    elif not (refresh_start < filter_start < checkbox_start < bulk_visibility_start < bulk_lock_start < next_method):
        errors.append("RightPanel live reload, cached filter and native mutation methods must remain structurally separate")
    else:
        reload_body = code[refresh_start:filter_start]
        filter_body = code[filter_start:checkbox_start]
        checkbox_body = code[checkbox_start:bulk_visibility_start]
        bulk_visibility_body = code[bulk_visibility_start:bulk_lock_start]
        bulk_lock_body = code[bulk_lock_start:next_method]
        for token in ("DrawingCatalogReader.ReadLayers(doc)", "ApplyLayerFilter();"):
            if token not in reload_body:
                errors.append("RightPanel ReloadLayers must re-read live CAD then rebuild the filtered view: " + token)
        if "DrawingCatalogReader.ReadLayers" in filter_body:
            errors.append("RightPanel ApplyLayerFilter must remain presentation-only over cached live snapshots")
        for token in ("view.Filter =", "view.Refresh();", "_viewModel.SetLayerCounts"):
            if token not in filter_body:
                errors.append("RightPanel cached collection-view filter contract missing: " + token)
        for forbidden in ("_refreshingLayers", "_viewModel.Layers.Clear", "new SolidColorBrush", "LayerVisibilityService"):
            if forbidden in filter_body:
                errors.append("RightPanel presentation-only filter must not rebuild rows or mutate CAD: " + forbidden)
        for label, body, service in (
            ("checkbox", checkbox_body, "LayerVisibilityService.SetVisible"),
            ("bulk visibility", bulk_visibility_body, "LayerVisibilityService.SetVisible"),
            ("bulk lock", bulk_lock_body, "LayerVisibilityService.SetLocked"),
        ):
            if service not in body or "ReloadLayers();" not in body:
                errors.append("RightPanel " + label + " mutation must write native state then reload live layer snapshots")
        if "if (_refreshingLayers) return" not in checkbox_body:
            errors.append("RightPanel checkbox mutation must ignore events raised while rebuilding the cached view")

print("QS3D RightPanel layer-fidelity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: RightPanel reads native layer RGB/lock state, renders the real color swatch, exposes guarded lock/unlock mutation, and prevents visibility checkbox handlers from writing CAD state during UI refresh.")
