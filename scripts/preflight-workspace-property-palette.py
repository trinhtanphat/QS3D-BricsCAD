#!/usr/bin/env python3
from pathlib import Path
import sys
import xml.etree.ElementTree as ET

ROOT = Path(__file__).resolve().parents[1]
UI = ROOT / "src" / "QS3D.BricsCAD.V25" / "UI"
xaml = UI / "WorkspacePanel.xaml"
filter_code = UI / "WorkspacePanel.PropertyFiltering.cs"
selection_code = UI / "WorkspacePanel.SelectionInspection.cs"
multi_code = UI / "WorkspacePanel.MultiSelectionProperties.cs"
view_model_code = UI / "ViewModels" / "WorkspaceViewModel.cs"
bulk_code = ROOT / "src" / "QS3D.Core" / "Selection" / "SemanticSelectionBulkEditService.cs"
policy_code = ROOT / "src" / "QS3D.Core" / "Services" / "SemanticPropertyEditPolicy.cs"
atomicity_smoke = ROOT / "tests" / "QS3D.Core.SmokeTests" / "SemanticMutationAtomicitySmoke.cs"
errors = []

if not xaml.is_file():
    errors.append("missing WorkspacePanel.xaml")
else:
    try:
        ET.parse(xaml)
    except ET.ParseError as exc:
        errors.append("WorkspacePanel.xaml is not well-formed: " + str(exc))
    text = xaml.read_text(encoding="utf-8")
    for token in (
        'x:Name="PropertySearch"',
        'TextChanged="OnPropertySearchChanged"',
        'Click="OnClearPropertySearchClick"',
        'Text="Family kế thừa • Instance override • CAD khóa"',
        'Text="{Binding Properties.Count, StringFormat={}{0} dòng}"',
        'Text="{Binding Properties.Count, StringFormat={}{0} thuộc tính}"',
        'Value="Override"',
        'Value="CAD / đọc"',
        'x:Key="WorkspacePropertyRow"',
        'x:Key="WorkspaceSearchBand"',
        'MinWidth="220"',
    ):
        if token not in text:
            errors.append("Workspace upgraded property palette missing: " + token)

if not filter_code.is_file():
    errors.append("missing WorkspacePanel.PropertyFiltering.cs")
else:
    text = filter_code.read_text(encoding="utf-8")
    for token in (
        "private const int MaxPropertySearchTokens = 12;",
        "private void OnWorkspaceDataContextChanged",
        "PreviewKeyDown -= OnPropertyFilterShortcut;",
        "PreviewKeyDown += OnPropertyFilterShortcut;",
        "private void OnPropertyFilterShortcut",
        "modifiers == (ModifierKeys.Control | ModifierKeys.Shift) && e.Key == Key.F",
        "PropertySearch?.Focus();",
        "PropertySearch?.SelectAll();",
        "modifiers == ModifierKeys.None && e.Key == Key.Enter",
        "PropertyList != null && PropertyList.IsKeyboardFocusWithin",
        "var combo = FindPropertyEditorAncestor<ComboBox>(source);",
        "if (combo != null && combo.IsEditable)",
        "if (combo.IsDropDownOpen) return;",
        "combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();",
        "var textBox = FindPropertyEditorAncestor<TextBox>(source);",
        "textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();",
        "private static T? FindPropertyEditorAncestor<T>(DependencyObject? source) where T : DependencyObject",
        "current = ParentOf(current);",
        "e.Key == Key.Escape",
        "PropertySearch.IsKeyboardFocusWithin",
        "PropertySearch.Clear();",
        "private void OnPropertySearchChanged",
        "private void OnClearPropertySearchClick",
        "private void ApplyPropertyFilter()",
        "CollectionViewSource.GetDefaultView(PropertyList?.ItemsSource)",
        ".Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)",
        ".Take(MaxPropertySearchTokens)",
        "tokens.All(token => MatchesPropertyToken(row, token))",
        "private static bool MatchesPropertyToken",
        "Contains(row.Group, token)",
        "Contains(row.Name, token)",
        "Contains(row.Unit, token)",
        "Contains(row.Value, token)",
        "Contains(row.EditorKind, token)",
        "row.Choices.Any(choice => Contains(choice, token))",
        'Contains("CAD đọc khóa readonly source nguồn", token)',
        'Contains("Instance override ghi đè", token)',
        "StringComparison.CurrentCultureIgnoreCase",
    ):
        if token not in text:
            errors.append("Workspace property filter/editor keyboard UX missing: " + token)

    combo_guard = text.find("if (combo != null && combo.IsEditable)")
    dropdown_guard = text.find("if (combo.IsDropDownOpen) return;", combo_guard)
    combo_commit = text.find("combo.GetBindingExpression(ComboBox.TextProperty)?.UpdateSource();", combo_guard)
    if combo_guard < 0 or dropdown_guard < combo_guard or combo_commit < dropdown_guard:
        errors.append("Enter must defer to an open editable ComboBox dropdown before using Enter as an explicit property commit")

    for forbidden in (
        "GetOrCreate(",
        "ExistingProjectMutationContext",
        "ProjectFamilyService",
        ".Touch(",
        "SetProperty(",
        "SendStringToExecute",
    ):
        if forbidden in text:
            errors.append("Workspace property filter/keyboard routing must not directly mutate project/CAD: " + forbidden)

