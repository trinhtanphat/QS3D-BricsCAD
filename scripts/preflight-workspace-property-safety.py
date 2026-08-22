#!/usr/bin/env python3
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
errors = []
workspace = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/WorkspaceViewModel.cs"
panel = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.xaml.cs"
selection_panel = ROOT / "src/QS3D.BricsCAD.V25/UI/WorkspacePanel.SelectionInspection.cs"
row = ROOT / "src/QS3D.BricsCAD.V25/UI/ViewModels/PropertyRowViewModel.cs"
element = ROOT / "src/QS3D.Core/Domain/ProjectElement.cs"

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
        "row.Reset = () => ResetInstanceProperty(element, family, key, row);",
        "private void ResetInstanceProperty(ProjectElement element, ProjectFamily family, string key, PropertyRowViewModel row)",
        'TryGetCurrentProjectForMutation("Đặt lại Instance property", out var project)',
        "ownedElement == null || !ReferenceEquals(ownedElement, element) || ownedFamily == null || !ReferenceEquals(ownedFamily, family)",
        "if (!ownedFamily.Properties.TryGetValue(key, out var liveFamilyRaw))",
        "row.Value = ToDisplayValue(key, liveFamilyRaw ?? string.Empty);",
        'ProjectSemanticMutationExecutor.Execute(',
        '"Workspace single-instance property edit"',
        "element.SetProperty(key, next);",
        "project.Touch();",
    )
    for token in required:
        if token not in text:
            errors.append("Workspace property safety missing: " + token)
    for stale in (
        'case "BottomOffsetM": return "Cao độ đáy";',
        'case "TopOffsetM": return "Cao độ đỉnh";',
        "row.Reset = () => row.Value = ToDisplayValue(key, familyValue);",
        "element.SetProperty(key, next);\n            element.MarkDirty(ElementDirtyFlags.All);",
    ):
        if stale in text:
            errors.append("Workspace still exposes stale/over-invalidating property behavior: " + stale)

if not element.is_file():
    errors.append("missing ProjectElement.cs")
else:
    text = element.read_text(encoding="utf-8")
    for token in (
        "var affectsGeneratedGeometry = ElementGeometryPolicy.AffectsGeneratedGeometry(Category, key);",
        "var affectsGeneratedOutput = ElementGeometryPolicy.AffectsGeneratedOutput(Category, key);",
        "var flags = ElementDirtyFlags.Properties | ElementDirtyFlags.Quantity;",
        "if (affectsGeneratedGeometry) flags |= ElementDirtyFlags.Geometry;",
        "MarkDirtyCore(flags, affectsGeneratedOutput);",
    ):
        if token not in text:
            errors.append("ProjectElement property-specific dirty/stale policy missing: " + token)
    if "MarkDirtyCore(flags, true);" in text:
        errors.append("ProjectElement must not mark generated geometry stale for non-geometric property changes")

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
        "if (project == null || _inspection.Count == 0)",
        "TryResolveSemanticSelection(project, _inspection, out var selectedElements, out var selectionError)",
        "if (selectedElements.Count > 1)",
        "PresentMultiSelection(project, selectedElements)",
        "var singleElement = selectedElements.Count == 1 ? selectedElements[0] : null",
        "_viewModel.SetSelectedElement(null);",
    ):
        if token not in text:
            errors.append("Workspace read-only selection safety missing: " + token)

if not row.is_file():
    errors.append("missing PropertyRowViewModel.cs")
else:
    text = row.read_text(encoding="utf-8")
    for token in (
        "private bool _isReadOnly;",
        "if (_isReadOnly == value) return;",
        "if (_isReadOnly && _canReset)",
        "_canReset = false;",
        "OnChanged(nameof(CanReset));",
        "OnChanged(nameof(IsEditable));",
        "var next = !_isReadOnly && value;",
        "if (_canReset == next) return;",
        "if ((IsReadOnly || Apply == null) && string.Equals(_value, requested, StringComparison.Ordinal)) return;",
        "var next = !IsReadOnly && Apply != null ? Apply(requested) ?? string.Empty : requested;",
        "bool.TryParse(text, out var parsed)",
        'text.Equals("yes", StringComparison.OrdinalIgnoreCase)',
        'text.Equals("on", StringComparison.OrdinalIgnoreCase)',
        'text.Equals("bật", StringComparison.CurrentCultureIgnoreCase)',
    ):
        if token not in text:
            errors.append("Property row reactive/revalidation/boolean safety missing: " + token)
    if "if (string.Equals(_value, requested, StringComparison.Ordinal)) return;" in text:
        errors.append("Editable PropertyRow values must not skip Apply solely because the displayed text is unchanged; live semantic state may have changed modelessly")
    guarded_no_op = text.find("if ((IsReadOnly || Apply == null) && string.Equals(_value, requested, StringComparison.Ordinal)) return;")
    apply = text.find("var next = !IsReadOnly && Apply != null ? Apply(requested) ?? string.Empty : requested;")
    if guarded_no_op < 0 or apply < 0 or guarded_no_op > apply:
        errors.append("Only read-only/unbound display no-ops may short-circuit before Apply")

print("QS3D Workspace property safety preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)
print("PASS: Workspace property rows honor Core property-specific dirty/stale invalidation, read-only rows cannot retain reset state, editable same-text commits revalidate live modeless state, Instance reset resolves live Family state, selection scope fails closed, and numeric/boolean editors stay validated.")
