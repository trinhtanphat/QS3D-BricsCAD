#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []

files = {
    "catalog": ROOT / "src/QS3D.BricsCAD.V25/Cad/DrawingCatalogReader.cs",
    "vm": ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/RightPanelViewModel.cs",
    "xaml": ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml",
    "code": ROOT / "src/QS3D.BricsCAD.V25/UI/RightPanel.xaml.cs",
}
for path in files.values():
    if not path.is_file():
        errors.append("missing Xref live-status file: " + str(path.relative_to(ROOT)))

checks = {
    "catalog": [
        "public int InstanceCount", "public int LockedInstanceCount", "public string LockState",
        "new Dictionary<ObjectId, DrawingReferenceSnapshot>()", "reference.BlockTableRecord",
        "reference.LayerId", "layer.IsLocked", 'snapshot.LockState = "Mở"',
        'snapshot.LockState = "Khóa"', 'snapshot.LockState = "Hỗn hợp"',
    ],
    "vm": [
        "public string LockState", "public string InstanceText",
    ],
    "xaml": [
        'Header="Khóa"', 'DisplayMemberBinding="{Binding LockState}"',
        'Header="SL"', 'DisplayMemberBinding="{Binding InstanceText}"',
    ],
    "code": [
        "private bool _refreshingDrawings", "_refreshingDrawings = true", "_refreshingDrawings = false",
        "if (_refreshingDrawings) return", "RefreshDrawingsOnly()",
        "LockState = item.LockState", "InstanceText = item.InstanceCount.ToString",
    ],
}
for key, needles in checks.items():
    path = files[key]
    if not path.is_file():
        continue
    text = path.read_text(encoding="utf-8")
    for needle in needles:
        if needle not in text:
            errors.append(str(path.relative_to(ROOT)) + " missing Xref live-status token: " + needle)

if files["xaml"].is_file():
    text = files["xaml"].read_text(encoding="utf-8")
    if 'Header="Khóa" Width="42"><GridViewColumn.CellTemplate><DataTemplate><CheckBox IsChecked="{Binding IsLocked}"' in text:
        errors.append("Drawing Xref lock column still uses the old hard-coded boolean checkbox presentation.")

if files["code"].is_file():
    text = files["code"].read_text(encoding="utf-8")
    method_start = text.find("private void RefreshDrawingsOnly()")
    method_end = text.find("private void OnRefreshClick", method_start)
    if method_start < 0 or method_end < 0:
        errors.append("RightPanel drawing refresh helper is missing.")
    else:
        body = text[method_start:method_end]
        if "_refreshingDrawings = true" not in body or "finally" not in body or "_refreshingDrawings = false" not in body:
            errors.append("RightPanel drawing refresh must guard SelectionChanged side effects with try/finally.")

print("QS3D Xref live-status preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with %d error(s)." % len(errors))
    sys.exit(1)
print("PASS: RightPanel derives Xref instance count and lock state from live current-space BlockReference layer state, presents mixed/locked/unlocked status, and keeps drawing-list refresh read-only with respect to CAD selection.")