if not selection_code.is_file():
    errors.append("missing WorkspacePanel.SelectionInspection.cs")
else:
    text = selection_code.read_text(encoding="utf-8")
    for token in (
        "TryResolveSemanticSelection(project, _inspection",
        "selectedElements.Count > 1",
        "PresentMultiSelection(project, selectedElements)",
        "RestoreMultiSelectionPresentationState()",
        "selectedElements.Count == 1 ? selectedElements[0] : null",
    ):
        if token not in text:
            errors.append("Workspace semantic selection routing missing: " + token)
    if "project == null || _inspection.Count != 1" in text:
        errors.append("legacy exclusive single-selection gate still blocks semantic multi-selection presentation")

if not multi_code.is_file():
    errors.append("missing WorkspacePanel.MultiSelectionProperties.cs")
else:
    text = multi_code.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Model;",
        "using QS3D.Core.Services;",
        "SemanticSelectionInspector.Inspect(project, ids)",
        "summary.PresentCount",
        "inspection.Count",
        "ExistingProjectMutationContext.TryGet",
        "ReferenceEquals(currentProject, presentedProject)",
        "TryResolveSemanticSelection(currentProject, _inspection",
        "SameSemanticSelection(presentedIds, currentIds)",
        "ExecuteAtomic(",
        "new SemanticSelectionBulkEditService().SetProperty",
        "private static bool IsMultiSelectionReadOnlyKey(string key) =>",
        "!SemanticPropertyEditPolicy.IsEditablePropertyKey(key);",
        "FamilyList.IsEnabled = false",
        "_viewModel.PropertyScopes.Clear()",
        "var commonFamilyId = !inspection.Family.IsMixed",
        "(inspection.Family.Value ?? string.Empty).Trim()",
        "commonFamilyId.Length > 0 ? project.FindFamily(commonFamilyId) : null",
    ):
        if token not in text:
            errors.append("Workspace multi-selection inspector missing guard/presentation token: " + token)

    if "MultiSelectionSourceDerivedKeys" in text:
        errors.append("Workspace multi-selection must not maintain a parallel source-derived/editability denylist; use SemanticPropertyEditPolicy")

    readonly_start = text.find("private static bool IsMultiSelectionReadOnlyKey(string key)")
    readonly_end = text.find("private string NormalizeMultiPropertyValue(", readonly_start)
    readonly_body = text[readonly_start:readonly_end] if readonly_start >= 0 and readonly_end > readonly_start else ""
    if "!SemanticPropertyEditPolicy.IsEditablePropertyKey(key)" not in readonly_body:
        errors.append("multi-selection read-only classification must delegate to the canonical Core SemanticPropertyEditPolicy")
    for stale_policy_fragment in (
        'normalized.Equals("ElementId"',
        'normalized.EndsWith("Ref"',
        'normalized.IndexOf("Handle"',
        'normalized.StartsWith("QS3D.Generated"',
        'normalized.StartsWith("PhysicalOpeningCut"',
    ):
        if stale_policy_fragment in readonly_body:
            errors.append("multi-selection read-only helper still duplicates Core edit policy: " + stale_policy_fragment)

    value_assignment = text.find("row.Value = summary.IsMixed")
    apply_assignment = text.find("row.Apply = value => ApplyMultiSelectionProperty")
    if value_assignment < 0 or apply_assignment < 0 or value_assignment > apply_assignment:
        errors.append("multi-selection row must assign presentation Value before wiring Apply")

    if ".Touch(" in text:
        errors.append("Workspace multi-selection adapter must not touch ProjectState directly")
    for forbidden in (
        "using Bricscad.DatabaseServices;",
        "using Teigha.DatabaseServices;",
        "SendStringToExecute",
    ):
        if forbidden in text:
            errors.append("Workspace multi-selection adapter crossed the semantic/CAD mutation boundary: " + forbidden)

if not view_model_code.is_file():
    errors.append("missing WorkspaceViewModel.cs")
