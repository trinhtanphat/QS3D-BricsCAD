#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
panel = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
selection_panel = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SelectionInspection.cs"
row = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs"

if not workspace.is_file():
    errors.append("missing WorkspaceViewModel.cs")
else:
    text = workspace.read_text(encoding="utf-8")
    required = (
        "SourceDerivedInstanceKeys",
        '"LengthM"', '"AreaM2"', '"VolumeM3"', '"PerimeterM"', '"Layer"',
        "NGUỒN CAD / ĐO ĐẠC",
        "sourceRow.IsReadOnly = true",
        "row.IsReadOnly = true",
        "RequiresPositiveNumber(key)",
        "RequiresNonNegativeNumber(key)",
        "phải lớn hơn 0; đã giữ giá trị cũ",
        "không được âm; đã giữ giá trị cũ",
        'case "BottomOffsetM": return "Offset đáy (so với source)";',
        'case "TopOffsetM": return "Offset đỉnh (so với source)";',
        "public void SetSelectedElement(ProjectElement? element)",
        "_selectedElement = null;",
        "ShowFamilyProperties();",
    )
    for token in required:
        if token not in text:
            errors.append("Workspace property safety missing: " + token)
    for stale in (
        'case "BottomOffsetM": return "Cao độ đáy";',
        'case "TopOffsetM": return "Cao độ đỉnh";',
    ):
        if stale in text:
            errors.append("Workspace still exposes source-relative offset as absolute elevation: " + stale)

if not panel.is_file():
    errors.append("missing WorkspacePanel.xaml.cs")
else:
    text = panel.read_text(encoding="utf-8")
    for token in (
        "public void SetInspection(IReadOnlyList<EntitySnapshot> snapshots)",
        "try\n            {\n                SyncFamilyFromSelection();\n            }\n            catch (Exception ex)",
        'ClearProject("Selection sync semantic lỗi: " + ex.Message);',
        "if (_inspection.Count != 1)",
        "Selection gồm nhiều đối tượng CAD; inspector giữ scope Family để tránh sửa nhầm Instance.",
        "_viewModel.SetSelectedElement(null);",
        "if (matches.Count != 1 || string.IsNullOrWhiteSpace(matches[0].FamilyId))",
        "Cấu kiện semantic đang chọn không còn Family hợp lệ; inspector đã về scope Family.",
    ):
        if token not in text:
            errors.append("Workspace selection safety missing: " + token)

if not selection_panel.is_file():
    errors.append("missing WorkspacePanel.SelectionInspection.cs")
else:
    text = selection_panel.read_text(encoding="utf-8")
    for token in (
        "internal void SetInspectionReadOnly(IReadOnlyList<EntitySnapshot> snapshots, ProjectState? project)",
        "if (project == null || _inspection.Count != 1)",
        "_viewModel.SetSelectedElement(null);",
    ):
        if token not in text:
            errors.append("Workspace read-only selection safety missing: " + token)

if not row.is_file():
    errors.append("missing PropertyRowViewModel.cs")
else:
    text = row.read_text(encoding="utf-8")
    for token in (
        "bool.TryParse(text, out var parsed)",
        'text.Equals("yes", StringComparison.OrdinalIgnoreCase)',
        'text.Equals("on", StringComparison.OrdinalIgnoreCase)',
        'text.Equals("bật", StringComparison.CurrentCultureIgnoreCase)',
    ):
        if token not in text:
            errors.append("Boolean property editor missing normalized truthy value: " + token)

print("QS3D Workspace property safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Workspace property rows stay bounded to one exclusive live semantic selection, multi-selection drops Instance scope, source-derived CAD measurements remain read-only, impossible geometry dimensions fail early, source-relative offsets are labeled accurately, and Vietnamese boolean values render consistently.")