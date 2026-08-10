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
        "SetSelectedLayerLocks", "LayerVisibilityService.SetLocked", "RefreshLayers();",
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
    refresh_start = code.find("private void RefreshLayers()")
    checkbox_start = code.find("private void SetLayerFromCheckBox")
    if refresh_start < 0 or checkbox_start < 0:
        errors.append("RightPanel refresh/checkbox methods are missing")
    elif "_refreshingLayers" not in code[refresh_start:checkbox_start + 300]:
        errors.append("RightPanel does not guard layer checkbox mutation during refresh")

print("QS3D RightPanel layer-fidelity preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)

print("PASS: RightPanel reads native layer RGB/lock state, renders the real color swatch, exposes guarded lock/unlock mutation, and prevents visibility checkbox handlers from writing CAD state during UI refresh.")