else:
    text = view_model_code.read_text(encoding="utf-8")
    for token in (
        "using QS3D.Core.Services;",
        "MeasuredSolidQuantityPolicy.VolumeProperty",
        "MeasuredSolidQuantityPolicy.SurfaceAreaProperty",
        "var isReadOnlyInstanceProperty = !SemanticPropertyEditPolicy.IsEditablePropertyKey(key);",
        'row.Group = IsSourceDerivedInstanceKey(key) ? "NGUỒN CAD / ĐO ĐẠC" : "HỆ THỐNG / CHỈ ĐỌC";',
        'Status = "Không thể cập nhật " + DisplayNameFor(key) + ": đây là thuộc tính nguồn/identity/ownership chỉ đọc.";',
        'Status = "Không thể đặt lại " + DisplayNameFor(key) + ": đây là thuộc tính nguồn/identity/ownership chỉ đọc.";',
    ):
        if token not in text:
            errors.append("Workspace single-selection edit policy guard missing: " + token)
    if "hasInstance && IsSourceDerivedInstanceKey(key)" in text:
        errors.append("source-derived Family keys must stay read-only even before an Instance override exists")

    load_start = text.find("private void LoadInstanceProperties(")
    load_end = text.find("private PropertyRowViewModel CreatePropertyRow(", load_start)
    load_body = text[load_start:load_end] if load_start >= 0 and load_end > load_start else ""
    policy_read = load_body.find("!SemanticPropertyEditPolicy.IsEditablePropertyKey(key)")
    readonly_set = load_body.find("row.IsReadOnly = true;", policy_read)
    apply_wiring = load_body.find("row.Apply = value =>", policy_read)
    if policy_read < 0 or readonly_set < policy_read or apply_wiring < readonly_set:
        errors.append("single-selection presentation must classify Core-blocked keys before wiring editable Instance callbacks")

    apply_start = text.find("private string ApplyInstanceProperty(")
    apply_end = text.find("private void ResetInstanceProperty(", apply_start)
    apply_body = text[apply_start:apply_end] if apply_start >= 0 and apply_end > apply_start else ""
    guard = apply_body.find("SemanticPropertyEditPolicy.IsEditablePropertyKey(key)")
    set_property = apply_body.find("element.SetProperty(key, next)")
    touch = apply_body.find("project.Touch()")
    if guard < 0 or set_property < 0 or touch < 0 or guard > set_property or guard > touch:
        errors.append("single-selection Instance mutation must fail closed through Core edit policy before SetProperty/Touch")

if not policy_code.is_file():
    errors.append("missing SemanticPropertyEditPolicy.cs")
else:
    text = policy_code.read_text(encoding="utf-8")
    for token in (
        "public static class SemanticPropertyEditPolicy",
        "public static bool IsEditablePropertyKey(string propertyName)",
        "MeasuredSolidQuantityPolicy.VolumeProperty",
        "MeasuredSolidQuantityPolicy.SurfaceAreaProperty",
        "internal static string RequireEditablePropertyKey(string propertyName)",
        "EditBlockReason(propertyName.Trim()) == null",
    ):
        if token not in text:
            errors.append("Core semantic property edit policy contract missing: " + token)

if not bulk_code.is_file():
    errors.append("missing SemanticSelectionBulkEditService.cs")
else:
    text = bulk_code.read_text(encoding="utf-8")
    for token in (
        "SemanticPropertyEditPolicy.RequireEditablePropertyKey(propertyName)",
        "SemanticSelectionInspector.Inspect(project, elementIds)",
        'ProjectSemanticMutationExecutor.Execute(project, "selection.bulk.set-property"',
        'ProjectSemanticMutationExecutor.Execute(project, "selection.bulk.multiply-numeric-property"',
        "project.Touch();",
    ):
        if token not in text:
            errors.append("Semantic bulk-edit contract missing: " + token)

if not atomicity_smoke.is_file():
    errors.append("missing SemanticMutationAtomicitySmoke.cs")
else:
    text = atomicity_smoke.read_text(encoding="utf-8")
    for token in (
        "SelectionBulkSetPropertyOverflowRollsBack();",
        "SelectionBulkMultiplyOverflowRollsBack();",
        "new SemanticSelectionBulkEditService().SetProperty",
        "new SemanticSelectionBulkEditService().MultiplyNumericProperty",
        "Failed semantic-selection set partially changed the property.",
        "Failed semantic-selection multiply partially changed the property.",
    ):
        if token not in text:
            errors.append("Semantic selection atomicity smoke missing: " + token)

print("QS3D Workspace property palette preflight")
if errors:
    for error in errors:
        print("ERROR:", error)
    print("FAILED with", len(errors), "error(s).")
    sys.exit(1)

print("PASS: Workspace single- and multi-selection presentation now share the canonical Core semantic edit policy; stale/project guards and rollback-protected semantic mutation boundaries remain enforced without a parallel multi-selection denylist.")
